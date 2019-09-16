#region

using System.Collections.Generic;
using System.Linq;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Utility
{
    /// <summary>
    ///     Utility class that implements calculations of novelty measures (such as behavioral distance).
    /// </summary>
    /// <typeparam name="TGenome"></typeparam>
    public static class NoveltyUtils<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        ///     Calculates the distance in behavior space from the given genome's behaviors against every member of the given
        ///     population and, if applicable, the individuals in the novelty archive.  It's novelty score is then the sum of the
        ///     calculated distances to the k-nearest neighbors divided by the specified number of nearest neighbors.
        /// </summary>
        /// <param name="trialData">Behavioral information from simulation trial(s).</param>
        /// <param name="population">The current population of genomes against which to evaluate behavioral distance.</param>
        /// <param name="nearestNeighbors">The number of nearest neighbors to consider for the behavioral novelty calculation.</param>
        /// <param name="archive">The cross-generational archive of novel genomes against which to also compare (optional).</param>
        /// <returns></returns>
        public static double CalculateBehavioralDistance(IList<TrialInfo> trialData, IList<TGenome> population,
            int nearestNeighbors, INoveltyArchive<TGenome> archive = null)
        {
            double totalDistance = 0;
            
            // Iterate through each genome in the population, calculating the distance from it 
            // to the genome under evaluation and recording said distance
            var distances = population.Select(genome =>
                    BehaviorInfo.CalculateDistance(trialData, genome.EvaluationInfo.TrialData))
                .ToList();

            // If a novelty archive is being maintained, also calculate the distance between 
            // the genome under evaluation and every genome in the novelty archive
            if (archive != null)
            {
                distances.AddRange(
                    archive.Archive.Select(
                        genome =>
                            BehaviorInfo.CalculateDistance(trialData,
                                genome.EvaluationInfo.TrialData)));
            }

            // Sort the distance in ascending order, bringing the genomes that are closer 
            // in behavior space to the front of the list
            distances.Sort();

            // If the default number of nearest neighbors is more than the number of individuals whose distance 
            // to compare, then cap the number of nearest numbers to that number of genomes
            if (nearestNeighbors > distances.Count)
                nearestNeighbors = distances.Count;

            // Iterate through each of the nearest neighbors, summing the distance to each
            for (var neighbor = 0; neighbor < nearestNeighbors; neighbor++)
            {
                totalDistance += distances[neighbor];
            }

            // Return the novelty score as the sum of the distances to each of the k-nearest neighbors 
            // divided by the number of nearest neighbors
            return totalDistance / nearestNeighbors;
        }
    }
}