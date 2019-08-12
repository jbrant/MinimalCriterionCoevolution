#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MCC_Domains.MazeNavigation.Bootstrappers;
using MCC_Domains.Utils;
using Redzen.Random;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Maze;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Utility;

#endregion

namespace MCC_Domains.MazeNavigation
{
    /// <inheritdoc />
    /// <summary>
    ///     Base class for maze navigation MCC experiments.
    /// </summary>
    public abstract class BaseMCCMazeNavigationExperiment : IMCCExperiment
    {
        #region Public Properties

        /// <inheritdoc />
        /// <summary>
        ///     The name of the experiment.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     The description of the experiment.
        /// </summary>
        public string Description { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     The default (max) agent population size.
        /// </summary>
        public int AgentDefaultPopulationSize { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     The default (max) maze population size.
        /// </summary>
        public int MazeDefaultPopulationSize { get; private set; }

        /// <summary>
        ///     The number of agent genomes needed for the initialization algorithm.
        /// </summary>
        public int AgentInitializationGenomeCount { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        ///     The number of agent genomes in the agent seed population.
        /// </summary>
        public int AgentSeedGenomeCount { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     The number of maze genomes in the maze seed population.
        /// </summary>
        public int MazeSeedGenomeCount { get; private set; }

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

        /// <summary>
        ///     The minimum distance to the target required in order to have "solved" the maze.
        /// </summary>
        protected int MinSuccessDistance;

        /// <summary>
        ///     The minimum number of mazes that the agent under evaluation must solve in order to meet the minimal criteria.
        /// </summary>
        protected int NumMazeSuccessCriteria;

        /// <summary>
        ///     The minimum number of agents that must solve the maze under evaluation in order to meet this portion of the minimal
        ///     criteria.
        /// </summary>
        protected int NumAgentSuccessCriteria;

        /// <summary>
        ///     The minimum number of agents that must fail to solve the maze under evaluation in order to meet this portion of the
        ///     minimal criteria.
        /// </summary>
        protected int NumAgentFailedCriteria;

        /// <summary>
        ///     The width of the evolved maze environments.
        /// </summary>
        protected int MazeHeight;

        /// <summary>
        ///     The height of the evolved maze environments.
        /// </summary>
        protected int MazeWidth;

        /// <summary>
        ///     The maximum height of maze quadrants.
        /// </summary>
        protected int MazeQuadrantHeight;

        /// <summary>
        ///     The maximum width of maze quadrants.
        /// </summary>
        protected int MazeQuadrantWidth;

        /// <summary>
        ///     The multiplier for scaling the maze to larger sizes.
        /// </summary>
        protected int MazeScaleMultiplier;

        #endregion

        #region Instance Variables

        /// <summary>
        ///     Initialization algorithm for producing an initial population with the requisite number of viable genomes.
        /// </summary>
        private MCCMazeNavigationInitializer _mazeNavigationInitializer;

        /// <summary>
        ///     The maximum number of evaluations allowed during the initialization phase before it is restarted.
        /// </summary>
        private uint? _maxInitializationEvaluations;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        /// <summary>
        ///     Creates a new genome factory for maze navigator agents.
        /// </summary>
        /// <returns>The constructed agent genome factory.</returns>
        public IGenomeFactory<NeatGenome> CreateAgentGenomeFactory()
        {
            return new NeatGenomeFactory(AnnInputCount, AnnOutputCount, NeatGenomeParameters);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates a new genome factory for mazes.
        /// </summary>
        /// <returns>The constructed maze genome factory.</returns>
        public IGenomeFactory<MazeGenome> CreateMazeGenomeFactory()
        {
            return new MazeGenomeFactory(MazeGenomeParameters, MazeHeight, MazeWidth, MazeQuadrantHeight,
                MazeQuadrantWidth);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Save a population of agent genomes to an XmlWriter.
        /// </summary>
        public void SaveAgentPopulation(XmlWriter xw, IList<NeatGenome> agentGenomeList)
        {
            NeatGenomeXmlIO.WriteComplete(xw, agentGenomeList, false);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Save a population of maze genomes to an XmlWriter.
        /// </summary>
        public void SaveMazePopulation(XmlWriter xw, IList<MazeGenome> mazeGenomeList)
        {
            MazeGenomeXmlIO.WriteComplete(xw, mazeGenomeList);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes the MCC maze navigation experiment by reading in all of the configuration parameters and
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

            // Set experiment-specific parameters
            MinSuccessDistance = XmlUtils.GetValueAsInt(xmlConfig, "MinSuccessDistance");
            MazeHeight = XmlUtils.GetValueAsInt(xmlConfig, "MazeHeight");
            MazeWidth = XmlUtils.GetValueAsInt(xmlConfig, "MazeWidth");
            MazeQuadrantHeight = XmlUtils.GetValueAsInt(xmlConfig, "MazeQuadrantHeight");
            MazeQuadrantWidth = XmlUtils.GetValueAsInt(xmlConfig, "MazeQuadrantWidth");
            MazeScaleMultiplier = XmlUtils.GetValueAsInt(xmlConfig, "MazeScaleMultiplier");

            // Get success/failure criteria constraints
            NumMazeSuccessCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumMazesSolvedCriteria");
            NumAgentSuccessCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumAgentsSolvedCriteria");
            NumAgentFailedCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumAgentsFailedCriteria");

            // Read in the maximum number of initialization evaluations
            _maxInitializationEvaluations = XmlUtils.GetValueAsUInt(xmlConfig, "MaxInitializationEvaluations");

            // Initialize the initialization algorithm
            _mazeNavigationInitializer =
                ExperimentUtils.DetermineMCCInitializer(
                    xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement);

            // Setup initialization algorithm
            _mazeNavigationInitializer.SetAlgorithmParameters(
                xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement,
                ActivationScheme.AcyclicNetwork, NumAgentSuccessCriteria, 0);

            // Pass in maze experiment specific parameters 
            // (note that a new maze structure is created here for the sole purpose of extracting the maze dimensions and calculating max distance to target)
            _mazeNavigationInitializer.SetEnvironmentParameters(MinSuccessDistance,
                new MazeDecoder(MazeScaleMultiplier).Decode(
                    new MazeGenomeFactory(MazeGenomeParameters, MazeHeight, MazeWidth, MazeQuadrantHeight,
                        MazeQuadrantWidth).CreateGenome(0)));

            // The size of the randomly generated agent genome pool from which to evolve agent bootstraps
            AgentInitializationGenomeCount = _mazeNavigationInitializer.PopulationSize;
        }

        /// <summary>
        ///     Evolves the requisite number of agents to meet the MC for each maze in the initial population. This is performed
        ///     using a non-MCC based algorithm (such as fitness or novelty search).
        /// </summary>
        /// <param name="agentPopulation">The agents (NEAT genomes) in the initial, randomly generated population.</param>
        /// <param name="mazePopulation">The mazes in the initial population (either randomly generated or read from a file).</param>
        /// <param name="agentGenomeFactory">The factory class for producing agent (NEAT) genomes.</param>
        /// <param name="numAgents">The number of seed agents to evolve.</param>
        /// <returns>
        ///     The list of viable agents, each of whom is able to solve at least one of the initial mazes and, in totality,
        ///     meet the MC for solving each of the mazes.
        /// </returns>
        protected List<NeatGenome> EvolveSeedAgents(List<NeatGenome> agentPopulation, List<MazeGenome> mazePopulation,
            IGenomeFactory<NeatGenome> agentGenomeFactory, int numAgents)
        {
            var seedAgentPopulation = new List<NeatGenome>();

            // Create maze decoder to decode initialization mazes
            var mazeDecoder = new MazeDecoder(MazeScaleMultiplier);

            // Loop through every maze and evolve the requisite number of viable genomes that solve it
            for (var idx = 0; idx < mazePopulation.Count; idx++)
            {
                Console.WriteLine(@"Evolving viable agents for maze population index {0} and maze ID {1}", idx,
                    mazePopulation[idx].Id);

                // Evolve the number of agents required to meet the success MC for the current maze
                var viableMazeAgents = _mazeNavigationInitializer.EvolveViableAgents(agentGenomeFactory,
                    agentPopulation.ToList(), mazeDecoder.Decode(mazePopulation[idx]), _maxInitializationEvaluations,
                    ActivationScheme, ParallelOptions);

                // Add the viable agent genomes who solve the current maze (but avoid adding duplicates, as identified by the genome ID)
                // Note that it's fine to have multiple mazes solved by the same agent, so in this case, we'll leave the agent
                // in the pool of seed agent genomes
                foreach (
                    var viableMazeAgent in
                    viableMazeAgents.Where(
                        viableMazeAgent =>
                            seedAgentPopulation.Select(sap => sap.Id).Contains(viableMazeAgent.Id) == false))
                {
                    seedAgentPopulation.Add(viableMazeAgent);
                }
            }

            // If we still lack the genomes to fill out seed agent gnome count while still satisfying the maze MC,
            // iteratively pick a random maze and evolve agents on that maze until we reach the requisite number
            while (seedAgentPopulation.ToList().Count < numAgents)
            {
                var rndMazePicker = RandomDefaults.CreateRandomSource();

                // Pick a random maze on which to evolve agent(s)
                var mazeGenome = mazePopulation[rndMazePicker.Next(mazePopulation.Count - 1)];

                Console.WriteLine(
                    @"Continuing viable agent evolution on maze {0}, with {1} of {2} required agents in place",
                    mazeGenome.Id, seedAgentPopulation.Count, numAgents);

                // Evolve the number of agents required to meet the success MC for the maze
                var viableMazeAgents = _mazeNavigationInitializer.EvolveViableAgents(agentGenomeFactory,
                    agentPopulation.ToList(), mazeDecoder.Decode(mazeGenome), _maxInitializationEvaluations,
                    ActivationScheme, ParallelOptions);

                // Iterate through each viable agent and remove them if they've already solved a maze or add them to the list
                // of viable agents if they have not
                foreach (var viableMazeAgent in viableMazeAgents)
                {
                    // If they agent has already solved maze and is in the list of viable agents, remove that agent
                    // from the pool of seed genomes (this is done because here, we're interested in getting unique
                    // agents and want to avoid an endless loop wherein the same viable agents are returned)
                    if (seedAgentPopulation.Select(sap => sap.Id).Contains(viableMazeAgent.Id))
                    {
                        agentPopulation.Remove(viableMazeAgent);
                    }
                    // Otherwise, add that agent to the list of viable agents
                    else
                    {
                        seedAgentPopulation.Add(viableMazeAgent);
                    }
                }
            }

            return seedAgentPopulation;
        }

        /// <summary>
        ///     Ensure that the pre-evolved agent population facilitates the satisfaction of both their MC and the MC of the seed
        ///     mazes.
        /// </summary>
        /// <param name="agentPopulation">The pre-evolved agent population.</param>
        /// <param name="mazePopulation">The maze population.</param>
        /// <param name="agentEvaluator">The agent evaluator.</param>
        /// <param name="mazeEvaluator">The maze evaluator.</param>
        /// <returns>
        ///     Boolean flag indicating whether the seed agents meet their MC and facilitate satisfaction of the maze
        ///     population MC.
        /// </returns>
        protected static bool VerifyPreevolvedSeedAgents(List<NeatGenome> agentPopulation,
            List<MazeGenome> mazePopulation, IGenomeEvaluator<NeatGenome> agentEvaluator,
            IGenomeEvaluator<MazeGenome> mazeEvaluator)
        {
            // Update agent and maze evaluators such that seed agents will be evaluated against mazes and vice versa
            agentEvaluator.UpdateEvaluationBaseline(mazeEvaluator.DecodeGenomes(mazePopulation));
            mazeEvaluator.UpdateEvaluationBaseline(agentEvaluator.DecodeGenomes(agentPopulation));

            // Run MC evaluation for both populations
            agentEvaluator.Evaluate(agentPopulation, 0);
            mazeEvaluator.Evaluate(mazePopulation, 0);

            // In order to be a valid seed, the agents must facilitate the satisfaction of both populations' MC
            // 1. The agents must all satisfy their MC with respect to the mazes
            // 2. The agent population must facilitate satisfaction of the maze MC (e.g. to be solve by at least one agent)
            return agentPopulation.All(g => g.EvaluationInfo.IsViable) &&
                   mazePopulation.All(g => g.EvaluationInfo.IsViable);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Zero argument wrapper method for instantiating the coveolution algorithm container.  This uses default agent and
        ///     maze population sizes as the only configuration parameters.
        /// </summary>
        /// <returns>The instantiated MCC algorithm container.</returns>
        public abstract IMCCAlgorithmContainer<NeatGenome, MazeGenome> CreateMCCAlgorithmContainer();

        /// <inheritdoc />
        /// <summary>
        ///     Creates the MCC algorithm container using the given agent and maze population sizes.
        /// </summary>
        /// <param name="populationSize1">The agent population size.</param>
        /// <param name="populationSize2">The maze population size.</param>
        /// <returns>The instantiated MCC algorithm container.</returns>
        public abstract IMCCAlgorithmContainer<NeatGenome, MazeGenome> CreateMCCAlgorithmContainer(
            int populationSize1, int populationSize2);

        /// <inheritdoc />
        /// <summary>
        ///     Creates the evolution algorithm container using the given factories and genome lists.
        /// </summary>
        /// <param name="genomeFactory1">The agent genome factory.</param>
        /// <param name="genomeFactory2">The maze genome factory.</param>
        /// <param name="genomeList1">The agent genome list.</param>
        /// <param name="genomeList2">The maze genome list.</param>
        /// <param name="isAgentListPreevolved">
        ///     Indicates whether the given agents have been pre-evolved to satisfy the MC with
        ///     respect to the maze population.
        /// </param>
        /// <returns>The instantiated MCC algorithm container.</returns>
        public abstract IMCCAlgorithmContainer<NeatGenome, MazeGenome> CreateMCCAlgorithmContainer(
            IGenomeFactory<NeatGenome> genomeFactory1,
            IGenomeFactory<MazeGenome> genomeFactory2, List<NeatGenome> genomeList1, List<MazeGenome> genomeList2,
            bool isAgentListPreevolved);

        #endregion
    }
}