#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpNeat.Core;

#endregion

namespace SharpNeat.MinimalCriterias
{
    /// <summary>
    ///     Defines the calculations for determining whether a given behavior satisfies the mileage criteria.
    /// </summary>
    public class MileageCriteria : IMinimalCriteria
    {
        /// <summary>
        ///     Hard-coded number of dimensions in euclidean space.
        /// </summary>
        private const int EuclideanDimensions = 2;

        /// <summary>
        ///     The maximum allowed number of update cycles without a change to the minimal criteria.
        /// </summary>
        private readonly double? _maxUpdateCyclesWithoutChange = 5;

        /// <summary>
        ///     The random number generator which generates new MCs when the current one gets stuck.
        /// </summary>
        private readonly Random _randomNumGenerator = new Random();

        /// <summary>
        ///     The x-component of the starting position.
        /// </summary>
        private readonly double _startXPosition;

        /// <summary>
        ///     The y-component of the starting position;
        /// </summary>
        private readonly double _startYPosition;

        /// <summary>
        ///     The minimum mileage that the candidate agent had to travel to be considered viable.
        /// </summary>
        private double _minimumMileage;

        /// <summary>
        ///     The number of times the minimal criteria has been updated without a significant change in that criteria.
        /// </summary>
        private int _numUpdateCyclesWithoutChange;

        /// <summary>
        ///     Constructor for the mileage minimal criteria.
        /// </summary>
        /// <param name="startingXLocation">The x-component of the starting position.</param>
        /// <param name="startingYLocation">The y-component of the starting position.</param>
        /// <param name="minimumMileage">The minimum mileage that an agent has to travel.</param>
        public MileageCriteria(double startingXLocation, double startingYLocation, double minimumMileage)
        {
            _startXPosition = startingXLocation;
            _startYPosition = startingYLocation;
            _minimumMileage = minimumMileage;
        }

        /// <summary>
        ///     Updates the minimal criteria based on characteristics of the current population.
        /// </summary>
        /// <typeparam name="TGenome">Genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void UpdateMinimalCriteria<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>
        {
            // Calculate the mean mileage traversed over all of the individuals in the population            
            double newMileageMc =
                population.Sum(
                    genome =>
                        CalculateIndividualMileage(new BehaviorInfo(genome.EvaluationInfo.BehaviorCharacterization))) / population.Count;
            
            // If the change between the two mileage criterias is less than 1 unit, then it is considered marginal
            bool isCriteriaChangeMarginal = Math.Abs(newMileageMc - _minimumMileage) < 1;

            // If the distance between the new MC and the previous MC is marginal and the number of updates
            // without a modification to the MC has been surpassed, reset the MC
            if (_maxUpdateCyclesWithoutChange != null && isCriteriaChangeMarginal &&
                _numUpdateCyclesWithoutChange >= _maxUpdateCyclesWithoutChange)
            {
                // Pick a random MC somewhere between 0 and the current MC
                newMileageMc = _randomNumGenerator.Next(0, (int)_minimumMileage);

                // Reset the number of MC update cycles without an MC change to 0
                _numUpdateCyclesWithoutChange = 0;
            }

            // Otherwise, if the change is marginal but the requisite number of update cycles with no change has
            // not been surpassed, then just increment the number of update cycles with no MC change
            else if (_maxUpdateCyclesWithoutChange != null && isCriteriaChangeMarginal)
            {
                // Increment the number of MC update cycles that resulted in no change to the MC
                _numUpdateCyclesWithoutChange++;
            }

            // Assign the updated minimal criteria
            _minimumMileage = newMileageMc;
        }

        /// <summary>
        ///     Evaluate whether the given behavior characterization satisfies the minimal criteria based on the mileage (computed
        ///     as the distance transited between any two consecutive timesteps).
        /// </summary>
        /// <param name="behaviorInfo">The behavior info indicating the full trajectory of the agent.</param>
        /// <returns>Boolean value indicating whether the given behavior characterization satisfies the minimal criteria.</returns>
        public bool DoesCharacterizationSatisfyMinimalCriteria(BehaviorInfo behaviorInfo)
        {
            // If the behavior dimensionality doesn't align with the specified dimensionality, we can't compare it
            if (behaviorInfo.Behaviors.Length%EuclideanDimensions != 0)
            {
                throw new SharpNeatException(
                    "Cannot evaluate minimal criteria constraints because the behavior characterization is not of the correct dimensionality.");
            }

            // Only return true if the mileage is at least the minimum required mileage
            return Math.Round(CalculateIndividualMileage(behaviorInfo)) >= Math.Round(_minimumMileage);
        }

        /// <summary>
        ///     Returns the scalar value of the minimal criteria.
        /// </summary>
        /// <returns>The scalar value of the minimal criteria.</returns>
        public dynamic GetMinimalCriteriaValue()
        {
            return _minimumMileage;
        }

        /// <summary>
        ///     Calculates the total mileage traversed for the given behavior info (which is the ending position for every
        ///     timestep).
        /// </summary>
        /// <param name="behaviorInfo">The behavior info for which to calculate the mileage.</param>
        /// <returns>The total mileage for the trial.</returns>
        private double CalculateIndividualMileage(BehaviorInfo behaviorInfo)
        {
            double mileage = 0;

            for (int curPosition = 0;
                curPosition < behaviorInfo.Behaviors.Length/EuclideanDimensions;
                curPosition += EuclideanDimensions)
            {
                // Extract x and y components of location
                double curXPosition = behaviorInfo.Behaviors[curPosition];
                double curYPosition = behaviorInfo.Behaviors[curPosition + 1];

                // If this is the first behavior, calculate euclidean distance between the ending position
                // after the first timestep and the starting position
                if (curPosition < EuclideanDimensions)
                {
                    mileage +=
                        Math.Sqrt(Math.Pow(curXPosition - _startXPosition, 2) +
                                  Math.Pow(curYPosition - _startYPosition, 2));
                }

                // Otherwise, calculate the euclidean distance between the ending position at this timestep
                // and the ending position from the previous timestep
                else
                {
                    double prevXPosition = behaviorInfo.Behaviors[curPosition - EuclideanDimensions];
                    double prevYPosition = behaviorInfo.Behaviors[curPosition - EuclideanDimensions + 1];

                    mileage +=
                        Math.Sqrt(Math.Pow(curXPosition - prevXPosition, 2) + Math.Pow(curYPosition - prevYPosition, 2));
                }
            }

            return mileage;
        }
    }
}