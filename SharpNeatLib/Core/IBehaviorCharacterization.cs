using System.Collections.Generic;

namespace SharpNeat.Core
{
    /// <summary>
    ///     Interface for genome behavior characterizations.
    /// </summary>
    public interface IBehaviorCharacterization
    {
        /// <summary>
        ///     The behaviors of a genotype (represented as a list of double-precision values).
        /// </summary>
        List<double> Behaviors { get; }

        /// <summary>
        ///     Calculates the distance to another behavior characterization in a given behavior space.  Note that for the
        ///     comparison to be valid, the given behavior characterizations need to be the same.
        /// </summary>
        /// <param name="bcToCompare">The behavior characterization against which to compare.</param>
        /// <returns>A numeric value representing behavioral similarity.</returns>
        double CalculateDistance(IBehaviorCharacterization bcToCompare);

        /// <summary>
        ///     Updates the behavior characterization with the numeric list of new (or replacement) behaviors.
        /// </summary>
        /// <param name="newBehaviors">The numeric list of behaviors with which to update this characterization.</param>
        void UpdateBehaviors(List<double> newBehaviors);
    }
}