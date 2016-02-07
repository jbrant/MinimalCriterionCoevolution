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
        ///     The maximum allowed number of update cycles without a change to the minimal criteria.
        /// </summary>
        private readonly double? _maxUpdateCyclesWithoutChange;

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
        private double _minimumDistanceFromOrigin;

        /// <summary>
        ///     The number of times the minimal criteria has been updated without a significant change in that criteria.
        /// </summary>
        private int _numUpdateCyclesWithoutChange;

        private bool _reverseMinimalCriteria;

        /// <summary>
        ///     Constructor for the euclidean distance minimal criteria.
        /// </summary>
        /// <param name="xLocation">The x-component of the starting position.</param>
        /// <param name="yLocation">The y-component of the starting position.</param>
        /// <param name="minimumDistanceFromOrigin"></param>
        public EuclideanDistanceCriteria(double xLocation, double yLocation, double minimumDistanceFromOrigin)
        {
            _startXPosition = xLocation;
            _startYPosition = yLocation;
            _minimumDistanceFromOrigin = minimumDistanceFromOrigin;
        }

        /// <summary>
        ///     Constructor for the euclidean distance minimal criteria.
        /// </summary>
        /// <param name="xLocation">The x-component of the starting position.</param>
        /// <param name="yLocation">The y-component of the starting position.</param>
        /// <param name="minimumDistanceFromOrigin"></param>
        /// <param name="maxUpdateCyclesWithoutChange">
        ///     The maximum number of calls to update the minimal criteria that don't result
        ///     in a significant change allowed (i.e. before the minimal criteria is forcibly modified).
        /// </param>
        public EuclideanDistanceCriteria(double xLocation, double yLocation, double minimumDistanceFromOrigin,
            double? maxUpdateCyclesWithoutChange) : this(xLocation, yLocation, minimumDistanceFromOrigin)
        {
            _maxUpdateCyclesWithoutChange = maxUpdateCyclesWithoutChange;
        }

        /// <summary>
        ///     Updates the minimal criteria based on the (harmonic) mean distance of the population from the starting location.
        /// </summary>
        /// <typeparam name="TGenome">Genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void UpdateMinimalCriteria<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>
        {
            _reverseMinimalCriteria = false;

            // Update the minimal criteria to be the mean of the distance from the starting location over
            // every genome in the population
            double newMC = population.Sum(
                genome =>
                    Math.Sqrt(Math.Pow(genome.EvaluationInfo.BehaviorCharacterization[0] - _startXPosition, 2) +
                              Math.Pow(genome.EvaluationInfo.BehaviorCharacterization[1] - _startYPosition, 2)))/
                           population.Count;

            bool isCriteriaChangeMarginal = Math.Abs(newMC - _minimumDistanceFromOrigin) < 1;

            // If the distance between the new MC and the previous MC is marginal and the number of updates
            // without a modification to the MC has been surpassed, reset the MC
            if (_maxUpdateCyclesWithoutChange != null && isCriteriaChangeMarginal &&
                _numUpdateCyclesWithoutChange >= _maxUpdateCyclesWithoutChange)
            {
                // Pick a random MC somewhere between 0 and the current MC
                newMC = _randomNumGenerator.Next(0, (int) _minimumDistanceFromOrigin);
                /*foreach (TGenome genome in population)
                {
                    double distanceFromStart =
                        Math.Sqrt(Math.Pow(genome.EvaluationInfo.BehaviorCharacterization[0] - _startXPosition, 2) +
                                  Math.Pow(genome.EvaluationInfo.BehaviorCharacterization[1] - _startYPosition, 2));

                    if (distanceFromStart < newMC)
                    {
                        newMC = distanceFromStart;
                    }
                }*/

                // Increment MC by 1 so something matches it
                //newMC++;

                // Reset the number of MC update cycles without an MC change to 0
                _numUpdateCyclesWithoutChange = 0;

                //_reverseMinimalCriteria = true;
            }

            // Otherwise, if the change is marginal but the requisite number of update cycles with no change has
            // not been surpassed, then just increment the number of update cycles with no MC change
            else if (_maxUpdateCyclesWithoutChange != null && isCriteriaChangeMarginal)
            {
                // Increment the number of MC update cycles that resulted in no change to the MC
                _numUpdateCyclesWithoutChange++;
            }

            // Assign the minimal criteria
            _minimumDistanceFromOrigin = newMC;
        }

        /// <summary>
        ///     Evaluates whether the given behavior characterization satisfies the minimal criteria base on the euclidean distance
        ///     that was traversed.
        /// </summary>
        /// <param name="behaviorInfo">The behavior info indicating the ending position of the agent.</param>
        /// <param name="allowCriteriaReversal">
        ///     Permits reversing the minimal criteria (such that only those who do *not* meet the
        ///     minimal criteria are valid).
        /// </param>
        /// <returns>Boolean value indicating whether the given behavior characterization satisfies the minimal criteria.</returns>
        public bool DoesCharacterizationSatisfyMinimalCriteria(BehaviorInfo behaviorInfo, bool allowCriteriaReversal)
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

            bool isMinimalCriteriaSatisifed = Math.Round(distance) >= Math.Round(_minimumDistanceFromOrigin);

            // Only return true if the distance is at least the minimum required distance
            return (allowCriteriaReversal && _reverseMinimalCriteria) ? !isMinimalCriteriaSatisifed : isMinimalCriteriaSatisifed;
        }

        /// <summary>
        ///     Returns the scalar value of the minimal criteria.
        /// </summary>
        /// <returns>The scalar value of the minimal criteria.</returns>
        public dynamic GetMinimalCriteriaValue()
        {
            return _minimumDistanceFromOrigin;
        }
    }
}