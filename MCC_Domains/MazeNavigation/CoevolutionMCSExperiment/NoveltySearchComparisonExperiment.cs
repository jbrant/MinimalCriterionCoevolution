#region

using System.Collections.Generic;
using ExperimentEntities;
using MCC_Domains.Utils;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.NoveltyArchives;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;
using SharpNeat.SpeciationStrategies;

#endregion

namespace MCC_Domains.MazeNavigation.CoevolutionMCSExperiment
{
    public class NoveltySearchComparisonExperiment : BaseMazeNavigationExperiment
    {
        private readonly MazeStructure _evaluationMaze;
        private double _archiveAdditionThreshold;
        private double _archiveThresholdDecreaseMultiplier;
        private double _archiveThresholdIncreaseMultiplier;
        private int _batchSize;
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private int _maxGenerationArchiveAddition;
        private int _maxGenerationsWithoutArchiveAddition;
        private int _nearestNeighbors;
        private int _populationEvaluationFrequency;

        /// <summary>
        ///     Novelty search comparison experiment constructor.
        /// </summary>
        /// <param name="evaluationMaze">The maze on which novelty search comparison evaluations will be performed.</param>
        public NoveltySearchComparisonExperiment(MazeStructure evaluationMaze)
        {
            _evaluationMaze = evaluationMaze;
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
        }

        public override INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList,
            ulong startingEvaluations)
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
                speciationStrategy, complexityRegulationStrategy, _batchSize, _populationEvaluationFrequency);

            // Create IBlackBox evaluator.
            MazeNavigatorNoveltySearchInitializationEvaluator mazeNavigatorEvaluator =
                new MazeNavigatorNoveltySearchInitializationEvaluator(MinSuccessDistance, MaxDistanceToTarget,
                    _evaluationMaze, _behaviorCharacterizationFactory, startingEvaluations);

            // Create a novelty archive
            AbstractNoveltyArchive<NeatGenome> archive =
                new BehavioralNoveltyArchive<NeatGenome>(_archiveAdditionThreshold,
                    _archiveThresholdDecreaseMultiplier, _archiveThresholdIncreaseMultiplier,
                    _maxGenerationArchiveAddition, _maxGenerationsWithoutArchiveAddition);

            // Create the genome evaluator
            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(CreateGenomeDecoder(), mazeNavigatorEvaluator,
                    SelectionType.SteadyState, SearchType.NoveltySearch, _nearestNeighbors, archive);

            // Initialize the evolution algorithm.
            ea.Initialize(fitnessEvaluator, genomeFactory, genomeList, null, MaxEvaluations, archive);

            // Finished. Return the evolution algorithm
            return ea;
        }
    }
}