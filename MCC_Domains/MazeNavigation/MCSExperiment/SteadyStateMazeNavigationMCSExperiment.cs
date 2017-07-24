#region

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using ExperimentEntities;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    public class SteadyStateMazeNavigationMCSExperiment : BaseMazeNavigationExperiment
    {
        private int _batchSize;
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private IDataLogger _evaluationDataLogger;
        private IDataLogger _evolutionDataLogger;

        /// <summary>
        ///     Path/File to which to write generational data log.
        /// </summary>
        private string _generationalLogFile;

        private string _mcsSelectionMethod;
        private int _populationEvaluationFrequency;

        public override void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger,
            IDataLogger evaluationDataLogger)
        {
            base.Initialize(name, xmlConfig, evolutionDataLogger, evaluationDataLogger);

            // Read in the behavior characterization
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(xmlConfig,
                "BehaviorConfig");

            // Read in steady-state specific parameters
            _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");
            _populationEvaluationFrequency = XmlUtils.GetValueAsInt(xmlConfig, "PopulationEvaluationFrequency");

            // Read in MCS selection method
            _mcsSelectionMethod = XmlUtils.TryGetValueAsString(xmlConfig, "McsSelectionMethod");

            // Read in log file path/name
            _evolutionDataLogger = evolutionDataLogger ??
                                   ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution);
            _evaluationDataLogger = evaluationDataLogger ??
                                    ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evaluation);
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
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(
                experimentDictionary, true);

            // Read in steady-state specific parameters
            _batchSize = experimentDictionary.Primary_OffspringBatchSize ?? default(int);
            _populationEvaluationFrequency = experimentDictionary.Primary_PopulationEvaluationFrequency ?? default(int);

            // Read in MCS selection method
            _mcsSelectionMethod = experimentDictionary.Primary_SelectionAlgorithmName;

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
            AbstractNeatEvolutionAlgorithm<NeatGenome> ea =
                new SteadyStateNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                    speciationStrategy, complexityRegulationStrategy, _batchSize, _populationEvaluationFrequency,
                    RunPhase.Primary, _evolutionDataLogger);

            // Create IBlackBox evaluator.
            MazeNavigationMCSEvaluator mazeNavigationEvaluator = new MazeNavigationMCSEvaluator(MaxDistanceToTarget,
                MazeVariant, MinSuccessDistance, _behaviorCharacterizationFactory);

            // Create genome decoder.
            var genomeDecoder = CreateGenomeDecoder();

//            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
//                new SerialGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
//                    SearchType.MinimalCriteriaSearch);

            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                    SelectionType.SteadyState, SearchType.MinimalCriteriaSearch, _evaluationDataLogger);

            // Initialize the evolution algorithm.
            ea.Initialize(fitnessEvaluator, genomeFactory, genomeList, null, MaxEvaluations);

            // Finished. Return the evolution algorithm
            return ea;
        }
    }
}