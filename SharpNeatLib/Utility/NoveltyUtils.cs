using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;

namespace SharpNeat.Utility
{
    public static class NoveltyUtils<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        public static double CalculateNovelty(double[] genomeBehaviors, IList<TGenome> population, int nearestNeighbors)
        {
            double totalDistance = 0;
            var distances = new List<double>();

            foreach (var genome in population)
            {
                double distance = BehaviorInfo.CalculateDistance(genomeBehaviors,
                    genome.EvaluationInfo.BehaviorCharacterization);
   
                distances.Add(BehaviorInfo.CalculateDistance(genomeBehaviors, genome.EvaluationInfo.BehaviorCharacterization));
            }

            distances.Sort();

            if (nearestNeighbors > distances.Count)
                nearestNeighbors = distances.Count;

            for (int neighbor = 0; neighbor < nearestNeighbors; neighbor++)
            {
                totalDistance += distances[neighbor];
            }

            return totalDistance;
        }
    }
}