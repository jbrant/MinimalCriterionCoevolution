#region

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

#endregion

namespace SharpNeat.Domains.MazeNavigation.Bootstrappers
{
    /// <summary>
    ///     Parent initializer class which only contains boiler plate intialization code for domain-specific (child)
    ///     intializers.
    /// </summary>
    public class MazeNavigationInitializer
    {
        #region Constructors

        /// <summary>
        ///     Enforces instantiation only through child classes.
        /// </summary>
        protected MazeNavigationInitializer()
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Constructs and initializes the initialization algorithm.
        /// </summary>
        /// <param name="xmlConfig">The XML configuration for the initialization algorithm.</param>
        /// <param name="inputCount">The number of input neurons.</param>
        /// <param name="outputCount">The number of output neurons.</param>
        /// <returns>The constructed initialization algorithm.</returns>
        public virtual void SetAlgorithmParameters(XmlElement xmlConfig, int inputCount, int outputCount)
        {
            // Read NEAT genome parameters
            NeatGenomeParameters neatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);

            // Read NEAT evolution parameters
            NeatEvolutionAlgorithmParameters = ExperimentUtils.ReadNeatEvolutionAlgorithmParameters(xmlConfig);

            // Create genome factory specifically for the initialization algorithm 
            // (this is primarily because the initialization algorithm will quite likely have different NEAT parameters)
            GenomeFactory = new NeatGenomeFactory(inputCount, outputCount, neatGenomeParameters);

            // Get complexity constraint parameters
            ComplexityRegulationStrategyDefinition = XmlUtils.TryGetValueAsString(xmlConfig,
                "ComplexityRegulationStrategy");
            ComplexityThreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");
        }
        
        /// <summary>
        ///     Sets configuration variables specific to the maze navigation simulation.
        /// </summary>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="maxTimesteps">The maximum number of time steps for which to run the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance to the target location for the maze to be considered "solved".</param>
        public virtual void SetEnvironmentParameters(int maxDistanceToTarget, int maxTimesteps, int minSuccessDistance)
        {
            MaxDistanceToTarget = maxDistanceToTarget;
            MaxTimesteps = maxTimesteps;
            MinSuccessDistance = minSuccessDistance;
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
        public virtual void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> genomeList,
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ulong startingEvaluations)
        {
            ParallelOptions = parallelOptions;
            InitialPopulation = genomeList;
            StartingEvaluations = startingEvaluations;
            GenomeDecoder = genomeDecoder;

            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            SpeciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, parallelOptions);

            // Create complexity regulation strategy.
            ComplexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategyDefinition,
                    ComplexityThreshold);
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The evolutionary algorithm supporting the initialization process.
        /// </summary>
        protected AbstractNeatEvolutionAlgorithm<NeatGenome> InitializationEa;

        /// <summary>
        ///     The genome factory specifically for the initialization algorithm (this is because the initialization algorithm will
        ///     likely have different algorithm parameters than the primary algorithm).
        /// </summary>
        protected IGenomeFactory<NeatGenome> GenomeFactory;

        /// <summary>
        ///     The genome decoder for the agents.
        /// </summary>
        protected IGenomeDecoder<NeatGenome, IBlackBox> GenomeDecoder;

        /// <summary>
        ///     The parameters controlling the NEAT algorithm.
        /// </summary>
        protected NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters;

        /// <summary>
        ///     The complexity regulation strategy.
        /// </summary>
        protected string ComplexityRegulationStrategyDefinition;

        /// <summary>
        ///     The complexity threshold.
        /// </summary>
        protected int? ComplexityThreshold;

        /// <summary>
        ///     Controls the number of threads that are kicked off as part of the initialization algorithm execution.
        /// </summary>
        protected ParallelOptions ParallelOptions;

        /// <summary>
        ///     The initial population of random agents.
        /// </summary>
        protected List<NeatGenome> InitialPopulation;

        /// <summary>
        ///     The starting number of evaluations by which to offset the total number of evaluations for initialization.
        /// </summary>
        protected ulong StartingEvaluations;

        /// <summary>
        ///     The maximum distance to the target possible.
        /// </summary>
        protected int MaxDistanceToTarget;

        /// <summary>
        ///     The maximum number of timesteps allowed for a single trial.
        /// </summary>
        protected int MaxTimesteps;

        /// <summary>
        ///     The minimum distance to the target permitted for the navigator to have successfully solved the maze.
        /// </summary>
        protected int MinSuccessDistance;

        /// <summary>
        ///     The speciation strategy used by the EA.
        /// </summary>
        protected ISpeciationStrategy<NeatGenome> SpeciationStrategy;

        /// <summary>
        ///     The complexity regulation strategy used by the EA.
        /// </summary>
        protected IComplexityRegulationStrategy ComplexityRegulationStrategy;

        #endregion

        #region Public methods

        #endregion

        #region Private methods

        #endregion
    }
}