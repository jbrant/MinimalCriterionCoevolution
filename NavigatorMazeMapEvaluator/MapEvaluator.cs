#region

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using ExperimentEntities;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Maze;
using SharpNeat.Decoders.Neat;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;

#endregion

namespace NavigatorMazeMapEvaluator
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
            _mazeDecoder = new MazeDecoder(experimentParameters.MazeHeight, experimentParameters.MazeWidth,
                experimentParameters.MazeScaleMultiplier);

            // Initialize evaluation units
            EvaluationUnits = new List<MazeNavigatorEvaluationUnit>();

            // Create default maze factory (NEAT factory will be set later based on structure of first genome encountered)
            _mazeGenomeFactory = new MazeGenomeFactory();
            _neatGenomeFactory = new NeatGenomeFactory(agentInputNeuronCount, agentOutputNeuronCount);

            // Set experiment parameters
            _experimentParameters = experimentParameters;
        }

        /// <summary>
        ///     Preps for running the maze navigation simulations by constructing all combinations of mazes/navigators that need to
        ///     be evaluated in tandem.
        /// </summary>
        /// <param name="mazes">The mazes that were in the maze queue during the given batch.</param>
        /// <param name="navigators">The navigators that were in the navigator queue during the given batch.</param>
        public void Initialize(IList<CoevolutionMCSMazeExperimentGenome> mazes,
            IList<CoevolutionMCSNavigatorExperimentGenome> navigators)
        {
            IList<NeatGenome> cachedAgents = new List<NeatGenome>(navigators.Count);

            foreach (CoevolutionMCSMazeExperimentGenome serializedMaze in mazes)
            {
                MazeGenome mazeGenome;

                // Deserialize the maze XML into a maze genome
                using (XmlReader xmlReader = XmlReader.Create(new StringReader(serializedMaze.GenomeXml)))
                {
                    mazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, _mazeGenomeFactory);
                }

                // If no agents have been deserialized yet, we need to parse the XML and load the genotype list
                if (cachedAgents.Count < 1)
                {
                    // Go through every serialized navigator genome, deserialize it, and use it with the maze to build
                    // a new evaluation unit
                    foreach (CoevolutionMCSNavigatorExperimentGenome serializedNavigator in navigators)
                    {
                        NeatGenome agentGenome;

                        // Read in the current navigator agent genome
                        using (XmlReader xmlReader = XmlReader.Create(new StringReader(serializedNavigator.GenomeXml)))
                        {
                            agentGenome = NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false, _neatGenomeFactory);
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
                    foreach (NeatGenome cachedAgent in cachedAgents)
                    {
                        EvaluationUnits.Add(new MazeNavigatorEvaluationUnit(_mazeDecoder.Decode(mazeGenome),
                            _agentDecoder.Decode(cachedAgent), serializedMaze.GenomeID, (int) cachedAgent.Id));
                    }
                }
            }
        }

        /// <summary>
        ///     Executes a trial/simulation for all evaluation units (i.e. maze/navigator combinations).
        /// </summary>
        public void RunBatchEvaluation()
        {
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