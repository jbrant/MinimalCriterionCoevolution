using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;

namespace SharpNeat.MinimalCriterias
{
    public class MileageCriteria : IMinimalCriteria
    {
        /// <summary>
        ///     Hard-coded number of dimensions in euclidean space.
        /// </summary>
        private const int EuclideanDimensions = 2;

        /// <summary>
        ///     The minimum mileage that the candidate agent had to travel to be considered viable.
        /// </summary>
        private readonly double _minimumMileage;

        /// <summary>
        /// Constructor for the mileage minimal criteria.
        /// </summary>
        /// <param name="minimumMileage">The minimum mileage that an agent has to travel.</param>
        public MileageCriteria(double minimumMileage)
        {
            _minimumMileage = minimumMileage;
        }

        public bool DoesCharacterizationSatisfyMinimalCriteria(BehaviorInfo behaviorInfo)
        {
            throw new NotImplementedException();
        }
    }
}
