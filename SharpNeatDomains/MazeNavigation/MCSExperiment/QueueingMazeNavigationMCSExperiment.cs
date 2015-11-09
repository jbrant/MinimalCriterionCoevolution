#region

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using ExperimentEntities;
using SharpNeat.Behaviors;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.MinimalCriterias;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    public class QueueingMazeNavigationMCSExperiment : BaseMazeNavigationExperiment
    {
        private int _batchSize;
        private IBehaviorCharacterization _behaviorCharacterization;
        private IDataLogger _evaluationDataLogger;
        private IDataLogger _evolutionDataLogger;
        private AbstractNeatEvolutionAlgorithm<NeatGenome> _initializationEa;

        public override void Initialize(string name, XmlElement xmlConfig)
        {
            base.Initialize(name, xmlConfig);

            // Read in the behavior characterization
            _behaviorCharacterization = ExperimentUtils.ReadBehaviorCharacterization(xmlConfig);

            // Read in number of offspring to produce in a single batch
            _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");

            // Read in log file path/name
            _evolutionDataLogger = ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution);
            _evaluationDataLogger = ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evaluation);

            // Setup initialization algorithm
            _initializationEa =
                initializeInitializationAlgorithm(
                    xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement);
        }

        public override void Initialize(ExperimentDictionary experimentDictionary)
        {
            base.Initialize(experimentDictionary);

            // Ensure the start position and minimum distance constraint are not null
            Debug.Assert(experimentDictionary.Primary_MCS_MinimalCriteriaStartX != null,
                "experimentDictionary.Primary_MCS_MinimalCriteriaStartX != null");
            Debug.Assert(experimentDictionary.Primary_MCS_MinimalCriteriaStartY != null,
                "experimentDictionary.Primary_MCS_MinimalCriteriaStartY != null");
            Debug.Assert(experimentDictionary.Primary_MCS_MinimalCriteriaThreshold != null,
                "experimentDictionary.Primary_MCS_MinimalCriteriaThreshold != null");

            // Read in the behavior characterization
            _behaviorCharacterization =
                new EndPointBehaviorCharacterization(
                    new EuclideanDistanceCriteria((double) experimentDictionary.Primary_MCS_MinimalCriteriaStartX,
                        (double) experimentDictionary.Primary_MCS_MinimalCriteriaStartY,
                        (double) experimentDictionary.Primary_MCS_MinimalCriteriaThreshold));

            // Read in number of offspring to produce in a single batch
            _batchSize = experimentDictionary.Primary_OffspringBatchSize ?? default(int);

            // Read in log file path/name
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
        /// <returns>Constructed evolutionary algorithm</returns>
        public override INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList)
        {
            // Create complexity regulation strategy.
            var complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategy, Complexitythreshold);

            // Create the evolution algorithm.
            AbstractNeatEvolutionAlgorithm<NeatGenome> ea =
                new QueueingNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                    complexityRegulationStrategy, _batchSize, _evolutionDataLogger);

            // Create IBlackBox evaluator.
            var mazeNavigationEvaluator = new MazeNavigationMCSEvaluator(MaxDistanceToTarget, MaxTimesteps,
                MazeVariant,
                MinSuccessDistance, _behaviorCharacterization);

            // Create genome decoder.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

//            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
//                new SerialGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
//                    SearchType.MinimalCriteriaSearch);

            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch, _evaluationDataLogger);

            // Initialize the evolution algorithm.
            ea.Initialize(fitnessEvaluator, genomeFactory, genomeList, null, MaxEvaluations);

            // Finished. Return the evolution algorithm
            return ea;
        }

        /// <summary>
        ///     Constructs and initializes the MCS initialization algorithm (novelty search).
        /// </summary>
        /// <param name="xmlConfig">The XML configuration for the initialization algorithm.</param>
        /// <returns>The constructed initialization algorithm.</returns>
        private AbstractNeatEvolutionAlgorithm<NeatGenome> initializeInitializationAlgorithm(XmlElement xmlConfig)
        {
            double archiveAdditionThreshold;
            double archiveThresholdDecreaseMultiplier;
            double archiveThresholdIncreaseMultiplier;
            int maxGenerationArchiveAddition;
            int maxGenerationsWithoutArchiveAddition;

            // Get complexity constraint parameters
            string complexityRegulationStrategyDefinition = XmlUtils.TryGetValueAsString(xmlConfig,
                "ComplexityRegulationStrategy");
            int? complexityThreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");

            // Read in the behavior characterization
            IBehaviorCharacterization behaviorCharacterization = ExperimentUtils.ReadBehaviorCharacterization(xmlConfig);

            // Read in the novelty archive parameters
            ExperimentUtils.ReadNoveltyParameters(xmlConfig, out archiveAdditionThreshold,
                out archiveThresholdDecreaseMultiplier, out archiveThresholdIncreaseMultiplier,
                out maxGenerationArchiveAddition, out maxGenerationsWithoutArchiveAddition);

            // Read in nearest neighbors for behavior distance calculations
            int nearestNeighbors = XmlUtils.GetValueAsInt(xmlConfig, "NearestNeighbors");

            // Read in steady-state specific parameters
            int batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");
            int populationEvaluationFrequency = XmlUtils.GetValueAsInt(xmlConfig, "PopulationEvaluationFrequency");

            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy =
                new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, ParallelOptions);

            // Create complexity regulation strategy.
            IComplexityRegulationStrategy complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(complexityRegulationStrategyDefinition,
                    complexityThreshold);

            // Create the initialization evolution algorithm.
            return new SteadyStateNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                speciationStrategy, complexityRegulationStrategy, batchSize, populationEvaluationFrequency);
        }

        private List<NeatGenome> initializeViableGenomes(List<NeatGenome> initialGenomes)
        {
            return null;
        }
    }
}