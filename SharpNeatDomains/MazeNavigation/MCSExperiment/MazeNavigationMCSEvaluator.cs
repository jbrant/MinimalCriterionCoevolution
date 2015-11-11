#region

using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    public class MazeNavigationMCSEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        private readonly IBehaviorCharacterization _behaviorCharacterization;
        private readonly int? _maxDistanceToTarget;
        private readonly int? _maxTimesteps;
        private readonly MazeVariant _mazeVariant;
        private readonly int? _minSuccessDistance;
        private readonly object evaluationLock = new object();
        private bool _stopConditionSatisfied;

        internal MazeNavigationMCSEvaluator(int? maxDistanceToTarget, int? maxTimesteps, MazeVariant mazeVariant,
            int? minSuccessDistance, IBehaviorCharacterization behaviorCharacterization, ulong initializationEvaluations = 0)
        {
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimesteps;
            _mazeVariant = mazeVariant;
            _minSuccessDistance = minSuccessDistance;
            _behaviorCharacterization = behaviorCharacterization;
            EvaluationCount = initializationEvaluations;
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
            bool stopConditionSatisfied = false;

            // Instantiate the maze world
            var world = new MazeNavigationWorld<BehaviorInfo>(_mazeVariant, _minSuccessDistance, _maxDistanceToTarget,
                _maxTimesteps, _behaviorCharacterization);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(phenome, SearchType.MinimalCriteriaSearch,
                out stopConditionSatisfied);

            // Set the objective distance
            trialInfo.ObjectiveDistance = world.GetDistanceToTarget();

            // Check if the current location satisfies the minimal criteria
            if (_behaviorCharacterization.IsMinimalCriteriaSatisfied(trialInfo) == false)
            {
                trialInfo.DoesBehaviorSatisfyMinimalCriteria = false;
            }

            // If the navigator reached the goal, stop the experiment
            if (stopConditionSatisfied)
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
            evaluationLogger?.LogHeader(
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