#region

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
    public class MCCSpeciationExperiment : BaseMCCMazeNavigationExperiment
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

            // Check species range constraints
            if (_agentNumSpecies < 0)
                message = $"Agent species count [{_agentNumSpecies}], if specified, must be a zero or positive integer";
            else if (_mazeNumSpecies < 0)
                message = $"Maze species count [{_mazeNumSpecies}], if specified, must be a zero or positive integer";
            else if (_agentNumSpecies > AgentSeedGenomeCount)
                message =
                    $"Agent species count [{_agentNumSpecies}] must be no greater than the agent seed genome count [{AgentSeedGenomeCount}] (otherwise there will be empty species)";
            else if (_mazeNumSpecies > MazeSeedGenomeCount)
                message =
                    $"Maze species count [{_mazeNumSpecies}] must be no greater than the maze seed genome count [{MazeSeedGenomeCount}] (otherwise there will be empty species)";
            // Ensure that batch size evenly divisible by number of species
            else if (NavigatorBatchSize % _agentNumSpecies != 0)
                message =
                    $"Agent batch size [{NavigatorBatchSize}] must be evenly divisible by the number of species [{_agentNumSpecies}]";
            else if (MazeBatchSize % _mazeNumSpecies != 0)
                message =
                    $"Maze batch size [{MazeBatchSize}] must be evenly divisible by the number of species [{_mazeNumSpecies}]";
            // Check base class parameters
            else if (base.ValidateConfigParameters(out var errorMessage))
                message = errorMessage;

            // Return configuration validity status based on whether an error message was set
            return message != null;
        }

        #endregion

        #region Private members

        /// <summary>
        ///     The number of species in the agent queue.
        /// </summary>
        private int _agentNumSpecies;

        /// <summary>
        ///     The number of species in the maze queue.
        /// </summary>
        private int _mazeNumSpecies;

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

            // Read the number of agent and maze species
            _agentNumSpecies = XmlUtils.GetValueAsInt(xmlConfig, "AgentNumSpecies");
            _mazeNumSpecies = XmlUtils.GetValueAsInt(xmlConfig, "MazeNumSpecies");

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

            // Create new evolution field elements map with all fields enabled
            _navigatorLogFieldEnableMap = MazeNavEvolutionFieldElements.PopulateEvolutionFieldElementsEnableMap();

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

            // Disable logging fields not relevant to agent evolution in MCC experiment
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.SpecieCount] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.AsexualOffspringCount] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.SexualOffspringCount] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.InterspeciesOffspringCount] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinimalCriteriaThreshold] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinimalCriteriaPointX] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinimalCriteriaPointY] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MaxFitness] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MeanFitness] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MeanSpecieChampFitness] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MinSpecieSize] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.MaxSpecieSize] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.ChampGenomeGenomeId] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.ChampGenomeFitness] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.ChampGenomeBirthGeneration] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.ChampGenomeConnectionGeneCount] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.ChampGenomeNeuronGeneCount] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.ChampGenomeTotalGeneCount] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.ChampGenomeEvaluationCount] = false;
            _navigatorLogFieldEnableMap[MazeNavEvolutionFieldElements.ChampGenomeXml] = false;
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
                [MazeNavEvolutionFieldElements.RunPhase] = false,
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
                : EvolveSeedAgents(genomeList1, genomeList2, genomeFactory1, AgentSeedGenomeCount);

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
                SpecieCount = _agentNumSpecies,
                MaxSpecieSize = AgentDefaultPopulationSize / _agentNumSpecies
            };

            // Create the maze evolution algorithm parameters
            var mazeEaParams = new EvolutionAlgorithmParameters
            {
                SpecieCount = _mazeNumSpecies,
                MaxSpecieSize = MazeDefaultPopulationSize / _mazeNumSpecies
            };

            // Create the NEAT (i.e. navigator) queueing evolution algorithm
            AbstractEvolutionAlgorithm<NeatGenome> neatEvolutionAlgorithm = new QueueEvolutionAlgorithm<NeatGenome>(
                neatEaParams, new NeatAlgorithmStats(neatEaParams),
                new ParallelKMeansClusteringStrategy<NeatGenome>(new ManhattanDistanceMetric(1.0, 0.0, 10.0),
                    ParallelOptions), null, NavigatorBatchSize, RunPhase.Primary, _navigatorEvolutionDataLogger,
                _navigatorLogFieldEnableMap, _navigatorPopulationDataLogger, _navigatorGenomeDataLogger,
                _navigatorSimulationTrialDataLogger);

            // Create the maze queueing evolution algorithm
            AbstractEvolutionAlgorithm<MazeGenome> mazeEvolutionAlgorithm = new QueueEvolutionAlgorithm<MazeGenome>(
                mazeEaParams, new MazeAlgorithmStats(mazeEaParams),
                new ParallelKMeansClusteringStrategy<MazeGenome>(new ManhattanDistanceMetric(1.0, 0.0, 10.0),
                    ParallelOptions), null, MazeBatchSize, RunPhase.Primary, _mazeEvolutionDataLogger,
                _mazeLogFieldEnableMap, _mazePopulationDataLogger, _mazeGenomeDataLogger,
                _mazeSimulationTrialDataLogger);

            // Create the maze phenome evaluator
            IPhenomeEvaluator<MazeStructure, BehaviorInfo> mazeEvaluator =
                new MazeEnvironmentMCCEvaluator(MinSuccessDistance, BehaviorCharacterizationFactory,
                    NumAgentSuccessCriteria, 0);

            // Create navigator phenome evaluator
            IPhenomeEvaluator<IBlackBox, BehaviorInfo> navigatorEvaluator =
                new MazeNavigatorMCCEvaluator(MinSuccessDistance, BehaviorCharacterizationFactory,
                    NumMazeSuccessCriteria);

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