#region

using System.Collections.Generic;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Utility
{
    /// <summary>
    ///     Encapsulates utility methods for evaluating the fitness and behavior of genomes.  This essentially captures logic
    ///     that is common between varying implementations of IGenomeEvaluator.
    /// </summary>
    /// <typeparam name="TGenome">The genome type under evaluation.</typeparam>
    /// <typeparam name="TPhenome">The phenotype corresponding to that genome.</typeparam>
    public static class EvaluationUtils<TGenome, TPhenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        /// <summary>
        ///     Evaluates the behavior and resulting "fitness" (behavioral novelty) of a given genome.  The phenome is decoded on
        ///     every run instead of caching it.
        /// </summary>
        /// <param name="genome">The genome under evaluation.</param>
        /// <param name="genomeDecoder">The decoder for decoding the genotype to its phenotypic representation.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="currentGeneration">The generation during which the given genome is being evaluated.</param>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public static void EvaluateBehavior_NonCaching(TGenome genome, IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator, uint currentGeneration,
            IDataLogger evaluationLogger, bool decodeGenomeToXml)
        {
            var phenome = genomeDecoder.Decode(genome);

            if (null == phenome)
            {
                // Non-viable genome.
                genome.EvaluationInfo.SetFitness(0.0);
                genome.EvaluationInfo.AuxFitnessArr = null;
                genome.EvaluationInfo.BehaviorCharacterization = new double[0];
            }
            else
            {
                // Evaluate the behavior, update the genome's behavior characterization, calculate the distance to the domain objective,
                // and indicate if the genome is viable based on whether the minimal criteria was satisfied
                var behaviorInfo = phenomeEvaluator.Evaluate(phenome, currentGeneration,
                    evaluationLogger);
                genome.EvaluationInfo.BehaviorCharacterization = behaviorInfo.Behaviors;
                genome.EvaluationInfo.ObjectiveDistance = behaviorInfo.ObjectiveDistance;
                genome.EvaluationInfo.IsViable = behaviorInfo.DoesBehaviorSatisfyMinimalCriteria;
                genome.EvaluationInfo.NicheId = behaviorInfo.NicheId;
            }
        }

        /// <summary>
        ///     Evaluates the behavior and resulting "fitness" (behavioral novelty) of a given genome.  We first try to retrieve
        ///     the
        ///     phenome from the genomes cache and then decode the genome if it has not yet been cached.
        /// </summary>
        /// <param name="genome">The genome under evaluation.</param>
        /// <param name="genomeDecoder">The decoder for decoding the genotype to its phenotypic representation.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="currentGeneration">The generation during which the given genome is being evaluated.</param>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public static void EvaluateBehavior_Caching(TGenome genome, IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator, uint currentGeneration,
            IDataLogger evaluationLogger, bool decodeGenomeToXml)
        {
            var phenome = (TPhenome) genome.CachedPhenome;

            if (null == phenome)
            {
                // Decode the phenome and store a ref against the genome.
                phenome = genomeDecoder.Decode(genome);
                genome.CachedPhenome = phenome;
            }

            if (null == phenome)
            {
                // Non-viable genome.
                genome.EvaluationInfo.SetFitness(0.0);
                genome.EvaluationInfo.AuxFitnessArr = null;
                genome.EvaluationInfo.BehaviorCharacterization = new double[0];
            }
            else
            {
                // Evaluate the behavior, update the genome's behavior characterization, calculate the distance to the domain objective,
                // and indicate if the genome is viable based on whether the minimal criteria was satisfied
                var behaviorInfo = phenomeEvaluator.Evaluate(phenome, currentGeneration,
                    evaluationLogger);
                genome.EvaluationInfo.BehaviorCharacterization = behaviorInfo.Behaviors;
                genome.EvaluationInfo.ObjectiveDistance = behaviorInfo.ObjectiveDistance;
                genome.EvaluationInfo.IsViable = behaviorInfo.DoesBehaviorSatisfyMinimalCriteria;
                genome.EvaluationInfo.NicheId = behaviorInfo.NicheId;
            }
        }

        /// <summary>
        ///     Evaluates the fitness of a given genome, decoding to its phenotypic representation on every invocation.
        /// </summary>
        /// <param name="genome">The genome to evaluate.</param>
        /// <param name="genomeDecoder">The decoder for decoding the genotype to its phenotypic representation.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="currentGeneration">The generation during which the given genome is being evaluated.</param>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public static void EvaluateFitness_NonCaching(TGenome genome, IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, FitnessInfo> phenomeEvaluator, uint currentGeneration,
            IDataLogger evaluationLogger, bool decodeGenomeToXml)
        {
            var phenome = genomeDecoder.Decode(genome);

            if (null == phenome)
            {
                // Non-viable genome.
                genome.EvaluationInfo.SetFitness(0.0);
                genome.EvaluationInfo.AuxFitnessArr = null;
            }
            else
            {
                // Run evaluation and set fitness/auxiliary fitness
                var fitnessInfo = phenomeEvaluator.Evaluate(phenome, currentGeneration,
                    evaluationLogger);
                genome.EvaluationInfo.SetFitness(fitnessInfo.Fitness);
                genome.EvaluationInfo.AuxFitnessArr = fitnessInfo.AuxFitnessArr;
                genome.EvaluationInfo.NicheId = fitnessInfo.NicheId;
                genome.EvaluationInfo.ObjectiveDistance = fitnessInfo.ObjectiveDistance;
            }
        }

        /// <summary>
        ///     Evaluates the fitness of a given genome, checking first for a cached copy of its phenotype before decoding to its
        ///     phenotypic representation.
        /// </summary>
        /// <param name="genome">The genome to evaluate.</param>
        /// <param name="genomeDecoder">The decoder for decoding the genotype to its phenotypic representation.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="currentGeneration">The generation during which the given genome is being evaluated.</param>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public static void EvaluateFitness_Caching(TGenome genome, IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, FitnessInfo> phenomeEvaluator, uint currentGeneration,
            IDataLogger evaluationLogger, bool decodeGenomeToXml)
        {
            var phenome = (TPhenome) genome.CachedPhenome;

            if (null == phenome)
            {
                // Decode the phenome and store a ref against the genome.
                phenome = genomeDecoder.Decode(genome);
                genome.CachedPhenome = phenome;
            }

            if (null == phenome)
            {
                // Non-viable genome.
                genome.EvaluationInfo.SetFitness(0.0);
                genome.EvaluationInfo.AuxFitnessArr = null;
            }
            else
            {
                var fitnessInfo = phenomeEvaluator.Evaluate(phenome, currentGeneration,
                    evaluationLogger);
                genome.EvaluationInfo.SetFitness(fitnessInfo.Fitness);
                genome.EvaluationInfo.AuxFitnessArr = fitnessInfo.AuxFitnessArr;
                genome.EvaluationInfo.NicheId = fitnessInfo.NicheId;
                genome.EvaluationInfo.ObjectiveDistance = fitnessInfo.ObjectiveDistance;
            }
        }

        /// <summary>
        ///     Evaluates the fitness of the given genome as the its behavioral novelty as compared to the rest of the population.
        /// </summary>
        /// <param name="genome">The genome to evaluate.</param>
        /// <param name="genomeList">The population against which the genome is being evaluated.</param>
        /// <param name="nearestNeighbors">The number of nearest neighbors in behavior space against which to evaluate.</param>
        /// <param name="noveltyArchive">The archive of novel individuals.</param>
        /// <param name="applyViabilityConstraint">
        ///     Indicates rather viability should be taken into account when assigned fitness
        ///     values (i.e. if the genome is not viable, it is assigned a fitness of zero).
        /// </param>
        public static void EvaluateFitness(TGenome genome, IList<TGenome> genomeList, int nearestNeighbors,
            INoveltyArchive<TGenome> noveltyArchive, bool applyViabilityConstraint)
        {
            FitnessInfo fitnessInfo;

            // If the genome is not viable, set the fitness (i.e. behavioral novelty) to zero
            if (applyViabilityConstraint && genome.EvaluationInfo.IsViable == false)
            {
                fitnessInfo = FitnessInfo.Zero;
            }
            else
            {
                // Compare the current genome's behavior to its k-nearest neighbors in behavior space
                var fitness =
                    NoveltyUtils<TGenome>.CalculateBehavioralDistance(genome.EvaluationInfo.BehaviorCharacterization,
                        genomeList, nearestNeighbors, noveltyArchive);
                fitnessInfo = new FitnessInfo(fitness, fitness);
            }

            // Update the fitness as the behavioral novelty
            genome.EvaluationInfo.SetFitness(fitnessInfo.Fitness);
            genome.EvaluationInfo.AuxFitnessArr = fitnessInfo.AuxFitnessArr;
            genome.EvaluationInfo.NicheId = fitnessInfo.NicheId;
        }

        /// <summary>
        ///     Evaluates the fitness of the given genome as either the objective distance to the domain-specific target (depending
        ///     on the whether the given flag is set) or to a random value, so long as it satisfies the minimal criteria.
        /// </summary>
        /// <param name="genome">The genome to evaluate.</param>
        /// <param name="assignObjectiveDistanceAsFitness">
        ///     Boolean indicator specifying whether to assign the distance to the
        ///     objective as a proxy for fitness.
        /// </param>
        public static void EvaluateFitness(TGenome genome, bool assignObjectiveDistanceAsFitness)
        {
            FitnessInfo fitnessInfo;

            // If the flag is set, assign the calculated objective distance as the fitness.  
            // This is mostly for display purposes because the methods that use this (i.e. MCS) 
            // are not objectively driven.
            if (assignObjectiveDistanceAsFitness)
            {
                fitnessInfo = new FitnessInfo(genome.EvaluationInfo.ObjectiveDistance,
                    genome.EvaluationInfo.ObjectiveDistance);
            }
            // Otherwise, we're going to assign a random fitness score (since there is no other heuristic)
            else
            {
                // Create new random number generator without a seed
                var rng = new FastRandom();

                // If the genome is not viable, set the fitness (i.e. behavioral novelty) to zero
                if (genome.EvaluationInfo.IsViable == false)
                {
                    fitnessInfo = FitnessInfo.Zero;
                }
                else
                {
                    // Generate new random fitness value
                    var randomFitness = rng.NextDouble();

                    // Set random value as fitness
                    fitnessInfo = new FitnessInfo(randomFitness, randomFitness);
                }
            }

            // Update the genome fitness as the randomly generated double
            genome.EvaluationInfo.SetFitness(fitnessInfo.Fitness);
            genome.EvaluationInfo.AuxFitnessArr = fitnessInfo.AuxFitnessArr;
        }

        /// <summary>
        ///     Decodes all of the genomes in the given list to their respective phenotypes.
        /// </summary>
        /// <param name="genomeList">The list of genomes to decode.</param>
        /// <param name="genomeDecoder">The decoder to use.</param>
        /// <returns>The decoded list of phenotypes.</returns>
        public static IEnumerable<TPhenome> DecodeGenomes(IList<TGenome> genomeList,
            IGenomeDecoder<TGenome, TPhenome> genomeDecoder)
        {
            IList<TPhenome> decodedPhenomes = new List<TPhenome>(genomeList.Count);

            foreach (var genome in genomeList)
            {
                // Try to get the cached (already decoded) phenome if it exists
                var curPhenome = (TPhenome) genome.CachedPhenome;

                // If there's not a decoded phenome cached, we have to decode it
                if (null == curPhenome)
                {
                    // Decode the phenome and store a ref against the genome.
                    curPhenome = genomeDecoder.Decode(genome);
                    genome.CachedPhenome = curPhenome;
                }

                // Add the phenome to the list of decoded phenomes
                decodedPhenomes.Add(curPhenome);
            }

            return decodedPhenomes;
        }
    }
}