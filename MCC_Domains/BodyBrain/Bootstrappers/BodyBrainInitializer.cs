using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MCC_Domains.BodyBrain.MCCExperiment;
using MCC_Domains.Utils;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain
{
    public abstract class BodyBrainInitializer
    {
        #region Instance variables

        /// <summary>
        ///     The evolutionary algorithm supporting the initialization process.
        /// </summary>
        protected AbstractComplexifyingEvolutionAlgorithm<NeatGenome> InitializationEa;

        #endregion

        #region Constructors

        /// <summary>
        ///     BodyBrainInitializer constructor.
        /// </summary>
        /// <param name="brainType">The type of brain controller (e.g. neural network or phase offset controller).</param>
        protected BodyBrainInitializer(BrainType brainType)
        {
            BrainType = brainType;
        }

        #endregion

        #region Protected members

        /// <summary>
        ///     The unique name of the experiment being executed.
        /// </summary>
        protected string ExperimentName;

        /// <summary>
        ///     The current run number of the given experiment.
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
        ///     Whether or not the selection algorithm is generational (the alternative is steady-state).
        /// </summary>
        protected bool IsGenerational;

        /// <summary>
        ///     The number of individuals to evaluate within a single batch.
        /// </summary>
        protected int BatchSize;

        /// <summary>
        ///     The number of batches that are evaluated between evaluating the entire population.
        /// </summary>
        protected int PopulationEvaluationFrequency;

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
        ///     Collection of simulator configuration properties, including result output directories, configuration files and
        ///     config file parsing information.
        /// </summary>
        protected SimulationProperties SimulationProperties;

        /// <summary>
        ///     The type of brain controller (e.g. neural network or phase offset controller).
        /// </summary>
        protected BrainType BrainType;

        #endregion

        #region Public methods

        public virtual void SetAlgorithmParameters(XmlElement xmlConfig, string experimentName, int run, bool isAcyclic,
            int numSuccessfulBrains)
        {
            // Set experiment name and run
            ExperimentName = experimentName;
            Run = run;

            // Read NEAT genome parameters
            // Save off genome parameters specifically for the initialization algorithm 
            NeatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);
            NeatGenomeParameters.FeedforwardOnly = isAcyclic;

            // Read selection algorithm type
            IsGenerational = XmlUtils.TryGetValueAsString(xmlConfig, "SelectionAlgorithm")
                .Equals("Generational", StringComparison.InvariantCultureIgnoreCase);

            // Read in steady-state specific parameters
            BatchSize = XmlUtils.TryGetValueAsInt(xmlConfig, "OffspringBatchSize") ?? 0;
            PopulationEvaluationFrequency = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationEvaluationFrequency") ?? 0;

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
        /// <param name="simulationProperties">
        ///     Collection of simulator configuration properties, including result output
        ///     directories, configuration files and config file parsing information.
        /// </param>
        public void SetSimulationParameters(double minAmbulationDistance, SimulationProperties simulationProperties)
        {
            MinAmbulationDistance = minAmbulationDistance;
            SimulationProperties = simulationProperties;
        }

        /// <summary>
        ///     Evolves the requisite number of brains who satisfy the MC of the given body.
        /// </summary>
        /// <param name="genomeFactory">The agent genome factory.</param>
        /// <param name="seedBrainList">The seed population of brains.</param>
        /// <param name="brainGenomeDecoder">The genome decoder for the brain population.</param>
        /// <param name="body">The voxel body on which brains are to be evaluated.</param>
        /// <param name="maxInitializationEvals">
        ///     The maximum number of evaluations to run algorithm before restarting with new,
        ///     randomly generated population.
        /// </param>
        /// <param name="parallelOptions">Synchronous/Asynchronous execution settings.</param>
        /// <param name="maxBodyRestarts">
        ///     The maximum number of times that evolution can be restarted on the given body (defaults
        ///     to an effectively infinite number of restarts).
        /// </param>
        /// <returns>The list of viable brain genomes.</returns>
        public IList<NeatGenome> EvolveViableBrains(IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> seedBrainList, IGenomeDecoder<NeatGenome, IBlackBox> brainGenomeDecoder, VoxelBody body,
            ulong maxInitializationEvals, ParallelOptions parallelOptions, int maxBodyRestarts = int.MaxValue)
        {
            List<NeatGenome> viableBrains;
            var restartCount = 0;

            do
            {
                // Reset the genome factory from previous runs (so we don't accumulate innovations across multiple restarts)
                genomeFactory = new CppnGenomeFactory(genomeFactory as CppnGenomeFactory);
                
                // Instantiate the internal initialization algorithm
                InitializeAlgorithm(parallelOptions, seedBrainList.ToList(), genomeFactory, brainGenomeDecoder, body,
                    0);

                // Run the initialization algorithm until the requested number of viable seed genomes are found
                viableBrains = RunEvolution(maxInitializationEvals, restartCount);

                restartCount++;

                // Repeat if no viable genomes were discovered and the number of evolution restarts is less than
                // the maximum allowed
            } while (viableBrains == null && restartCount < maxBodyRestarts);

            return viableBrains;
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
            IGenomeFactory<NeatGenome> brainGenomeFactory, IGenomeDecoder<NeatGenome, IBlackBox> brainGenomeDecoder,
            VoxelBody body, ulong startingEvaluations);

        /// <summary>
        ///     Runs the initialization algorithm until the specified number of viable genomes (i.e. genomes that meets the minimal
        ///     criteria) are found and returns those genomes along with the total number of evaluations that were executed to find
        ///     them.
        /// </summary>
        /// <param name="maxEvaluations">
        ///     The maximum number of evaluations that can be executed before the initialization process
        ///     is restarted.  This prevents getting stuck for a long time and/or ending up with unnecessarily complex networks.
        /// </param>
        /// <param name="restartCount">
        ///     The number of times the initialization process has been restarted (this is only used for
        ///     status logging purposes).
        /// </param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public abstract List<NeatGenome> RunEvolution(ulong maxEvaluations, int restartCount);

        #endregion
    }
}