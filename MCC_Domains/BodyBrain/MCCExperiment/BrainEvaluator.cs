using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain.MCCExperiment
{
    /// <summary>
    ///     Defines evaluation routine for brains within an MCC framework.
    /// </summary>
    public class BrainEvaluator : IPhenomeEvaluator<VoxelBrain, BehaviorInfo>
    {
        #region Constructor

        /// <summary>
        ///     BrainEvaluator constructor.
        /// </summary>
        /// <param name="simulationProperties">
        ///     Collection of simulator configuration properties, including result output
        ///     directories, configuration files and config file parsing information.
        /// </param>
        /// <param name="minAmbulationDistance">The minimum distance that the robot must traverse to meet the MC.</param>
        /// <param name="experimentName">The human-readable name of the experiment configuration being executed.</param>
        /// <param name="run">The current run number of the experiment being executed.</param>
        /// <param name="numBodiesSolvedCriteria">
        ///     The number of bodies that must be successfully ambulated to meet the minimal
        ///     criteria.
        /// </param>
        /// <param name="resourceLimit">
        ///     The number of times a body can be used for successful ambulation that contribute to meeting
        ///     a brain's MC.
        /// </param>
        /// <param name="evaluationLogger">Per-evaluation data logger (optional).</param>
        /// <param name="resourceUsageLogger">
        ///     Resource usage logger that records number of times body has been used to satisfy
        ///     brain MC (optional).
        /// </param>
        public BrainEvaluator(SimulationProperties simulationProperties, double minAmbulationDistance,
            int numBodiesSolvedCriteria, string experimentName, int run, int resourceLimit = 0,
            IDataLogger evaluationLogger = null, IDataLogger resourceUsageLogger = null)
        {
            _simulationProperties = simulationProperties;
            _minAmbulationDistance = minAmbulationDistance;
            _experimentName = experimentName;
            _run = run;
            _numBodiesSolvedCriteria = numBodiesSolvedCriteria;
            _resourceLimit = resourceLimit;
            _resourceUsageLogger = resourceUsageLogger;
            _evaluationLogger = evaluationLogger;

            // Set resource limited flag based on value of resource limit
            _isResourceLimited = resourceLimit > 0;

            // Instantiate the body-usage map to track brain usage if resource limitation is enabled
            if (_isResourceLimited)
            {
                _bodyUsageMap = new Dictionary<uint, int>();
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Adds new voxel bodies to the resource usage map, removes those that are over their resource limit and removes voxel
        ///     bodies from the usage map that have gone extinct (i.e. been removed from the population).
        /// </summary>
        private void UpdateBodyUsage()
        {
            // Add new voxel bodies to the usage map
            foreach (var newBody in _bodies.Where(x => _bodyUsageMap.Keys.Any(y => y == x.GenomeId) == false))
            {
                _bodyUsageMap.Add(newBody.GenomeId, 0);
            }

            // Remove bodies that are at or over their resource limit as evaluation candidatess
            foreach (var overLimitBody in _bodies.Where(x => _bodyUsageMap[x.GenomeId] >= _resourceLimit).ToList())
            {
                _bodies.Remove(overLimitBody);
            }

            // Remove bodies that are no longer extant from body usage map
            foreach (var extinctBody in _bodyUsageMap.Keys.Where(x => _bodies.Any(y => y.GenomeId == x) == false)
                .ToList())
            {
                _bodyUsageMap.Remove(extinctBody);
            }
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The list of voxel body configurations to evaluate against the given brains.
        /// </summary>
        private IList<VoxelBody> _bodies;

        /// <summary>
        ///     The number of bodies that must be successfully ambulated to meet the minimal criteria.
        /// </summary>
        private readonly int _numBodiesSolvedCriteria;

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
        ///     Resource usage logger that records number of times a body has been used to satisfy brain MC.
        /// </summary>
        private readonly IDataLogger _resourceUsageLogger;

        /// <summary>
        ///     The number of times a body can be used for successful ambulation that contribute to meeting a brain's MC.
        /// </summary>
        private readonly int _resourceLimit;

        /// <summary>
        ///     Flag indicating whether bodies have an upper limit regarding the number of times they can be used for satisfying a
        ///     brain MC (i.e. limited resources).
        /// </summary>
        private readonly bool _isResourceLimited;

        /// <summary>
        ///     Dictionary that contains mapping between body genome IDs and a count of their usage for satisfying brain MC.
        /// </summary>
        private readonly IDictionary<uint, int> _bodyUsageMap;

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
        ///     Attempts ambulation of one or more bodies using the given brain.
        /// </summary>
        /// <param name="brain">The brain responsible for ambulating one or more bodies.</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <returns>A BehaviorInfo, which encapsulates the final location of the robot at the end of a trial.</returns>
        public BehaviorInfo Evaluate(VoxelBrain brain, uint currentGeneration)
        {
            var curSuccesses = 0;
            var behaviorInfo = new BehaviorInfo();

            for (var cnt = 0; cnt < _bodies.Count && curSuccesses < _numBodiesSolvedCriteria; cnt++)
            {
                var isSuccessful = false;
                ulong threadLocalEvaluationCount;

                // Get the current body under evaluation
                var body = _bodies[cnt];

                lock (_evaluationLock)
                {
                    // Increment evaluation count
                    threadLocalEvaluationCount = EvaluationCount++;
                }

                // Construct configuration file path
                var simConfigFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("config_braineval",
                    _simulationProperties.SimConfigOutputDirectory, _experimentName, _run, brain.GenomeId,
                    body.GenomeId);

                // Construct output file path
                var simResultFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("result_braineval",
                    _simulationProperties.SimResultsDirectory, _experimentName, _run, brain.GenomeId,
                    body.GenomeId);

                // Write configuration file
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
                    if (_isResourceLimited)
                    {
                        lock (_evaluationLock)
                        {
                            // Successful ambulation is discounted if body is at or above resource limit
                            if (_bodyUsageMap[body.GenomeId] >= _resourceLimit)
                            {
                                // Remove configuration and output files
                                File.Delete(simConfigFilePath);
                                File.Delete(simResultFilePath);
                                
                                continue;                                
                            }

                            // Only increment successes if ambulated body is below resource limit
                            _bodyUsageMap[body.GenomeId]++;
                            curSuccesses++;

                            // Set success flag
                            isSuccessful = true;
                        }
                    }
                    else
                    {
                        // Increment successes
                        curSuccesses++;

                        // Set success flag
                        isSuccessful = true;
                    }
                }

                // Record simulation trial info
                behaviorInfo.TrialData.Add(new TrialInfo(isSuccessful, simResults.Distance, simResults.SimulationTime,
                    brain.GenomeId, new[] {simResults.Location.X, simResults.Location.Y}));

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
            if (curSuccesses >= _numBodiesSolvedCriteria)
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
            // Open loggers
            _evaluationLogger?.Open();
            _resourceUsageLogger?.Open();

            // Set the run phase
            _evaluationLogger?.UpdateRunPhase(RunPhase.Primary);

            // Log the evaluation logger header
            _evaluationLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, 0),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization),
                new LoggableElement(EvaluationFieldElements.IsViable, false)
            }, new SimulationResults(0, 0, 0, 0).GetLoggableElements());

            // Log the usage logger header
            _resourceUsageLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(ResourceUsageFieldElements.Generation, 0),
                new LoggableElement(ResourceUsageFieldElements.GenomeId, null),
                new LoggableElement(ResourceUsageFieldElements.UsageCount, 0)
            });
        }

        /// <inheritdoc />
        /// <summary>
        ///     Updates the collection of bodies to use for future evaluations.
        /// </summary>
        /// <param name="evaluatorPhenomes">The complete collection of available bodies.</param>
        /// <param name="lastGeneration">The generation that was just executed.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes, uint lastGeneration)
        {
            // Store off extant bodies
            _bodies = (IList<VoxelBody>) evaluatorPhenomes;

            // Update resource usage if enabled
            if (_isResourceLimited)
            {
                // Update the voxel body usage map and remove candidate bodies that have exceeded their usage limit
                UpdateBodyUsage();

                // Don't attempt to log if the file stream is closed
                if (!(_resourceUsageLogger?.IsStreamOpen() ?? false)) return;

                // Log resource usages per genome ID
                foreach (var body in _bodies)
                {
                    _resourceUsageLogger?.LogRow(new List<LoggableElement>
                    {
                        new LoggableElement(ResourceUsageFieldElements.Generation, lastGeneration),
                        new LoggableElement(ResourceUsageFieldElements.GenomeId, body.GenomeId),
                        new LoggableElement(ResourceUsageFieldElements.UsageCount, _bodyUsageMap[body.GenomeId])
                    });
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Cleans up evaluator state after end of execution or upon execution interruption.  In particular, this
        ///     closes out any existing evaluation logger instance.
        /// </summary>
        public void Cleanup()
        {
            _evaluationLogger.Close();
            _resourceUsageLogger?.Close();
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