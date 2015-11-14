namespace SharpNeat.Core
{
    /// <summary>
    ///     Interface for behavior characterization factory.  Behavior characterization factories construct new behavior
    ///     characterizations with an optional minimal criteria.
    /// </summary>
    public interface IBehaviorCharacterizationFactory
    {
        /// <summary>
        ///     Generates a behavior characterization of the appropriate type with the pre-specified minimal criteria (if
        ///     applicable).
        /// </summary>
        /// <returns>The constructed behavior characterization.</returns>
        IBehaviorCharacterization CreateBehaviorCharacterization();

        /// <summary>
        ///     Generates a behavior characterization of the appropriate type with the given minimal criteria.
        /// </summary>
        /// <param name="minimalCriteria"></param>
        /// <returns>The constructed behavior characterization with the specified minimal criteria.</returns>
        IBehaviorCharacterization CreateBehaviorCharacterization(IMinimalCriteria minimalCriteria);
    }
}