#region

using System.Collections.Generic;
using System.Linq;
using Redzen.Random;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MCC_Domains.MazeNavigation.MCCExperiment
{
    /// <inheritdoc />
    /// <summary>
    ///     Defines evaluation routine for mazes within a minimal criterion coevolution (MCC) framework.
    /// </summary>
    public class MazeEnvironmentMCCEvaluator : IPhenomeEvaluator<MazeStructure, BehaviorInfo>
    {
        #region Constructors

        /// <summary>
        ///     Maze Environment MCS evaluator constructor.
        /// </summary>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="numAgentsSolvedCriteria">
        ///     The number of successful attempts at maze navigation in order to satisfy the
        ///     minimal criterion.
        /// </param>
        /// <param name="numAgentsFailedCriteria">
        ///     The number of failed attempts at maze navigation in order to satisfy the minimal
        ///     criterion.
        /// </param>
        /// <param name="evaluationLogger">Per-evaluation data logger (optional).</param>
        public MazeEnvironmentMCCEvaluator(int minSuccessDistance,
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory, int numAgentsSolvedCriteria,
            int numAgentsFailedCriteria, IDataLogger evaluationLogger = null)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            _numAgentsSolvedCriteria = numAgentsSolvedCriteria;
            _numAgentsFailedCriteria = numAgentsFailedCriteria;
            _evaluationLogger = evaluationLogger;

            // Create factory for maze world generation
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
        ///     The list of of maze navigator controllers against which to evaluate the given maze configurations.
        /// </summary>
        private IList<IBlackBox> _agentControllers;

        /// <summary>
        ///     The number of navigation attempts that must succeed for meeting the minimal criteria.
        /// </summary>
        private readonly int _numAgentsSolvedCriteria;

        /// <summary>
        ///     The number of navigation attempts that must fail for meeting the minimal criteria.
        /// </summary>
        private readonly int _numAgentsFailedCriteria;

        /// <summary>
        ///     Per-evaluation data logger (generates one row per maze trial).
        /// </summary>
        private readonly IDataLogger _evaluationLogger;

        /// <summary>
        ///     Random number generator that controls evaluation selection order.
        /// </summary>
        private readonly IRandomSource _rng = RandomDefaults.CreateRandomSource();

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
        ///     Runs a collection of agents through a maze until the minimal criteria is satisfied.
        /// </summary>
        /// <param name="mazeStructure">
        ///     The structure of the maze under evaluation (namely, the walls in said maze, their passages,
        ///     and the start/goal locations).
        /// </param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <returns>A behavior info (which is a type of behavior-based trial information).</returns>
        public BehaviorInfo Evaluate(MazeStructure mazeStructure, uint currentGeneration)
        {
            var curSuccesses = 0;
            var curFailures = 0;
            var behaviorInfo = new BehaviorInfo();

            foreach (var cnt in Enumerable.Range(0, _agentControllers.Count).OrderBy(x => _rng.Next()))
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
                var world = _multiMazeWorldFactory.CreateMazeNavigationWorld(mazeStructure, behaviorCharacterization);

                // Run a single trial
                var trialBehavior = world.RunBehaviorTrial(_agentControllers[cnt].Clone(), out var goalReached);

                // Set the objective distance
                var objectiveDistance = world.GetDistanceToTarget();

                // Log trial information
                _evaluationLogger?.LogRow(new List<LoggableElement>
                    {
                        new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                        new LoggableElement(EvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                        new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                        new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Primary)
                    },
                    world.GetLoggableElements());

                // If the navigator reached the goal, increment the running count of successes
                if (goalReached)
                {
                    curSuccesses++;
                    isSuccessful = true;
                }
                // Otherwise, increment the number of failures
                else
                    curFailures++;

                // Add simulation trial info
                behaviorInfo.TrialData.Add(new TrialInfo(isSuccessful, objectiveDistance,
                    world.GetSimulationTimesteps(), _agentControllers[cnt].GenomeId, trialBehavior));

                // Continue to the next iteration if the MC has still not yet been satisfied
                if (curSuccesses < _numAgentsSolvedCriteria || curFailures < _numAgentsFailedCriteria) continue;

                // If the number of successful maze navigations and failed maze navigations are both equivalent to their
                // respective minimums, then the minimal criteria has been satisfied so terminate the evaluation loop
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
            }, _multiMazeWorldFactory.CreateMazeNavigationWorld(new MazeStructure(0, 0, 1), null).GetLoggableElements());
        }

        /// <inheritdoc />
        /// <summary>
        ///     Updates the collection of maze navigators to use for future evaluations.
        /// </summary>
        /// <param name="evaluatorPhenomes">The complete collection of available maze navigators.</param>
        /// <param name="lastGeneration">The generation that was just executed.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes, uint lastGeneration)
        {
            _agentControllers = (IList<IBlackBox>) evaluatorPhenomes;
        }

        /// <summary>
        ///     Cleans up evaluator state after end of execution or upon execution interruption.  In particular, this
        ///     closes out any existing evaluation logger instance.
        /// </summary>
        public void Cleanup()
        {
            _evaluationLogger?.Close();
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