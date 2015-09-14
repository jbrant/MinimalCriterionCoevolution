#region

using System.Collections.Generic;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

#endregion

namespace SharpNeat.Domains.MazeNavigation.NoveltyExperiment
{
    internal class SteadyStateMazeNavigationRandomExperiment : BaseMazeNavigationExperiment
    {
        private int _batchSize;

        /// <summary>
        ///     Path/File to which to write generational data log.
        /// </summary>
        private string _generationalLogFile;
        private int _populationEvaluationFrequency;

        public override void Initialize(string name, XmlElement xmlConfig)
        {
            base.Initialize(name, xmlConfig);
            
            // Read in steady-state specific parameters
            _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");
            _populationEvaluationFrequency = XmlUtils.GetValueAsInt(xmlConfig, "PopulationEvaluationFrequency");

            // Read in log file path/name
            _generationalLogFile = XmlUtils.TryGetValueAsString(xmlConfig, "GenerationalLogFile");
        }

        /// <summary>
        ///     Create and return a SteadyStateNeatEvolutionAlgorithm object (specific to fitness-based evaluations) ready for
        ///     running the
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
            FileDataLogger logger = null;

            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy =
                new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, ParallelOptions);

            // Create complexity regulation strategy.
            var complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategy, Complexitythreshold);

            // Initialize the logger
            if (_generationalLogFile != null)
            {
                logger =
                    new FileDataLogger(_generationalLogFile);
            }

            // Create the evolution algorithm.
            var ea = new SteadyStateNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                speciationStrategy, complexityRegulationStrategy, _batchSize, _populationEvaluationFrequency, logger);

            // Create IBlackBox evaluator.
            var mazeNavigationEvaluator = new MazeNavigationRandomEvaluator(MaxDistanceToTarget, MaxTimesteps,
                MazeVariant, MinSuccessDistance);

            // Create genome decoder.
            var genomeDecoder = CreateGenomeDecoder();

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