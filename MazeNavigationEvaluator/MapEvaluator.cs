#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using ExperimentEntities;
using MazeExperimentSupportLib;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Maze;
using SharpNeat.Decoders.Neat;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeNavigationEvaluator
{
    /// <summary>
    ///     Sets up and evaluates the combination all of the given mazes and maze navigators, running a separate trial for each
    ///     distinct combination and recording the results for subsuquent logging and image generation.
    /// </summary>
    public class MapEvaluator
    {
        #region Properties

        /// <summary>
        ///     The individual maze/navigator evaluations that need to be simulated and evaluated.
        /// </summary>
        public IList<MazeNavigatorEvaluationUnit> EvaluationUnits { get; }

        #endregion

        #region Private members

        /// <summary>
        ///     The decoder for the navigator (agent) genomes.
        /// </summary>
        private readonly NeatGenomeDecoder _agentDecoder;

        /// <summary>
        ///     The parameters controlling the experiment configuration and execution.
        /// </summary>
        private readonly ExperimentParameters _experimentParameters;

        /// <summary>
        ///     The decoder for the maze genomes.
        /// </summary>
        private readonly MazeDecoder _mazeDecoder;

        /// <summary>
        ///     The factory used for constructing maze genomes.
        /// </summary>
        private readonly MazeGenomeFactory _mazeGenomeFactory;

        /// <summary>
        ///     The factory used for constructing navigator (agent) genomes.
        /// </summary>
        private readonly NeatGenomeFactory _neatGenomeFactory;

        /// <summary>
        ///     List of distinct agent IDs that have been submitted for trajectory analysis (used during aggregate run analysis
        ///     wherein we don't want to duplicate agents every batch).
        /// </summary>
        private readonly IList<int> _agentGenomeIds;

        /// <summary>
        ///     Map of maze IDs and their associated structural encoding.  This is a distinct set of mazes that's intended to
        ///     persist throughout the analysis of a run.
        /// </summary>
        private readonly IDictionary<int, MazeStructure> _mazeIdStructureMap;

        #endregion

        #region Public methods

        /// <summary>
        ///     The map evaluator constructor.
        /// </summary>
        /// <param name="experimentParameters">The experiment definition and control parameters.</param>
        /// <param name="agentInputNeuronCount">The number of input neurons in the agent neural controller.</param>
        /// <param name="agentOutputNeuronCount">The number of output neurons in the agent neural controller.</param>
        public MapEvaluator(ExperimentParameters experimentParameters, int agentInputNeuronCount,
            int agentOutputNeuronCount)
        {
            // Create the NEAT genome (agent) decoder - acyclic activation is always used
            _agentDecoder = new NeatGenomeDecoder(NetworkActivationScheme.CreateAcyclicScheme());

            // Create the maze decoder
            _mazeDecoder = new MazeDecoder(experimentParameters.MazeScaleMultiplier);

            // Initialize evaluation units
            EvaluationUnits = new List<MazeNavigatorEvaluationUnit>();

            // Create maze factory with default dimensions (NEAT factory will be set later based on structure of first
            // genome encountered)
            _mazeGenomeFactory = new MazeGenomeFactory(experimentParameters.MazeHeight, experimentParameters.MazeWidth,
                experimentParameters.MazeQuadrantHeight, experimentParameters.MazeQuadrantWidth);
            _neatGenomeFactory = new NeatGenomeFactory(agentInputNeuronCount, agentOutputNeuronCount);

            // Set experiment parameters
            _experimentParameters = experimentParameters;

            // Create new agent ID list and maze ID/structure map
            _agentGenomeIds = new List<int>();
            _mazeIdStructureMap = new Dictionary<int, MazeStructure>();
        }

        /// <summary>
        ///     Preps for running the maze navigation simulations by decoding the given maze/navigator combinations genomes.  These
        ///     are presumably combinations that were determined to be successful in solving the respective maze.
        /// </summary>
        /// <param name="navigationCombos">The combinations of mazes and navigators.</param>
        public void Initialize(
            IEnumerable<Tuple<MCCExperimentMazeGenome, MCCExperimentNavigatorGenome>> navigationCombos)
        {
            foreach (var navigationCombo in navigationCombos)
            {
                MazeGenome mazeGenome;
                NeatGenome navigatorGenome;

                // Deserialize the maze XML into a maze genome
                using (var xmlReader = XmlReader.Create(new StringReader(navigationCombo.Item1.GenomeXml)))
                {
                    mazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, _mazeGenomeFactory);
                }

                // Deserialize the navigator XML into a NEAT genome
                using (var xmlReader = XmlReader.Create(new StringReader(navigationCombo.Item2.GenomeXml)))
                {
                    navigatorGenome = NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false, _neatGenomeFactory);
                }

                // Decode to maze and navigator phenomes and add to the evaluation units list
                EvaluationUnits.Add(new MazeNavigatorEvaluationUnit(_mazeDecoder.Decode(mazeGenome),
                    _agentDecoder.Decode(navigatorGenome), navigationCombo.Item1.GenomeID,
                    navigationCombo.Item2.GenomeID));
            }
        }

        /// <summary>
        ///     Preps for running the maze navigation simulations by constructing all combinations of mazes/navigators that need to
        ///     be evaluated in tandem.
        /// </summary>
        /// <param name="mazes">The mazes that were in the maze queue during the given batch.</param>
        /// <param name="navigators">The navigators that were in the navigator queue during the given batch.</param>
        public void Initialize(IEnumerable<MCCExperimentMazeGenome> mazes,
            IList<MCCExperimentNavigatorGenome> navigators)
        {
            IList<NeatGenome> cachedAgents = new List<NeatGenome>(navigators.Count);

            foreach (var serializedMaze in mazes)
            {
                MazeGenome mazeGenome;

                // Deserialize the maze XML into a maze genome
                using (var xmlReader = XmlReader.Create(new StringReader(serializedMaze.GenomeXml)))
                {
                    mazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, _mazeGenomeFactory);
                }

                // If no agents have been deserialized yet, we need to parse the XML and load the genotype list
                if (cachedAgents.Count < 1)
                {
                    // Go through every serialized navigator genome, deserialize it, and use it with the maze to build
                    // a new evaluation unit
                    foreach (var serializedNavigator in navigators)
                    {
                        NeatGenome agentGenome;

                        // Read in the current navigator agent genome
                        using (var xmlReader = XmlReader.Create(new StringReader(serializedNavigator.GenomeXml)))
                        {
                            agentGenome =
                                NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false, _neatGenomeFactory);
                        }

                        // Decode to maze and navigator phenomes and add to the evaluation units list
                        EvaluationUnits.Add(new MazeNavigatorEvaluationUnit(_mazeDecoder.Decode(mazeGenome),
                            _agentDecoder.Decode(agentGenome), serializedMaze.GenomeID, serializedNavigator.GenomeID));

                        // Also add to the list of cached genomes
                        cachedAgents.Add(agentGenome);
                    }
                }
                // Otherwise, skip the deserialization process
                else
                {
                    // Add each genome with the current maze to create new evaluation units
                    foreach (var cachedAgent in cachedAgents)
                    {
                        EvaluationUnits.Add(new MazeNavigatorEvaluationUnit(_mazeDecoder.Decode(mazeGenome),
                            _agentDecoder.Decode(cachedAgent), serializedMaze.GenomeID, (int) cachedAgent.Id));
                    }
                }
            }
        }

        /// <summary>
        ///     Preps for generating the maze structures by decoding the given list of maze genomes.  This is in support of
        ///     post-hoc analyses that doesn't consider navigator trajectories.
        /// </summary>
        /// <param name="mazes">The mazes that were in the maze queue during the given batch.</param>
        public void Initialize(IEnumerable<MCCExperimentMazeGenome> mazes)
        {
            foreach (var serializedMaze in mazes)
            {
                MazeGenome mazeGenome;

                // Deserialize the maze XML into a maze genome
                using (var xmlReader = XmlReader.Create(new StringReader(serializedMaze.GenomeXml)))
                {
                    mazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, _mazeGenomeFactory);
                }

                // Decode to maze phenome and add to the maze/id map
                _mazeIdStructureMap.Add(serializedMaze.GenomeID, _mazeDecoder.Decode(mazeGenome));
            }
        }

        /// <summary>
        ///     Preps for evaluation via decoding agents to their phenome representation and creating a separate evaluation unit
        ///     for each non-evaluated agent/maze combination.
        /// </summary>
        /// <param name="agents">The agents that are in the queue at the given time.</param>
        public void Initialize(IEnumerable<MCCExperimentNavigatorGenome> agents)
        {
            // Build a separate evaluation unit for each agent/maze combination, but only consider those agents
            // who have not already been evaluated
            foreach (
                var serializedAgent in
                agents.Where(agentGenome => _agentGenomeIds.Contains(agentGenome.GenomeID) == false))
            {
                NeatGenome agentGenome;

                // Read in the current navigator agent genome
                using (var xmlReader = XmlReader.Create(new StringReader(serializedAgent.GenomeXml)))
                {
                    agentGenome = NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false, _neatGenomeFactory);
                }

                // Iterate through every maze and add an evaluation unit for that maze and the agent
                foreach (var maze in _mazeIdStructureMap)
                {
                    // Only need to decode the agent genome as the mazes have already been decoded
                    EvaluationUnits.Add(new MazeNavigatorEvaluationUnit(maze.Value, _agentDecoder.Decode(agentGenome),
                        maze.Key, serializedAgent.GenomeID));
                }

                // Add the agent genome ID to the list of agents that have been evaluated
                _agentGenomeIds.Add(serializedAgent.GenomeID);
            }
        }

        /// <summary>
        ///     Executes a trial/simulation for all evaluation units (i.e. maze/navigator combinations).
        /// </summary>
        public void RunTrajectoryEvaluations()
        {
            // Check integrtiy of evaluation units
            foreach (var evaluationUnit in EvaluationUnits.Where(evaluationUnit =>
                evaluationUnit.AgentPhenome == null || evaluationUnit.MazePhenome == null))
            {
                throw new Exception(
                    $"Malformed evaluation unit for agent {evaluationUnit.AgentId} and maze {evaluationUnit.MazeId} - each evaluation unit must contain a decoded maze and agent phenome.");
            }

            // Execute all maze/navigator simulations in parallel
            Parallel.ForEach(EvaluationUnits,
                delegate(MazeNavigatorEvaluationUnit evaluationUnit)
                {
                    EvaluationHandler.EvaluateMazeNavigatorUnit(evaluationUnit, _experimentParameters);
                });
        }

        #endregion
    }
}