#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.MinimalCriterias
{
    /// <summary>
    ///     Defines the calculations for determining whether a given behavior satisfies the criteria based on its distance from
    ///     the moving population centroid.
    /// </summary>
    public class PopulationCentroidEuclideanDistanceCriteria : IMinimalCriteria
    {
        /// <summary>
        ///     Hard-coded number of dimensions in euclidean space.
        /// </summary>
        private const int EuclideanDimensions = 2;

        /// <summary>
        ///     The minimum distance from the population centroid (in any direction) that the candidate agent had to travel to be
        ///     considered viable.
        /// </summary>
        private double _minimumDistanceFromCentroid;

        /// <summary>
        ///     The center of the current population (since recalculated).
        /// </summary>
        private Point2DDouble _populationCentroid;

        /// <summary>
        ///     Constructor for the population centroid euclidean distance minimal criteria.
        /// </summary>
        public PopulationCentroidEuclideanDistanceCriteria(double initialDistanceFromCentroid)
        {
            _populationCentroid = new Point2DDouble(0, 0);
            _minimumDistanceFromCentroid = initialDistanceFromCentroid;
        }

        /// <summary>
        ///     Updates the minimal criteria based on the (harmonic) mean distance of the population from the starting location.
        /// </summary>
        /// <typeparam name="TGenome">Genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void UpdateMinimalCriteria<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>
        {
            // Recalculate the population centroid
            _populationCentroid =
                new Point2DDouble(
                    population.Sum(genomeXLocation => genomeXLocation.EvaluationInfo.BehaviorCharacterization[0])/
                    population.Count,
                    population.Sum(genomeYLocation => genomeYLocation.EvaluationInfo.BehaviorCharacterization[1])/
                    population.Count);

            // Update the minimal criteria to be equivalent to the mean distance from the newly calculated centroid
            _minimumDistanceFromCentroid = population.Sum(
                genome =>
                    Math.Sqrt(Math.Pow(genome.EvaluationInfo.BehaviorCharacterization[0] - _populationCentroid.X, 2) +
                              Math.Pow(genome.EvaluationInfo.BehaviorCharacterization[1] - _populationCentroid.Y, 2)))/
                                           population.Count;
        }

        /// <summary>
        ///     Evaluates whether the given behavior characterization satisfies the minimal criteria base on the euclidean distance
        ///     from the population centroid.
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
                Math.Sqrt(Math.Pow(endXPosition - _populationCentroid.X, 2) +
                          Math.Pow(endYPosition - _populationCentroid.Y, 2));

            // Minimal criteria is satisfied only if the distance from the centroid is greater than 
            // or equal to the minimum distance
            return Math.Round(distance) >= Math.Round(_minimumDistanceFromCentroid);
        }

        /// <summary>
        ///     Returns PopulationCentroidEuclideanDistanceCriteria loggable elements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable elements for PopulationCentroidEuclideanDistanceCriteria.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            List<LoggableElement> loggableElements = new List<LoggableElement>();

            // Log minimal criteria threshold if enabled
            if (logFieldEnableMap != null &&
                logFieldEnableMap.ContainsKey(EvolutionFieldElements.MinimalCriteriaThreshold) &&
                logFieldEnableMap[EvolutionFieldElements.MinimalCriteriaThreshold])
            {
                loggableElements.Add(new LoggableElement(EvolutionFieldElements.MinimalCriteriaThreshold,
                    _minimumDistanceFromCentroid));
            }

            // Log the population centroid if both X/Y coordinates are enabled
            if (logFieldEnableMap != null &&
                logFieldEnableMap.ContainsKey(EvolutionFieldElements.MinimalCriteriaPointX) &&
                logFieldEnableMap.ContainsKey(EvolutionFieldElements.MinimalCriteriaPointY) &&
                logFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointX] &&
                logFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointY])
            {
                loggableElements.Add(new LoggableElement(EvolutionFieldElements.MinimalCriteriaPointX,
                    _populationCentroid.X));
                loggableElements.Add(new LoggableElement(EvolutionFieldElements.MinimalCriteriaPointY,
                    _populationCentroid.Y));
            }

            return loggableElements;
        }
    }
}