#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment;
using SharpNeat.EliteArchives;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.NoveltyArchives;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.Bootstrappers
{
    public class NoveltySearchCoevolutionMazeNavigationInitializer : CoevolutionMazeNavigationInitializer
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
            // Set the boiler plate coevolution parameters and minimal criterions
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
        /// <param name="mazeEnvironment">The maze on which to evaluate the navigators.</param>
        /// <param name="genomeDecoder">The decoder to translate genomes into phenotypes.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceeded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        public override void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> genomeList,
            MazeStructure mazeEnvironment,
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ulong startingEvaluations)
        {
            // Set the boiler plate algorithm parameters
            base.InitializeAlgorithm(parallelOptions, genomeList, genomeDecoder, startingEvaluations);

            // Create the initialization evolution algorithm.
            InitializationEa = new SteadyStateNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                SpeciationStrategy, ComplexityRegulationStrategy, _batchSize, _populationEvaluationFrequency,
                RunPhase.Initialization, NavigatorEvolutionDataLogger, NavigatorEvolutionLogFieldEnableMap);

            // Create IBlackBox evaluator.
            MazeNavigatorNoveltySearchInitializationEvaluator mazeNavigatorEvaluator =
                new MazeNavigatorNoveltySearchInitializationEvaluator(MaxTimesteps, MinSuccessDistance,
                    MaxDistanceToTarget, mazeEnvironment, _behaviorCharacterizationFactory, startingEvaluations);

            // Create a novelty archive
            AbstractNoveltyArchive<NeatGenome> archive =
                new BehavioralNoveltyArchive<NeatGenome>(_archiveAdditionThreshold,
                    _archiveThresholdDecreaseMultiplier, _archiveThresholdIncreaseMultiplier,
                    _maxGenerationArchiveAddition, _maxGenerationsWithoutArchiveAddition);

            // Create the genome evaluator
            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigatorEvaluator,
                    SelectionType.SteadyState, SearchType.NoveltySearch, _nearestNeighbors, archive);

            // Only pull the number of genomes from the list equivalent to the initialization algorithm population size
            // (this is to handle the case where the list was created in accordance with the primary algorithm 
            // population size, which is quite likely larger)
            genomeList = genomeList.Take(PopulationSize).ToList();

            // Initialize the evolution algorithm
            InitializationEa.Initialize(fitnessEvaluator, GenomeFactory, genomeList, null, null, archive);
        }

        /// <summary>
        ///     Runs the initialization algorithm until the specified number of viable genomes (i.e. genomes that meets the minimal
        ///     criteria) are found and returns those genomes along with the total number of evaluations that were executed to find
        ///     them.
        /// </summary>
        /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genomes.</param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public override List<NeatGenome> EvolveViableGenomes(out ulong totalEvaluations)
        {
            // Create list of viable genomes
            List<NeatGenome> viableGenomes = new List<NeatGenome>(MinSuccessfulAgentCount + MinUnsuccessfulAgentCount);

            do
            {
                // Start the algorithm
                InitializationEa.StartContinue();

                // Ping for status every few hundred milliseconds
                while (RunState.Terminated != InitializationEa.RunState &&
                       RunState.Paused != InitializationEa.RunState)
                {
                    Console.WriteLine(@"Current Evaluations: {0}  Mean Complexity: {1}  Closest Genome Distance: {2}",
                        InitializationEa.CurrentEvaluations, InitializationEa.Statistics._meanComplexity,
                        InitializationEa.GenomeList.Min(genome => genome.EvaluationInfo.ObjectiveDistance));

                    Thread.Sleep(200);
                }

                // Add all of the genomes that have solved the maze
                viableGenomes.AddRange(
                    InitializationEa.GenomeList.Where(
                        genome => genome.EvaluationInfo.ObjectiveDistance <= MinSuccessDistance)
                        .Take(MinSuccessfulAgentCount));
            } while (viableGenomes.Count < MinSuccessfulAgentCount);

            // Add the remainder of genomes who have not solved the maze
            // (note that the intuition for doing this after the loop is that most will not have solved)
            viableGenomes.AddRange(
                InitializationEa.GenomeList.Where(genome => genome.EvaluationInfo.ObjectiveDistance > MinSuccessDistance)
                    .Take(MinUnsuccessfulAgentCount));

            // Ensure that the above statement was able to get the required number of unsuccessful agent genomes
            if (viableGenomes.Count(genome => genome.EvaluationInfo.ObjectiveDistance > MinSuccessDistance) <
                MinUnsuccessfulAgentCount)
            {
                throw new SharpNeatException(
                    "Novelty search coevolution initialization algorithm failed to produce the requisite number of unsuccessful agent genomes.");
            }

            // Set the total number of evaluations that were executed as part of the initialization process
            totalEvaluations = InitializationEa.CurrentEvaluations;

            return viableGenomes;
        }

        #endregion
    }
}