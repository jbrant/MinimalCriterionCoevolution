#region

using System.Collections.Generic;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a factory for creating new end-point behavior characterization instances.
    /// </summary>
    public class EndPointBehaviorCharacterizationFactory : IBehaviorCharacterizationFactory
    {
        /// <summary>
        ///     The minimal criteria to set on the constructed behavior characterizations (optional).
        /// </summary>
        private readonly IMinimalCriteria _minimalCriteria;

        /// <summary>
        ///     Constructor for end-point behavior characterization factory.
        /// </summary>
        /// <param name="minimalCriteria">The minimal criteria to set on the constructed end-point behavior characterizations.</param>
        public EndPointBehaviorCharacterizationFactory(IMinimalCriteria minimalCriteria)
        {
            _minimalCriteria = minimalCriteria;
        }

        /// <summary>
        ///     Constructs a new end-point behavior characterization with the minimal criteria held by the factory (if applicable).
        /// </summary>
        /// <returns>Constructed end-point behavior characterization.</returns>
        public IBehaviorCharacterization CreateBehaviorCharacterization()
        {
            return new EndPointBehaviorCharacterization(_minimalCriteria, false);
        }

        /// <summary>
        ///     Constructs a new end-point behavior characterization with the minimal criteria held by the factory (if applicable)
        ///     and a flag indicating whether the minimal criteria is permitted to be reversed when determining viability.
        /// </summary>
        /// <returns>Constructed end-point behavior characterization.</returns>
        public IBehaviorCharacterization CreateBehaviorCharacterization(bool allowReverseCriteria)
        {
            return new EndPointBehaviorCharacterization(_minimalCriteria, allowReverseCriteria);
        }

        /// <summary>
        ///     Constructs a new end-point behavior characterization with the specified minimal criteria.
        /// </summary>
        /// <param name="minimalCriteria">The custom minimal criteria to set on the behavior characterization.</param>
        /// <returns>Constructed end-point behavior characterization with the custom minimal criteria.</returns>
        public IBehaviorCharacterization CreateBehaviorCharacterization(IMinimalCriteria minimalCriteria)
        {
            return new EndPointBehaviorCharacterization(minimalCriteria, false);
        }

        /// <summary>
        ///     Calls the update procedure on the minimal criteria stored within the behavior characterization factory.
        /// </summary>
        /// <typeparam name="TGenome">Genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void UpdateBehaviorCharacterizationMinimalCriteria<TGenome>(List<TGenome> population)
            where TGenome : class, IGenome<TGenome>
        {
            _minimalCriteria.UpdateMinimalCriteria(population);
        }

        /// <summary>
        ///     Returns the scalar value of the minimal criteria.
        /// </summary>
        /// <returns>The scalar value of the minimal criteria.</returns>
        public dynamic GetMinimalCriteriaScalarValue()
        {
            return _minimalCriteria.GetMinimalCriteriaValue();
        }
    }
}