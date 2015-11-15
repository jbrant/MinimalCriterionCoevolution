#region

using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    internal class MazeNavigationNoveltyInitializationEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        private readonly IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private readonly int? _maxDistanceToTarget;
        private readonly int? _maxTimesteps;
        private readonly MazeVariant _mazeVariant;
        private readonly int? _minSuccessDistance;
        private readonly object evaluationLock = new object();

        internal MazeNavigationNoveltyInitializationEvaluator(int? maxDistanceToTarget, int? maxTimesteps,
            MazeVariant mazeVariant,
            int? minSuccessDistance, IBehaviorCharacterizationFactory behaviorCharacterizationFactory)
        {
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimesteps;
            _mazeVariant = mazeVariant;
            _minSuccessDistance = minSuccessDistance;
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
        }

        /// <summary>
        ///     Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that the evolutionary algorithm/search
        ///     should stop.  This property's value can remain false to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied { get; private set; }

        public BehaviorInfo Evaluate(IBlackBox phenome, uint currentGeneration, IDataLogger evaluationLogger)
        {
            ulong threadLocalEvaluationCount;
            lock (evaluationLock)
            {
                // Increment evaluation count
                threadLocalEvaluationCount = EvaluationCount++;
            }

            // Default the stop condition satisfied to false
            bool goalReached = false;

            // Generate new behavior characterization
            IBehaviorCharacterization behaviorCharacterization =
                _behaviorCharacterizationFactory.CreateBehaviorCharacterization();

            // Instantiate the maze world
            MazeNavigationWorld<BehaviorInfo> world = new MazeNavigationWorld<BehaviorInfo>(_mazeVariant,
                _minSuccessDistance, _maxDistanceToTarget,
                _maxTimesteps, behaviorCharacterization);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(phenome, SearchType.NoveltySearch, out goalReached);

            // Set the objective distance
            trialInfo.ObjectiveDistance = world.GetDistanceToTarget();

            // Check if the current location satisfies the minimal criteria
            if (behaviorCharacterization.IsMinimalCriteriaSatisfied(trialInfo) == false)
            {
                trialInfo.DoesBehaviorSatisfyMinimalCriteria = false;
            }

            // If the navigator reached the goal or if the minimal criteria has been satisfied, then stop the experiment
            if (goalReached || trialInfo.DoesBehaviorSatisfyMinimalCriteria)
                StopConditionSatisfied = true;

            // Log trial information
            evaluationLogger?.LogRow(new List<LoggableElement>
            {
                new LoggableElement(NoveltyEvaluationFieldElements.Generation, currentGeneration),
                new LoggableElement(NoveltyEvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                new LoggableElement(NoveltyEvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied)
            },
                world.GetLoggableElements());

            return trialInfo;
        }

        /// <summary>
        ///     Initializes the logger and writes header.
        /// </summary>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        public void Initialize(IDataLogger evaluationLogger)
        {
            evaluationLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(NoveltyEvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(NoveltyEvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied)
            },
                new MazeNavigationWorld<FitnessInfo>(_mazeVariant, _minSuccessDistance, _maxDistanceToTarget,
                    _maxTimesteps).GetLoggableElements());
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }
    }
}