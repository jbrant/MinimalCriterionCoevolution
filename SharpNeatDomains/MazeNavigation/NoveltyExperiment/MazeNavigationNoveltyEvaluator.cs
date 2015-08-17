using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation.NoveltyExperiment
{
    internal class MazeNavigationNoveltyEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        private readonly int? _maxDistanceToTarget;
        private readonly int? _maxTimesteps;
        private readonly MazeVariant _mazeVariant;
        private readonly int? _minSuccessDistance;

        internal MazeNavigationNoveltyEvaluator(int? maxDistanceToTarget, int? maxTimesteps, MazeVariant mazeVariant,
            int? minSuccessDistance)
        {
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimesteps;
            _mazeVariant = mazeVariant;
            _minSuccessDistance = minSuccessDistance;
        }

        /// <summary>
        ///     Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that the evolutionary algorithm/search
        ///     should stop.  This property's value can remain false to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied => false;
        
        public BehaviorInfo Evaluate(IBlackBox phenome)
        {
            // Increment evaluation count
            EvaluationCount++;

            // Instantiate the maze world
            var world = new MazeNavigationWorld<BehaviorInfo>(_mazeVariant, _minSuccessDistance, _maxDistanceToTarget,
                _maxTimesteps);

            // Run a single trial
            return world.RunTrial(phenome, EvaluationType.NoveltySearch);
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }        
    }
}