using System.Collections.Generic;
using SharpNeat.Core;

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines an empty/null behavior characterization.
    /// </summary>
    public class NullBehaviorCharacterization : IBehaviorCharacterization
    {
        /// <summary>
        ///     Null array of behaviors.
        /// </summary>
        public List<double> Behaviors => null;

        /// <summary>
        ///     Returns a zero distance given the lack of a behavior characterization.
        /// </summary>
        /// <param name="bcToCompare">The behavior characterization against which to compare.</param>
        /// <returns>A zero-valued behavioral distance.</returns>
        public double CalculateDistance(IBehaviorCharacterization bcToCompare)
        {
            return 0;
        }

        /// <summary>
        ///     Does nothing given that there are no behaviors to update.
        /// </summary>
        /// <param name="newBehaviors">The list of new behaviors.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
        }
    }
}