using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MCC_Domains.BodyBrain.MCCExperiment;
using MCC_Domains.Utils;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.Decoders.Voxel;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Voxels;
using SharpNeat.SpeciationStrategies;

namespace MCC_Domains.BodyBrain
{
    public abstract class BodyBrainInitializer
    {
        #region Constructors

        /// <summary>
        ///     Enforces instantiation only through child classes.
        /// </summary>
        protected BodyBrainInitializer()
        {
        }
        
        #endregion

        #region Instance variables

        /// <summary>
        ///     The evolutionary algorithm supporting the initialization process.
        /// </summary>
        protected AbstractComplexifyingEvolutionAlgorithm<NeatGenome> InitializationEa;
        
        #endregion
        
        #region Protected members

        /// <summary>
        /// The unique name of the experiment being executed.
        /// </summary>
        protected string ExperimentName;

        /// <summary>
        /// The current run number of the given experiment.
        /// </summary>
        protected int Run;
        
        /// <summary>
        ///     The genome parameters specifically for the initialization algorithm (this is because the initialization algorithm
        ///     will likely have different algorithm parameters than the primary algorithm).  Note that only the parameters are
        ///     stored because they temporarily replace the parameters on an *existing* genome factory.  This allows transition
        ///     into the primary phase of the experiment by simply replacing the parameters, but keeping the rest of the factory
        ///     state (e.g. innovation IDs, gene IDs, etc.) intact.
        /// </summary>
        protected NeatGenomeParameters NeatGenomeParameters;

        /// <summary>
        ///     The parameters controlling the NEAT algorithm.
        /// </summary>
        protected EvolutionAlgorithmParameters EvolutionAlgorithmParameters;

        /// <summary>
        ///     The complexity regulation strategy.
        /// </summary>
        protected string ComplexityRegulationStrategyDefinition;

        /// <summary>
        ///     The complexity threshold.
        /// </summary>
        protected int? ComplexityThreshold;

        /// <summary>
        ///     The population size for the initialization algorithm.
        /// </summary>
        public int PopulationSize;

        /// <summary>
        ///     The minimum number of successful robot brains.
        /// </summary>
        protected int MinSuccessfulBrainCount;

        /// <summary>
        ///     The minimum ambulation distance that a brain must effect in the body to be considered successful.
        /// </summary>
        protected double MinAmbulationDistance;

        /// <summary>
        /// Collection of simulator configuration properties, including result output directories, configuration files and config file parsing information.
        /// </summary>
        protected SimulationProperties SimulationProperties;
        
        #endregion

        #region Public methods

        public virtual void SetAlgorithmParameters(XmlElement xmlConfig, string experimentName, int run, bool isAcyclic, int numSuccessfulBrains)
        {
            // Set experiment name and run
            ExperimentName = experimentName;
            Run = run;
            
            // Read NEAT genome parameters
            // Save off genome parameters specifically for the initialization algorithm 
            NeatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);
            NeatGenomeParameters.FeedforwardOnly = isAcyclic;

            // Read NEAT evolution parameters
            EvolutionAlgorithmParameters = ExperimentUtils.ReadNeatEvolutionAlgorithmParameters(xmlConfig);

            // Get complexity constraint parameters
            ComplexityRegulationStrategyDefinition = XmlUtils.TryGetValueAsString(xmlConfig,
                "ComplexityRegulationStrategy");
            ComplexityThreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");

            // Set the static population size
            PopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");

            // Set the minimum number of successful and unsuccessful brains
            MinSuccessfulBrainCount = numSuccessfulBrains;
        }

        /// <summary>
        ///     Sets configuration variables specific to body/brain simulation.
        /// </summary>
        /// <param name="minAmbulationDistance">
        ///     The minimum ambulation distance that a brain must effect in the body to be
        ///     considered successful.
        /// </param>
        /// <param name="simulationProperties">Collection of simulator configuration properties, including result output directories, configuration files and config file parsing information.</param>
        public void SetSimulationParameters(double minAmbulationDistance, SimulationProperties simulationProperties)
        {
            MinAmbulationDistance = minAmbulationDistance;
            SimulationProperties = simulationProperties;
        }

        public IEnumerable<NeatGenome> EvolveViableBrains(IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> seedBrainList, IGenomeDecoder<NeatGenome, VoxelBrain> brainGenomeDecoder, VoxelBody body,
            uint? maxInitializationEvals, NetworkActivationScheme activationScheme, ParallelOptions parallelOptions)
        {
            List<NeatGenome> viableMazeAgents;
            uint restartCount = 0;
            ulong initializationEvaluations;
            
            do
            {
                // Instantiate the internal initialization algorithm
                InitializeAlgorithm(parallelOptions, seedBrainList.ToList(), genomeFactory, brainGenomeDecoder, body,
                    0);

                // Run the initialization algorithm until the requested number of viable seed genomes are found
                viableMazeAgents = RunEvolution(out initializationEvaluations, maxInitializationEvals, restartCount);

                restartCount++;

                // Repeat if maximum allotted evaluations is exceeded
            } while (maxInitializationEvals != null && viableMazeAgents == null &&
                     initializationEvaluations > maxInitializationEvals);

            return viableMazeAgents;
        }

        #endregion

        #region Abstract methods

        /// <summary>
        ///     Configures and instantiates the initialization evolutionary algorithm.
        /// </summary>
        /// <param name="parallelOptions">Synchronous/Asynchronous execution settings.</param>
        /// <param name="brainGenomeList">The initial population of brain genomes.</param>
        /// <param name="brainGenomeFactory">The genome factory for brains initialized by the main evolution thread.</param>
        /// <param name="brainGenomeDecoder">The decoder to translate brain genomes into phenotypes.</param>
        /// <param name="body">The phenotypic description of the robot body.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        protected abstract void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> brainGenomeList,
            IGenomeFactory<NeatGenome> brainGenomeFactory, IGenomeDecoder<NeatGenome, VoxelBrain> brainGenomeDecoder,
            VoxelBody body, ulong startingEvaluations);
        
        /// <summary>
        ///     Runs the initialization algorithm until the specified number of viable genomes (i.e. genomes that meets the minimal
        ///     criteria) are found and returns those genomes along with the total number of evaluations that were executed to find
        ///     them.
        /// </summary>
        /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genomes.</param>
        /// <param name="maxEvaluations">
        ///     The maximum number of evaluations that can be executed before the initialization process
        ///     is restarted.  This prevents getting stuck for a long time and/or ending up with unnecessarily complex networks.
        /// </param>
        /// <param name="restartCount">
        ///     The number of times the initialization process has been restarted (this is only used for
        ///     status logging purposes).
        /// </param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public abstract List<NeatGenome> RunEvolution(out ulong totalEvaluations, uint? maxEvaluations,
            uint restartCount);

        #endregion
    }
}