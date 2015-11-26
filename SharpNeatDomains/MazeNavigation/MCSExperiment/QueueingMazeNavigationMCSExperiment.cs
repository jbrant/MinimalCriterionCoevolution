#region

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ExperimentEntities;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.EliteArchives;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.NoveltyArchives;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    public class QueueingMazeNavigationMCSExperiment : BaseMazeNavigationExperiment
    {
        private int _batchSize;
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private int _bridgingMagnitude;
        private IDataLogger _evaluationDataLogger;
        private IDataLogger _evolutionDataLogger;
        private InitializationAlgorithm _initializationAlgorithm;

        public override void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger,
            IDataLogger evaluationDataLogger)
        {
            base.Initialize(name, xmlConfig, evolutionDataLogger, evaluationDataLogger);

            // Read in the behavior characterization
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(xmlConfig,
                "BehaviorConfig");

            // Read in number of offspring to produce in a single batch
            _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");

            // Read in the bridging magnitude
            _bridgingMagnitude = XmlUtils.GetValueAsInt(xmlConfig, "BridgingMagnitude");

            // Read in log file path/name
            _evolutionDataLogger = evolutionDataLogger ??
                                   ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution);
            _evaluationDataLogger = evaluationDataLogger ??
                                    ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evaluation);

            // Initialize the initialization algorithm
            _initializationAlgorithm = new InitializationAlgorithm(MaxEvaluations, _evolutionDataLogger,
                _evaluationDataLogger,
                SerializeGenomeToXml);

            // Setup initialization algorithm
            _initializationAlgorithm.SetAlgorithmParameters(
                xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement);

            // Pass in maze experiment specific parameters
            _initializationAlgorithm.SetEnvironmentParameters(MaxDistanceToTarget, MaxTimesteps, MazeVariant,
                MinSuccessDistance);
        }

        public override void Initialize(ExperimentDictionary experimentDictionary)
        {
            base.Initialize(experimentDictionary);

            // Read in the behavior characterization
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(
                experimentDictionary, true);

            // Read in number of offspring to produce in a single batch
            _batchSize = experimentDictionary.Primary_OffspringBatchSize ?? default(int);

            // Read in log file path/name
            _evolutionDataLogger = new McsExperimentEvaluationEntityDataLogger(experimentDictionary.ExperimentName);
            _evaluationDataLogger =
                new McsExperimentOrganismStateEntityDataLogger(experimentDictionary.ExperimentName);

            // Initialize the initialization algorithm
            _initializationAlgorithm = new InitializationAlgorithm(MaxEvaluations, _evolutionDataLogger,
                _evaluationDataLogger,
                SerializeGenomeToXml);

            // Setup initialization algorithm
            _initializationAlgorithm.SetAlgorithmParameters(experimentDictionary);

            // Pass in maze experiment specific parameters
            _initializationAlgorithm.SetEnvironmentParameters(MaxDistanceToTarget, MaxTimesteps, MazeVariant,
                MinSuccessDistance);
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
            ulong initializationEvaluations;

            // Instantiate the internal initialization algorithm
            _initializationAlgorithm.InitializeAlgorithm(ParallelOptions, genomeFactory, genomeList,
                CreateGenomeDecoder(), NeatEvolutionAlgorithmParameters);

            // Run the algorithm until a viable genome is found
            NeatGenome genomeSeed = _initializationAlgorithm.EvolveViableGenome(out initializationEvaluations);

            // Create complexity regulation strategy.
            var complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategy, Complexitythreshold);

            // Create the evolution algorithm.
            AbstractNeatEvolutionAlgorithm<NeatGenome> ea =
                new QueueingNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                    complexityRegulationStrategy, _batchSize, RunPhase.Primary, _evolutionDataLogger);

            // Create IBlackBox evaluator.
            IPhenomeEvaluator<IBlackBox, BehaviorInfo> mazeNavigationEvaluator =
                new MazeNavigationMCSEvaluator(MaxDistanceToTarget, MaxTimesteps,
                    MazeVariant, MinSuccessDistance, _behaviorCharacterizationFactory, initializationEvaluations,
                    _bridgingMagnitude);

            // Create genome decoder.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

//            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
//                new SerialGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
//                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch, _evaluationDataLogger, _decodeGenomeToXml);

            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch, _evaluationDataLogger,
                    SerializeGenomeToXml);

            // Initialize the evolution algorithm.
            ea.Initialize(fitnessEvaluator, genomeFactory, new List<NeatGenome> {genomeSeed}, DefaultPopulationSize,
                null, MaxEvaluations);

            // Finished. Return the evolution algorithm
            return ea;
        }

        private class InitializationAlgorithm
        {
            private readonly IDataLogger _evaluationDataLogger;
            private readonly IDataLogger _evolutionDataLogger;
            private readonly ulong? _maxEvaluations;
            private readonly bool _serializeGenomeToXml;
            private double _archiveAdditionThreshold;
            private double _archiveThresholdDecreaseMultiplier;
            private double _archiveThresholdIncreaseMultiplier;
            private int _batchSize;
            private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
            private string _complexityRegulationStrategyDefinition;
            private int? _complexityThreshold;
            private AbstractNeatEvolutionAlgorithm<NeatGenome> _initializationEa;
            private int? _maxDistanceToTarget;
            private int _maxGenerationArchiveAddition;
            private int _maxGenerationsWithoutArchiveAddition;
            private int? _maxTimesteps;
            private MazeVariant _mazeVariant;
            private int? _minSuccessDistance;
            private int _nearestNeighbors;
            private int _populationEvaluationFrequency;

            /// <summary>
            ///     Initialization algorithm constructor.
            /// </summary>
            /// <param name="evolutionDataLogger">Sets the evolution logger reference from the parent algorithm.</param>
            /// <param name="evaluationDataLogger">Sets the evaluation logger reference from the parent algorithm.</param>
            /// <param name="serializeGenomeToXml">Whether each evaluated genome should be serialized to XML.</param>
            public InitializationAlgorithm(ulong? maxEvaluations, IDataLogger evolutionDataLogger,
                IDataLogger evaluationDataLogger,
                bool? serializeGenomeToXml)
            {
                _maxEvaluations = maxEvaluations;
                _evolutionDataLogger = evolutionDataLogger;
                _evaluationDataLogger = evaluationDataLogger;
                _serializeGenomeToXml = serializeGenomeToXml ?? false;
            }

            /// <summary>
            ///     Constructs and initializes the MCS initialization algorithm (novelty search).
            /// </summary>
            /// <param name="xmlConfig">The XML configuration for the initialization algorithm.</param>
            /// <returns>The constructed initialization algorithm.</returns>
            public void SetAlgorithmParameters(XmlElement xmlConfig)
            {
                // Get complexity constraint parameters
                _complexityRegulationStrategyDefinition = XmlUtils.TryGetValueAsString(xmlConfig,
                    "ComplexityRegulationStrategy");
                _complexityThreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");

                // Read in the behavior characterization
                _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(xmlConfig,
                    "InitBehaviorConfig");

                // Read in the novelty archive parameters
                ExperimentUtils.ReadNoveltyParameters(xmlConfig, out _archiveAdditionThreshold,
                    out _archiveThresholdDecreaseMultiplier, out _archiveThresholdIncreaseMultiplier,
                    out _maxGenerationArchiveAddition, out _maxGenerationsWithoutArchiveAddition);

                // Read in nearest neighbors for behavior distance calculations
                _nearestNeighbors = XmlUtils.GetValueAsInt(xmlConfig, "NearestNeighbors");

                // Read in steady-state specific parameters
                _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");
                _populationEvaluationFrequency = XmlUtils.GetValueAsInt(xmlConfig, "PopulationEvaluationFrequency");
            }

            /// <summary>
            ///     Constructs and initializes the MCS initialization algorithm (novelty search) using the database configuration.
            /// </summary>
            /// <param name="xmlConfig">The XML configuration for the initialization algorithm.</param>
            /// <returns>The constructed initialization algorithm.</returns>
            public void SetAlgorithmParameters(ExperimentDictionary experimentDictionary)
            {
                // Get complexity constraint parameters
                _complexityRegulationStrategyDefinition =
                    experimentDictionary.Initialization_ComplexityRegulationStrategy;
                _complexityThreshold = experimentDictionary.Initialization_ComplexityThreshold;

                // Read in the behavior characterization
                _behaviorCharacterizationFactory =
                    ExperimentUtils.ReadBehaviorCharacterizationFactory(experimentDictionary, false);

                // Read in the novelty archive parameters
                _archiveAdditionThreshold =
                    experimentDictionary.Initialization_NoveltySearch_ArchiveAdditionThreshold ?? default(double);
                _archiveThresholdDecreaseMultiplier =
                    experimentDictionary.Initialization_NoveltySearch_ArchiveThresholdDecreaseMultiplier ??
                    default(double);
                _archiveThresholdIncreaseMultiplier =
                    experimentDictionary.Initialization_NoveltySearch_ArchiveThresholdIncreaseMultiplier ??
                    default(double);
                _maxGenerationArchiveAddition =
                    experimentDictionary.Initialization_NoveltySearch_MaxGenerationsWithArchiveAddition ??
                    default(int);
                _maxGenerationsWithoutArchiveAddition =
                    experimentDictionary.Initialization_NoveltySearch_MaxGenerationsWithoutArchiveAddition ??
                    default(int);

                // Read in nearest neighbors for behavior distance calculations
                _nearestNeighbors = experimentDictionary.Initialization_NoveltySearch_NearestNeighbors ?? default(int);

                // Read in steady-state specific parameters
                _batchSize = experimentDictionary.Initialization_OffspringBatchSize ?? default(int);
                _populationEvaluationFrequency = experimentDictionary.Initialization_PopulationEvaluationFrequency ??
                                                 default(int);
            }

            /// <summary>
            ///     Sets configuration variables specific to the maze navigation simulation.
            /// </summary>
            /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
            /// <param name="maxTimesteps">The maximum number of time steps for which to run the simulation.</param>
            /// <param name="mazeVariant">The maze variant to run (i.e. medium/hard maze).</param>
            /// <param name="minSuccessDistance">The minimum distance to the target location for the maze to be considered "solved".</param>
            public void SetEnvironmentParameters(int? maxDistanceToTarget, int? maxTimesteps, MazeVariant mazeVariant,
                int? minSuccessDistance)
            {
                _maxDistanceToTarget = maxDistanceToTarget;
                _maxTimesteps = maxTimesteps;
                _minSuccessDistance = minSuccessDistance;
                _mazeVariant = mazeVariant;
            }

            /// <summary>
            ///     Configures and instantiates the initialization evolutionary algorithm.
            /// </summary>
            /// <param name="parallelOptions">Synchronous/Asynchronous execution settings.</param>
            /// <param name="genomeFactory">The factory used to generate genomes.</param>
            /// <param name="genomeList">The initial population of genomes.</param>
            /// <param name="genomeDecoder">The decoder to translate genomes into phenotypes.</param>
            /// <param name="neatParameters">The NEAT EA parameters.</param>
            public void InitializeAlgorithm(ParallelOptions parallelOptions, IGenomeFactory<NeatGenome> genomeFactory,
                List<NeatGenome> genomeList, IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder,
                NeatEvolutionAlgorithmParameters neatParameters)
            {
                // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
                IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
                ISpeciationStrategy<NeatGenome> speciationStrategy =
                    new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, parallelOptions);

                // Create complexity regulation strategy.
                IComplexityRegulationStrategy complexityRegulationStrategy =
                    ExperimentUtils.CreateComplexityRegulationStrategy(_complexityRegulationStrategyDefinition,
                        _complexityThreshold);

                // Create the initialization evolution algorithm.
                _initializationEa = new SteadyStateNeatEvolutionAlgorithm<NeatGenome>(neatParameters,
                    speciationStrategy, complexityRegulationStrategy, _batchSize, _populationEvaluationFrequency,
                    RunPhase.Initialization, _evolutionDataLogger);

                // Create IBlackBox evaluator.
                MazeNavigationMCSInitializationEvaluator mazeNavigationEvaluator =
                    new MazeNavigationMCSInitializationEvaluator(_maxDistanceToTarget, _maxTimesteps,
                        _mazeVariant,
                        _minSuccessDistance, _behaviorCharacterizationFactory);

                // Create a novelty archive.
                AbstractNoveltyArchive<NeatGenome> archive =
                    new BehavioralNoveltyArchive<NeatGenome>(_archiveAdditionThreshold,
                        _archiveThresholdDecreaseMultiplier, _archiveThresholdIncreaseMultiplier,
                        _maxGenerationArchiveAddition, _maxGenerationsWithoutArchiveAddition);

//                IGenomeEvaluator<NeatGenome> fitnessEvaluator =
//                    new SerialGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
//                        SelectionType.SteadyState, SearchType.NoveltySearch,
//                        _nearestNeighbors, archive, _evaluationDataLogger, _serializeGenomeToXml);

                IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                    new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                        SelectionType.SteadyState, SearchType.NoveltySearch,
                        _nearestNeighbors, archive, _evaluationDataLogger, _serializeGenomeToXml);

                // Initialize the evolution algorithm.
                _initializationEa.Initialize(fitnessEvaluator, genomeFactory, genomeList, null, _maxEvaluations, archive);
            }

            /// <summary>
            ///     Runs the initialization algorithm until a viable genome (i.e. one that meets the minimal criteria) is found and
            ///     returns that genome along with the total number of evaluations that were executed to find it.
            /// </summary>
            /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genome.</param>
            /// <returns>The seed genome that meets the minimal criteria.</returns>
            public NeatGenome EvolveViableGenome(out ulong totalEvaluations)
            {
                // Start the algorithm
                _initializationEa.StartContinue();

                // Ping for status every couple of seconds
                while (RunState.Terminated != _initializationEa.RunState &&
                       RunState.Paused != _initializationEa.RunState)
                {
                    Thread.Sleep(2000);
                }

                // Get the list of genomes (which presumably contain at least one viable genome)
                IList<NeatGenome> initializedGenomes = _initializationEa.GenomeList;

                // Iterate through the initialized genomes until one is found that has been deemed viable
                NeatGenome viableGenome =
                    initializedGenomes.FirstOrDefault(curGenome => curGenome.EvaluationInfo.IsViable);

                // Make sure the genome is not null (this shouldn't happen as the initialization algorithm should
                // continue to run until a viable genome is found)
                if (viableGenome == null)
                {
                    throw new SharpNeatException("MCS initialization algorithm failed to find a viable genome.");
                }

                // Get the number of evaluations it took to find a viable genome
                totalEvaluations = _initializationEa.CurrentEvaluations;

                return viableGenome;
            }
        }
    }
}