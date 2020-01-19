using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MCC_Domains.BodyBrain.MCCExperiment;
using MCC_Domains.Utils;
using Redzen.Random;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Substrate;
using SharpNeat.Decoders.Neat;
using SharpNeat.Genomes.Substrate;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Voxels;
using SharpNeat.Utility;

namespace MCC_Domains.BodyBrain
{
    public abstract class BaseBodyBrainExperiment : IBodyBrainExperiment
    {
        #region Public properties

        /// <inheritdoc />
        /// <summary>
        ///     The name of the experiment.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     The default (max) brain population size.
        /// </summary>
        public int BrainDefaultPopulationSize { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     The default (max) body population size.
        /// </summary>
        public int BodyDefaultPopulationSize { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     The number of CPPN genomes in the brain seed population.
        /// </summary>
        public int BrainSeedGenomeCount { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     The number of CPPN genomes in the body seed population.
        /// </summary>
        public int BodySeedGenomeCount { get; private set; }

        #endregion

        #region Protected members

        /// <summary>
        ///     The current run number.
        /// </summary>
        protected int Run { get; private set; }

        /// <summary>
        ///     Switches between synchronous and asynchronous execution (with user-defined number of threads).
        /// </summary>
        protected ParallelOptions ParallelOptions;

        /// <summary>
        ///     The NEAT genome parameters to use for the experiment.
        /// </summary>
        protected NeatGenomeParameters NeatGenomeParameters;

        /// <summary>
        /// The substrate-specific mutation probabilities.
        /// </summary>
        protected NeatSubstrateGenomeParameters BodyGenomeParameters;

        /// <summary>
        /// The maximum size of a voxel body along each dimension.
        /// </summary>
        protected int MaxBodySize;
        
        /// <summary>
        /// The body decoder converting CPPN genomes to voxel bodies.
        /// </summary>
        protected IGenomeDecoder<NeatSubstrateGenome, IBlackBoxSubstrate> BodyDecoder;
        
        /// <summary>
        /// The brain decoder converting CPPN genomes to voxel controllers.
        /// </summary>
        protected IGenomeDecoder<NeatGenome, IBlackBox> BrainDecoder;

        /// <summary>
        ///     The resource limit for bodies (i.e. the maximum number of times that a body can be used by a brain for satisfying
        ///     the brain's MC).
        /// </summary>
        protected int ResourceLimit;

        /// <summary>
        ///     The number of brains to be evaluated in a single evaluation "batch".
        /// </summary>
        protected int BrainBatchSize;

        /// <summary>
        ///     The number of bodies to be evaluated in a single evaluation "batch".
        /// </summary>
        protected int BodyBatchSize;

        /// <summary>
        ///     The maximum number of batches allowed (optional).
        /// </summary>
        protected int? MaxBatches;

        /// <summary>
        ///     The minimum distance a body must traverse in the environment to meet the body and its controller to meet their MC.
        /// </summary>
        protected double MinAmbulationDistance;

        /// <summary>
        ///     The minimum number of bodies that the brain under evaluation must solve in order to meet the minimal criteria.
        /// </summary>
        protected int NumBodySuccessCriteria;

        /// <summary>
        ///     The minimum number of brains that must solve the body under evaluation in order to meet this portion of the minimal
        ///     criteria.
        /// </summary>
        protected int NumBrainSuccessCriteria;

        /// <summary>
        ///     Collection of simulator configuration properties, including result output directories, configuration files and
        ///     config file parsing information.
        /// </summary>
        protected SimulationProperties SimulationProperties;

        #endregion

        #region Instance variables

        /// <summary>
        ///     The maximum number of brain evolution restarts on a particular, randomly-generated body before it is discarded and
        ///     another generated.
        /// </summary>
        private int _maxBodyInitializationRestarts;

        /// <summary>
        ///     The maximum number of initialization evaluations allowed for a population of randomly-generated brains before they
        ///     are discarded and evolution is restarted.
        /// </summary>
        private ulong _maxBrainInitializationEvaluations;

        /// <summary>
        ///     Initialization algorithm for producing an initial population with the requisite number of viable genomes.
        /// </summary>
        private BodyBrainInitializer _bodyBrainInitializer;

        /// <summary>
        ///     The number of brains needed for the initialization algorithm.
        /// </summary>
        private int _brainInitializationGenomeCount;
        
        /// <summary>
        ///     The activation scheme (i.e. cyclic or acyclic).
        /// </summary>
        private NetworkActivationScheme _activationScheme;

        #endregion

        #region Public methods

        public abstract void Initialize(string name, int run, string simConfigDirectory, string simResultsDirectory,
            string simExecutableFile, XmlElement xmlConfig, string logFileDirectory);

        /// <summary>
        ///     Initializes the MCC body/brain coevolution experiment by reading in all of the configuration parameters and setting
        ///     up the bootstrap/initialization algorithm.
        /// </summary>
        /// <param name="name">The name of the experiment to be executed.</param>
        /// <param name="run">The run number to be executed.</param>
        /// <param name="simConfigDirectory">The path to the directory for writing generated simulation configuration files.</param>
        /// <param name="simResultsDirectory">The path to the directory for writing simulation results.</param>
        /// <param name="simExecutableFile">The path to the executable simulator.</param>
        /// <param name="xmlConfig">The reference to the XML experiment configuration file.</param>
        protected void Initialize(string name, int run, string simConfigDirectory, string simResultsDirectory,
            string simExecutableFile, XmlElement xmlConfig)
        {
            // Set boiler plate properties
            Name = name;
            Run = run;
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            ParallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);

            // Set the genome parameters
            NeatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);
            NeatGenomeParameters.FeedforwardOnly = _activationScheme.AcyclicNetwork;
            
            // Set body mutation parameters
            BodyGenomeParameters =
                BodyBrainExperimentUtils.ReadBodyGenomeParameters(xmlConfig);
            
            // Set max body size
            MaxBodySize = XmlUtils.GetValueAsInt(xmlConfig, "MaxBodySize");

            // Set resource limit parameter
            ResourceLimit = XmlUtils.GetValueAsInt(xmlConfig, "ResourceLimit");

            // Set evolutionary algorithm parameters
            BrainDefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "BrainPopulationSize");
            BodyDefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "BodyPopulationSize");
            BrainSeedGenomeCount = XmlUtils.GetValueAsInt(xmlConfig, "BrainSeedGenomeCount");
            BodySeedGenomeCount = XmlUtils.GetValueAsInt(xmlConfig, "BodySeedGenomeCount");
            BrainBatchSize = XmlUtils.GetValueAsInt(xmlConfig, "BrainOffspringBatchSize");
            BodyBatchSize = XmlUtils.GetValueAsInt(xmlConfig, "BodyOffspringBatchSize");
            
            // Set run-time bounding parameters
            MaxBatches = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxGenerations");

            // Set success/failure criteria constraints
            MinAmbulationDistance = XmlUtils.GetValueAsDouble(xmlConfig, "MinAmbulationDistance");
            NumBodySuccessCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumBodiesSolvedCriteria");
            NumBrainSuccessCriteria = XmlUtils.GetValueAsInt(xmlConfig, "NumBrainsSolvedCriteria");

            // Set simulation directories and configuration properties
            SimulationProperties = BodyBrainExperimentUtils.ReadSimulationProperties(xmlConfig, simConfigDirectory,
                simResultsDirectory, simExecutableFile);

            // Set the maximum number of initialization iterations/evaluations
            _maxBodyInitializationRestarts = XmlUtils.GetValueAsInt(xmlConfig, "MaxBodyInitializationRestarts");
            _maxBrainInitializationEvaluations =
                XmlUtils.GetValueAsULong(xmlConfig, "MaxBrainInitializationEvaluations");

            // Initialize the initialization algorithm
            _bodyBrainInitializer = BodyBrainExperimentUtils.DetermineMCCBodyBrainInitializer(
                xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement,
                SimulationProperties.BrainType);

            // Setup initialization algorithm
            _bodyBrainInitializer.SetAlgorithmParameters(
                xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement,
                Name, run, _activationScheme.AcyclicNetwork, NumBrainSuccessCriteria);

            // Configure simulation
            _bodyBrainInitializer.SetSimulationParameters(MinAmbulationDistance, SimulationProperties);

            // The size of the randomly generated agent genome pool from which to evolve agent bootstraps
            _brainInitializationGenomeCount = _bodyBrainInitializer.PopulationSize;
            
            // Create body and brain genome decoders
            BrainDecoder = new NeatGenomeDecoder(_activationScheme);
            BodyDecoder = new NeatSubstrateGenomeDecoder(_activationScheme);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates a new CPPN genome factory for brains.
        /// </summary>
        /// <returns>The constructed brain genome factory.</returns>
        public abstract IGenomeFactory<NeatGenome> CreateBrainGenomeFactory();

        /// <inheritdoc />
        /// <summary>
        ///     Creates a new CPPN genome factory for bodies.
        /// </summary>
        /// <returns>The constructed body genome factory.</returns>
        public abstract IGenomeFactory<NeatSubstrateGenome> CreateBodyGenomeFactory();

        /// <inheritdoc />
        /// <summary>
        ///     Save a population of brain genomes to an XmlWriter.
        /// </summary>
        /// <param name="xw">Reference to the XML writer.</param>
        /// <param name="brainGenomeList">The list of brain genomes to write.</param>
        public void SaveBrainPopulation(XmlWriter xw, IList<NeatGenome> brainGenomeList)
        {
            NeatGenomeXmlIO.WriteComplete(xw, brainGenomeList, true);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Save a population of body genomes to an XmlWriter.
        /// </summary>
        /// <param name="xw">Reference to the XML writer.</param>
        /// <param name="bodyGenomeList">The list of body genomes to write.</param>
        public void SaveBodyPopulation(XmlWriter xw, IList<NeatSubstrateGenome> bodyGenomeList)
        {
            NeatSubstrateGenomeXmlIO.WriteComplete(xw, bodyGenomeList, true);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates the evolution algorithm container using the given factories and genome lists.
        /// </summary>
        /// <param name="brainGenomeFactory">The brain genome factory.</param>
        /// <param name="bodyGenomeFactory">The body genome factory.</param>
        /// <param name="brainGenomes">The brain genome list.</param>
        /// <param name="bodyGenomes">The body genome list.</param>
        /// <returns>The instantiated MCC algorithm container.</returns>
        public abstract IMCCAlgorithmContainer<NeatGenome, NeatSubstrateGenome> CreateMCCAlgorithmContainer(
            IGenomeFactory<NeatGenome> brainGenomeFactory, IGenomeFactory<NeatSubstrateGenome> bodyGenomeFactory,
            List<NeatGenome> brainGenomes, List<NeatSubstrateGenome> bodyGenomes);

        #endregion

        #region Protected methods

        /// <summary>
        ///     Checks ranges and other experiment settings to ensure that the configuration is valid.
        /// </summary>
        /// <param name="message">
        ///     Error message denoting specific configuration violation (only set if an invalid configuration was
        ///     identified).
        /// </param>
        /// <returns>Boolean flag indicating whether the experiment configuration is valid.</returns>
        protected bool ValidateConfigParameters(out string message)
        {
            // Set error message to null by default
            message = null;

            // Check population constraints
            if (BrainDefaultPopulationSize <= 0)
                message = $"Brain population size [{BrainDefaultPopulationSize}] must be a non-zero integer";
            else if (BodyDefaultPopulationSize <= 0)
                message = $"Body population size [{BodyDefaultPopulationSize}] must be a non-zero integer";
            // Check seed range constraints
            else if (BrainSeedGenomeCount <= 0)
                message = $"Brain seed genome count [{BrainSeedGenomeCount}] must be a non-zero integer";
            else if (BodySeedGenomeCount <= 0)
                message = $"Body seed genome count [{BodySeedGenomeCount}] must be a non-zero integer";
            else if (BrainSeedGenomeCount > BrainDefaultPopulationSize)
                message =
                    $"Brain seed genome count [{BrainSeedGenomeCount}] must be no greater than the brain population size [{BrainDefaultPopulationSize}]";
            else if (BodySeedGenomeCount > BodyDefaultPopulationSize)
                message =
                    $"Body seed genome count [{BodySeedGenomeCount}] must be no greater than the body population size [{BodyDefaultPopulationSize}]";
            // Check evaluation time range constraints
            else if (MaxBatches <= 0)
                message = $"Max batches [{MaxBatches}] must be an integer greater than 0";
            // Check minimal criterion constraints
            else if (NumBodySuccessCriteria > BodyDefaultPopulationSize)
                message =
                    $"Bodies solved minimal criterion [{NumBodySuccessCriteria}] must be no greater than the body population size";
            else if (NumBrainSuccessCriteria > BrainDefaultPopulationSize)
                message =
                    $"Brains solved minimal criterion [{NumBrainSuccessCriteria}] must be no greater than the brain population size";
            else if (MinAmbulationDistance <= 0)
                message = $"Ambulation distance [{MinAmbulationDistance}] must be greater than 0";
            // Check resource constraint setting
            else if (ResourceLimit < 1)
                message =
                    $"Resource limit [{ResourceLimit}] must be greater than 1, otherwise body cannot be used to satisfy the MC of any brain";
            else if (ResourceLimit * BodySeedGenomeCount < BrainSeedGenomeCount)
                message =
                    $"Product of resource limit [{ResourceLimit}] and maze seed genome count [{BodySeedGenomeCount}] must be at least as large as agent seed genome count [{BrainSeedGenomeCount}], otherwise not all agent seed genomes can be evolved";

            return message != null;
        }

        /// <summary>
        ///     Randomly generates bodies and evolves brains that can effectively ambulate each body to meet the MC. This is
        ///     repeated until the requisite number of seed bodies and seed brains is evolved (with one or more brains being able
        ///     to ambulate each of the bodies). A non-MCC based algorithm (such as fitness or novelty search) is used for this
        ///     bootstrap process.
        /// </summary>
        /// <param name="bodyGenomeFactory">The factory object for producing new CPPNs for the body population.</param>
        /// <param name="brainGenomeFactory">The factory object for producing new CPPNs for the brain population.</param>
        /// <returns>
        ///     A tuple containing a list of bodies (in the first position) and a list of brains (in the second position), with
        ///     each body being ambulated (such that it and the brain meet the MC) by one or more brains.
        /// </returns>
        protected Tuple<List<NeatSubstrateGenome>, List<NeatGenome>> EvolveSeedBodyBrains(
            IGenomeFactory<NeatSubstrateGenome> bodyGenomeFactory, IGenomeFactory<NeatGenome> brainGenomeFactory)
        {
            var seedBodyPopulation = new List<NeatSubstrateGenome>(BodySeedGenomeCount);
            var seedBrainPopulation = new List<NeatGenome>(BrainSeedGenomeCount);
            var bodySolutionCount = new Dictionary<uint, int>();

            // Compute the max number of brains that should be added per body to avoid exceeding the brain seed count
            var perBodyBrainCount = Math.Min(ResourceLimit,
                Convert.ToInt32(Math.Floor((double) BrainSeedGenomeCount / BodySeedGenomeCount)));

            for (var idx = 0; idx < BodySeedGenomeCount; idx++)
            {
                var viableBrainsEvolved = false;
                var numBodyRestarts = 0;

                do
                {
                    // Generate new body
                    var body = bodyGenomeFactory.CreateGenomeList(1, 0)[0];

                    // Extract the body ID
                    var bodyId = body.Id;

                    // Create population of randomly-initialized brain CPPNs
                    var brainPopulation = brainGenomeFactory.CreateGenomeList(_brainInitializationGenomeCount, 0);

                    Console.WriteLine($"Evolving viable brains for body population index {idx} with ID {bodyId}");

                    // Evolve the number of brains required to meet the success MC for the current body
                    var viableBrains = _bodyBrainInitializer.EvolveViableBrains(brainGenomeFactory,
                        brainPopulation.ToList(), BrainDecoder, new VoxelBody(BodyDecoder.Decode(body)),
                        _maxBrainInitializationEvaluations, ParallelOptions, _maxBodyInitializationRestarts);

                    // Proceed to the next iteration if no solutions were evolved
                    if (viableBrains == null)
                    {
                        // Increment restarts
                        numBodyRestarts++;

                        Console.WriteLine(
                            $"Restarting evolution [{numBodyRestarts}] times for body index [{idx}]");

                        continue;
                    }

                    viableBrainsEvolved = true;

                    // Add body genome to the seed body population
                    seedBodyPopulation.Add(body);

                    // Add body to dictionary of bodies with solutions and initialize solution count to 0
                    bodySolutionCount.Add(bodyId, 0);

                    // Add the viable brain genomes who solve the current body (but avoid adding duplicates,
                    // as identified by the genome ID). It is acceptable for the same brain to control multiple bodies,
                    // so those brains that solve the current body are left in the pool of seed genomes
                    foreach (var viableBrain in viableBrains
                        .Where(x => seedBrainPopulation.Select(sbp => sbp.Id).Contains(x.Id) == false)
                        .Take(perBodyBrainCount))
                    {
                        // Increment the number of body solutions
                        bodySolutionCount[bodyId]++;

                        // Add viable brain to the population
                        seedBrainPopulation.Add(viableBrain);
                    }
                } while (!viableBrainsEvolved);
            }

            // If we still lack the genomes to fill out seed brain genome count while still satisfying the body MC,
            // iteratively pick a random body and evolve brains on that body until the requisite number is reached
            while (seedBrainPopulation.Count < BrainSeedGenomeCount)
            {
                var rndBodyPicker = RandomDefaults.CreateRandomSource();

                // Compute the amount remaining to fill out the brain seed count
                var brainsRemaining = BrainSeedGenomeCount - seedBrainPopulation.Count;

                // Restrict to bodies that are still under their resource limit
                var bodiesUnderResourceLimit =
                    seedBodyPopulation.Where(x => bodySolutionCount[x.Id] < ResourceLimit).ToList();

                // Pick a random body on which to evolve brains
                var bodyGenome = bodiesUnderResourceLimit[rndBodyPicker.Next(bodiesUnderResourceLimit.Count - 1)];

                // Get max number of brains that can be added for body (we don't want to exceed the brain seed size)
                var maxSolutions = Math.Min(ResourceLimit - bodySolutionCount[bodyGenome.Id], brainsRemaining);

                Console.WriteLine(
                    $"Continuing viable brain evolution on body {bodyGenome.Id}, with {seedBrainPopulation.Count} of {BrainSeedGenomeCount} required brains in place");

                // Evolve the number of brains required to meet the success MC for the body
                var viableBodyBrains = _bodyBrainInitializer.EvolveViableBrains(brainGenomeFactory,
                    brainGenomeFactory.CreateGenomeList(_brainInitializationGenomeCount, 0), BrainDecoder,
                    new VoxelBody(BodyDecoder.Decode(bodyGenome)), _maxBrainInitializationEvaluations, ParallelOptions);

                // Iterate through each viable brain and remove them if they've already solved a body
                // or add them to the list of viable brains if they have not
                foreach (var viableBrain in viableBodyBrains.Take(maxSolutions))
                {
                    // Increment number of body solutions
                    bodySolutionCount[bodyGenome.Id]++;

                    // Add viable brain to the population
                    seedBrainPopulation.Add(viableBrain);
                }
            }

            return new Tuple<List<NeatSubstrateGenome>, List<NeatGenome>>(seedBodyPopulation, seedBrainPopulation);
        }

        /// <summary>
        ///     Ensure that the pre-evolved brain population facilitates the satisfaction of both their MC and the MC of the seed
        ///     bodies.
        /// </summary>
        /// <param name="brainPopulation">The pre-evolved brain population.</param>
        /// <param name="bodyPopulation">The body population.</param>
        /// <param name="brainEvaluator">The brain evaluator.</param>
        /// <param name="bodyEvaluator">The body evaluator.</param>
        /// <returns>
        ///     Boolean flag indicating whether the seed brains meet their MC and facilitate satisfaction of the body population
        ///     MC.
        /// </returns>
        protected static bool VerifyPreevolvedSeedPopulations(List<NeatGenome> brainPopulation,
            List<NeatSubstrateGenome> bodyPopulation, IGenomeEvaluator<NeatGenome> brainEvaluator,
            IGenomeEvaluator<NeatSubstrateGenome> bodyEvaluator)
        {
            // Update brain and body evaluators such that seed brains will be evaluated against bodies and vice versa
            brainEvaluator.UpdateEvaluationBaseline(bodyEvaluator.DecodeGenomes(bodyPopulation), 0);
            bodyEvaluator.UpdateEvaluationBaseline(brainEvaluator.DecodeGenomes(brainPopulation), 0);

            // Run MC evaluation for both populations
            brainEvaluator.Evaluate(brainPopulation, 0);
            bodyEvaluator.Evaluate(bodyPopulation, 0);

            // In order to be a valid seed, the brains and bodies must all satisfy their MC with respect to each other
            return brainPopulation.All(g => g.EvaluationInfo.IsViable) &&
                   bodyPopulation.All(g => g.EvaluationInfo.IsViable);
        }

        #endregion
    }
}