using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    internal class MazeNavigationEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        /// <summary>
        ///     Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that the evolutionary algorithm/search
        ///     should stop.  This property's value can remain false to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied { get; }

        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            // Increment eval count
            EvaluationCount++;

            return new FitnessInfo();
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }

        #region Class Variables

        // Evaluator state

        #endregion
    }
}