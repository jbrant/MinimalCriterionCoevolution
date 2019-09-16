#region

using System.Collections.Generic;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a behavior characterization based on the end-point of an agent.  As such, the behavior is simply defined as
    ///     a coordinate in n-dimensional space.
    /// </summary>
    public class EndPointBehaviorCharacterization : IBehaviorCharacterization
    {
        /// <summary>
        ///     The double array of behaviors.  Since this is an end-point characterization, it should only contain the number of
        ///     elements equivalent to the dimensionality of the state space.
        /// </summary>
        private List<double> _behaviors;

        /// <summary>
        ///     Updates the behavior array.  This equates to simply replacing the behavior array with the coordinates of the new
        ///     end point.
        /// </summary>
        /// <param name="newBehaviors">The new end point with which to characterize the behavior state.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
            // Overwrite the existing behavior array with the current location
            _behaviors = newBehaviors;
        }

        /// <summary>
        ///     Converts behavior characterization to an array of doubles.
        /// </summary>
        /// <returns>Behavior characterization as an array of doubles.</returns>
        public double[] GetBehaviorCharacterizationAsArray()
        {
            return _behaviors.ToArray();
        }
    }
}