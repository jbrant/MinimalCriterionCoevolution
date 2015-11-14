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

        /// <summary>
        ///     Converts behavior characterization to an array of doubles.
        /// </summary>
        /// <returns>Behavior characterization as an array of doubles.</returns>
        double[] GetBehaviorCharacterizationAsArray();
    }
}