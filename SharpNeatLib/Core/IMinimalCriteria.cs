namespace SharpNeat.Core
{
    /// <summary>
    ///     Interface for minimal criterion specifications.
    /// </summary>
    public interface IMinimalCriteria
    {
        /// <summary>
        ///     Evaluates whether the given behavior characterization satisfies the minimal criteria.
        /// </summary>
        /// <param name="behaviorInfo">The behavior info.</param>
        /// <returns>Boolean value indicating whether the given behavior characterization satisfies the minimal criteria.</returns>
        bool DoesCharacterizationSatisfyMinimalCriteria(BehaviorInfo behaviorInfo);
    }
}