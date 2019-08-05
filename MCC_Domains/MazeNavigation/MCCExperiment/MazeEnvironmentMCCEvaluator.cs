#region

using System;
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
        public MazeEnvironmentMCCEvaluator(int minSuccessDistance,
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory, int numAgentsSolvedCriteria,
            int numAgentsFailedCriteria)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            _numAgentsSolvedCriteria = numAgentsSolvedCriteria;
            _numAgentsFailedCriteria = numAgentsFailedCriteria;

            // Create factory for maze world generation
            _multiMazeWorldFactory = new MultiMazeNavigationWorldFactory<BehaviorInfo>(minSuccessDistance);
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
        private readonly MultiMazeNavigationWorldFactory<BehaviorInfo> _multiMazeWorldFactory;

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
        /// <param name="evaluationLogger">Reference to the evaluation logger.</param>
        /// <returns>A behavior info (which is a type of behavior-based trial information).</returns>
        public BehaviorInfo Evaluate(MazeStructure mazeStructure, uint currentGeneration,
            IDataLogger evaluationLogger)
        {
            var curSuccesses = 0;
            var curFailures = 0;

            // TODO: Note that this will get overwritten until the last successful attempt (may need a better way of handling this for logging purposes)
            var trialInfo = BehaviorInfo.NoBehavior;

            for (var cnt = 0;
                cnt < _agentControllers.Count &&
                (curSuccesses < _numAgentsSolvedCriteria || curFailures < _numAgentsFailedCriteria);
                cnt++)
            {
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
                trialInfo = world.RunTrial(_agentControllers[cnt].Clone(), SearchType.MinimalCriteriaSearch,
                    out var goalReached);

                // Set the objective distance
                trialInfo.ObjectiveDistance = world.GetDistanceToTarget();

                // Log trial information
                evaluationLogger?.LogRow(new List<LoggableElement>
                    {
                        new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                        new LoggableElement(EvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                        new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                        new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Primary),
                        new LoggableElement(EvaluationFieldElements.IsViable,
                            trialInfo.DoesBehaviorSatisfyMinimalCriteria)
                    },
                    world.GetLoggableElements());

                // If the navigator reached the goal, increment the running count of successes
                if (goalReached)
                    curSuccesses++;
                // Otherwise, increment the number of failures
                else
                    curFailures++;
            }

            // If the number of successful maze navigations and failed maze navigations were both equivalent to their
            // respective minimums, then the minimal criteria has been satisfied
            if (curSuccesses >= _numAgentsSolvedCriteria && curFailures >= _numAgentsFailedCriteria)
            {
                trialInfo.DoesBehaviorSatisfyMinimalCriteria = true;
            }

            return trialInfo;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes the logger and writes header.
        /// </summary>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        public void Initialize(IDataLogger evaluationLogger)
        {
            // Set the run phase
            evaluationLogger?.UpdateRunPhase(RunPhase.Primary);

            // Log the header
            evaluationLogger?.LogHeader(new List<LoggableElement>
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
        ///     Update the evaluator based on some characteristic of the given population.
        /// </summary>
        /// <typeparam name="TGenome">The genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void Update<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Updates the collection of maze navigators to use for future evaluations.
        /// </summary>
        /// <param name="evaluatorPhenomes">The complete collection of available maze navigators.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes)
        {
            _agentControllers = (IList<IBlackBox>) evaluatorPhenomes;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Returns MazeEnvironmentMCSEvaluator loggable elements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable elements for MazeEnvironmentMCSEvaluator.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return _behaviorCharacterizationFactory.GetLoggableElements(logFieldEnableMap);
        }

        #endregion
    }
}