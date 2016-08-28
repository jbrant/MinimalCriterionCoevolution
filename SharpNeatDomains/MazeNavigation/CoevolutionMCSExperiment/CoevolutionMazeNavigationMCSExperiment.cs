#region

using System.Collections.Generic;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Decoders.Maze;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.Domains.MazeNavigation.Bootstrappers;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;
using SharpNeat.SpeciationStrategies;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment
{
    public class CoevolutionMazeNavigationMCSExperiment : BaseCoevolutionMazeNavigationExperiment
    {
        #region Public Methods

        /// <summary>
        ///     Initializes the coevolution maze navigation experiment by reading in all of the configuration parameters and
        ///     setting up the bootstrapping/initialization algorithm.
        /// </summary>
        /// <param name="name">The name of the experiment.</param>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="navigatorEvolutionLogger">The navigator evolution data logger.</param>
        /// <param name="navigatorGenomeLogger">The navigator genome logger.</param>
        /// <param name="mazeEvolutionLogger">The maze evolution data logger.</param>
        /// <param name="mazeGenomeLogger">The maze genome logger.</param>
        public override void Initialize(string name, XmlElement xmlConfig,
            IDataLogger navigatorEvolutionLogger = null, IDataLogger navigatorGenomeLogger = null,
            IDataLogger mazeEvolutionLogger = null, IDataLogger mazeGenomeLogger = null)
        {
            // Initialize boiler plate parameters
            base.Initialize(name, xmlConfig, navigatorEvolutionLogger, navigatorGenomeLogger,
                mazeEvolutionLogger,
                mazeGenomeLogger);

            // Set experiment-specific parameters
            _maxTimesteps = XmlUtils.GetValueAsInt(xmlConfig, "MaxTimesteps");
            _minSuccessDistance = XmlUtils.GetValueAsInt(xmlConfig, "MinSuccessDistance");
            _mazeHeight = XmlUtils.GetValueAsInt(xmlConfig, "MazeHeight");
            _mazeWidth = XmlUtils.GetValueAsInt(xmlConfig, "MazeWidth");
            _mazeScaleMultiplier = XmlUtils.GetValueAsInt(xmlConfig, "MazeScaleMultiplier");

            // Get success/failure criteria constraints
            _numMazeSuccessCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumMazesSolvedCriteria");
            _numAgentSuccessCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumAgentsSolvedCriteria");
            _numAgentFailedCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumAgentsFailedCriteria");

            // Read in log file path/name
            _navigatorEvolutionDataLogger = navigatorEvolutionLogger ??
                                            ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution,
                                                "NavigatorLoggingConfig");
            _navigatorPopulationGenomesDataLogger = navigatorGenomeLogger ??
                                                    ExperimentUtils.ReadDataLogger(xmlConfig,
                                                        LoggingType.PopulationGenomes, "NavigatorLoggingConfig");
            _mazeEvolutionDataLogger = mazeEvolutionLogger ??
                                       ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution,
                                           "MazeLoggingConfig");
            _mazePopulationGenomesDataLogger = mazeGenomeLogger ??
                                               ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.PopulationGenomes,
                                                   "MazeLoggingConfig");

            // Create new evolution field elements map with all fields enabled
            _navigatorLogFieldEnableMap = EvolutionFieldElements.PopulateEvolutionFieldElementsEnableMap();

            // Also add default population logging configuration
            foreach (
                KeyValuePair<FieldElement, bool> populationLoggingPair in
                    PopulationGenomesFieldElements.PopulatePopulationGenomesFieldElementsEnableMap())
            {
                _navigatorLogFieldEnableMap.Add(populationLoggingPair.Key, populationLoggingPair.Value);
            }

            // Disable logging fields not relevant to coevolution experiment
            _navigatorLogFieldEnableMap[EvolutionFieldElements.SpecieCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.AsexualOffspringCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.SexualOffspringCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.InterspeciesOffspringCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaThreshold] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointX] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointY] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxFitness] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanFitness] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanSpecieChampFitness] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinSpecieSize] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxSpecieSize] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeGenomeId] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeFitness] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeBirthGeneration] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeConnectionGeneCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeNeuronGeneCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeTotalGeneCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeEvaluationCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeBehaviorX] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeBehaviorY] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeDistanceToTarget] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeXml] = false;

            // Create a maze logger configuration with the same configuration as the navigator one
            _mazeLogFieldEnableMap = new Dictionary<FieldElement, bool>(_navigatorLogFieldEnableMap);

            // Make on change to the maze logger configuration to switch off run phase logging
            _mazeLogFieldEnableMap[EvolutionFieldElements.RunPhase] = false;
            _mazeLogFieldEnableMap[PopulationGenomesFieldElements.RunPhase] = false;

            // Read in the number of batches between population logging
            _populationLoggingBatchInterval = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationLoggingBatchInterval");

            // Read in whether the individual specie sizes are being capped (defaults to false)
            _isSpecieFixedSize = XmlUtils.TryGetValueAsBool(xmlConfig, "SpecieSizeFixed") ?? default(bool);

            // Initialize the initialization algorithm
            _mazeNavigationInitializer =
                ExperimentUtils.DetermineCoevolutionInitializer(
                    xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement);

            // Set the initialization loggers
            _mazeNavigationInitializer.SetDataLoggers(_navigatorEvolutionDataLogger,
                _navigatorPopulationGenomesDataLogger, _navigatorLogFieldEnableMap, _populationLoggingBatchInterval);

            // Setup initialization algorithm
            _mazeNavigationInitializer.SetAlgorithmParameters(
                xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement, AnnInputCount,
                AnnOutputCount, _numAgentSuccessCriteria, _numAgentFailedCriteria);

            // Pass in maze experiment specific parameters 
            // (note that a new maze structure is created here for the sole purpose of extracting the maze dimensions and calculating max distance to target)
            _mazeNavigationInitializer.SetEnvironmentParameters(_maxTimesteps, _minSuccessDistance,
                new MazeDecoder(_mazeHeight, _mazeWidth, _mazeScaleMultiplier).Decode(
                    new MazeGenomeFactory(MazeGenomeParameters, null, null).CreateGenome(0)));
        }

        /// <summary>
        ///     Zero argument wrapper method for instantiating the coveolution algorithm container.  This uses default agent and
        ///     maze population sizes as the only configuration parameters.
        /// </summary>
        /// <returns>The instantiated coevolution algorithm container.</returns>
        public override ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer()
        {
            return CreateCoevolutionAlgorithmContainer(AgentSeedGenomeCount, MazeSeedGenomeCount);
        }

        /// <summary>
        ///     Creates the coevolution algorithm container using the given agent and maze population sizes.
        /// </summary>
        /// <param name="populationSize1">The agent population size.</param>
        /// <param name="populationSize2">The maze population size.</param>
        /// <returns>The instantiated coevolution algorithm container.</returns>
        public override ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer(
            int populationSize1, int populationSize2)
        {
            // Create a genome factory for the NEAT genomes
            IGenomeFactory<NeatGenome> neatGenomeFactory = new NeatGenomeFactory(AnnInputCount, AnnOutputCount,
                NeatGenomeParameters);

            // Create a genome factory for the maze genomes
            IGenomeFactory<MazeGenome> mazeGenomeFactory = new MazeGenomeFactory(MazeGenomeParameters, _mazeHeight,
                _mazeWidth);

            // Create an initial population of maze navigators
            List<NeatGenome> neatGenomeList = neatGenomeFactory.CreateGenomeList(populationSize1, 0);

            // Create an initial population of mazes
            // NOTE: the population is set to 1 here because we're just starting with a single, completely open maze space
            List<MazeGenome> mazeGenomeList = mazeGenomeFactory.CreateGenomeList(populationSize2, 0);

            // Create the evolution algorithm container
            return CreateCoevolutionAlgorithmContainer(neatGenomeFactory, mazeGenomeFactory, neatGenomeList,
                mazeGenomeList);
        }

        /// <summary>
        ///     Creates the evolution algorithm container using the given factories and genome lists.
        /// </summary>
        /// <param name="genomeFactory1">The agent genome factory.</param>
        /// <param name="genomeFactory2">The maze genome factory.</param>
        /// <param name="genomeList1">The agent genome list.</param>
        /// <param name="genomeList2">The maze genome list.</param>
        /// <returns>The instantiated coevolution algorithm container.</returns>
        public override ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer(
            IGenomeFactory<NeatGenome> genomeFactory1,
            IGenomeFactory<MazeGenome> genomeFactory2, List<NeatGenome> genomeList1, List<MazeGenome> genomeList2)
        {
            ulong initializationEvaluations;

            // Compute the maze max complexity
            ((MazeGenomeFactory) genomeFactory2).MaxComplexity = MazeUtils.DetermineMaxPartitions(_mazeHeight,
                _mazeWidth, 200);

            // Go ahead and log static initialization maze genome
            _mazeNavigationInitializer.LogStartingMazeGenome(genomeList2[0], _mazePopulationGenomesDataLogger);

            // Instantiate the internal initialization algorithm
            _mazeNavigationInitializer.InitializeAlgorithm(ParallelOptions, genomeList1,
                new MazeDecoder(_mazeHeight, _mazeWidth, _mazeScaleMultiplier).Decode(genomeList2[0]),
                new NeatGenomeDecoder(ActivationScheme), 0);

            // Run the initialization algorithm until the requested number of viable seed genomes are found
            List<NeatGenome> seedAgentPopulation =
                _mazeNavigationInitializer.EvolveViableGenomes(out initializationEvaluations);

            // Set dummy fitness so that seed maze(s) will be marked as evaluated
            foreach (MazeGenome mazeGenome in genomeList2)
            {
                mazeGenome.EvaluationInfo.SetFitness(0);
            }

            // Create the NEAT (i.e. navigator) queueing evolution algorithm
            AbstractEvolutionAlgorithm<NeatGenome> neatEvolutionAlgorithm =
                new QueueingNeatEvolutionAlgorithm<NeatGenome>(
                    new NeatEvolutionAlgorithmParameters
                    {
                        SpecieCount = AgentNumSpecies,
                        MaxSpecieSize = AgentDefaultPopulationSize/AgentNumSpecies
                    },
                    new ParallelKMeansClusteringStrategy<NeatGenome>(new ManhattanDistanceMetric(1.0, 0.0, 10.0),
                        ParallelOptions), null,
                    NavigatorBatchSize, RunPhase.Primary, false, false, _navigatorEvolutionDataLogger,
                    _navigatorLogFieldEnableMap, _navigatorPopulationGenomesDataLogger, _populationLoggingBatchInterval,
                    _isSpecieFixedSize);

            // Create the maze queueing evolution algorithm
            AbstractEvolutionAlgorithm<MazeGenome> mazeEvolutionAlgorithm =
                new QueueingNeatEvolutionAlgorithm<MazeGenome>(
                    new NeatEvolutionAlgorithmParameters
                    {
                        SpecieCount = MazeNumSpecies,
                        MaxSpecieSize = MazeDefaultPopulationSize/MazeNumSpecies
                    },
                    new ParallelKMeansClusteringStrategy<MazeGenome>(new ManhattanDistanceMetric(1.0, 0.0, 10.0),
                        ParallelOptions), null,
                    MazeBatchSize, RunPhase.Primary, false, false, _mazeEvolutionDataLogger, _mazeLogFieldEnableMap,
                    _mazePopulationGenomesDataLogger, _populationLoggingBatchInterval, _isSpecieFixedSize);

            // Create the maze phenome evaluator
            IPhenomeEvaluator<MazeStructure, BehaviorInfo> mazeEvaluator = new MazeEnvironmentMCSEvaluator(
                _maxTimesteps,
                _minSuccessDistance, BehaviorCharacterizationFactory, _numAgentSuccessCriteria, _numAgentFailedCriteria);

            // Create navigator phenome evaluator
            IPhenomeEvaluator<IBlackBox, BehaviorInfo> navigatorEvaluator = new MazeNavigatorMCSEvaluator(
                _maxTimesteps, _minSuccessDistance, BehaviorCharacterizationFactory, _numMazeSuccessCriteria);

            // Create maze genome decoder
            IGenomeDecoder<MazeGenome, MazeStructure> mazeGenomeDecoder = new MazeDecoder(_mazeHeight, _mazeWidth,
                _mazeScaleMultiplier);

            // Create navigator genome decoder
            IGenomeDecoder<NeatGenome, IBlackBox> navigatorGenomeDecoder = new NeatGenomeDecoder(ActivationScheme);

            // Create the maze genome evaluator
            IGenomeEvaluator<MazeGenome> mazeFitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<MazeGenome, MazeStructure>(mazeGenomeDecoder, mazeEvaluator,
                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch, ParallelOptions);

            // Create navigator genome evaluator
            IGenomeEvaluator<NeatGenome> navigatorFitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(navigatorGenomeDecoder, navigatorEvaluator,
                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch, ParallelOptions);

            // Create the coevolution container
            ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> coevolutionAlgorithmContainer =
                new CoevolutionAlgorithmContainer<NeatGenome, MazeGenome>(neatEvolutionAlgorithm, mazeEvolutionAlgorithm);

            // Initialize the container and component algorithms
            coevolutionAlgorithmContainer.Initialize(navigatorFitnessEvaluator, genomeFactory1, seedAgentPopulation,
                AgentDefaultPopulationSize, mazeFitnessEvaluator, genomeFactory2, genomeList2, MazeDefaultPopulationSize,
                MaxGenerations, MaxEvaluations);

            return coevolutionAlgorithmContainer;
        }

        #endregion

        #region Instance Variables

        /// <summary>
        ///     Initialization algorithm for producing an initial population with the requisite number of viable genomes.
        /// </summary>
        private CoevolutionMazeNavigationInitializer _mazeNavigationInitializer;

        /// <summary>
        ///     The maximum number of timesteps allowed for a single simulation.
        /// </summary>
        private int _maxTimesteps;

        /// <summary>
        ///     The minimum distance to the target required in order to have "solved" the maze.
        /// </summary>
        private int _minSuccessDistance;

        /// <summary>
        ///     The minimum number of mazes that the agent under evaluation must solve in order to meet the minimal criteria.
        /// </summary>
        private int _numMazeSuccessCriteria;

        /// <summary>
        ///     The minimum number of agents that must solve the maze under evaluation in order to meet this portion of the minimal
        ///     criteria.
        /// </summary>
        private int _numAgentSuccessCriteria;

        /// <summary>
        ///     The minimum number of agents that must fail to solve the maze under evaluation in order to meet this portion of the
        ///     minimal criteria.
        /// </summary>
        private int _numAgentFailedCriteria;

        /// <summary>
        ///     The width of the evolved maze environments.
        /// </summary>
        private int _mazeHeight;

        /// <summary>
        ///     The height of the evolved maze environments.
        /// </summary>
        private int _mazeWidth;

        /// <summary>
        ///     The multiplier for scaling the maze to larger sizes.
        /// </summary>
        private int _mazeScaleMultiplier;

        /// <summary>
        ///     Logs statistics about the navigator populations for every batch.
        /// </summary>
        private IDataLogger _navigatorEvolutionDataLogger;

        /// <summary>
        ///     Logs the XML definitions of the extant navigator population on a periodic basis.
        /// </summary>
        private IDataLogger _navigatorPopulationGenomesDataLogger;

        /// <summary>
        ///     Logs statistics about the maze populations for every batch.
        /// </summary>
        private IDataLogger _mazeEvolutionDataLogger;

        /// <summary>
        ///     Logs the XML definitions of the extant maze population on a periodic basis.
        /// </summary>
        private IDataLogger _mazePopulationGenomesDataLogger;

        /// <summary>
        ///     Dictionary which indicates logger fields to be enabled/disabled for navigator genomes.
        /// </summary>
        private IDictionary<FieldElement, bool> _navigatorLogFieldEnableMap;

        /// <summary>
        ///     Dictionary which indicates logger fields to be enabled/disabled for maze genomes.
        /// </summary>
        private IDictionary<FieldElement, bool> _mazeLogFieldEnableMap;

        /// <summary>
        ///     Controls the number of batches between population definitions (i.e. genome XML) being logged.
        /// </summary>
        private int? _populationLoggingBatchInterval;

        /// <summary>
        ///     Indicates whether each species should be capped at a maximum size.
        /// </summary>
        private bool _isSpecieFixedSize;

        #endregion
    }
}