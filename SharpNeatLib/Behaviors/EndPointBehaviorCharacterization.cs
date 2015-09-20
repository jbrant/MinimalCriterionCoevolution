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
        ///     Default end-point behavior characterization constructor.
        /// </summary>
        public EndPointBehaviorCharacterization()
        {
        }

        /// <summary>
        ///     End-point behavior characterization constructor accepting a minimal criteria definition.
        /// </summary>
        /// <param name="minimalCriteria"></param>
        public EndPointBehaviorCharacterization(IMinimalCriteria minimalCriteria)
        {
            MinimalCriteria = minimalCriteria;
        }

        /// <summary>
        ///     The double array of behaviors.  Since this is an end-point characterization, it should only contain the number of
        ///     elements equivalent to the dimensionality of the state space.
        /// </summary>
        public List<double> Behaviors { get; private set; }

        /// <summary>
        ///     The minimal criteria which the behavior must meet in order to be considered viable.
        /// </summary>
        public IMinimalCriteria MinimalCriteria { get; set; }

        /// <summary>
        ///     Updates the behavior array.  This equates to simply replacing the behavior array with the coordinates of the new
        ///     end point.
        /// </summary>
        /// <param name="newBehaviors">The new end point with which to characterize the behavior state.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
            // Overwrite the existing behavior array with the current location
            Behaviors = newBehaviors;
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
            // If there is no minimal criteria, then by definition it has been met
            return MinimalCriteria?.DoesCharacterizationSatisfyMinimalCriteria(behaviorInfo) ?? true;
        }
    }
}