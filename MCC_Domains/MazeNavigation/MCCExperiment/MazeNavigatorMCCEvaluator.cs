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
    ///     Defines evaluation routine for agents (maze navigators) within a minimal criterion coevolution (MCC) framework.
    /// </summary>
    public class MazeNavigatorMCSEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
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
        public MazeNavigatorMCSEvaluator(int minSuccessDistance,
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory, int agentNumSuccessesCriteria)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            _agentNumSuccessesCriteria = agentNumSuccessesCriteria;
            EvaluationCount = 0;

            // Create factory for generating multiple mazes
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
        ///     The number of mazes that the agent must navigate in order to meet the minimal criteria.
        /// </summary>
        private readonly int _agentNumSuccessesCriteria;

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
        /// <param name="evaluationLogger">Reference to the evaluation logger.</param>
        /// <returns>A behavior info (which is a type of behavior-based trial information).</returns>
        public BehaviorInfo Evaluate(IBlackBox agent, uint currentGeneration,
            IDataLogger evaluationLogger)
        {
            var curSuccesses = 0;

            // TODO: Note that this will get overwritten until the last successful attempt (may need a better way of handling this for logging purposes)
            var trialInfo = BehaviorInfo.NoBehavior;

            for (var cnt = 0; cnt < _multiMazeWorldFactory.NumMazes && curSuccesses < _agentNumSuccessesCriteria; cnt++)
            {
                var threadLocalEvaluationCount = default(ulong);
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
                trialInfo = world.RunTrial(agent, SearchType.MinimalCriteriaSearch, out var goalReached);

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

                // If the navigator reached the goal, update the running count of successes
                if (goalReached)
                    curSuccesses++;
            }

            // If the number of successful maze navigations was equivalent to the minimum required,
            // then the minimal criteria has been satisfied
            if (curSuccesses >= _agentNumSuccessesCriteria)
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
        ///     Updates the collection of mazes to use for future evaluations.
        /// </summary>
        /// <param name="evaluatorPhenomes">The complete collection of available mazes.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes)
        {
            _multiMazeWorldFactory.SetMazeConfigurations((IList<MazeStructure>) evaluatorPhenomes);
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
        ///     Returns MazeNavigatorMCSEvaluator loggable elements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable elements for MazeNavigatorMCSEvaluator.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return _behaviorCharacterizationFactory.GetLoggableElements(logFieldEnableMap);
        }

        #endregion
    }
}