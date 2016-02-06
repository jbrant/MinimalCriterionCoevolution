#region

using System.Collections.Generic;

#endregion

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
        ///     Generates a behavior characterization of the appropriate type with the pre-specified minimal criteria (if
        ///     applicable) and flag indicating whether reversing the viability criteria is permitted.
        /// </summary>
        /// <param name="allowReverseCriteria">Flag indicating whether reversal of the minimal criteria is permitted.</param>
        /// <returns></returns>
        IBehaviorCharacterization CreateBehaviorCharacterization(bool allowReverseCriteria);

        /// <summary>
        ///     Generates a behavior characterization of the appropriate type with the given minimal criteria.
        /// </summary>
        /// <param name="minimalCriteria"></param>
        /// <returns>The constructed behavior characterization with the specified minimal criteria.</returns>
        IBehaviorCharacterization CreateBehaviorCharacterization(IMinimalCriteria minimalCriteria);

        /// <summary>
        ///     Calls the update procedure on the minimal criteria stored within the behavior characterization factory.
        /// </summary>
        /// <typeparam name="TGenome">Genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        void UpdateBehaviorCharacterizationMinimalCriteria<TGenome>(List<TGenome> population)
            where TGenome : class, IGenome<TGenome>;

        /// <summary>
        ///     Returns the scalar value of the minimal criteria.
        /// </summary>
        /// <returns>The scalar value of the minimal criteria.</returns>
        dynamic GetMinimalCriteriaScalarValue();
    }
}