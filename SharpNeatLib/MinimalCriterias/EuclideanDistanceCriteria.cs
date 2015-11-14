#region

using System;
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
        ///     The minimum distance that the candidate agent had to travel to be considered viable.
        /// </summary>
        private readonly double _minimumDistanceTraveled;

        /// <summary>
        ///     The x-component of the starting position.
        /// </summary>
        private readonly double _startXPosition;

        /// <summary>
        ///     The y-component of the starting position;
        /// </summary>
        private readonly double _startYPosition;

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
            return distance >= _minimumDistanceTraveled;
        }
    }
}