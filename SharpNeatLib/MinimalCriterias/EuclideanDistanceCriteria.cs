#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpNeat.Core;

#endregion

namespace SharpNeat.MinimalCriterias
{
    /// <summary>
    ///     Defines the calculations for determining whether a given behavior satisfies the euclidean distance criteria.
    /// </summary>
    public class EuclideanDistanceCriteria : IMinimalCriteria
    {
        /// <summary>
        ///     Hard-coded number of dimensions in euclidean space.
        /// </summary>
        private const int EuclideanDimensions = 2;

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
        ///     The minimum distance that the candidate agent had to travel to be considered viable.
        /// </summary>
        private double _minimumDistanceTraveled;

        /// <summary>
        ///     The number of times the minimal criteria has been updated without a significant change in that criteria.
        /// </summary>
        private int _numUpdateCyclesWithoutChange;

        /// <summary>
        ///     Constructor for the euclidean distance minimal criteria.
        /// </summary>
        /// <param name="xLocation">The x-component of the starting position.</param>
        /// <param name="yLocation">The y-component of the starting position.</param>
        /// <param name="minimumDistanceTraveled"></param>
        public EuclideanDistanceCriteria(double xLocation, double yLocation, double minimumDistanceTraveled)
        {
            _startXPosition = xLocation;
            _startYPosition = yLocation;
            _minimumDistanceTraveled = minimumDistanceTraveled;
        }

        /// <summary>
        ///     Updates the minimal criteria based on the (harmonic) mean distance of the population from the starting location.
        /// </summary>
        /// <typeparam name="TGenome">Genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void UpdateMinimalCriteria<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>
        {
            // Update the minimal criteria to be the mean of the distance from the starting location over
            // every genome in the population
            double newMC = population.Sum(
                genome =>
                    Math.Sqrt(Math.Pow(genome.EvaluationInfo.BehaviorCharacterization[0] - _startXPosition, 2) +
                              Math.Pow(genome.EvaluationInfo.BehaviorCharacterization[1] - _startYPosition, 2)))/
                           population.Count;

            bool isCriteriaChangeMarginal = Math.Abs(newMC - _minimumDistanceTraveled) < 1;

            // If the distance between the new MC and the previous MC is marginal and the number of updates
            // without a modification to the MC has been surpassed, reset the MC
            // TODO: This MC margin of error might need to be parameterized
            if (isCriteriaChangeMarginal && _numUpdateCyclesWithoutChange >= 3)
            {
                // TODO: Pick a random MC somewhere between 0 and the current MC
                //newMC = _randomNumGenerator.Next(0, (int) _minimumDistanceTraveled);
                newMC = 0;

                // Reset the number of MC update cycles without an MC change to 0
                _numUpdateCyclesWithoutChange = 0;
            }

            // Otherwise, if the change is marginal but the requisite number of update cycles with no change has
            // not been surpassed, then just increment the number of update cycles with no MC change
            else if (isCriteriaChangeMarginal)
            {
                // Increment the number of MC update cycles that resulted in no change to the MC
                _numUpdateCyclesWithoutChange++;
            }
            
            // Assign the minimal criteria
            _minimumDistanceTraveled = newMC;
        }

        /// <summary>
        ///     Evaluates whether the given behavior characterization satisfies the minimal criteria base on the euclidean distance
        ///     that was traversed.
        /// </summary>
        /// <param name="behaviorInfo">The behavior info indicating the ending position of the agent.</param>
        /// <returns>Boolean value indicating whether the given behavior characterization satisfies the minimal criteria.</returns>
        public bool DoesCharacterizationSatisfyMinimalCriteria(BehaviorInfo behaviorInfo)
        {
            // If the behavior dimensionality doesn't match, we can't compare it
            if (behaviorInfo.Behaviors.Length != EuclideanDimensions)
            {
                throw new SharpNeatException(
                    "Cannot evaluate minimal criteria constraints because the behavior characterization is not of the correct dimensionality.");
            }

            // Extract x and y components of location
            double endXPosition = behaviorInfo.Behaviors[0];
            double endYPosition = behaviorInfo.Behaviors[1];

            // Calculate the euclidean distance between the start and end position
            double distance =
                Math.Sqrt(Math.Pow(endXPosition - _startXPosition, 2) + Math.Pow(endYPosition - _startYPosition, 2));

            // Only return true if the distance is at least the minimum required distance
            return Math.Round(distance) >= Math.Round(_minimumDistanceTraveled);
            //return Math.Round(distance, 6) >= Math.Round(_minimumDistanceTraveled, 6);
        }
    }
}