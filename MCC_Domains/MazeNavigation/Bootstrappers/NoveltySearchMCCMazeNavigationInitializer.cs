#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MCC_Domains.MazeNavigation.MCCExperiment;
using MCC_Domains.Utils;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.NoveltyArchives;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MCC_Domains.MazeNavigation.Bootstrappers
{
    public class NoveltySearchMCCMazeNavigationInitializer : MCCMazeNavigationInitializer
    {
        #region Instance variables

        private double _archiveAdditionThreshold;
        private double _archiveThresholdDecreaseMultiplier;
        private double _archiveThresholdIncreaseMultiplier;
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private int _maxGenerationArchiveAddition;
        private int _maxGenerationsWithoutArchiveAddition;
        private int _nearestNeighbors;
        private int _batchSize;
        private int _populationEvaluationFrequency;

        #endregion

        #region Public methods

        /// <summary>
        ///     Constructs and initializes the maze navigator initialization algorithm (fitness using generational selection).  In
        ///     particular, this sets additional novelty search configuration parameters.
        /// </summary>
        /// <param name="xmlConfig">The XML configuration for the initialization algorithm.</param>
        /// <param name="inputCount">The number of input neurons.</param>
        /// <param name="outputCount">The number of output neurons.</param>
        /// <param name="numSuccessfulAgents">The minimum number of successful maze navigators that must be produced.</param>
        /// <param name="numUnsuccessfulAgents">The minimum number of unsuccessful maze navigators that must be produced.</param>
        /// <returns>The constructed initialization algorithm.</returns>
        public override void SetAlgorithmParameters(XmlElement xmlConfig, int inputCount, int outputCount,
            int numSuccessfulAgents, int numUnsuccessfulAgents)
        {
            // Set the boiler plate MCC parameters and minimal criterions
            base.SetAlgorithmParameters(xmlConfig, inputCount, outputCount, numSuccessfulAgents, numUnsuccessfulAgents);

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
        ///     Configures and instantiates the initialization evolutionary algorithm.
        /// </summary>
        /// <param name="parallelOptions">Synchronous/Asynchronous execution settings.</param>
        /// <param name="genomeList">The initial population of genomes.</param>
        /// <param name="genomeFactory">The genome factory initialized by the main evolution thread.</param>
        /// <param name="mazeEnvironment">The maze on which to evaluate the navigators.</param>
        /// <param name="genomeDecoder">The decoder to translate genomes into phenotypes.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceeded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        public override void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> genomeList,
            IGenomeFactory<NeatGenome> genomeFactory, MazeStructure mazeEnvironment,
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ulong startingEvaluations)
        {
            // Set the boiler plate algorithm parameters
            base.InitializeAlgorithm(parallelOptions, genomeList, genomeDecoder, startingEvaluations);

            // Create the initialization evolution algorithm.
            InitializationEa = new SteadyStateComplexifyingEvolutionAlgorithm<NeatGenome>(EvolutionAlgorithmParameters,
                SpeciationStrategy, ComplexityRegulationStrategy, _batchSize, _populationEvaluationFrequency,
                RunPhase.Initialization, NavigatorEvolutionDataLogger, NavigatorEvolutionLogFieldEnableMap,
                NavigatorPopulationDataLogger, PopulationLoggingBatchInterval);

            // Create IBlackBox evaluator.
            MazeNavigatorNoveltySearchInitializationEvaluator mazeNavigatorEvaluator =
                new MazeNavigatorNoveltySearchInitializationEvaluator(MinSuccessDistance,
                    MaxDistanceToTarget, mazeEnvironment, _behaviorCharacterizationFactory, startingEvaluations);

            // Create a novelty archive
            AbstractNoveltyArchive<NeatGenome> archive =
                new BehavioralNoveltyArchive<NeatGenome>(_archiveAdditionThreshold,
                    _archiveThresholdDecreaseMultiplier, _archiveThresholdIncreaseMultiplier,
                    _maxGenerationArchiveAddition, _maxGenerationsWithoutArchiveAddition);

            // Create the genome evaluator
            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigatorEvaluator, SearchType.NoveltySearch, _nearestNeighbors);

            // Only pull the number of genomes from the list equivalent to the initialization algorithm population size
            // (this is to handle the case where the list was created in accordance with the primary algorithm 
            // population size, which is quite likely larger)
            genomeList = genomeList.Take(PopulationSize).ToList();

            // Replace genome factory primary NEAT parameters with initialization parameters
            ((NeatGenomeFactory) genomeFactory).ResetNeatGenomeParameters(NeatGenomeParameters);

            // Initialize the evolution algorithm
            InitializationEa.Initialize(fitnessEvaluator, genomeFactory, genomeList, null, null, archive);
        }

        /// <summary>
        ///     Runs the initialization algorithm until the specified number of viable genomes (i.e. genomes that meets the minimal
        ///     criteria) are found and returns those genomes along with the total number of evaluations that were executed to find
        ///     them.
        /// </summary>
        /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genomes.</param>
        /// <param name="maxEvaluations">
        ///     The maximum number of evaluations that can be executed before the initialization process
        ///     is restarted.  This prevents getting stuck for a long time and/or ending up with unecessarily complex networks.
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
            List<NeatGenome> viableGenomes = new List<NeatGenome>(MinSuccessfulAgentCount + MinUnsuccessfulAgentCount);

            do
            {
                Console.Out.WriteLine("Starting up the algorithm on restart #{0}", restartCount);

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
                            genome.EvaluationInfo.ObjectiveDistance <= MinSuccessDistance)
                        .Take(MinSuccessfulAgentCount));

                Console.Out.WriteLine("Extracted [{0}] of [{1}] viable genomes in [{2}] evaluations",
                    viableGenomes.Count, MinSuccessfulAgentCount, InitializationEa.CurrentEvaluations);
            } while (viableGenomes.Count < MinSuccessfulAgentCount);

            // Add the remainder of genomes who have not solved the maze
            // (note that the intuition for doing this after the loop is that most will not have solved)
            viableGenomes.AddRange(
                InitializationEa.GenomeList.Where(
                    genome =>
                        genome.EvaluationInfo != null && genome.EvaluationInfo.ObjectiveDistance > MinSuccessDistance)
                    .Take(MinUnsuccessfulAgentCount));

            // Ensure that the above statement was able to get the required number of unsuccessful agent genomes
            if (viableGenomes.Count(genome => genome.EvaluationInfo.ObjectiveDistance > MinSuccessDistance) <
                MinUnsuccessfulAgentCount)
            {
                throw new SharpNeatException(
                    "Novelty search MCC initialization algorithm failed to produce the requisite number of unsuccessful agent genomes.");
            }

            // Set the total number of evaluations that were executed as part of the initialization process
            totalEvaluations = InitializationEa.CurrentEvaluations;

            return viableGenomes;
        }

        #endregion
    }
}