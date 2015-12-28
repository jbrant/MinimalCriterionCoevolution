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
using SharpNeat.Domains.MazeNavigation.MCSExperiment;
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

namespace SharpNeat.Domains.MazeNavigation
{
    public class NoveltySearchMazeNavigationInitializer
    {
        private readonly IDataLogger _evaluationDataLogger;
        private readonly IDataLogger _evolutionDataLogger;
        private readonly IDictionary<FieldElement, bool> _initializationLogFieldEnableMap;
        private readonly ulong? _maxEvaluations;
        private readonly bool _serializeGenomeToXml;
        private double _archiveAdditionThreshold;
        private double _archiveThresholdDecreaseMultiplier;
        private double _archiveThresholdIncreaseMultiplier;
        private int _batchSize;
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private string _complexityRegulationStrategyDefinition;
        private int? _complexityThreshold;
        private IGenomeFactory<NeatGenome> _genomeFactory;
        private AbstractNeatEvolutionAlgorithm<NeatGenome> _initializationEa;
        private int _maxDistanceToTarget;
        private int _maxGenerationArchiveAddition;
        private int _maxGenerationsWithoutArchiveAddition;
        private int _maxTimesteps;
        private MazeVariant _mazeVariant;
        private int _minSuccessDistance;
        private int _nearestNeighbors;
        private NeatEvolutionAlgorithmParameters _neatEvolutionAlgorithmParameters;
        private int _populationEvaluationFrequency;

        /// <summary>
        ///     Initialization algorithm constructor.
        /// </summary>
        /// <param name="evolutionDataLogger">Sets the evolution logger reference from the parent algorithm.</param>
        /// <param name="evaluationDataLogger">Sets the evaluation logger reference from the parent algorithm.</param>
        /// <param name="serializeGenomeToXml">Whether each evaluated genome should be serialized to XML.</param>
        public NoveltySearchMazeNavigationInitializer(ulong? maxEvaluations, IDataLogger evolutionDataLogger,
            IDataLogger evaluationDataLogger,
            bool? serializeGenomeToXml)
        {
            _maxEvaluations = maxEvaluations;
            _evolutionDataLogger = evolutionDataLogger;
            _evaluationDataLogger = evaluationDataLogger;
            _serializeGenomeToXml = serializeGenomeToXml ?? false;

            // Setup log field enable/disable map
            _initializationLogFieldEnableMap = new Dictionary<FieldElement, bool>
            {
                {EvolutionFieldElements.ChampGenomeFitness, true}
            };
            if (_serializeGenomeToXml)
            {
                _initializationLogFieldEnableMap.Add(EvolutionFieldElements.ChampGenomeXml, true);
            }
        }

        public int PopulationSize { get; private set; }

        /// <summary>
        ///     Constructs and initializes the MCS initialization algorithm (novelty search).
        /// </summary>
        /// <param name="xmlConfig">The XML configuration for the initialization algorithm.</param>
        /// <param name="inputCount">The number of input neurons.</param>
        /// <param name="outputCount">The number of output neurons.</param>
        /// <returns>The constructed initialization algorithm.</returns>
        public void SetAlgorithmParameters(XmlElement xmlConfig, int inputCount, int outputCount)
        {
            // Read the target population size
            PopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");

            // Read NEAT genome parameters
            NeatGenomeParameters neatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);

            // Read NEAT evolution parameters
            _neatEvolutionAlgorithmParameters = ExperimentUtils.ReadNeatEvolutionAlgorithmParameters(xmlConfig);

            // Create genome factory specifically for the initialization algorithm 
            // (this is primarily because the initialization algorithm will quite likely have different NEAT parameters)
            _genomeFactory = new NeatGenomeFactory(inputCount, outputCount, neatGenomeParameters);

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
        /// <param name="experimentDictionary">The reference to the experiment dictionary entity.</param>
        /// <param name="inputCount">The number of input neurons.</param>
        /// <param name="outputCount">The number of output neurons.</param>
        /// <returns>The constructed initialization algorithm.</returns>
        public void SetAlgorithmParameters(ExperimentDictionary experimentDictionary, int inputCount,
            int outputCount)
        {
            // Read NEAT parameters
            NeatGenomeParameters neatGenomeParameters =
                ExperimentUtils.ReadNeatGenomeParameters(experimentDictionary, false);

            // Read NEAT evolution parameters
            _neatEvolutionAlgorithmParameters =
                ExperimentUtils.ReadNeatEvolutionAlgorithmParameters(experimentDictionary, false);

            // Create genome factory specifically for the initialization algorithm 
            // (this is primarily because the initialization algorithm will quite likely have different NEAT parameters)
            _genomeFactory = new NeatGenomeFactory(inputCount, outputCount, neatGenomeParameters);

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
        public void SetEnvironmentParameters(int maxDistanceToTarget, int maxTimesteps, MazeVariant mazeVariant,
            int minSuccessDistance)
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
        /// <param name="genomeList">The initial population of genomes.</param>
        /// <param name="genomeDecoder">The decoder to translate genomes into phenotypes.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceeded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        public void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> genomeList,
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ulong startingEvaluations)
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
            _initializationEa = new SteadyStateNeatEvolutionAlgorithm<NeatGenome>(_neatEvolutionAlgorithmParameters,
                speciationStrategy, complexityRegulationStrategy, _batchSize, _populationEvaluationFrequency,
                RunPhase.Initialization, _evolutionDataLogger, _initializationLogFieldEnableMap);

            // Create IBlackBox evaluator.
            MazeNavigationMCSInitializationEvaluator mazeNavigationEvaluator =
                new MazeNavigationMCSInitializationEvaluator(_maxDistanceToTarget, _maxTimesteps,
                    _mazeVariant,
                    _minSuccessDistance, _behaviorCharacterizationFactory, startingEvaluations);

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

            // Only pull the number of genomes from the list equivalent to the initialization algorithm population size
            // (this is to handle the case where the list was created in accordance with the primary algorithm 
            // population size, which could have been larger)
            genomeList = genomeList.Take(PopulationSize).ToList();

            // Initialize the evolution algorithm.
            _initializationEa.Initialize(fitnessEvaluator, _genomeFactory, genomeList, PopulationSize, null,
                _maxEvaluations + startingEvaluations,
                archive);
        }

        /// <summary>
        ///     Runs the initialization algorithm until a viable genome (i.e. one that meets the minimal criteria) is found and
        ///     returns that genome along with the total number of evaluations that were executed to find it.
        /// </summary>
        /// <param name="useObjectiveDistanceFitness">
        ///     Flag indicating whether to replace fitness on evolved viable seed genomes
        ///     with distance to the target (objective).
        /// </param>
        /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genome.</param>
        /// <returns>The seed genome that meets the minimal criteria.</returns>
        public NeatGenome EvolveViableGenome(bool useObjectiveDistanceFitness, out ulong totalEvaluations)
        {
            // Iterate through the initialized genomes until one is found that has been deemed viable
            NeatGenome viableGenome = EvolveViableGenomes(1, useObjectiveDistanceFitness, out totalEvaluations)[0];

            // Make sure the genome is not null (this shouldn't happen as the initialization algorithm should
            // continue to run until a viable genome is found)
            if (viableGenome == null)
            {
                throw new SharpNeatException("MCS initialization algorithm failed to find a viable genome.");
            }

            return viableGenome;
        }

        /// <summary>
        ///     Runs the initialization algorithm until the specified number of viable genomes (i.e. genomes that meets the minimal
        ///     criteria) are found and returns those genomes along with the total number of evaluations that were executed to find
        ///     them.
        /// </summary>
        /// <param name="numViableGenomes">The number of distinct viable genomes to find.</param>
        /// <param name="useObjectiveDistanceFitness">
        ///     Flag indicating whether to replace fitness on evolved viable seed genomes
        ///     with distance to the target (objective).
        /// </param>
        /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genomes.</param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public List<NeatGenome> EvolveViableGenomes(int numViableGenomes, bool useObjectiveDistanceFitness,
            out ulong totalEvaluations)
        {
            HashSet<NeatGenome> viableGenomes = new HashSet<NeatGenome>();

            // Rerun algorithm until the specified number of *distinct* genomes are found, which all
            // satisfy the minimal criteria
            do
            {
                // Start the algorithm
                _initializationEa.StartContinue();

                // Ping for status every couple of seconds
                while (RunState.Terminated != _initializationEa.RunState &&
                       RunState.Paused != _initializationEa.RunState)
                {
                    Thread.Sleep(200);
                }

                // Get the list of genomes, filter out those who don't satisfy the minimal criteria
                // (i.e. are not viable), and union them with the current set of distinct genomes that
                // satisfy the MC (thereby removing any duplicate genomes that have already been added)
                viableGenomes.UnionWith(
                    _initializationEa.GenomeList.Where(curGenome => curGenome.EvaluationInfo.IsViable).ToList());

                // Update the total number of evaluations
                totalEvaluations = _initializationEa.CurrentEvaluations;
            } while (viableGenomes.Count < numViableGenomes);

            // Ensure that the initialization algorithm was able to find the requested number of viable genomes
            if (viableGenomes.Count < numViableGenomes)
            {
                throw new SharpNeatException(
                    "MCS initialization algorithm failed to find the requested number of viable genomes.");
            }

            // If flag is set, replace fitness on all viable genomes with objective distance
            // (they will currently be based on behavioral novelty)
            if (useObjectiveDistanceFitness)
            {
                // Replace fitness on all viable genomes with distance to the target
                foreach (NeatGenome viableGenome in viableGenomes)
                {
                    viableGenome.EvaluationInfo.SetFitness(viableGenome.EvaluationInfo.ObjectiveDistance);
                }
            }

            // Convert the set of viable genomes to a list and return
            return viableGenomes.ToList();
        }
    }
}