#region

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Maze;
using SharpNeat.Decoders.Neat;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment
{
    public class CoevolutionMazeNavigationMCSExperiment : ICoevolutionExperiment
    {
        #region Public Methods

        public void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger = null,
            IDataLogger evaluationDataLogger = null)
        {
            // Set boiler plate properties
            Name = name;
            Description = XmlUtils.GetValueAsString(xmlConfig, "Description");
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            _parallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);
            _serializeGenomeToXml = XmlUtils.TryGetValueAsBool(xmlConfig, "DecodeGenomesToXml") ?? false;

            // Set the genome parameters
            //_neatEvolutionAlgorithmParameters = ExperimentUtils.ReadNeatEvolutionAlgorithmParameters(xmlConfig);
            _neatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);
            _mazeGenomeParameters = ExperimentUtils.ReadMazeGenomeParameters(xmlConfig);

            // Configure evolutionary algorithm parameters
            DefaultPopulationSize1 = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationSize1") ?? default(int);
            DefaultPopulationSize2 = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationSize2") ?? default(int);
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(xmlConfig,
                "BehaviorConfig");
            _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");

            // Set run-time bounding parameters
            _maxGenerations = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxGenerations");
            _maxEvaluations = XmlUtils.TryGetValueAsULong(xmlConfig, "MaxEvaluations");

            // Set experiment-specific parameters
            _maxTimesteps = XmlUtils.GetValueAsInt(xmlConfig, "MaxTimesteps");
            _minSuccessDistance = XmlUtils.GetValueAsInt(xmlConfig, "MinSuccessDistance");

            // Get success/failure criteria constraints
            _numMazeSuccessCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumMazesSolvedCriteria");
            _numAgentSuccessCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumAgentsSolvedCriteria");
            _numAgentFailedCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumAgentsFailedCriteria");

            // TODO: Setup logging here
        }

        public ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer()
        {
            return CreateCoevolutionAlgorithmContainer(DefaultPopulationSize1, DefaultPopulationSize2);
        }

        public ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer(
            int populationSize1, int populationSize2)
        {
            // Create a genome factory for the NEAT genomes
            IGenomeFactory<NeatGenome> neatGenomeFactory = new NeatGenomeFactory(_annInputCount, _annOutputCount,
                _neatGenomeParameters);

            // Create a genome factory for the maze genomes
            IGenomeFactory<MazeGenome> mazeGenomeFactory = new MazeGenomeFactory();

            // Create an initial population of maze navigators
            List<NeatGenome> neatGenomeList = neatGenomeFactory.CreateGenomeList(populationSize1, 0);

            // Create an initial population of mazes
            List<MazeGenome> mazeGenomeList = mazeGenomeFactory.CreateGenomeList(populationSize2, 0);

            // Create the evolution algorithm container
            return CreateCoevolutionAlgorithmContainer(neatGenomeFactory, mazeGenomeFactory, neatGenomeList,
                mazeGenomeList);
        }

        public ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer(
            IGenomeFactory<NeatGenome> genomeFactory1,
            IGenomeFactory<MazeGenome> genomeFactory2, List<NeatGenome> genomeList1, List<MazeGenome> genomeList2)
        {
            // Create the NEAT (i.e. navigator) queueing evolution algorithm
            AbstractEvolutionAlgorithm<NeatGenome> neatEvolutionAlgorithm =
                new QueueingNeatEvolutionAlgorithm<NeatGenome>(new NeatEvolutionAlgorithmParameters(), null, _batchSize);

            // Create the maze queueing evolution algorithm
            AbstractEvolutionAlgorithm<MazeGenome> mazeEvolutionAlgorithm =
                new QueueingNeatEvolutionAlgorithm<MazeGenome>(new NeatEvolutionAlgorithmParameters(), null, _batchSize);

            // Create the maze phenome evaluator
            IPhenomeEvaluator<MazeStructure, BehaviorInfo> mazeEvaluator = new MazeEnvironmentMCSEvaluator(
                _maxTimesteps,
                _minSuccessDistance, _behaviorCharacterizationFactory, _numAgentSuccessCriteria, _numAgentFailedCriteria);

            // Create navigator phenome evaluator
            IPhenomeEvaluator<IBlackBox, BehaviorInfo> navigatorEvaluator = new MazeNavigatorMCSEvaluator(
                _maxTimesteps, _minSuccessDistance, _behaviorCharacterizationFactory, _numMazeSuccessCriteria);

            // Create maze genome decoder
            IGenomeDecoder<MazeGenome, MazeStructure> mazeGenomeDecoder = new MazeDecoder(_mazeHeight, _mazeWidth);

            // Create navigator genome decoder
            IGenomeDecoder<NeatGenome, IBlackBox> navigatorGenomeDecoder = new NeatGenomeDecoder(_activationScheme);

            // Create the maze genome evaluator
            IGenomeEvaluator<MazeGenome> mazeFitnessEvaluator =
                new SerialGenomeBehaviorEvaluator<MazeGenome, MazeStructure>(mazeGenomeDecoder, mazeEvaluator,
                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch);

            // Create navigator genome evaluator
            IGenomeEvaluator<NeatGenome> navigatorFitnessEvaluator =
                new SerialGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(navigatorGenomeDecoder, navigatorEvaluator,
                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch);

            // Create the coevolution container
            ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> coevolutionAlgorithmContainer =
                new CoevolutionAlgorithmContainer<NeatGenome, MazeGenome>(neatEvolutionAlgorithm, mazeEvolutionAlgorithm);

            // Initialize the container and component algorithms
            coevolutionAlgorithmContainer.Initialize(navigatorFitnessEvaluator, genomeFactory1, genomeList1,
                mazeFitnessEvaluator, genomeFactory2, genomeList2, _maxGenerations, _maxEvaluations);

            return coevolutionAlgorithmContainer;
        }

        #endregion

        #region Public Properties

        public string Name { get; private set; }

        public string Description { get; private set; }

        public int DefaultPopulationSize1 { get; private set; }

        public int DefaultPopulationSize2 { get; private set; }

        #endregion

        #region Instance Variables

        /// <summary>
        ///     The activation scheme (i.e. cyclic or acyclic).
        /// </summary>
        private NetworkActivationScheme _activationScheme;

        /// <summary>
        ///     Switches between synchronous and asynchronous execution (with user-defined number of threads).
        /// </summary>
        private ParallelOptions _parallelOptions;

        /// <summary>
        ///     Dictates whether genome XML should be serialized and logged.
        /// </summary>
        private bool _serializeGenomeToXml;

        /// <summary>
        ///     The NEAT genome parameters to use for the experiment.
        /// </summary>
        private NeatGenomeParameters _neatGenomeParameters;

        /// <summary>
        ///     The maze genome parameters to use for the experiment.
        /// </summary>
        private MazeGenomeParameters _mazeGenomeParameters;

        /// <summary>
        ///     The number of neural network inputs.
        /// </summary>
        private int _annInputCount;

        /// <summary>
        ///     The number of neural network outputs.
        /// </summary>
        private int _annOutputCount;

        /// <summary>
        ///     The number of individuals to be evaluated in a single evaluation "batch".
        /// </summary>
        private int _batchSize;

        /// <summary>
        ///     The factory used for producing behavior characterizations.
        /// </summary>
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;

        /// <summary>
        ///     The maximum number of evaluations allowed (optional).
        /// </summary>
        private ulong? _maxEvaluations;

        /// <summary>
        ///     The maximum number of generations allowed (optional).
        /// </summary>
        private int? _maxGenerations;

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

        #endregion
    }
}