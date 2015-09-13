using System.Collections.Generic;
using SharpNeat.Core;

namespace SharpNeat.Utility
{
    public static class BehaviorUtils<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        public static double CalculateBehavioralDistance(double[] genomeBehaviors, IList<TGenome> population,
            int nearestNeighbors, INoveltyArchive<TGenome> archive = null)
        {
            double totalDistance = 0;
            var distances = new List<double>();

            foreach (var genome in population)
            {
                var distance = BehaviorInfo.CalculateDistance(genomeBehaviors,
                    genome.EvaluationInfo.BehaviorCharacterization);

                distances.Add(distance);
            }
            
            if (archive != null)
            {
                foreach (var genome in archive.Archive)
                {
                    var distance = BehaviorInfo.CalculateDistance(genomeBehaviors,
                        genome.EvaluationInfo.BehaviorCharacterization);

                    distances.Add(BehaviorInfo.CalculateDistance(genomeBehaviors,
                        genome.EvaluationInfo.BehaviorCharacterization));
                }
            }

            distances.Sort();

            if (nearestNeighbors > distances.Count)
                nearestNeighbors = distances.Count;

            for (var neighbor = 0; neighbor < nearestNeighbors; neighbor++)
            {
                totalDistance += distances[neighbor];
            }

            return totalDistance / nearestNeighbors;
        }
    }
}