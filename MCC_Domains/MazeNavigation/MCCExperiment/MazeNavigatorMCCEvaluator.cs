#region

using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MCC_Domains.MazeNavigation.MCCExperiment
{
    /// <inheritdoc />
    /// <summary>
    ///     Defines evaluation routine for agents (maze navigators) within a minimal criterion coevolution (MCC) framework.
    /// </summary>
    public class MazeNavigatorMCCEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        #region Constructors

        /// <summary>
        ///     Maze Navigator MCS evaluator constructor.
        /// </summary>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="agentNumSuccessesCriteria">
        ///     The number of mazes that must be solved successfully in order to satisfy the
        ///     minimal criterion.
        /// </param>
        /// <param name="resourceLimit">
        ///     The number of times a maze can be used for successful navigations that contribute to
        ///     meeting an agent's MC.
        /// </param>
        /// <param name="evaluationLogger">Per-evaluation data logger (optional).</param>
        /// <param name="resourceUsageLogger">
        ///     Resource usage logger that records number of times maze has been used to satisfy
        ///     agent MC (optional).
        /// </param>
        public MazeNavigatorMCCEvaluator(int minSuccessDistance,
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory, int agentNumSuccessesCriteria,
            int resourceLimit = 0, IDataLogger evaluationLogger = null, IDataLogger resourceUsageLogger = null)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            _agentNumSuccessesCriteria = agentNumSuccessesCriteria;
            _resourceUsageLogger = resourceUsageLogger;
            _resourceLimit = resourceLimit;
            _evaluationLogger = evaluationLogger;
            EvaluationCount = 0;

            // Set resource limited flag based on value of resource limit
            _isResourceLimited = resourceLimit > 0;

            // Create factory for generating multiple mazes
            _multiMazeWorldFactory = new MultiMazeNavigationWorldFactory(minSuccessDistance);
        }

        #endregion

        #region Private Members

        /// <summary>
        ///     The behavior characterization factory.
        /// </summary>
        private readonly IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;

        /// <summary>
        ///     Lock object for synchronizing evaluation counter increments.
        /// </summary>
        private readonly object _evaluationLock = new object();

        /// <summary>
        ///     The multi maze navigation world factory.
        /// </summary>
        private readonly MultiMazeNavigationWorldFactory _multiMazeWorldFactory;

        /// <summary>
        ///     The number of mazes that the agent must navigate in order to meet the minimal criteria.
        /// </summary>
        private readonly int _agentNumSuccessesCriteria;

        /// <summary>
        ///     Per-evaluation data logger (generates one row per maze trial).
        /// </summary>
        private readonly IDataLogger _evaluationLogger;

        /// <summary>
        ///     Resource usage logger that records number of times maze has been used to satisfy agent MC.
        /// </summary>
        private readonly IDataLogger _resourceUsageLogger;

        /// <summary>
        ///     The number of times a maze can be used for successful navigations that contribute to meeting an agent's MC.
        /// </summary>
        private readonly int _resourceLimit;

        /// <summary>
        ///     Flag indicating whether mazes have an upper limit regarding the number of times they can be used for satisfying an
        ///     agent MC (i.e. limited resources).
        /// </summary>
        private readonly bool _isResourceLimited;

        #endregion

        #region Public Properties

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
        public bool StopConditionSatisfied => false;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        /// <summary>
        ///     Runs an agent (i.e. the maze navigator) through a collection of mazes until the minimal criteria is satisfied.
        /// </summary>
        /// <param name="agent">The maze navigator brain (ANN).</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <returns>A behavior info (which is a type of behavior-based trial information).</returns>
        public BehaviorInfo Evaluate(IBlackBox agent, uint currentGeneration)
        {
            var curSuccesses = 0;
            var behaviorInfo = new BehaviorInfo();

            for (var cnt = 0; cnt < _multiMazeWorldFactory.NumMazes && curSuccesses < _agentNumSuccessesCriteria; cnt++)
            {
                var isSuccessful = false;
                ulong threadLocalEvaluationCount;

                lock (_evaluationLock)
                {
                    // Increment evaluation count
                    threadLocalEvaluationCount = EvaluationCount++;
                }

                // Generate new behavior characterization
                var behaviorCharacterization = _behaviorCharacterizationFactory.CreateBehaviorCharacterization();

                // Generate a new maze world
                var world = _multiMazeWorldFactory.CreateMazeNavigationWorld(cnt, behaviorCharacterization);

                // Run a single trial
                var trialBehavior = world.RunBehaviorTrial(agent, out var goalReached);

                // Record the objective distance
                var objectiveDistance = world.GetDistanceToTarget();

                // Log trial information
                _evaluationLogger?.LogRow(new List<LoggableElement>
                    {
                        new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                        new LoggableElement(EvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                        new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                        new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Primary),
                        new LoggableElement(EvaluationFieldElements.IsViable,
                            goalReached)
                    },
                    world.GetLoggableElements());

                // If the navigator reached the goal, update the running count of successes
                if (goalReached)
                {
                    // If resource limitations are imposed, we also need to increment the number of times the current
                    // maze has been used to satisfy an agent MC
                    if (_isResourceLimited)
                    {
                        lock (_evaluationLock)
                        {
                            // Successful navigation is discounted if maze is at or above resource limit
                            if (!_multiMazeWorldFactory.IsMazeUnderResourceLimit(cnt, _resourceLimit)) continue;

                            // Only increment successes if solved maze is below resource limit
                            _multiMazeWorldFactory.IncrementSuccessfulMazeNavigationCount(cnt);
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

                // Add simulation trial info
                behaviorInfo.TrialData.Add(new TrialInfo(isSuccessful, objectiveDistance,
                    world.GetSimulationTimesteps(), _multiMazeWorldFactory.GetMazeGenomeId(cnt), trialBehavior));
            }

            // If the number of successful maze navigations was equivalent to the minimum required,
            // then the minimal criteria has been satisfied
            if (curSuccesses >= _agentNumSuccessesCriteria)
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

            // Set the run phase on evaluation logger
            _evaluationLogger?.UpdateRunPhase(RunPhase.Primary);

            // Log the evaluation logger header
            _evaluationLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, 0),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization),
                new LoggableElement(EvaluationFieldElements.IsViable, false)
            }, _multiMazeWorldFactory.CreateMazeNavigationWorld(new MazeStructure(0, 0, 1), null).GetLoggableElements());

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
        ///     Updates the collection of mazes to use for future evaluations.
        /// </summary>
        /// <param name="evaluatorPhenomes">The complete collection of available mazes.</param>
        /// <param name="lastGeneration">The generation that was just executed.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes, uint lastGeneration)
        {
            // Update resource usage if enabled
            if (_isResourceLimited)
            {
                // Don't attempt to log if the file stream is closed
                if (_resourceUsageLogger?.IsStreamOpen() ?? false);
                {
                    // Log resource usages per genome ID
                    for (int cnt = 0; cnt < _multiMazeWorldFactory.NumMazes; cnt++)
                    {
                        _resourceUsageLogger?.LogRow(new List<LoggableElement>
                        {
                            new LoggableElement(ResourceUsageFieldElements.Generation, lastGeneration),
                            new LoggableElement(ResourceUsageFieldElements.GenomeId,
                                _multiMazeWorldFactory.GetMazeGenomeId(cnt)),
                            new LoggableElement(ResourceUsageFieldElements.UsageCount,
                                _multiMazeWorldFactory.GetViabilityUsageCount(cnt))
                        });
                    }
                }
            }
            
            // Cast to maze genomes/phenomes
            var mazePhenomes = (IList<MazeStructure>) evaluatorPhenomes;

            // Set the new maze configurations on the factory
            _multiMazeWorldFactory.SetMazeConfigurations(mazePhenomes);
            
            // Increment resource usage count as appropriate
            _multiMazeWorldFactory.UpdateMazePhenomeUsage(mazePhenomes);
        }

        /// <summary>
        ///     Cleans up evaluator state after end of execution or upon execution interruption.  In particular, this
        ///     closes out any existing evaluation logger and resource usage logger instance.
        /// </summary>
        public void Cleanup()
        {
            _evaluationLogger?.Close();
            _resourceUsageLogger?.Close();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }

        #endregion
    }
}