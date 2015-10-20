#region

using System.Collections.Generic;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EliteArchives;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.NoveltyArchives;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

#endregion

namespace SharpNeat.Domains.MazeNavigation.NoveltyExperiment
{
    public class SteadyStateMazeNavigationNoveltyExperiment : BaseMazeNavigationExperiment
    {
        private double _archiveAdditionThreshold;
        private double _archiveThresholdDecreaseMultiplier;
        private double _archiveThresholdIncreaseMultiplier;
        private int _batchSize;
        private IBehaviorCharacterization _behaviorCharacterization;
        private IDataLogger _evaluationDataLogger;
        // Logging configurations
        private IDataLogger _evolutionDataLogger;
        private int _maxGenerationArchiveAddition;
        private int _maxGenerationsWithoutArchiveAddition;
        private int _nearestNeighbors;
        private int _populationEvaluationFrequency;

        public override void Initialize(string name, XmlElement xmlConfig)
        {
            base.Initialize(name, xmlConfig);

            // Read in the behavior characterization
            _behaviorCharacterization = ExperimentUtils.ReadBehaviorCharacterization(xmlConfig);

            // Read in the novelty archive parameters
            ExperimentUtils.ReadNoveltyParameters(xmlConfig, out _archiveAdditionThreshold,
                out _archiveThresholdDecreaseMultiplier, out _archiveThresholdIncreaseMultiplier,
                out _maxGenerationArchiveAddition, out _maxGenerationsWithoutArchiveAddition);

            // Read in nearest neighbors for behavior distance calculations
            _nearestNeighbors = XmlUtils.GetValueAsInt(xmlConfig, "NearestNeighbors");

            // Read in steady-state specific parameters
            _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");
            _populationEvaluationFrequency = XmlUtils.GetValueAsInt(xmlConfig, "PopulationEvaluationFrequency");

            // Read in log file path/name
            _evolutionDataLogger = ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution);
            _evaluationDataLogger = ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evaluation);
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
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy =
                new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, ParallelOptions);

            // Create complexity regulation strategy.
            var complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategy, Complexitythreshold);

            // Create the evolution algorithm.
            var ea = new SteadyStateNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                speciationStrategy, complexityRegulationStrategy, _batchSize, _populationEvaluationFrequency,
                _evolutionDataLogger);

            // Create IBlackBox evaluator.
            var mazeNavigationEvaluator = new MazeNavigationNoveltyEvaluator(MaxDistanceToTarget, MaxTimesteps,
                MazeVariant,
                MinSuccessDistance, _behaviorCharacterization);

            // Create genome decoder.
            var genomeDecoder = CreateGenomeDecoder();

            // Create a novelty archive.
            AbstractNoveltyArchive<NeatGenome> archive =
                new BehavioralNoveltyArchive<NeatGenome>(_archiveAdditionThreshold,
                    _archiveThresholdDecreaseMultiplier, _archiveThresholdIncreaseMultiplier,
                    _maxGenerationArchiveAddition, _maxGenerationsWithoutArchiveAddition);

//            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
//                new SerialGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
//                    EvaluationType.NoveltySearch,
//                    _nearestNeighbors, archive, _evaluationDataLogger);

            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                    EvaluationType.NoveltySearch,
                    _nearestNeighbors, archive, _evaluationDataLogger);

            // Initialize the evolution algorithm.
            ea.Initialize(fitnessEvaluator, genomeFactory, genomeList, archive);

            // Finished. Return the evolution algorithm
            return ea;
        }
    }
}