using System.Collections.Generic;
using SharpNeat.Core;

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a behavior characterization based on the end-point of an agent.  As such, the behavior is simply defined as
    ///     a coordinate in n-dimensional space.
    /// </summary>
    internal class EndPointBehaviorCharacterization : IBehaviorCharacterization
    {
        /// <summary>
        ///     The double array of behaviors.  Since this is an end-point characterization, it should only contain the number of
        ///     elements equivalent to the dimensionality of the state space.
        /// </summary>
        public List<double> Behaviors { get; private set; }

        /// <summary>
        ///     Calculates the distance between this behavior characterization and the given behavior characterization.
        /// </summary>
        /// <param name="bcToCompare">
        ///     The behavior characterization against which to calculate the distance.  Note that this
        ///     behavior characterization needs to be an end-point characterization in order to compare them.
        /// </param>
        /// <returns>The distance between the behavior characterizations.</returns>
        public double CalculateDistance(IBehaviorCharacterization bcToCompare)
        {
            // If the behavior characterization to compare is not the same, it doesn't
            // make sense to compare them
            if (!(bcToCompare is EndPointBehaviorCharacterization))
            {
                // TODO: Probably throw an exception here since it doesn't make sense to compare behavior characterizations that are not of the same type
            }

            double distance = 0;

            // Compare the behavior arrays in an element-wise fashion
            for (var position = 0; position < Behaviors.Count; position++)
            {
                var delta = Behaviors[position] - bcToCompare.Behaviors[position];
                distance += delta*delta;
            }

            return distance;
        }

        /// <summary>
        ///     Updates the behavior array.  This equates to simply replacing the behavior array with the coordinates of the new
        ///     end point.
        /// </summary>
        /// <param name="newBehaviors"></param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
            // Overwrite the existing behavior array with the current location
            Behaviors = newBehaviors;
        }
    }
}