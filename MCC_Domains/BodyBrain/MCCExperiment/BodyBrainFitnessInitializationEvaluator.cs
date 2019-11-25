using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain.MCCExperiment
{
    /// <summary>
    ///     Defines evaluation rules and process for MCC initialization in body/brain experiments using a fitness-based EA.
    /// </summary>
    public class BodyBrainFitnessInitializationEvaluator : IPhenomeEvaluator<IBlackBox, FitnessInfo>
    {
        #region Public properties
        
        /// <inheritdoc />
        /// <summary>
        ///     Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that the evolutionary algorithm/search
        ///     should stop.  This property's value can remain false to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied { get; private set; }
        
        #region Instance variables
        
        /// <summary>
        ///     The body on which brains (controllers) are evaluated.
        /// </summary>
        private readonly VoxelBody _voxelBody;

        /// <summary>
        ///     Collection of simulator configuration properties, including result output directories, configuration files and
        ///     config file parsing information.
        /// </summary>
        private readonly SimulationProperties _simulationProperties;

        /// <summary>
        ///     The minimum distance that the robot must traverse to meet the MC.
        /// </summary>
        private readonly double _minAmbulationDistance;

        /// <summary>
        ///     The human-readable name of the experiment configuration being executed.
        /// </summary>
        private readonly string _experimentName;

        /// <summary>
        ///     The current run number of the experiment being executed.
        /// </summary>
        private readonly int _run;

        /// <summary>
        ///     Per-evaluation data logger (generates one row per trial).
        /// </summary>
        private readonly IDataLogger _evaluationLogger;

        /// <summary>
        ///     Lock object for synchronizing evaluation counter increments.
        /// </summary>
        private readonly object _evaluationLock = new object();
        
        #endregion
        
        #endregion

        #region Constructor

        /// <summary>
        ///     Body/brain fitness initialization constructor.
        /// </summary>
        /// <param name="body">The body on which brains (controllers) are evaluated.</param>
        /// <param name="simulationProperties">
        ///     Collection of simulator configuration properties, including result output
        ///     directories, configuration files and config file parsing information.
        /// </param>
        /// <param name="minAmbulationDistance">The minimum distance that the robot must traverse to meet the MC.</param>
        /// <param name="experimentName">The human-readable name of the experiment configuration being executed.</param>
        /// <param name="run">The current run number of the experiment being executed.</param>
        /// <param name="startingEvaluations">
        ///     The total number of evaluations that have already been performed at the time of
        ///     initialization.
        /// </param>
        /// <param name="evaluationLogger">Per-evaluation data logger (optional).</param>
        public BodyBrainFitnessInitializationEvaluator(VoxelBody body,
            SimulationProperties simulationProperties, double minAmbulationDistance, string experimentName, int run,
            ulong startingEvaluations = 0, IDataLogger evaluationLogger = null)
        {
            EvaluationCount = startingEvaluations;
            _voxelBody = body;
            _simulationProperties = simulationProperties;
            _minAmbulationDistance = minAmbulationDistance;
            _experimentName = experimentName;
            _run = run;
            _evaluationLogger = evaluationLogger;
        }

        #endregion
        
        #region Interface methods

        /// <summary>
        ///     Runs a brain (neural network controller) through a single body ambulation trial.
        /// </summary>
        /// <param name="brainCppn">The CPPN encoding the per-voxel brain responsible for ambulating one or more bodies.</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <returns>A FitnessInfo, which encapsulates the distance that the robot traveled.</returns>
        public FitnessInfo Evaluate(IBlackBox brainCppn, uint currentGeneration)
        {
            lock (_evaluationLock)
            {
                // Increment evaluation count
                EvaluationCount++;
            }
            
            // Create new voxel brain given the initial substrate dimensions
            var brain = new VoxelBrain(brainCppn, _simulationProperties.InitialXDimension,
                _simulationProperties.InitialYDimension, _simulationProperties.InitialZDimension,
                _simulationProperties.NumBrainConnections);
            
            // Construct configuration file path
            var simConfigFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("config_init", "vxa",
                _simulationProperties.SimConfigOutputDirectory, _experimentName, _run, _voxelBody.GenomeId,
                brain.GenomeId);
            
            // Construct output file path
            var simResultFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("result_init", "xml",
                _simulationProperties.SimResultsDirectory, _experimentName, _run, _voxelBody.GenomeId, brain.GenomeId);

            // Write configuration file
            BodyBrainExperimentUtils.WriteVoxelyzeSimulationFile(_simulationProperties.SimConfigTemplateFile,
                simConfigFilePath, simResultFilePath, brain, _voxelBody, _minAmbulationDistance,
                _simulationProperties.SimOutputXPath, _simulationProperties.StructurePropertiesXPath,
                _simulationProperties.MinimalCriterionXPath);

            // Configure the simulation, execute and wait for completion
            using (var process =
                Process.Start(
                    BodyBrainExperimentUtils.ConfigureSimulationExecution(_simulationProperties.SimExecutableFile,
                        simConfigFilePath)))
            {
                process?.WaitForExit();
            }

            // Read distance traversed from the results file
            var simResults = BodyBrainExperimentUtils.ReadSimulationResults(simResultFilePath);
            
            // Set the stop condition flag if the ambulation MC has been met
            if (simResults.Distance >= _minAmbulationDistance)
            {
                StopConditionSatisfied = true;
            }
            
            // Log trial information
            _evaluationLogger?.LogRow(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization)
            }, simResults.GetLoggableElements());
            
            // Remove configuration and output files
            File.Delete(simConfigFilePath);
            File.Delete(simResultFilePath);
            
            // Return fitness as distance traversed
            return new FitnessInfo(simResults.Distance, simResults.Distance);
        }

        /// <summary>
        ///     Initializes the evaluation logger and writes header.
        /// </summary>
        public void Initialize()
        {
            // Set the run phase
            _evaluationLogger?.UpdateRunPhase(RunPhase.Initialization);

            // Log the header
            _evaluationLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, 0),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization),
                new LoggableElement(EvaluationFieldElements.IsViable, false)
            }, new SimulationResults(0, 0, 0, 0).GetLoggableElements());
        }

        /// <summary>
        ///     Updates the evaluation environment or criteria against which the phenomes under evaluation are being compared. This
        ///     isn't used during the initialization process - only during MCC execution itself.
        /// </summary>
        /// <param name="evaluatorPhenomes">The new phenomes to compare against.</param>
        /// <param name="lastGeneration">The generation or evaluation batch that was just executed.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes, uint lastGeneration)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///     Cleans up evaluator state after end of execution or upon execution interruption.  In particular, this closes out
        ///     any existing evaluation logger instance.
        /// </summary>
        public void Cleanup()
        {
            _evaluationLogger?.Close();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This is not needed for the body/brain coevolution task.
        /// </summary>
        public void Reset()
        {
        }
        
        #endregion
    }
}