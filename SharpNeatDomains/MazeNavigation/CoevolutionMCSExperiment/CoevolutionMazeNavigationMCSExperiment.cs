#region

using System.Collections.Generic;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Decoders.Maze;
using SharpNeat.Decoders.Neat;
using SharpNeat.Domains.MazeNavigation.Bootstrappers;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

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
        /// <param name="seedMazePath">The path to the genome file or directory containing the seed maze(s).</param>
        /// <param name="navigatorEvolutionLogger">The navigator evolution data logger.</param>
        /// <param name="navigatorGenomeLogger">The navigator genome logger.</param>
        /// <param name="mazeEvolutionLogger">The maze evolution data logger.</param>
        /// <param name="mazeGenomeLogger">The maze genome logger.</param>
        public override void Initialize(string name, XmlElement xmlConfig, string seedMazePath,
            IDataLogger navigatorEvolutionLogger = null,
            IDataLogger navigatorGenomeLogger = null, IDataLogger mazeEvolutionLogger = null,
            IDataLogger mazeGenomeLogger = null)
        {
            // Initialize boiler plate parameters
            base.Initialize(name, xmlConfig, seedMazePath, navigatorEvolutionLogger, navigatorGenomeLogger,
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

            // Read in seed maze

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
            _logFieldEnableMap = EvolutionFieldElements.PopulateEvolutionFieldElementsEnableMap();

            // Disable logging fields not relevant to coevolution experiment            
            _logFieldEnableMap[EvolutionFieldElements.RunPhase] = false;
            _logFieldEnableMap[EvolutionFieldElements.SpecieCount] = false;
            _logFieldEnableMap[EvolutionFieldElements.AsexualOffspringCount] = false;
            _logFieldEnableMap[EvolutionFieldElements.SexualOffspringCount] = false;
            _logFieldEnableMap[EvolutionFieldElements.InterspeciesOffspringCount] = false;
            _logFieldEnableMap[EvolutionFieldElements.MinimalCriteriaThreshold] = false;
            _logFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointX] = false;
            _logFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointY] = false;
            _logFieldEnableMap[EvolutionFieldElements.MaxFitness] = false;
            _logFieldEnableMap[EvolutionFieldElements.MeanFitness] = false;
            _logFieldEnableMap[EvolutionFieldElements.MeanSpecieChampFitness] = false;
            _logFieldEnableMap[EvolutionFieldElements.MinSpecieSize] = false;
            _logFieldEnableMap[EvolutionFieldElements.MaxSpecieSize] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeGenomeId] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeFitness] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeBirthGeneration] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeConnectionGeneCount] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeNeuronGeneCount] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeTotalGeneCount] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeEvaluationCount] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeBehaviorX] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeBehaviorY] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeDistanceToTarget] = false;
            _logFieldEnableMap[EvolutionFieldElements.ChampGenomeXml] = false;

            // Read in the number of batches between population logging
            _populationLoggingBatchInterval = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationLoggingBatchInterval");

            // Initialize the initialization algorithm
            _mazeNavigationInitializer = new FitnessCoevolutionMazeNavigationInitializer();

            // Setup initialization algorithm
            _mazeNavigationInitializer.SetAlgorithmParameters(
                xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement, AnnInputCount,
                AnnOutputCount, _numAgentSuccessCriteria, _numAgentFailedCriteria);

            // Pass in maze experiment specific parameters 
            // (note that a new maze structure is created here for the sole purpose of extracting the maze dimensions and calculating max distance to target)
            _mazeNavigationInitializer.SetEnvironmentParameters(_maxTimesteps, _minSuccessDistance,
                new MazeDecoder(_mazeHeight, _mazeWidth, _mazeScaleMultiplier).Decode(
                    new MazeGenomeFactory(MazeGenomeParameters).CreateGenome(0)));
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
            IGenomeFactory<MazeGenome> mazeGenomeFactory = new MazeGenomeFactory(MazeGenomeParameters);

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

            // Instantiate the internal initialization algorithm
            _mazeNavigationInitializer.InitializeAlgorithm(ParallelOptions, genomeList1,
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
                new QueueingNeatEvolutionAlgorithm<NeatGenome>(new NeatEvolutionAlgorithmParameters(), null,
                    NavigatorBatchSize, RunPhase.Primary, false, false, _navigatorEvolutionDataLogger,
                    _logFieldEnableMap);

            // Create the maze queueing evolution algorithm
            AbstractEvolutionAlgorithm<MazeGenome> mazeEvolutionAlgorithm =
                new QueueingNeatEvolutionAlgorithm<MazeGenome>(new NeatEvolutionAlgorithmParameters(), null,
                    MazeBatchSize, RunPhase.Primary, false, false, _mazeEvolutionDataLogger, _logFieldEnableMap);

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
                new CoevolutionAlgorithmContainer<NeatGenome, MazeGenome>(neatEvolutionAlgorithm, mazeEvolutionAlgorithm,
                    _navigatorPopulationGenomesDataLogger, _mazePopulationGenomesDataLogger,
                    PopulationGenomesFieldElements.PopulatePopulationGenomesFieldElementsEnableMap(),
                    _populationLoggingBatchInterval);

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
        private FitnessCoevolutionMazeNavigationInitializer _mazeNavigationInitializer;

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
        ///     Dictionary which indicates logger fields to be enabled/disabled.
        /// </summary>
        private IDictionary<FieldElement, bool> _logFieldEnableMap;

        /// <summary>
        ///     Controls the number of batches between population definitions (i.e. genome XML) being logged.
        /// </summary>
        private int? _populationLoggingBatchInterval;

        #endregion
    }
}