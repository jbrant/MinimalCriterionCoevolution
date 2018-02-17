#region

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using MCC_Domains.Utils;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;

#endregion

namespace MCC_Domains.MazeNavigation
{
    public abstract class BaseCoevolutionMazeNavigationExperiment : ICoevolutionExperiment
    {
        #region Public Properties

        /// <summary>
        ///     The name of the experiment.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        ///     The description of the experiment.
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        ///     The default (max) agent population size.
        /// </summary>
        public int AgentDefaultPopulationSize { get; protected set; }

        /// <summary>
        ///     The default (max) maze population size.
        /// </summary>
        public int MazeDefaultPopulationSize { get; protected set; }

        /// <summary>
        ///     The number of agent genomes needed for the initialization algorithm.
        /// </summary>
        public int AgentInitializationGenomeCount { get; protected set; }

        /// <summary>
        ///     The number of agent genomes in the agent seed population.
        /// </summary>
        public int AgentSeedGenomeCount { get; protected set; }

        /// <summary>
        ///     The number of maze genomes in the maze seed population.
        /// </summary>
        public int MazeSeedGenomeCount { get; protected set; }

        #endregion

        #region Protected Members

        /// <summary>
        ///     The NEAT genome parameters to use for the experiment.
        /// </summary>
        protected NeatGenomeParameters NeatGenomeParameters;

        /// <summary>
        ///     The maze genome parameters to use for the experiment.
        /// </summary>
        protected MazeGenomeParameters MazeGenomeParameters;

        /// <summary>
        ///     The number of neural network inputs.
        /// </summary>
        protected const int AnnInputCount = 10;

        /// <summary>
        ///     The number of neural network outputs.
        /// </summary>
        protected const int AnnOutputCount = 2;

        /// <summary>
        ///     The number of species in the agent queue.
        /// </summary>
        protected int AgentNumSpecies = 1;

        /// <summary>
        ///     The number of species in the maze queue.
        /// </summary>
        protected int MazeNumSpecies = 1;

        /// <summary>
        ///     The activation scheme (i.e. cyclic or acyclic).
        /// </summary>
        protected NetworkActivationScheme ActivationScheme;

        /// <summary>
        ///     Switches between synchronous and asynchronous execution (with user-defined number of threads).
        /// </summary>
        protected ParallelOptions ParallelOptions;

        /// <summary>
        ///     Dictates whether genome XML should be serialized and logged.
        /// </summary>
        protected bool SerializeGenomeToXml;

        /// <summary>
        ///     The number of maze navigators to be evaluated in a single evaluation "batch".
        /// </summary>
        protected int NavigatorBatchSize;

        /// <summary>
        ///     The number of mazes to be evaluated in a single evaluation "batch".
        /// </summary>
        protected int MazeBatchSize;

        /// <summary>
        ///     The factory used for producing behavior characterizations.
        /// </summary>
        protected IBehaviorCharacterizationFactory BehaviorCharacterizationFactory;

        /// <summary>
        ///     The maximum number of evaluations allowed (optional).
        /// </summary>
        protected ulong? MaxEvaluations;

        /// <summary>
        ///     The maximum number of generations allowed (optional).
        /// </summary>
        protected int? MaxGenerations;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Creates a new genome factory for maze navigator agents.
        /// </summary>
        /// <returns>The constructed agent genome factory.</returns>
        public IGenomeFactory<NeatGenome> CreateAgentGenomeFactory()
        {
            return new NeatGenomeFactory(AnnInputCount, AnnOutputCount, NeatGenomeParameters);
        }

        /// <summary>
        ///     Creates a new genome factory for mazes.
        /// </summary>
        /// <returns>The constructed maze genome factory.</returns>
        public IGenomeFactory<MazeGenome> CreateMazeGenomeFactory()
        {
            return new MazeGenomeFactory(MazeGenomeParameters);
        }

        /// <summary>
        ///     Save a population of agent genomes to an XmlWriter.
        /// </summary>
        public void SaveAgentPopulation(XmlWriter xw, IList<NeatGenome> agentGenomeList)
        {
            NeatGenomeXmlIO.WriteComplete(xw, agentGenomeList, false);
        }

        /// <summary>
        ///     Save a population of maze genomes to an XmlWriter.
        /// </summary>
        public void SaveMazePopulation(XmlWriter xw, IList<MazeGenome> mazeGenomeList)
        {
            MazeGenomeXmlIO.WriteComplete(xw, mazeGenomeList);
        }

        /// <summary>
        ///     Initializes the coevolution maze navigation experiment by reading in all of the configuration parameters and
        ///     setting up the bootstrapping/initialization algorithm.
        /// </summary>
        /// <param name="name">The name of the experiment.</param>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="population1EvolutionLogger">The navigator evolution data logger.</param>
        /// <param name="population1PopulationLogger">The navigator population logger.</param>
        /// <param name="population1GenomeLogger">The navigator genome logger.</param>
        /// <param name="population2EvolutionLogger">The maze evolution data logger.</param>
        /// <param name="population2PopulationLogger">The maze population logger.</param>
        /// <param name="population2GenomeLogger">The maze genome logger.</param>
        public virtual void Initialize(string name, XmlElement xmlConfig,
            IDataLogger population1EvolutionLogger = null, IDataLogger population1PopulationLogger = null,
            IDataLogger population1GenomeLogger = null, IDataLogger population2EvolutionLogger = null,
            IDataLogger population2PopulationLogger = null, IDataLogger population2GenomeLogger = null)
        {
            // Set boiler plate properties
            Name = name;
            Description = XmlUtils.GetValueAsString(xmlConfig, "Description");
            ActivationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            ParallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);
            SerializeGenomeToXml = XmlUtils.TryGetValueAsBool(xmlConfig, "DecodeGenomesToXml") ?? false;

            // Set the genome parameters
            NeatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);
            NeatGenomeParameters.FeedforwardOnly = ActivationScheme.AcyclicNetwork;
            MazeGenomeParameters = ExperimentUtils.ReadMazeGenomeParameters(xmlConfig);

            // Configure evolutionary algorithm parameters
            AgentDefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "AgentPopulationSize");
            MazeDefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "MazePopulationSize");
            AgentSeedGenomeCount = XmlUtils.GetValueAsInt(xmlConfig, "AgentSeedGenomeCount");
            MazeSeedGenomeCount = XmlUtils.GetValueAsInt(xmlConfig, "MazeSeedGenomeCount");
            AgentNumSpecies = XmlUtils.GetValueAsInt(xmlConfig, "AgentNumSpecies");
            MazeNumSpecies = XmlUtils.GetValueAsInt(xmlConfig, "MazeNumSpecies");
            BehaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(xmlConfig,
                "BehaviorConfig");
            NavigatorBatchSize = XmlUtils.GetValueAsInt(xmlConfig, "NavigatorOffspringBatchSize");
            MazeBatchSize = XmlUtils.GetValueAsInt(xmlConfig, "MazeOffspringBatchSize");

            // Set run-time bounding parameters
            MaxGenerations = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxGenerations");
            MaxEvaluations = XmlUtils.TryGetValueAsULong(xmlConfig, "MaxEvaluations");
        }

        /// <summary>
        ///     Zero argument wrapper method for instantiating the coveolution algorithm container.  This uses default agent and
        ///     maze population sizes as the only configuration parameters.
        /// </summary>
        /// <returns>The instantiated coevolution algorithm container.</returns>
        public abstract ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer();

        /// <summary>
        ///     Creates the coevolution algorithm container using the given agent and maze population sizes.
        /// </summary>
        /// <param name="populationSize1">The agent population size.</param>
        /// <param name="populationSize2">The maze population size.</param>
        /// <returns>The instantiated coevolution algorithm container.</returns>
        public abstract ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer(
            int populationSize1, int populationSize2);

        /// <summary>
        ///     Creates the evolution algorithm container using the given factories and genome lists.
        /// </summary>
        /// <param name="genomeFactory1">The agent genome factory.</param>
        /// <param name="genomeFactory2">The maze genome factory.</param>
        /// <param name="genomeList1">The agent genome list.</param>
        /// <param name="genomeList2">The maze genome list.</param>
        /// <returns>The instantiated coevolution algorithm container.</returns>
        public abstract ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer(
            IGenomeFactory<NeatGenome> genomeFactory1,
            IGenomeFactory<MazeGenome> genomeFactory2, List<NeatGenome> genomeList1, List<MazeGenome> genomeList2);

        #endregion
    }
}