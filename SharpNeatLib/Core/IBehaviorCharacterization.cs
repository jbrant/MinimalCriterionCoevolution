#region

using System.Collections.Generic;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Interface for behavior characterizations.
    /// </summary>
    public interface IBehaviorCharacterization
    {
        /// <summary>
        ///     The behaviors of an individual (represented as a list of double-precision values).
        /// </summary>
        List<double> Behaviors { get; }

        /// <summary>
        ///     The minimal criteria which the behavior must meet in order to be considered viable.
        /// </summary>
        IMinimalCriteria MinimalCriteria { get; set; }

        /// <summary>
        ///     Updates the behavior characterization with the numeric list of new (or replacement) behaviors.
        /// </summary>
        /// <param name="newBehaviors">The numeric list of behaviors with which to update this characterization.</param>
        void UpdateBehaviors(List<double> newBehaviors);

        /// <summary>
        ///     Determine whether the given behavior instance meets the minimal criteria for this behavior.
        /// </summary>
        /// <param name="behaviorInfo">The post-evaluation behavior of the individual.</param>
        /// <returns></returns>
        bool IsMinimalCriteriaSatisfied(BehaviorInfo behaviorInfo);
    }
}