#region

using System.Collections.Generic;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Interface for minimal criterion specifications.
    /// </summary>
    public interface IMinimalCriteria
    {
        /// <summary>
        ///     Updates the minimal criteria based on characteristics of the current population.
        /// </summary>
        /// <typeparam name="TGenome">Genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        void UpdateMinimalCriteria<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>;

        /// <summary>
        ///     Evaluates whether the given behavior characterization satisfies the minimal criteria.
        /// </summary>
        /// <param name="behaviorInfo">The behavior info.</param>
        /// <returns>Boolean value indicating whether the given behavior characterization satisfies the minimal criteria.</returns>
        bool DoesCharacterizationSatisfyMinimalCriteria(BehaviorInfo behaviorInfo);
    }
}