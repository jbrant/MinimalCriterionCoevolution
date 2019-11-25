using System;
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
    ///     Defines evaluation rules and process for MCC initialization in body/brain experiments using the novelty search
    ///     algorithm.
    /// </summary>
    public class BodyBrainNoveltySearchInitializationEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        #region Constructor

        /// <summary>
        ///     Body/brain novelty search initialization constructor.
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
        public BodyBrainNoveltySearchInitializationEvaluator(VoxelBody body,
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

        #endregion

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

        #region Public methods

        /// <summary>
        ///     Runs a brain (neural network controller) through a single body ambulation trial.
        /// </summary>
        /// <param name="brain">The neural network controller for each voxel cell.</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <returns>A BehaviorInfo, which encapsulates the distance that the robot traveled.</returns>
        public BehaviorInfo Evaluate(IBlackBox brainCppn, uint currentGeneration)
        {
            var behaviorInfo = new BehaviorInfo();
            var isSuccessful = false;

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
                isSuccessful = true;
            }

            // Record simulation trial info
            behaviorInfo.TrialData.Add(new TrialInfo(isSuccessful, simResults.Distance, simResults.SimulationTime,
                _voxelBody.GenomeId, new[] {simResults.Location.X, simResults.Location.Y}));

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

            return behaviorInfo;
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
            throw new NotImplementedException();
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