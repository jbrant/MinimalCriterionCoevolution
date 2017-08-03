#region

using System.Collections.Generic;
using System.Xml;
using ExperimentEntities;
using MCC_Domains.Utils;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.NoveltyArchives;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace MCC_Domains.MazeNavigation.NoveltyExperiment
{
    public class SteadyStateMazeNavigationNoveltyExperiment : BaseMazeNavigationExperiment
    {
        private double _archiveAdditionThreshold;
        private double _archiveThresholdDecreaseMultiplier;
        private double _archiveThresholdIncreaseMultiplier;
        private int _batchSize;
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private IDataLogger _evaluationDataLogger;
        private IDataLogger _evolutionDataLogger;
        private int _maxGenerationArchiveAddition;
        private int _maxGenerationsWithoutArchiveAddition;
        private int _nearestNeighbors;
        private int _populationEvaluationFrequency;

        public override void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger,
            IDataLogger evaluationDataLogger)
        {
            base.Initialize(name, xmlConfig, evolutionDataLogger, evaluationDataLogger);

            // Read in the behavior characterization
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(xmlConfig,
                "BehaviorConfig");

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
            _evolutionDataLogger = evolutionDataLogger ??
                                   ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution);
            _evaluationDataLogger = evaluationDataLogger ??
                                    ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evaluation);
        }

        public override void Initialize(ExperimentDictionary experimentDictionary)
        {
            base.Initialize(experimentDictionary);

            // Read in behavior characterization
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(
                experimentDictionary, true);

            // Read in novelty archive parameters
            _archiveAdditionThreshold = experimentDictionary.Primary_NoveltySearch_ArchiveAdditionThreshold ??
                                        default(int);
            _archiveThresholdDecreaseMultiplier =
                experimentDictionary.Primary_NoveltySearch_ArchiveThresholdDecreaseMultiplier ?? default(double);
            _archiveThresholdIncreaseMultiplier =
                experimentDictionary.Primary_NoveltySearch_ArchiveThresholdIncreaseMultiplier ?? default(double);
            _maxGenerationArchiveAddition =
                experimentDictionary.Primary_NoveltySearch_MaxGenerationsWithArchiveAddition ?? default(int);
            _maxGenerationsWithoutArchiveAddition =
                experimentDictionary.Primary_NoveltySearch_MaxGenerationsWithoutArchiveAddition ?? default(int);

            // Read in nearest neighbors for behavior distance calculations
            _nearestNeighbors = experimentDictionary.Primary_NoveltySearch_NearestNeighbors ?? default(int);

            // Read in steady-state specific parameters
            _batchSize = experimentDictionary.Primary_OffspringBatchSize ?? default(int);
            _populationEvaluationFrequency = experimentDictionary.Primary_PopulationEvaluationFrequency ?? default(int);

            // Setup entity loggers
            _evolutionDataLogger = new NoveltyExperimentEvaluationEntityDataLogger(experimentDictionary.ExperimentName);
            _evaluationDataLogger =
                new NoveltyExperimentOrganismStateEntityDataLogger(experimentDictionary.ExperimentName);
        }

        /// <summary>
        ///     Create and return a SteadyStateNeatEvolutionAlgorithm object (specific to fitness-based evaluations) ready for
        ///     running the
        ///     NEAT algorithm/search based on the given genome factory and genome list.  Various sub-parts of the algorithm are
        ///     also constructed and connected up.
        /// </summary>
        /// <param name="genomeFactory">The genome factory from which to generate new genomes</param>
        /// <param name="genomeList">The current genome population</param>
        /// <param name="startingEvaluations">The number of evaluations that have been executed prior to the current run.</param>
        /// <returns>Constructed evolutionary algorithm</returns>
        public override INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList, ulong startingEvaluations)
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
                RunPhase.Primary, _evolutionDataLogger);

            // Create IBlackBox evaluator.
            var mazeNavigationEvaluator = new MazeNavigationNoveltyEvaluator(MaxDistanceToTarget, MazeVariant,
                MinSuccessDistance, _behaviorCharacterizationFactory);

            // Create genome decoder.
            var genomeDecoder = CreateGenomeDecoder();

            // Create a novelty archive.
            AbstractNoveltyArchive<NeatGenome> archive =
                new BehavioralNoveltyArchive<NeatGenome>(_archiveAdditionThreshold,
                    _archiveThresholdDecreaseMultiplier, _archiveThresholdIncreaseMultiplier,
                    _maxGenerationArchiveAddition, _maxGenerationsWithoutArchiveAddition);

//            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
//                new SerialGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
//                    SearchType.NoveltySearch,
//                    _nearestNeighbors, archive, _evaluationDataLogger);

            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                    SelectionType.SteadyState, SearchType.NoveltySearch,
                    _nearestNeighbors, archive, _evaluationDataLogger);

            // Initialize the evolution algorithm.
            ea.Initialize(fitnessEvaluator, genomeFactory, genomeList, null, MaxEvaluations, archive);

            // Finished. Return the evolution algorithm
            return ea;
        }
    }
}