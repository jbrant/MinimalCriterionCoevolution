using System.Collections.Generic;
using System.Xml;
using MCC_Domains.BodyBrain.MCCExperiment;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.Decoders.Voxel;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.Statistics;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Network;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain
{
    public class BodyBrainExperiment : BaseBodyBrainExperiment
    {
        #region Public Methods

        public override void Initialize(string name, int run, string simConfigDirectory, string simResultsDirectory,
            string simExecutableFile, XmlElement xmlConfig, string logFileDirectory)
        {
            // Initialize boiler-plate parameters
            base.Initialize(name, run, simConfigDirectory, simResultsDirectory, simExecutableFile, xmlConfig);

            // Initialize the data loggers for the given experiment/run
            _brainEvolutionDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - BrainEvolution.csv");
            _brainPopulationDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - BrainPopulation.csv");
            _brainGenomeDataLogger = new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - BrainGenomes.csv");
            _brainSimulationTrialDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - BrainTrials.csv");
            _bodyEvolutionDataLogger = new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - BodyEvolution.csv");
            _bodyPopulationDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - BodyPopulation.csv");
            _bodyGenomeDataLogger = new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - BodyGenomes.csv");
            _bodySimulationTrialDataLogger =
                new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - BodyTrials.csv");
            _bodyResourceUsageLogger = new FileDataLogger($"{logFileDirectory}\\{name} - Run{run} - ResourceUsage.csv");

            // Create new evolution field elements map with all fields enabled
            _brainLogFieldEnableMap = EvolutionFieldElements.PopulateEvolutionFieldElementsEnableMap();

            // Add default evolution logging configuration specific to body-brain experiment
            foreach (var evolutionLoggingPair in
                BodyBrainEvolutionFieldElements.PopulateEvolutionFieldElementsEnableMap())
            {
                _brainLogFieldEnableMap.Add(evolutionLoggingPair.Key, evolutionLoggingPair.Value);
            }

            // Add default population logging configuration
            foreach (var populationLoggingPair in PopulationFieldElements.PopulatePopulationFieldElementsEnableMap())
            {
                _brainLogFieldEnableMap.Add(populationLoggingPair.Key, populationLoggingPair.Value);
            }

            // Add default genome logging configuration
            foreach (var genomeLoggingPair in GenomeFieldElements.PopulateGenomeFieldElementsEnableMap())
            {
                _brainLogFieldEnableMap.Add(genomeLoggingPair.Key, genomeLoggingPair.Value);
            }

            // Add default trial logging configuration
            foreach (var trialLoggingPair in
                SimulationTrialFieldElements.PopulateSimulationTrialFieldElementsEnableMap())
            {
                _brainLogFieldEnableMap.Add(trialLoggingPair.Key, trialLoggingPair.Value);
            }

            // Disable logging fields not relevant to brain evolution in MCC experiment
            _brainLogFieldEnableMap[EvolutionFieldElements.SpecieCount] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.AsexualOffspringCount] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.SexualOffspringCount] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.InterspeciesOffspringCount] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaThreshold] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointX] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.MinimalCriteriaPointY] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.MaxFitness] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.MeanFitness] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.MeanSpecieChampFitness] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.MinSpecieSize] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.MaxSpecieSize] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.ChampGenomeGenomeId] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.ChampGenomeFitness] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.ChampGenomeBirthGeneration] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.ChampGenomeConnectionGeneCount] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.ChampGenomeNeuronGeneCount] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.ChampGenomeTotalGeneCount] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.ChampGenomeEvaluationCount] = false;
            _brainLogFieldEnableMap[EvolutionFieldElements.ChampGenomeXml] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MinVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MaxVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MeanVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MinFullProportion] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MaxFullProportion] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MeanFullProportion] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MinActiveVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MaxActiveVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MeanActiveVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MinPassiveVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MaxPassiveVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MeanPassiveVoxels] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MinActiveVoxelProportion] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MaxActiveVoxelProportion] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MeanActiveVoxelProportion] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MinPassiveVoxelProportion] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MaxPassiveVoxelProportion] = false;
            _brainLogFieldEnableMap[BodyBrainEvolutionFieldElements.MeanPassiveVoxelProportion] = false;

            // Create a body logger configuration, starting from the brain configuration but with pertinent fields enabled
            _bodyLogFieldEnableMap = new Dictionary<FieldElement, bool>(_brainLogFieldEnableMap)
            {
                [EvolutionFieldElements.RunPhase] = false,
                [PopulationFieldElements.RunPhase] = false,
                [BodyBrainEvolutionFieldElements.MinVoxels] = true,
                [BodyBrainEvolutionFieldElements.MaxVoxels] = true,
                [BodyBrainEvolutionFieldElements.MeanVoxels] = true,
                [BodyBrainEvolutionFieldElements.MaxVoxels] = true,
                [BodyBrainEvolutionFieldElements.MinFullProportion] = true,
                [BodyBrainEvolutionFieldElements.MaxFullProportion] = true,
                [BodyBrainEvolutionFieldElements.MeanFullProportion] = true,
                [BodyBrainEvolutionFieldElements.MinActiveVoxels] = true,
                [BodyBrainEvolutionFieldElements.MaxActiveVoxels] = true,
                [BodyBrainEvolutionFieldElements.MeanActiveVoxels] = true,
                [BodyBrainEvolutionFieldElements.MinPassiveVoxels] = true,
                [BodyBrainEvolutionFieldElements.MaxPassiveVoxels] = true,
                [BodyBrainEvolutionFieldElements.MeanPassiveVoxels] = true,
                [BodyBrainEvolutionFieldElements.MinActiveVoxelProportion] = true,
                [BodyBrainEvolutionFieldElements.MaxActiveVoxelProportion] = true,
                [BodyBrainEvolutionFieldElements.MeanActiveVoxelProportion] = true,
                [BodyBrainEvolutionFieldElements.MinPassiveVoxelProportion] = true,
                [BodyBrainEvolutionFieldElements.MaxPassiveVoxelProportion] = true,
                [BodyBrainEvolutionFieldElements.MeanPassiveVoxelProportion] = true
            };

            // Validate configuration parameter settings
            if (ValidateConfigParameters(out var errorMessagee)) throw new ConfigurationException(errorMessagee);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates a new CPPN genome factory for brains.
        /// </summary>
        /// <returns>The constructed brain genome factory.</returns>
        public override IGenomeFactory<NeatGenome> CreateBrainGenomeFactory()
        {
            return new CppnGenomeFactory(BrainCppnInputCount, BrainCppnOutputCount,
                DefaultActivationFunctionLibrary.CreateLibraryCppn(), NeatGenomeParameters);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates a new CPPN genome factory for bodies.
        /// </summary>
        /// <returns>The constructed body genome factory.</returns>
        public override IGenomeFactory<NeatGenome> CreateBodyGenomeFactory()
        {
            return new CppnGenomeFactory(BodyCppnInputCount, BodyCppnOutputCount,
                DefaultActivationFunctionLibrary.CreateLibraryCppn(), NeatGenomeParameters);
        }

        /// <summary>
        ///     Creates the evolution algorithm container using the given factories and genome lists.
        /// </summary>
        /// <param name="brainGenomeFactory">The brain genome factory.</param>
        /// <param name="bodyGenomeFactory">The body genome factory.</param>
        /// <param name="brainGenomes">The brain genome list.</param>
        /// <param name="bodyGenomes">The body genome list.</param>
        /// <returns>The instantiated MCC algorithm container.</returns>
        public override IMCCAlgorithmContainer<NeatGenome, NeatGenome> CreateMCCAlgorithmContainer(
            IGenomeFactory<NeatGenome> brainGenomeFactory, IGenomeFactory<NeatGenome> bodyGenomeFactory,
            List<NeatGenome> brainGenomes, List<NeatGenome> bodyGenomes)
        {
            List<NeatGenome> seedBodyPopulation;
            List<NeatGenome> seedBrainPopulation;

            // Create the brain genome decoder
            IGenomeDecoder<NeatGenome, VoxelBrain> brainGenomeDecoder = new VoxelBrainDecoder(ActivationScheme,
                SimulationProperties.InitialXDimension, SimulationProperties.InitialYDimension,
                SimulationProperties.InitialZDimension, SimulationProperties.NumBrainConnections);

            // Create the body genome decoder
            IGenomeDecoder<NeatGenome, VoxelBody> bodyGenomeDecoder = new VoxelBodyDecoder(ActivationScheme,
                SimulationProperties.InitialXDimension, SimulationProperties.InitialYDimension,
                SimulationProperties.InitialZDimension);

            // If both an initial body and brain population are specified, use them as the seed
            if (bodyGenomes != null && bodyGenomes.Count >= BodySeedGenomeCount && brainGenomes != null &&
                brainGenomes.Count >= BrainSeedGenomeCount)
            {
                seedBrainPopulation = brainGenomes;
                seedBodyPopulation = bodyGenomes;
            }
            // Otherwise, bodies will need to be generated and brains evolved to solve them
            else
            {
                var bodyBrainTuple = EvolveSeedBodyBrains(bodyGenomeFactory, bodyGenomeDecoder, brainGenomeFactory,
                    brainGenomeDecoder);
                seedBodyPopulation = bodyBrainTuple.Item1;
                seedBrainPopulation = bodyBrainTuple.Item2;
            }

            // Set dummy fitness so that seed bodies will be marked as evaluated
            foreach (var bodyGenome in seedBodyPopulation)
            {
                bodyGenome.EvaluationInfo.SetFitness(0);
            }

            // Create the NEAT parameters
            var eaParams = new EvolutionAlgorithmParameters
            {
                SpecieCount = 0
            };

            // Create the NEAT EA for brains
            AbstractEvolutionAlgorithm<NeatGenome> brainEvolutionAlgorithm =
                new QueueEvolutionAlgorithm<NeatGenome>(eaParams, new NeatAlgorithmStats(eaParams), null, null,
                    BrainBatchSize, RunPhase.Primary, _brainEvolutionDataLogger, _brainLogFieldEnableMap,
                    _brainPopulationDataLogger, _brainGenomeDataLogger, _brainSimulationTrialDataLogger);

            // Create the NEAT EA for bodies
            AbstractEvolutionAlgorithm<NeatGenome> bodyEvolutionAlgorithm =
                new QueueEvolutionAlgorithm<NeatGenome>(eaParams,
                    new VoxelBodyAlgorithmStats(eaParams, bodyGenomeDecoder), null, null, BodyBatchSize,
                    RunPhase.Primary, _bodyEvolutionDataLogger, _bodyLogFieldEnableMap, _bodyPopulationDataLogger,
                    _bodyGenomeDataLogger, _bodySimulationTrialDataLogger);

            // Create the brain phenome evaluator
            IPhenomeEvaluator<VoxelBrain, BehaviorInfo> brainEvaluator = new BrainEvaluator(SimulationProperties,
                MinAmbulationDistance, NumBodySuccessCriteria, Name, Run, ResourceLimit,
                resourceUsageLogger: _bodyResourceUsageLogger);

            // Create the body phenome evaluator
            IPhenomeEvaluator<VoxelBody, BehaviorInfo> bodyEvaluator = new BodyEvaluator(SimulationProperties,
                MinAmbulationDistance, NumBrainSuccessCriteria, Name, Run);

            // Create the brain genome evaluator
            IGenomeEvaluator<NeatGenome> brainViabilityEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, VoxelBrain>(brainGenomeDecoder, brainEvaluator,
                    SearchType.MinimalCriteriaSearch, ParallelOptions);

            // Create the body genome evaluator
            IGenomeEvaluator<NeatGenome> bodyViabilityEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, VoxelBody>(bodyGenomeDecoder, bodyEvaluator,
                    SearchType.MinimalCriteriaSearch, ParallelOptions);

            // Verify that both populations satisfy their MC so that MCC starts in a valid state
            if (VerifyPreevolvedSeedPopulations(seedBrainPopulation, seedBodyPopulation, brainViabilityEvaluator,
                    bodyViabilityEvaluator) == false)
            {
                throw new SharpNeatException("Seed brain/body populations failed viability verification.");
            }

            // Create the MCC container
            IMCCAlgorithmContainer<NeatGenome, NeatGenome> mccAlgorithmContainer =
                new MCCAlgorithmContainer<NeatGenome, NeatGenome>(brainEvolutionAlgorithm, bodyEvolutionAlgorithm);

            // Initialize the container and component algorithms
            mccAlgorithmContainer.Initialize(brainViabilityEvaluator, brainGenomeFactory, seedBrainPopulation,
                BrainDefaultPopulationSize, bodyViabilityEvaluator, bodyGenomeFactory, seedBodyPopulation,
                BodyDefaultPopulationSize, MaxBatches, null);

            return mccAlgorithmContainer;
        }

        #endregion

        #region Constants

        /// <summary>
        ///     The number of brain CPPN inputs (x/y/z location, distance and bias).
        /// </summary>
        private const int BrainCppnInputCount = 5;

        /// <summary>
        ///     The number of brain CPPN outputs (presence/weights of controller connections).
        /// </summary>
        private const int BrainCppnOutputCount = 32;

        /// <summary>
        ///     The number of body CPPN inputs (x/y/z location, distance and bias).
        /// </summary>
        private const int BodyCppnInputCount = 5;

        /// <summary>
        ///     The number of body CPPN outputs (material presence and type).
        /// </summary>
        private const int BodyCppnOutputCount = 2;

        #endregion

        #region Instance variables

        /// <summary>
        ///     Logs statistics about the brain populations for every batch.
        /// </summary>
        private IDataLogger _brainEvolutionDataLogger;

        /// <summary>
        ///     Logs the IDs of the extant brain population at every interval.
        /// </summary>
        private IDataLogger _brainPopulationDataLogger;

        /// <summary>
        ///     Logs the definitions of the brain population over the course of a run.
        /// </summary>
        private IDataLogger _brainGenomeDataLogger;

        /// <summary>
        ///     Logs the details and results of trials within a brain evaluation.
        /// </summary>
        private IDataLogger _brainSimulationTrialDataLogger;

        /// <summary>
        ///     Logs statistics about the body populations for every batch.
        /// </summary>
        private IDataLogger _bodyEvolutionDataLogger;

        /// <summary>
        ///     Logs the IDs of the extant body population at every interval.
        /// </summary>
        private IDataLogger _bodyPopulationDataLogger;

        /// <summary>
        ///     Logs the definitions of the body population over the course of a run.
        /// </summary>
        private IDataLogger _bodyGenomeDataLogger;

        /// <summary>
        ///     Logs the body resource usage over the course of a run.
        /// </summary>
        private IDataLogger _bodyResourceUsageLogger;

        /// <summary>
        ///     Logs the details and results of trials within a body evaluation.
        /// </summary>
        private IDataLogger _bodySimulationTrialDataLogger;

        /// <summary>
        ///     Dictionary which indicates logger fields to be enabled/disabled for brain genomes.
        /// </summary>
        private IDictionary<FieldElement, bool> _brainLogFieldEnableMap;

        /// <summary>
        ///     Dictionary which indicates logger fields to be enabled/disabled for body genomes.
        /// </summary>
        private IDictionary<FieldElement, bool> _bodyLogFieldEnableMap;

        #endregion
    }
}