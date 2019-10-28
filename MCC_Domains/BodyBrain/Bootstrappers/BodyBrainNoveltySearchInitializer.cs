using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MCC_Domains.BodyBrain.MCCExperiment;
using MCC_Domains.Utils;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.NoveltyArchives;
using SharpNeat.Phenomes.Voxels;
using SharpNeat.SpeciationStrategies;

namespace MCC_Domains.BodyBrain.Bootstrappers
{
    public class BodyBrainNoveltySearchInitializer : BodyBrainInitializer
    {
        #region Instance variables

        /// <summary>
        ///     Factory for constructing new behavior characterizations.
        /// </summary>
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;

        /// <summary>
        ///     The novelty threshold for incorporation into the novelty archive.
        /// </summary>
        private double _archiveAdditionThreshold;

        /// <summary>
        ///     The proportion by which the bar for entrance to the novelty archive should be lowered.
        /// </summary>
        private double _archiveThresholdDecreaseMultiplier;

        /// <summary>
        ///     The proportion by which the bar for entrance to the novelty archive should be increased.
        /// </summary>
        private double _archiveThresholdIncreaseMultiplier;

        /// <summary>
        ///     The maximum number of generations that are allowed to pass with archive addition before increasing the archive
        ///     novelty threshold.
        /// </summary>
        private int _maxGenerationArchiveAddition;

        /// <summary>
        ///     The maximum number of generations that are allowed to pass without archive addition before decreasing the archive
        ///     novelty threshold.
        /// </summary>
        private int _maxGenerationsWithoutArchiveAddition;

        /// <summary>
        ///     The number of neighbors within behavior space against which to assess behavioral novelty.
        /// </summary>
        private int _nearestNeighbors;

        /// <summary>
        ///     The number of individuals to evaluate within a single batch.
        /// </summary>
        private int _batchSize;

        /// <summary>
        ///     The number of batches that are evaluated between evaluating the entire population.
        /// </summary>
        private int _populationEvaluationFrequency;

        /// <summary>
        ///     The speciation strategy used by the EA.
        /// </summary>
        private ISpeciationStrategy<NeatGenome> _speciationStrategy;

        /// <summary>
        ///     The complexity regulation strategy used by the EA.
        /// </summary>
        private IComplexityRegulationStrategy _complexityRegulationStrategy;

        #endregion

        #region Public methods

        /// <summary>
        ///     Constructs and initializes the body/brain initialization algorithm (which uses steady-state novelty search). In
        ///     particular, this sets additional novelty search specific configuration parameters.
        /// </summary>
        /// <param name="xmlConfig">The XML configuration for the initialization algorithm.</param>
        /// <param name="experimentName">The name of the experiment being executed.</param>
        /// <param name="run">The run number of the experiment being executed.</param>
        /// <param name="isAcyclic">Flag indicating whether the network is acyclic (i.e. does not have recurrent connections).</param>
        /// <param name="numSuccessfulBrains">The minimum number of brains that must successfully ambulate the body.</param>
        public override void SetAlgorithmParameters(XmlElement xmlConfig, string experimentName, int run,
            bool isAcyclic, int numSuccessfulBrains)
        {
            // Set the boiler plate MCC parameters and minimal criterions
            base.SetAlgorithmParameters(xmlConfig, experimentName, run, isAcyclic, numSuccessfulBrains);

            // Read in the behavior characterization
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(xmlConfig,
                "InitBehaviorConfig");

            // Read in the novelty archive parameters
            ExperimentUtils.ReadNoveltyParameters(xmlConfig, out _archiveAdditionThreshold,
                out _archiveThresholdDecreaseMultiplier, out _archiveThresholdIncreaseMultiplier,
                out _maxGenerationArchiveAddition, out _maxGenerationsWithoutArchiveAddition);

            // Read in nearest neighbors for behavior distance calculations
            _nearestNeighbors = XmlUtils.GetValueAsInt(xmlConfig, "NearestNeighbors");

            // Read in steady-state specific parameters
            _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");
            _populationEvaluationFrequency = XmlUtils.GetValueAsInt(xmlConfig, "PopulationEvaluationFrequency");
        }

        /// <summary>
        ///     Configures and instantiates the initialization algorithm.
        /// </summary>
        /// <param name="parallelOptions">Synchronous/Asynchronous execution settings.</param>
        /// <param name="brainGenomeList">The initial population of brain genomes.</param>
        /// <param name="brainGenomeFactory">The brain genome factory initialized by the main evolution thread.</param>
        /// <param name="brainGenomeDecoder">The decoder that translates brains into neurocontrollers.</param>
        /// <param name="body">The body morphology on which the brains are evaluated.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceeded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        protected override void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> brainGenomeList,
            IGenomeFactory<NeatGenome> brainGenomeFactory, IGenomeDecoder<NeatGenome, VoxelBrain> brainGenomeDecoder,
            VoxelBody body, ulong startingEvaluations)
        {
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            _speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, parallelOptions);

            // Create complexity regulation strategy.
            _complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategyDefinition,
                    ComplexityThreshold);

            // Create the initialization evolution algorithm.
            // TODO: Add data loggers
            InitializationEa = new SteadyStateComplexifyingEvolutionAlgorithm<NeatGenome>(EvolutionAlgorithmParameters,
                _speciationStrategy, _complexityRegulationStrategy, _batchSize, _populationEvaluationFrequency,
                RunPhase.Initialization);

            // Create the brain novelty search initialization evaluator
            var brainEvaluator = new BodyBrainNoveltySearchInitializationEvaluator(body, SimulationProperties,
                MinAmbulationDistance, ExperimentName, Run, startingEvaluations);

            // Create a novelty archive
            AbstractNoveltyArchive<NeatGenome> archive =
                new BehavioralNoveltyArchive<NeatGenome>(_archiveAdditionThreshold,
                    _archiveThresholdDecreaseMultiplier, _archiveThresholdIncreaseMultiplier,
                    _maxGenerationArchiveAddition, _maxGenerationsWithoutArchiveAddition);

            // Create the brain genome evaluator
            IGenomeEvaluator<NeatGenome> behaviorEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, VoxelBrain>(brainGenomeDecoder, brainEvaluator,
                    SearchType.NoveltySearch, _nearestNeighbors);

            // Only pull the number of genomes from the list equivalent to the initialization algorithm population size
            brainGenomeList = brainGenomeList.Take(PopulationSize).ToList();

            // Replace genome factory primary NEAT parameters with initialization parameters
            ((CppnGenomeFactory) brainGenomeFactory).ResetNeatGenomeParameters(NeatGenomeParameters);

            // Initialize the evolution algorithm
            InitializationEa.Initialize(behaviorEvaluator, brainGenomeFactory, brainGenomeList, null, null, archive);
        }

        /// <summary>
        ///     Executes the initialization algorithm until the specific number of viable genomes (i.e. genomes that meets the
        ///     minimal criteria) are evolved, and returns those genomes along with the total number of evaluations that were
        ///     executed to discover them.
        /// </summary>
        /// <param name="totalEvaluations">The total number of evaluations required to evolve the seed genomes.</param>
        /// <param name="maxEvaluations">
        ///     The maximum number of evaluations that can be executed before the initialization process
        ///     is restarted.  This prevents getting stuck for a long time and/or ending up with unnecessarily complex networks.
        /// </param>
        /// <param name="restartCount">
        ///     The number of times the initialization process has been restarted (this is only used for
        ///     status logging purposes).
        /// </param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public override List<NeatGenome> RunEvolution(out ulong totalEvaluations, uint? maxEvaluations,
            uint restartCount)
        {
            // Create list of viable genomes
            var viableGenomes = new List<NeatGenome>(MinSuccessfulBrainCount);

            do
            {
                Console.Out.WriteLine($"Starting up the algorithm on restart #{restartCount}");

                // Start the algorithm
                InitializationEa.StartContinue();

                Console.Out.WriteLine("Going into algorithm wait loop...");

                // Ping for status every few hundred milliseconds
                while (RunState.Terminated != InitializationEa.RunState &&
                       RunState.Paused != InitializationEa.RunState)
                {
                    if (InitializationEa.CurrentEvaluations >= maxEvaluations)
                    {
                        // Record the total number of evaluations
                        totalEvaluations = InitializationEa.CurrentEvaluations;

                        // Halt the EA worker thread
                        InitializationEa.RequestPauseAndWait();

                        // Null out the EA and delete the thread
                        // (it's necessary to null out these resources so that the thread will be completely garbage collected)
                        InitializationEa.Reset();
                        InitializationEa = null;

                        // Note that the calling experiment must be able to handle this null return value (not great practice)
                        return null;
                    }

                    Thread.Sleep(200);
                }

                Console.Out.WriteLine("Attempting to extract viable genome from list...");

                // Add all of the genomes that have solved the maze
                viableGenomes.AddRange(
                    InitializationEa.GenomeList.Where(
                            genome =>
                                genome.EvaluationInfo != null &&
                                genome.EvaluationInfo.TrialData[0].ObjectiveDistance >= MinAmbulationDistance));

                Console.Out.WriteLine(
                    $"Extracted [{viableGenomes.Count}] of [{MinSuccessfulBrainCount}] required viable genomes in [{InitializationEa.CurrentEvaluations}] evaluations");
            } while (viableGenomes.Count < MinSuccessfulBrainCount);

            // Set the total number of evaluations that were executed as part of the initialization process
            totalEvaluations = InitializationEa.CurrentEvaluations;

            return viableGenomes;
        }

        #endregion
    }
}