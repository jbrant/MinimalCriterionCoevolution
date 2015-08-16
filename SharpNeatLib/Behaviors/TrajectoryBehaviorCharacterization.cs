using System.Collections.Generic;
using SharpNeat.Core;

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a behavior characterization based on the trajectory of an agent.  This reduces to capturing the position of
    ///     the agent in n-dimensional space at every time step.
    /// </summary>
    internal class TrajectoryBehaviorCharacterization : IBehaviorCharacterization
    {
        /// <summary>
        ///     The double array of behaviors.  Since this is a trajectory characterization, it will contain the position of the
        ///     agent for each time step.
        /// </summary>
        public List<double> Behaviors { get; private set; }

        /// <summary>
        ///     Calculates the distance between this behavior characterization and the given behavior characterization.
        /// </summary>
        /// <param name="bcToCompare">
        ///     The behavior characterization against which to calculate the distance.  Note that this
        ///     behavior characterization needs to be an trajectory characterization in order to compare them.
        /// </param>
        /// <returns></returns>
        public double CalculateDistance(IBehaviorCharacterization bcToCompare)
        {
            if (!(bcToCompare is TrajectoryBehaviorCharacterization))
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
        ///     Updates the behavior array.  This equates to appending the the end point to the existing behavior array, thus
        ///     establishing a measure of trajectory.
        /// </summary>
        /// <param name="newBehaviors">The new end point to append to the existing behavior state.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
            Behaviors.AddRange(newBehaviors);
        }
    }
}