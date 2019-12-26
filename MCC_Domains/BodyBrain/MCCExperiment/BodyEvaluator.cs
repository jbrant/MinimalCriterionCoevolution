using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Redzen.Random;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain.MCCExperiment
{
    /// <summary>
    ///     Defines evaluation routine for bodies within an MCC framework.
    /// </summary>
    public class BodyEvaluator : IPhenomeEvaluator<IBlackBoxSubstrate, BehaviorInfo>
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
        /// <param name="numBrainsSolvedCriteria">
        ///     The number of brains that must successfully ambulate the body to meet the minimal
        ///     criteria.
        /// </param>
        /// <param name="experimentName">The human-readable name of the experiment configuration being executed.</param>
        /// <param name="run">The current run number of the experiment being executed.</param>
        /// <param name="evaluationLogger">Per-evaluation data logger (optional).</param>
        public BodyEvaluator(SimulationProperties simulationProperties, double minAmbulationDistance,
            int numBrainsSolvedCriteria, string experimentName, int run,
            IDataLogger evaluationLogger = null)
        {
            _simulationProperties = simulationProperties;
            _minAmbulationDistance = minAmbulationDistance;
            _experimentName = experimentName;
            _run = run;
            _numBrainsSolvedCriteria = numBrainsSolvedCriteria;
            _evaluationLogger = evaluationLogger;

            // Create new factory for voxel brain generation
            _voxelBrainFactory = new VoxelBrainFactory(simulationProperties.NumBrainConnections);
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The voxel brain factory, which maintains voxel body controllers.
        /// </summary>
        private readonly VoxelBrainFactory _voxelBrainFactory;

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

        /// <summary>
        ///     Random number generator that controls evaluation selection order.
        /// </summary>
        private readonly IRandomSource _rng = RandomDefaults.CreateRandomSource();

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
        ///     Attempts ambulation of a body using one or more brains.
        /// </summary>
        /// <param name="bodyCppn">The CPPN which will produce the voxel body to be ambulated.</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <returns>A BehaviorInfo, which encapsulates the final location of the robot at the end of a trial.</returns>
        public BehaviorInfo Evaluate(IBlackBoxSubstrate bodyCppn, uint currentGeneration)
        {
            var curSuccesses = 0;
            var behaviorInfo = new BehaviorInfo();

            foreach (var cnt in Enumerable.Range(0, _voxelBrainFactory.NumBrains).OrderBy(x => _rng.Next()))
            {
                var isSuccessful = false;
                ulong threadLocalEvaluationCount;

                // Create new voxel body
                var body = new VoxelBody(bodyCppn);

                // Get the current brain under evaluation and scale to the voxel body size
                var brain = _voxelBrainFactory.GetVoxelBrain(cnt, body.LengthX, body.LengthY, body.LengthZ);

                lock (_evaluationLock)
                {
                    // Increment evaluation count
                    threadLocalEvaluationCount = EvaluationCount++;
                }

                // Construct configuration file path
                var simConfigFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("config_bodyeval", "vxa",
                    _simulationProperties.SimConfigOutputDirectory, _experimentName, _run, brain.GenomeId,
                    bodyCppn.GenomeId, false);

                // Construct output file path
                var simResultFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("result_bodyeval", "xml",
                    _simulationProperties.SimResultsDirectory, _experimentName, _run, brain.GenomeId, bodyCppn.GenomeId,
                    false);

                BodyBrainExperimentUtils.WriteVoxelyzeSimulationFile(_simulationProperties.SimConfigTemplateFile,
                    simConfigFilePath, simResultFilePath, brain, body, _minAmbulationDistance,
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
                    brain.GenomeId, new[] {simResults.Location.X, simResults.Location.Y}));

                // Remove configuration and output files
                File.Delete(simConfigFilePath);
                File.Delete(simResultFilePath);

                // Only attempt to log if the file stream is open
                if (_evaluationLogger?.IsStreamOpen() ?? false)
                {
                    // Log trial information
                    _evaluationLogger?.LogRow(new List<LoggableElement>
                    {
                        new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                        new LoggableElement(EvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                        new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                        new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization)
                    }, simResults.GetLoggableElements());
                }

                // Continue to the next iteration if the MC has still not yet been satisfied
                if (curSuccesses < _numBrainsSolvedCriteria) continue;

                // If the number of successful ambulations is greater than the minimum required, then the minimal criteria
                // has been satisfied, so terminate the evaluation loop
                behaviorInfo.DoesBehaviorSatisfyMinimalCriteria = true;
                break;
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
            _voxelBrainFactory.SetVoxelBrains((IList<IBlackBox>) evaluatorPhenomes);
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