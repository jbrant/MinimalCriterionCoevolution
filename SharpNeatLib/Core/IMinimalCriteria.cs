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
        /// <param name="allowCriteriaReversal">
        ///     Permits reversing the minimal criteria (such that only those who do *not* meet the
        ///     minimal criteria are valid).
        /// </param>
        /// <returns>Boolean value indicating whether the given behavior characterization satisfies the minimal criteria.</returns>
        bool DoesCharacterizationSatisfyMinimalCriteria(BehaviorInfo behaviorInfo, bool allowCriteriaReversal);

        /// <summary>
        ///     Returns the scalar value of the minimal criteria.
        /// </summary>
        /// <returns>The scalar value of the minimal criteria.</returns>
        dynamic GetMinimalCriteriaValue();
    }
}