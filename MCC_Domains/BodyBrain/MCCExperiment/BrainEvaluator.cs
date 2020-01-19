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
    ///     Defines evaluation routine for brains within an MCC framework.
    /// </summary>
    public class BrainEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
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
        /// <param name="numBodiesSolvedCriteria">
        ///     The number of bodies that must be successfully ambulated to meet the minimal
        ///     criteria.
        /// </param>
        /// <param name="experimentName">The human-readable name of the experiment configuration being executed.</param>
        /// <param name="run">The current run number of the experiment being executed.</param>
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
            int numBodiesSolvedCriteria,
            string experimentName, int run, int resourceLimit = 0, IDataLogger evaluationLogger = null,
            IDataLogger resourceUsageLogger = null)
        {
            _simulationProperties = simulationProperties;
            _minAmbulationDistance = minAmbulationDistance;
            _experimentName = experimentName;
            _run = run;
            _numBodiesSolvedCriteria = numBodiesSolvedCriteria;
            _resourceUsageLogger = resourceUsageLogger;
            _evaluationLogger = evaluationLogger;

            // Set resource limited flag based on value of resource limit
            _isResourceLimited = resourceLimit > 0;

            // Create new factory for voxel body generation
            _voxelBodyFactory = new VoxelBodyFactory(resourceLimit);
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The voxel body factory, which maintains voxel body evaluation environments.
        /// </summary>
        private readonly VoxelBodyFactory _voxelBodyFactory;

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
        ///     Flag indicating whether bodies have an upper limit regarding the number of times they can be used for satisfying a
        ///     brain MC (i.e. limited resources).
        /// </summary>
        private readonly bool _isResourceLimited;

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
        ///     Attempts ambulation of one or more bodies using the given brain.
        /// </summary>
        /// <param name="brainCppn">The CPPN encoding the per-voxel brain responsible for ambulating one or more bodies.</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <returns>A BehaviorInfo, which encapsulates the final location of the robot at the end of a trial.</returns>
        public BehaviorInfo Evaluate(IBlackBox brainCppn, uint currentGeneration)
        {
            var curSuccesses = 0;
            var behaviorInfo = new BehaviorInfo();

            foreach (var cnt in Enumerable.Range(0, _voxelBodyFactory.NumBodies).OrderBy(x => _rng.Next()))
            {
                var isSuccessful = false;
                ulong threadLocalEvaluationCount;

                // Get the current body under evaluation
                var body = _voxelBodyFactory.GetVoxelBody(cnt);

                // Create new voxel brain based on the dimensions of the current voxel body
                var brain = new VoxelAnnBrain(brainCppn, body.LengthX, body.LengthY, body.LengthZ,
                    _simulationProperties.NumBrainConnections);

                lock (_evaluationLock)
                {
                    // If the body is already at resource limit, short-circuit the current evaluation
                    if (_isResourceLimited && !_voxelBodyFactory.IsBodyUnderResourceLimit(cnt)) continue;

                    // Increment evaluation count
                    threadLocalEvaluationCount = EvaluationCount++;
                }

                // Construct configuration file path
                var simConfigFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("config_braineval", "vxa",
                    _simulationProperties.SimConfigOutputDirectory, _experimentName, _run, brainCppn.GenomeId,
                    body.GenomeId, true);

                // Construct output file path
                var simResultFilePath = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("result_braineval", "xml",
                    _simulationProperties.SimResultsDirectory, _experimentName, _run, brainCppn.GenomeId,
                    body.GenomeId, true);

                // Write configuration file
                BodyBrainExperimentUtils.WriteVoxelyzeSimulationFile(_simulationProperties.SimConfigTemplateFile,
                    simConfigFilePath, simResultFilePath, brain, body, _minAmbulationDistance,
                    vxaSimGaXPath: _simulationProperties.SimOutputXPath,
                    vxaStructureXPath: _simulationProperties.StructurePropertiesXPath,
                    vxaMcXPath: _simulationProperties.MinimalCriterionXPath);

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
                            if (!_voxelBodyFactory.IsBodyUnderResourceLimit(cnt))
                            {
                                // Remove configuration and output files
                                File.Delete(simConfigFilePath);
                                File.Delete(simResultFilePath);

                                continue;
                            }

                            // Only increment successes if ambulated body is below resource limit
                            _voxelBodyFactory.IncrementBodyUsageCount(cnt);
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
                    body.GenomeId, new[] {simResults.Location.X, simResults.Location.Y}));

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
                if (curSuccesses < _numBodiesSolvedCriteria) continue;

                // If the number of successful ambulations is greater than the minimum required,
                // then the minimal criteria has been satisfied so terminate the evaluation loop
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
            // Update resource usage if enabled
            if (_isResourceLimited)
            {
                // Don't attempt to log if the file stream is closed
                if (_resourceUsageLogger?.IsStreamOpen() ?? false)
                {
                    // Log resource usages per genome ID
                    for (var idx = 0; idx < _voxelBodyFactory.NumBodies; idx++)
                    {
                        _resourceUsageLogger?.LogRow(new List<LoggableElement>
                        {
                            new LoggableElement(ResourceUsageFieldElements.Generation, lastGeneration),
                            new LoggableElement(ResourceUsageFieldElements.GenomeId,
                                _voxelBodyFactory.GetBodyGenomeId(idx)),
                            new LoggableElement(ResourceUsageFieldElements.UsageCount,
                                _voxelBodyFactory.GetBodyUsageCount(idx))
                        });
                    }
                }

                // Store off new bodies and remove bodies that have aged out or exceed their usage limit
                _voxelBodyFactory.SetVoxelBodies((IList<IBlackBoxSubstrate>) evaluatorPhenomes);
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