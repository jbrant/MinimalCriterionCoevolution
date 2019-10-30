using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain.MCCExperiment
{
    /// <summary>
    ///     Defines evaluation routine for bodies within an MCC framework.
    /// </summary>
    public class BodyEvaluator : IPhenomeEvaluator<VoxelBody, BehaviorInfo>
    {
        #region Constructor

        /// <summary>
        ///     BodyEvaluator constructor.
        /// </summary>
        /// <param name="simulationProperties">
        ///     Collection of simulator configuration properties, including result output
        ///     directories, configuration files and config file parsing information.
        /// </param>
        /// <param name="minAmbulationDistance">The minimum distance that the robot must traverse to meet the MC.</param>
        /// <param name="experimentName">The human-readable name of the experiment configuration being executed.</param>
        /// <param name="run">The current run number of the experiment being executed.</param>
        /// <param name="numBrainsSolvedCriteria">
        ///     The number of brains that must successfully ambulate the body to meet the minimal
        ///     criteria.
        /// </param>
        /// <param name="evaluationLogger">Per-evaluation data logger (optional).</param>
        public BodyEvaluator(SimulationProperties simulationProperties, double minAmbulationDistance,
            int numBrainsSolvedCriteria, string experimentName, int run, IDataLogger evaluationLogger = null)
        {
            _simulationProperties = simulationProperties;
            _minAmbulationDistance = minAmbulationDistance;
            _experimentName = experimentName;
            _run = run;
            _numBrainsSolvedCriteria = numBrainsSolvedCriteria;
            _evaluationLogger = evaluationLogger;
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The list of brains to evaluate against the given voxel body configurations.
        /// </summary>
        private IList<VoxelBrain> _brains;

        /// <summary>
        ///     The number of brains that must successfully ambulate the body to meet the minimal criteria.
        /// </summary>
        private readonly int _numBrainsSolvedCriteria;

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

        #region Public properties

        /// <inheritdoc />
        /// <summary>
        ///     The total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     Indicates if some stopping condition has been reached - in this case, satisfying the minimal criterion or
        ///     exhausting the evaluation opportunities from the opposite population.
        /// </summary>
        public bool StopConditionSatisfied => false;

        #endregion

        #region Public methods

        /// <summary>
        /// Attempts ambulation of a body using one or more brains.
        /// </summary>
        /// <param name="body">The voxel body to be ambulated.</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <returns>A BehaviorInfo, which encapsulates the final location of the robot at the end of a trial.</returns>
        public BehaviorInfo Evaluate(VoxelBody body, uint currentGeneration)
        {
            var curSuccesses = 0;
            var behaviorInfo = new BehaviorInfo();

            for (var cnt = 0; cnt < _brains.Count && curSuccesses < _numBrainsSolvedCriteria; cnt++)
            {
                var isSuccessful = false;
                ulong threadLocalEvaluationCount;

                // Get the current brain under evaluation
                var brain = _brains[cnt];
                
                lock (_evaluationLock)
                {
                    // Increment evaluation count
                    threadLocalEvaluationCount = EvaluationCount++;
                }

                // Construct configuration file path
                var simConfigFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("config_bodyeval",
                    _simulationProperties.SimConfigOutputDirectory, _experimentName, _run, body.GenomeId,
                    brain.GenomeId);

                // Construct output file path
                var simResultFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("result_bodyeval",
                    _simulationProperties.SimResultsDirectory, _experimentName, _run, body.GenomeId,
                    brain.GenomeId);

                BodyBrainExperimentUtils.WriteVoxelyzeSimulationFile(_simulationProperties.SimConfigTemplateFile,
                    simConfigFilePath, _simulationProperties.SimOutputXPath,
                    _simulationProperties.StructurePropertiesXPath, _simulationProperties.MinimalCriterionXPath,
                    simResultFilePath, brain, body, _minAmbulationDistance);

                // Configure the simulation, execute and wait for completion
                using (var process =
                    Process.Start(
                        BodyBrainExperimentUtils.ConfigureSimulationExecution(_simulationProperties.SimExecutableFile,
                            simConfigFilePath)))
                {
                    process?.WaitForExit();
                }

                // Extract simulation results
                var simResults = BodyBrainExperimentUtils.ReadSimulationResults(simResultFilePath);

                // Set the stop condition flag if the ambulation MC has been met
                if (simResults.Distance >= _minAmbulationDistance)
                {
                    // Increment successes
                    curSuccesses++;
                    
                    // Set success flag
                    isSuccessful = true;
                }

                // Record simulation trial info
                behaviorInfo.TrialData.Add(new TrialInfo(isSuccessful, simResults.Distance, simResults.SimulationTime,
                    body.GenomeId, new[] {simResults.Location.X, simResults.Location.Y}));

                // Remove configuration and output files
                File.Delete(simConfigFilePath);
                File.Delete(simResultFilePath);
                
                // Don't attempt to log if the file stream is closed
                if (!(_evaluationLogger?.IsStreamOpen() ?? false)) continue;
                
                // Log trial information
                _evaluationLogger?.LogRow(new List<LoggableElement>
                {
                    new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                    new LoggableElement(EvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                    new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                    new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization)
                }, simResults.GetLoggableElements());
            }

            // If the number of successful ambulations is greater than the minimum required, then the minimal criteria
            // has been satisfied
            if (curSuccesses >= _numBrainsSolvedCriteria)
            {
                behaviorInfo.DoesBehaviorSatisfyMinimalCriteria = true;
            }

            return behaviorInfo;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes the logger and writes header.
        /// </summary>
        public void Initialize()
        {
            // Open logger
            _evaluationLogger?.Open();
            
            // Set the run phase
            _evaluationLogger?.UpdateRunPhase(RunPhase.Primary);

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

        /// <inheritdoc />
        /// <summary>
        ///     Updates the collection of brains to use for future evaluations.
        /// </summary>
        /// <param name="evaluatorPhenomes">The complete collection of available brains.</param>
        /// <param name="lastGeneration">The generation that was just executed.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes, uint lastGeneration)
        {
            _brains = (IList<VoxelBrain>) evaluatorPhenomes;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Cleans up evaluator state after end of execution or upon execution interruption.  In particular, this
        ///     closes out any existing evaluation logger instance.
        /// </summary>
        public void Cleanup()
        {
            _evaluationLogger.Close();
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