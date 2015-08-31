using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

namespace SharpNeat.Domains.MazeNavigation.FitnessExperiment
{
    internal class MazeNavigationFitnessExperiment : BaseMazeNavigationExperiment
    {
        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object (specific to fitness-based evaluations) ready for running the
        ///     NEAT algorithm/search based on the given genome factory and genome list.  Various sub-parts of the algorithm are
        ///     also constructed and connected up.
        /// </summary>
        /// <param name="genomeFactory">The genome factory from which to generate new genomes</param>
        /// <param name="genomeList">The current genome population</param>
        /// <returns>Constructed evolutionary algorithm</returns>
        public override INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList)
        {
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy =
                new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, ParallelOptions);

            // Create complexity regulation strategy.
            var complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategy, Complexitythreshold);

            // Create the evolution algorithm.
            var ea = new GenerationalNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters, speciationStrategy,
                complexityRegulationStrategy);

            // Create IBlackBox evaluator.
            var mazeNavigationEvaluator = new MazeNavigationFitnessEvaluator(MaxDistanceToTarget, MaxTimesteps, MazeVariant,
                MinSuccessDistance);

            // Create genome decoder.
            var genomeDecoder = CreateGenomeDecoder();

            // Create a genome list evaluator. This packages up the genome decoder with the genome evaluator.
            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeFitnessEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                    ParallelOptions);

            // Initialize the evolution algorithm.
            ea.Initialize(fitnessEvaluator, genomeFactory, genomeList);

            // Finished. Return the evolution algorithm
            return ea;
        }
    }
}