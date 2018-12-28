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
        #region Private members

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
        ///     Dictionary which indicates logger fields to be enabled/disabled for navigator genomes.
        /// </summary>
        private IDictionary<FieldElement, bool> _navigatorLogFieldEnableMap;

        /// <summary>
        ///     Dictionary which indicates logger fields to be enabled/disabled for maze genomes.
        /// </summary>
        private IDictionary<FieldElement, bool> _mazeLogFieldEnableMap;

        /// <summary>
        ///     Controls the number of batches between population definitions (i.e. genome XML) being logged.
        /// </summary>
        private int? _populationLoggingBatchInterval;

        #endregion

        #region Overridden methods

        /// <inheritdoc />
        /// <summary>
        ///     Initializes the MCC maze navigation experiment by reading in all of the configuration parameters and
        ///     setting up the bootstrapping/initialization algorithm.
        /// </summary>
        /// <param name="name">The name of the experiment.</param>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="navigatorEvolutionLogger">The navigator evolution data logger.</param>
        /// <param name="navigatorPopulationLogger">The navigator population logger.</param>
        /// <param name="navigatorGenomeLogger">The navigator genome logger.</param>
        /// <param name="mazeEvolutionLogger">The maze evolution data logger.</param>
        /// <param name="mazePopulationLogger">The maze population logger.</param>
        /// <param name="mazeGenomeLogger">The maze genome logger.</param>
        public override void Initialize(string name, XmlElement xmlConfig, IDataLogger navigatorEvolutionLogger = null,
            IDataLogger navigatorPopulationLogger = null, IDataLogger navigatorGenomeLogger = null,
            IDataLogger mazeEvolutionLogger = null, IDataLogger mazePopulationLogger = null,
            IDataLogger mazeGenomeLogger = null)
        {
            base.Initialize(name, xmlConfig, navigatorEvolutionLogger, navigatorGenomeLogger, mazeEvolutionLogger,
                mazeGenomeLogger);

            // Read in log file path/name
            _navigatorEvolutionDataLogger = navigatorEvolutionLogger ??
                                            ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution,
                                                "NavigatorLoggingConfig");
            _navigatorPopulationDataLogger = navigatorPopulationLogger ?? ExperimentUtils.ReadDataLogger(xmlConfig,
                                                 LoggingType.Population, "NavigatorLoggingConfig");
            _navigatorGenomeDataLogger = navigatorGenomeLogger ??
                                         ExperimentUtils.ReadDataLogger(xmlConfig,
                                             LoggingType.Genome, "NavigatorLoggingConfig");
            _mazeEvolutionDataLogger = mazeEvolutionLogger ??
                                       ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution,
                                           "MazeLoggingConfig");
            _mazePopulationDataLogger = mazePopulationLogger ??
                                        ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Population,
                                            "MazeLoggingConfig");
            _mazeGenomeDataLogger = mazeGenomeLogger ??
                                    ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Genome,
                                        "MazeLoggingConfig");

            // Create new evolution field elements map with all fields enabled
            _navigatorLogFieldEnableMap = EvolutionFieldElements.PopulateEvolutionFieldElementsEnableMap();

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
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeBehaviorX] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeBehaviorY] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeDistanceToTarget] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.ChampGenomeXml] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinWalls] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxWalls] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanWalls] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinWaypoints] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxWaypoints] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanWaypoints] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinJunctures] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxJunctures] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanJunctures] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinTrajectoryFacingOpenings] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxTrajectoryFacingOpenings] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanTrajectoryFacingOpenings] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinHeight] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxHeight] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanHeight] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MinWidth] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MaxWidth] = false;
            _navigatorLogFieldEnableMap[EvolutionFieldElements.MeanWidth] = false;

            // Create a maze logger configuration with the same configuration as the navigator one
            _mazeLogFieldEnableMap = new Dictionary<FieldElement, bool>(_navigatorLogFieldEnableMap)
            {
                [EvolutionFieldElements.RunPhase] = false,
                [PopulationFieldElements.RunPhase] = false,
                [EvolutionFieldElements.MinWalls] = true,
                [EvolutionFieldElements.MaxWalls] = true,
                [EvolutionFieldElements.MeanWalls] = true,
                [EvolutionFieldElements.MinWaypoints] = true,
                [EvolutionFieldElements.MaxWaypoints] = true,
                [EvolutionFieldElements.MeanWaypoints] = true,
                [EvolutionFieldElements.MinJunctures] = true,
                [EvolutionFieldElements.MaxJunctures] = true,
                [EvolutionFieldElements.MeanJunctures] = true,
                [EvolutionFieldElements.MinTrajectoryFacingOpenings] = true,
                [EvolutionFieldElements.MaxTrajectoryFacingOpenings] = true,
                [EvolutionFieldElements.MeanTrajectoryFacingOpenings] = true,
                [EvolutionFieldElements.MinHeight] = true,
                [EvolutionFieldElements.MaxHeight] = true,
                [EvolutionFieldElements.MeanHeight] = true,
                [EvolutionFieldElements.MinWidth] = true,
                [EvolutionFieldElements.MaxWidth] = true,
                [EvolutionFieldElements.MeanWidth] = true
            };

            // Read in the number of batches between population logging
            _populationLoggingBatchInterval = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationLoggingBatchInterval");
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
                MazeWidth);

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
                SpecieCount = AgentNumSpecies,
                MaxSpecieSize = AgentDefaultPopulationSize / AgentNumSpecies
            };

            // Create the maze evolution algorithm parameters
            var mazeEaParams = new EvolutionAlgorithmParameters
            {
                SpecieCount = MazeNumSpecies,
                MaxSpecieSize = MazeDefaultPopulationSize / MazeNumSpecies
            };

            // Create the NEAT (i.e. navigator) queueing evolution algorithm
            AbstractEvolutionAlgorithm<NeatGenome> neatEvolutionAlgorithm = new QueueEvolutionAlgorithm<NeatGenome>(
                neatEaParams, new NeatAlgorithmStats(neatEaParams),
                new ParallelKMeansClusteringStrategy<NeatGenome>(new ManhattanDistanceMetric(1.0, 0.0, 10.0),
                    ParallelOptions), null, NavigatorBatchSize, RunPhase.Primary, _navigatorEvolutionDataLogger,
                _navigatorLogFieldEnableMap, _navigatorPopulationDataLogger, _navigatorGenomeDataLogger,
                _populationLoggingBatchInterval);

            // Create the maze queueing evolution algorithm
            AbstractEvolutionAlgorithm<MazeGenome> mazeEvolutionAlgorithm = new QueueEvolutionAlgorithm<MazeGenome>(
                mazeEaParams, new MazeAlgorithmStats(mazeEaParams),
                new ParallelKMeansClusteringStrategy<MazeGenome>(new ManhattanDistanceMetric(1.0, 0.0, 10.0),
                    ParallelOptions), null, MazeBatchSize, RunPhase.Primary, _mazeEvolutionDataLogger,
                _mazeLogFieldEnableMap, _mazePopulationDataLogger, _mazeGenomeDataLogger,
                _populationLoggingBatchInterval);

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
                new ParallelGenomeBehaviorEvaluator<MazeGenome, MazeStructure>(mazeGenomeDecoder, mazeEvaluator, SearchType.MinimalCriteriaSearch, ParallelOptions);

            // Create navigator genome evaluator
            IGenomeEvaluator<NeatGenome> navigatorFitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(navigatorGenomeDecoder, navigatorEvaluator, SearchType.MinimalCriteriaSearch, ParallelOptions);

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