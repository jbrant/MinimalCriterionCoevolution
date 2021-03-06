﻿#region

using System.Collections.Generic;
using System.Xml;
using MCC_Domains.Utils;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.Decoders.Maze;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.Statistics;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;
using SharpNeat.SpeciationStrategies;

#endregion

namespace MCC_Domains.MazeNavigation.MCCExperiment
{
    /// <summary>
    ///     MCC experiment with speciated population queues. Selection and removal occurs from each of the species rather than
    ///     from the "global" queues (although, species are logical partitions - individuals are still physically stored in two
    ///     queues).
    /// </summary>
    public class MCCLimitedResourcesExperiment : BaseMCCMazeNavigationExperiment
    {
        #region Private methods

        /// <summary>
        ///     Checks ranges and other experiment settings to ensure that the configuration is valid.
        /// </summary>
        /// <param name="message">
        ///     Error message denoting specific configuration violation (only set if an invalid configuration was
        ///     identified).
        /// </param>
        /// <returns>Boolean flag indicating whether the experiment configuration is valid.</returns>
        protected override bool ValidateConfigParameters(out string message)
        {
            // Set error message to null by default
            message = null;

            // Check resource constraint setting
            if (_resourceLimit < 1)
                message =
                    $"Resource limit [{_resourceLimit}] must be greater than 1, otherwise maze cannot be used to satisfy the MC of any agent";
            else if (_resourceLimit * MazeSeedGenomeCount < AgentSeedGenomeCount)
                message =
                    $"Product of resource limit [{_resourceLimit}] and maze seed genome count [{MazeSeedGenomeCount}] must be at least as large as agent seed genome count [{AgentSeedGenomeCount}], otherwise not all agent seed genomes can be evolved";
            // Check base class parameters
            else if (base.ValidateConfigParameters(out var errorMessage))
                message = errorMessage;

            // Return configuration validity status based on whether an error message was set
            return message != null;
        }

        #endregion

        #region Private members

        /// <summary>
        ///     The resource limit for mazes (i.e. the maximum number of times that it can be used by an agent for satisfying the
        ///     agent's MC).
        /// </summary>
        private int _resourceLimit;

        /// <summary>
        ///     Logs statistics about the navigator populations for every batch.
        /// </summary>
        private IDataLogger _navigatorEvolutionDataLogger;

        /// <summary>
        ///     Logs the IDs of the extant navigator population at every interval.
        /// </summary>
        private IDataLogger _navigatorPopulationDataLogger;

        /// <summary>
        ///     Logs the definitions of the navigator population over the course of a run.
        /// </summary>
        private IDataLogger _navigatorGenomeDataLogger;

        /// <summary>
        ///     Logs the details and results of trials within a navigator evaluation.
        /// </summary>
        private IDataLogger _navigatorSimulationTrialDataLogger;

        /// <summary>
        ///     Logs statistics about the maze populations for every batch.
        /// </summary>
        private IDataLogger _mazeEvolutionDataLogger;

        /// <summary>
        ///     Logs the IDs of the extant maze population at every interval.
        /// </summary>
        private IDataLogger _mazePopulationDataLogger;

        /// <summary>
        ///     Logs the definitions of the maze population over the course of a run.
        /// </summary>
        private IDataLogger _mazeGenomeDataLogger;

        /// <summary>
        ///     Logs the maze resource usage over the course of a run.
        /// </summary>
        private IDataLogger _mazeResourceUsageLogger;

        /// <summary>
        ///     Logs the details and results of trials within a maze evaluation.
        /// </summary>
        private IDataLogger _mazeSimulationTrialDataLogger;

        /// <summary>
        ///     Dictionary which indicates logger fields to be enabled/disabled for navigator genomes.
        /// </summary>
        private IDictionary<FieldElement, bool> _navigatorLogFieldEnableMap;

        /// <summary>
        ///     Dictionary which indicates logger fields to be enabled/disabled for maze genomes.
        /// </summary>
        private IDictionary<FieldElement, bool> _mazeLogFieldEnableMap;

        #endregion

        #region Overridden methods

        /// <inheritdoc />
        /// <summary>
        ///     Initializes the MCC maze navigation experiment by reading in all of the configuration parameters and
        ///     setting up the bootstrapping/initialization algorithm.
        /// </summary>
        /// <param name="name">The name of the experiment.</param>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="logFileDirectory">The directory into which to write the evolution/evaluation log files.</param>
        /// <param name="runIdx">The numerical ID of the current run.</param>
        public override void Initialize(string name, XmlElement xmlConfig, string logFileDirectory, int runIdx)
        {
            // Initialize boiler plate parameters
            base.Initialize(name, xmlConfig);

            // Read resource limit parameter
            _resourceLimit = XmlUtils.GetValueAsInt(xmlConfig, "ResourceLimit");

            // Initialize the data loggers for the given experiment/run
            _navigatorEvolutionDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - NavigatorEvolution.csv");
            _navigatorPopulationDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - NavigatorPopulation.csv");
            _navigatorGenomeDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - NavigatorGenomes.csv");
            _navigatorSimulationTrialDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - NavigatorTrials.csv");
            _mazeEvolutionDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - MazeEvolution.csv");
            _mazePopulationDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - MazePopulation.csv");
            _mazeGenomeDataLogger = new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - MazeGenomes.csv");
            _mazeSimulationTrialDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - MazeTrials.csv");
            _mazeResourceUsageLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{runIdx} - ResourceUsage.csv");

            // Create new evolution field elements map with all fields enabled
            _navigatorLogFieldEnableMap = EvolutionFieldElements.PopulateEvolutionFieldElementsEnableMap();
            
            // Add default evolution logging configuration specific to maze navigation experiment
            foreach (var evolutionLoggingPair in
                MazeNavEvolutionFieldElements.PopulateEvolutionFieldElementsEnableMap())
            {
                _navigatorLogFieldEnableMap.Add(evolutionLoggingPair.Key, evolutionLoggingPair.Value);
            }

            // Add default population logging configuration
            foreach (var populationLoggingPair in PopulationFieldElements.PopulatePopulationFieldElementsEnableMap())
            {
                _navigatorLogFieldEnableMap.Add(populationLoggingPair.Key, populationLoggingPair.Value);
            }

            // Add default genome logging configuration
            foreach (var genomeLoggingPair in GenomeFieldElements.PopulateGenomeFieldElementsEnableMap())
            {
                _navigatorLogFieldEnableMap.Add(genomeLoggingPair.Key, genomeLoggingPair.Value);
            }

            // Add default trial logging configuration
            foreach (var trialLoggingPair in
                SimulationTrialFieldElements.PopulateSimulationTrialFieldElementsEnableMap())
            {
                _navigatorLogFieldEnableMap.Add(trialLoggingPair.Key, trialLoggingPair.Value);
            }

            // Disable logging fields not relevant to agent evolution in MCC experiment
            _navigatorLogFieldEnableMap[EvolutionFieldElements.SpecieCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.AsexualOffspringCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.SexualOffspringCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.InterspeciesOffspringCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaThreshold] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointX] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointY] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxFitness] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanFitness] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanSpecieChampFitness] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinSpecieSize] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxSpecieSize] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeGenomeId] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeFitness] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeBirthGeneration] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeConnectionGeneCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeNeuronGeneCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeTotalGeneCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeEvaluationCount] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeXml] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinWalls] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MaxWalls] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MeanWalls] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinWaypoints] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MaxWaypoints] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MeanWaypoints] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinJunctures] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MaxJunctures] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MeanJunctures] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinTrajectoryFacingOpenings] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MaxTrajectoryFacingOpenings] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MeanTrajectoryFacingOpenings] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinHeight] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MaxHeight] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MeanHeight] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinWidth] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MaxWidth] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MeanWidth] = false;

            // Create a maze logger configuration with the same configuration as the navigator one
            _mazeLogFieldEnableMap = new Dictionary<FieldElement, bool>(_navigatorLogFieldEnableMap)
            {
                [EvolutionFieldElements.RunPhase] = false,
                [PopulationFieldElements.RunPhase] = false,
                [MazeNavEvolutionFieldElements.MinWalls] = true,
                [MazeNavEvolutionFieldElements.MaxWalls] = true,
                [MazeNavEvolutionFieldElements.MeanWalls] = true,
                [MazeNavEvolutionFieldElements.MinWaypoints] = true,
                [MazeNavEvolutionFieldElements.MaxWaypoints] = true,
                [MazeNavEvolutionFieldElements.MeanWaypoints] = true,
                [MazeNavEvolutionFieldElements.MinJunctures] = true,
                [MazeNavEvolutionFieldElements.MaxJunctures] = true,
                [MazeNavEvolutionFieldElements.MeanJunctures] = true,
                [MazeNavEvolutionFieldElements.MinTrajectoryFacingOpenings] = true,
                [MazeNavEvolutionFieldElements.MaxTrajectoryFacingOpenings] = true,
                [MazeNavEvolutionFieldElements.MeanTrajectoryFacingOpenings] = true,
                [MazeNavEvolutionFieldElements.MinHeight] = true,
                [MazeNavEvolutionFieldElements.MaxHeight] = true,
                [MazeNavEvolutionFieldElements.MeanHeight] = true,
                [MazeNavEvolutionFieldElements.MinWidth] = true,
                [MazeNavEvolutionFieldElements.MaxWidth] = true,
                [MazeNavEvolutionFieldElements.MeanWidth] = true
            };

            // Validate experiment configuration parameters
            if (ValidateConfigParameters(out var errorMessage)) throw new ConfigurationException(errorMessage);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Zero argument wrapper method for instantiating the coevolution algorithm container.  This uses default agent and
        ///     maze population sizes as the only configuration parameters.
        /// </summary>
        /// <returns>The instantiated MCC algorithm container.</returns>
        public override IMCCAlgorithmContainer<NeatGenome, MazeGenome> CreateMCCAlgorithmContainer()
        {
            return CreateMCCAlgorithmContainer(AgentSeedGenomeCount, MazeSeedGenomeCount);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates the MCC algorithm container using the given agent and maze population sizes.
        /// </summary>
        /// <param name="populationSize1">The agent population size.</param>
        /// <param name="populationSize2">The maze population size.</param>
        /// <returns>The instantiated MCC algorithm container.</returns>
        public override IMCCAlgorithmContainer<NeatGenome, MazeGenome> CreateMCCAlgorithmContainer(
            int populationSize1, int populationSize2)
        {
            // Create a genome factory for the NEAT genomes
            IGenomeFactory<NeatGenome> neatGenomeFactory = new NeatGenomeFactory(AnnInputCount, AnnOutputCount,
                NeatGenomeParameters);

            // Create a genome factory for the maze genomes
            IGenomeFactory<MazeGenome> mazeGenomeFactory = new MazeGenomeFactory(MazeGenomeParameters, MazeHeight,
                MazeWidth, MazeQuadrantHeight, MazeQuadrantWidth);

            // Create an initial population of maze navigators
            var neatGenomeList = neatGenomeFactory.CreateGenomeList(populationSize1, 0);

            // Create an initial population of mazes
            // NOTE: the population is set to 1 here because we're just starting with a single, completely open maze space
            var mazeGenomeList = mazeGenomeFactory.CreateGenomeList(populationSize2, 0);

            // Create the evolution algorithm container
            return CreateMCCAlgorithmContainer(neatGenomeFactory, mazeGenomeFactory, neatGenomeList,
                mazeGenomeList, false);
        }

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
        public override IMCCAlgorithmContainer<NeatGenome, MazeGenome> CreateMCCAlgorithmContainer(
            IGenomeFactory<NeatGenome> genomeFactory1,
            IGenomeFactory<MazeGenome> genomeFactory2, List<NeatGenome> genomeList1, List<MazeGenome> genomeList2,
            bool isAgentListPreevolved)
        {
            // Either use pre-evolved agents or evolve the seed agents that meet the MC
            var seedAgentPopulation = isAgentListPreevolved
                ? genomeList1
                : EvolveSeedAgents(genomeList1, genomeList2, genomeFactory1, AgentSeedGenomeCount, _resourceLimit);

            // Set dummy fitness so that seed maze(s) will be marked as evaluated
            foreach (var mazeGenome in genomeList2)
            {
                mazeGenome.EvaluationInfo.SetFitness(0);
            }

            // Reset primary NEAT genome parameters on agent genome factory
            ((NeatGenomeFactory) genomeFactory1).ResetNeatGenomeParameters(NeatGenomeParameters);

            // Create the NEAT evolution algorithm parameters 
            var neatEaParams = new EvolutionAlgorithmParameters
            {
                SpecieCount = 0,
                MaxSpecieSize = AgentDefaultPopulationSize
            };

            // Create the maze evolution algorithm parameters
            var mazeEaParams = new EvolutionAlgorithmParameters
            {
                SpecieCount = 0,
                MaxSpecieSize = MazeDefaultPopulationSize
            };

            // Create the NEAT (i.e. navigator) queueing evolution algorithm
            AbstractEvolutionAlgorithm<NeatGenome> neatEvolutionAlgorithm = new QueueEvolutionAlgorithm<NeatGenome>(
                neatEaParams, new NeatAlgorithmStats(neatEaParams), null, NavigatorBatchSize, RunPhase.Primary,
                _navigatorEvolutionDataLogger, _navigatorLogFieldEnableMap, _navigatorPopulationDataLogger,
                _navigatorGenomeDataLogger, _navigatorSimulationTrialDataLogger);

            // Create the maze queueing evolution algorithm
            AbstractEvolutionAlgorithm<MazeGenome> mazeEvolutionAlgorithm = new QueueEvolutionAlgorithm<MazeGenome>(
                mazeEaParams, new MazeAlgorithmStats(mazeEaParams), null, MazeBatchSize, RunPhase.Primary,
                _mazeEvolutionDataLogger, _mazeLogFieldEnableMap, _mazePopulationDataLogger, _mazeGenomeDataLogger,
                _mazeSimulationTrialDataLogger);

            // Create the maze phenome evaluator
            IPhenomeEvaluator<MazeStructure, BehaviorInfo> mazeEvaluator =
                new MazeEnvironmentMCCEvaluator(MinSuccessDistance, BehaviorCharacterizationFactory,
                    NumAgentSuccessCriteria, 0);

            // Create navigator phenome evaluator
            IPhenomeEvaluator<IBlackBox, BehaviorInfo> navigatorEvaluator =
                new MazeNavigatorMCCEvaluator(MinSuccessDistance, BehaviorCharacterizationFactory,
                    NumMazeSuccessCriteria, _resourceLimit, resourceUsageLogger: _mazeResourceUsageLogger);

            // Create maze genome decoder
            IGenomeDecoder<MazeGenome, MazeStructure> mazeGenomeDecoder = new MazeDecoder(MazeScaleMultiplier);

            // Create navigator genome decoder
            IGenomeDecoder<NeatGenome, IBlackBox> navigatorGenomeDecoder = new NeatGenomeDecoder(ActivationScheme);

            // Create the maze genome evaluator
            IGenomeEvaluator<MazeGenome> mazeFitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<MazeGenome, MazeStructure>(mazeGenomeDecoder, mazeEvaluator,
                    SearchType.MinimalCriteriaSearch, ParallelOptions);

            // Create navigator genome evaluator
            IGenomeEvaluator<NeatGenome> navigatorFitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(navigatorGenomeDecoder, navigatorEvaluator,
                    SearchType.MinimalCriteriaSearch, ParallelOptions);

            // Verify that the seed agent population satisfies MC constraints of both populations so that MCC starts in
            // a valid state
            if (isAgentListPreevolved &&
                VerifyPreevolvedSeedAgents(genomeList1, genomeList2, navigatorFitnessEvaluator, mazeFitnessEvaluator) ==
                false)
            {
                throw new SharpNeatException("Seed agent population failed viability verification.");
            }

            // Create the MCC container
            IMCCAlgorithmContainer<NeatGenome, MazeGenome> mccAlgorithmContainer =
                new MCCAlgorithmContainer<NeatGenome, MazeGenome>(neatEvolutionAlgorithm, mazeEvolutionAlgorithm);

            // Initialize the container and component algorithms
            mccAlgorithmContainer.Initialize(navigatorFitnessEvaluator, genomeFactory1, seedAgentPopulation,
                AgentDefaultPopulationSize, mazeFitnessEvaluator, genomeFactory2, genomeList2,
                MazeDefaultPopulationSize,
                MaxGenerations, MaxEvaluations);

            return mccAlgorithmContainer;
        }

        #endregion
    }
}