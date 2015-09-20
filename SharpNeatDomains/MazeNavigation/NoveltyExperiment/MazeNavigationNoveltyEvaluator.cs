#region

using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.NoveltyExperiment
{
    internal class MazeNavigationNoveltyEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        private readonly IBehaviorCharacterization _behaviorCharacterization;
        private readonly int? _maxDistanceToTarget;
        private readonly int? _maxTimesteps;
        private readonly MazeVariant _mazeVariant;
        private readonly int? _minSuccessDistance;
        private bool _stopConditionSatisfied;

        internal MazeNavigationNoveltyEvaluator(int? maxDistanceToTarget, int? maxTimesteps, MazeVariant mazeVariant,
            int? minSuccessDistance, IBehaviorCharacterization behaviorCharacterization)
        {
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimesteps;
            _mazeVariant = mazeVariant;
            _minSuccessDistance = minSuccessDistance;
            _behaviorCharacterization = behaviorCharacterization;
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

        public BehaviorInfo Evaluate(IBlackBox phenome)
        {
            // Increment evaluation count
            EvaluationCount++;

            // Default the stop condition satisfied to false
            bool stopConditionSatisfied = false;

            // Instantiate the maze world
            var world = new MazeNavigationWorld<BehaviorInfo>(_mazeVariant, _minSuccessDistance, _maxDistanceToTarget,
                _maxTimesteps, _behaviorCharacterization);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(phenome, EvaluationType.NoveltySearch, out stopConditionSatisfied);

            // Check if the current location satisfies the minimal criteria
            if (_behaviorCharacterization.IsMinimalCriteriaSatisfied(trialInfo) == false)
            {
                trialInfo.DoesBehaviorSatisfyMinimalCriteria = false;
            }

            // If the navigator reached the goal, stop the experiment
            if (stopConditionSatisfied)
                StopConditionSatisfied = true;

            return trialInfo;
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }
    }
}