#region

using System;
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
        public List<double> Behaviors { get; private set; }

        /// <summary>
        ///     The minimal criteria which the behavior must meet in order to be considered viable.
        /// </summary>
        public IMinimalCriteria MinimalCriteria { get; set; }

        /// <summary>
        ///     Updates the behavior array.  This equates to appending the the end point to the existing behavior array, thus
        ///     establishing a measure of trajectory.
        /// </summary>
        /// <param name="newBehaviors">The new end point to append to the existing behavior state.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
            Behaviors.AddRange(newBehaviors);
        }

        /// <summary>
        ///     Evaluates whether the given behavior info meets the minimal criteria for this behavior characterization.
        /// </summary>
        /// <param name="behaviorInfo">The behavior info to evaluate.</param>
        /// <returns>
        ///     Boolean value indicating whether the given behavior info meets the minimal criteria for this behavior
        ///     characterization.
        /// </returns>
        public bool IsMinimalCriteriaSatisfied(BehaviorInfo behaviorInfo)
        {
            throw new NotImplementedException();
        }
    }
}