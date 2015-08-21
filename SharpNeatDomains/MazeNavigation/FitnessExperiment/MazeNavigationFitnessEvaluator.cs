using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    internal class MazeNavigationFitnessEvaluator : IPhenomeEvaluator<IBlackBox, FitnessInfo>
    {
        private readonly int? _maxDistanceToTarget;
        private readonly int? _maxTimesteps;
        private readonly MazeVariant _mazeVariant;
        private readonly int? _minSuccessDistance;
        private bool _stopConditionSatisfied;

        internal MazeNavigationFitnessEvaluator(int? maxDistanceToTarget, int? maxTimesteps, MazeVariant mazeVariant,
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
        public bool StopConditionSatisfied
        {
            get { return _stopConditionSatisfied; }
            set { _stopConditionSatisfied = value; }
        }

        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            // Increment eval count
            EvaluationCount++;

            // Default the stop condition satisfied to false
            _stopConditionSatisfied = false;

            // Instantiate the maze world
            var world = new MazeNavigationWorld<FitnessInfo>(_mazeVariant, _minSuccessDistance, _maxDistanceToTarget,
                _maxTimesteps);

            // Run a single trial
            return world.RunTrial(phenome, EvaluationType.Fitness, out _stopConditionSatisfied);
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }
    }
}