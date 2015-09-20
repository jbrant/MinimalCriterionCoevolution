#region

using System.Collections.Generic;
using SharpNeat.Core;

#endregion

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
        ///     The minimal criteria which the behavior must meet in order to be considered viable.
        /// </summary>
        public IMinimalCriteria MinimalCriteria { get; set; }

        /// <summary>
        ///     Does nothing given that there are no behaviors to update.
        /// </summary>
        /// <param name="newBehaviors">The list of new behaviors.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
        }

        /// <summary>
        ///     Evaluates whether the given behavior info meets the minimal criteria for this behavior characterization.  However,
        ///     given that this is the null behavior characterization, it will always meet the minimal criteria (given that there
        ///     probably isn't one).
        /// </summary>
        /// <param name="behaviorInfo">The behavior info to evaluate.</param>
        /// <returns>
        ///     Boolean value indicating whether the given behavior info meets the minimal criteria for this behavior
        ///     characterization.  This will always be true given that this is the null behavior characterization.
        /// </returns>
        public bool IsMinimalCriteriaSatisfied(BehaviorInfo behaviorInfo)
        {
            return true;
        }
    }
}