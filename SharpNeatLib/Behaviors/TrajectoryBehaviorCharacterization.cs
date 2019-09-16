#region

using System.Collections.Generic;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a behavior characterization based on the trajectory of an agent.  This reduces to capturing the position of
    ///     the agent in n-dimensional space at every time step.
    /// </summary>
    public class TrajectoryBehaviorCharacterization : IBehaviorCharacterization
    {
        /// <summary>
        ///     The double array of behaviors.  Since this is a trajectory characterization, it will contain the position of the
        ///     agent for each time step.
        /// </summary>
        private readonly List<double> _behaviors;

        /// <summary>
        ///     Default trajectory-behavior characterization constructor.
        /// </summary>
        public TrajectoryBehaviorCharacterization()
        {
            _behaviors = new List<double>();
        }

        /// <summary>
        ///     Updates the behavior array.  This equates to appending the the end point to the existing behavior array, thus
        ///     establishing a measure of trajectory.
        /// </summary>
        /// <param name="newBehaviors">The new end point to append to the existing behavior state.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
            _behaviors.AddRange(newBehaviors);
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