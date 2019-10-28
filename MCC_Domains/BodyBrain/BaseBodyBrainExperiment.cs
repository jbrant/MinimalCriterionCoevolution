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
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Voxels;

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
        ///     The activation scheme (i.e. cyclic or acyclic).
        /// </summary>
        protected NetworkActivationScheme ActivationScheme;

        /// <summary>
        ///     Switches between synchronous and asynchronous execution (with user-defined number of threads).
        /// </summary>
        protected ParallelOptions ParallelOptions;

        /// <summary>
        ///     The NEAT genome parameters to use for the experiment.
        /// </summary>
        protected NeatGenomeParameters NeatGenomeParameters;

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
        ///     The maximum number of evaluations allowed during the initialization phase before it is restarted.
        /// </summary>
        private uint? _maxInitializationEvaluations;

        /// <summary>
        ///     Initialization algorithm for producing an initial population with the requisite number of viable genomes.
        /// </summary>
        private BodyBrainInitializer _bodyBrainInitializer;

        /// <summary>
        ///     The number of brains needed for the initialization algorithm.
        /// </summary>
        private int BrainInitializationGenomeCount { get; set; }

        #endregion

        #region Public methods

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
        public void Initialize(string name, int run, string simConfigDirectory, string simResultsDirectory,
            string simExecutableFile, XmlElement xmlConfig)
        {
            // Set boiler plate properties
            Name = name;
            Run = run;
            ActivationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            ParallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);

            // Set the genome parameters
            NeatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);
            NeatGenomeParameters.FeedforwardOnly = ActivationScheme.AcyclicNetwork;

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

            // Set the maximum number of initialization evaluations
            _maxInitializationEvaluations = XmlUtils.GetValueAsUInt(xmlConfig, "MaxInitializationEvaluations");

            // Initialize the initialization algorithm
            _bodyBrainInitializer =
                BodyBrainExperimentUtils.DetermineMCCBodyBrainInitializer(
                    xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement);

            // Setup initialization algorithm
            _bodyBrainInitializer.SetAlgorithmParameters(
                xmlConfig.GetElementsByTagName("InitializationAlgorithmConfig", "")[0] as XmlElement,
                Name, run, ActivationScheme.AcyclicNetwork, NumBrainSuccessCriteria);

            // Configure simulation
            _bodyBrainInitializer.SetSimulationParameters(MinAmbulationDistance, SimulationProperties);

            // The size of the randomly generated agent genome pool from which to evolve agent bootstraps
            BrainInitializationGenomeCount = _bodyBrainInitializer.PopulationSize;
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
        public abstract IGenomeFactory<NeatGenome> CreateBodyGenomeFactory();

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
        public void SaveBodyPopulation(XmlWriter xw, IList<NeatGenome> bodyGenomeList)
        {
            NeatGenomeXmlIO.WriteComplete(xw, bodyGenomeList, true);
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
        public abstract IMCCAlgorithmContainer<NeatGenome, NeatGenome> CreateMCCAlgorithmContainer(
            IGenomeFactory<NeatGenome> brainGenomeFactory, IGenomeFactory<NeatGenome> bodyGenomeFactory,
            List<NeatGenome> brainGenomes, List<NeatGenome> bodyGenomes);

        #endregion

        #region Protected methods

        /// <summary>
        ///     Evolves the requisite number of brains to the meet the MC for each body in the initial population. This is
        ///     performed using a non-MCC based algorithm (such as a fitness-based method or novelty search).
        /// </summary>
        /// <param name="bodyPopulation">
        ///     The voxel bodies in the initial population (either provided at runtime or randomly
        ///     generated).
        /// </param>
        /// <param name="brainGenomeFactory">The factory object for producing new CPPNs for the brain population.</param>
        /// <param name="brainDecoder">The decoder used for converting CPPNs to brains.</param>
        /// <param name="bodyDecoder">The decoder used for converting CPPNs to voxel bodies.</param>
        /// <param name="numBrains">The number of seed brains to evolve.</param>
        /// <param name="resourceLimit">The resource limit for the body population (optional).</param>
        /// <returns>
        ///     The list of viable brains, each of whom is able to ambulate at least one of the initial bodies and, in
        ///     totality, meet the body MC for being ambulated by a given number of brains.
        /// </returns>
        protected List<NeatGenome> EvolveSeedBrains(List<NeatGenome> bodyPopulation,
            IGenomeFactory<NeatGenome> brainGenomeFactory, IGenomeDecoder<NeatGenome, VoxelBrain> brainDecoder,
            IGenomeDecoder<NeatGenome, VoxelBody> bodyDecoder, int numBrains, int resourceLimit = int.MaxValue)
        {
            var seedBrainPopulation = new List<NeatGenome>(BrainSeedGenomeCount);
            var bodySolutionCount = new Dictionary<uint, int>();

            // Compute the max number of brains that should be added per body to avoid exceeding the brain seed count
            var perBodyBrainCount = Math.Min(resourceLimit,
                Convert.ToInt32(Math.Floor((double) numBrains / bodyPopulation.Count)));
            
            // Create population of randomly-initialized brain CPPNs
            var brainPopulation = brainGenomeFactory.CreateGenomeList(BrainInitializationGenomeCount, 0);

            // Loop through every body and evolve the requisite number of viable brains that control it
            for (var idx = 0; idx < bodyPopulation.Count; idx++)
            {
                var bodyId = bodyPopulation[idx].Id;

                // Initialize body solution count to 0
                bodySolutionCount.Add(bodyId, 0);

                Console.WriteLine($"Evolving viable brains for body population index {idx} with ID {bodyId}");

                // Evolve the number of brains required to meet the success MC for the current body
                var viableBrains = _bodyBrainInitializer.EvolveViableBrains(brainGenomeFactory,
                    brainPopulation.ToList(), brainDecoder, bodyDecoder.Decode(bodyPopulation[idx]),
                    _maxInitializationEvaluations, ActivationScheme, ParallelOptions);

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
            }

            // If we still lack the genomes to fill out seed brain genome count while still satisfying the body MC,
            // iteratively pick a random body and evolve brains on that body until the requisite number is reached
            while (seedBrainPopulation.Count < numBrains)
            {
                var rndBodyPicker = RandomDefaults.CreateRandomSource();
                
                // Compute the amount remaining to fill out the brain seed count
                var brainsRemaining = numBrains - seedBrainPopulation.Count;

                // Restrict to bodies that are still under their resource limit
                var bodiesUnderResourceLimit =
                    bodyPopulation.Where(x => bodySolutionCount[x.Id] < resourceLimit).ToList();

                // Pick a random body on which to evolve brains
                var bodyGenome = bodiesUnderResourceLimit[rndBodyPicker.Next(bodiesUnderResourceLimit.Count - 1)];

                // Get max number of brains that can be added for body (we don't want to exceed the brain seed size)
                var maxSolutions = Math.Min(resourceLimit - bodySolutionCount[bodyGenome.Id], brainsRemaining);

                Console.WriteLine(
                    $"Continuing viable brain evolution on body {bodyGenome.Id}, with {seedBrainPopulation.Count} of {numBrains} required brains in place");

                // Evolve the number of brains required to meet the success MC for the body
                var viableBodyBrains = _bodyBrainInitializer.EvolveViableBrains(brainGenomeFactory,
                    brainPopulation.ToList(), brainDecoder, bodyDecoder.Decode(bodyGenome),
                    _maxInitializationEvaluations, ActivationScheme, ParallelOptions);

                // Iterate through each viable brain and remove them if they've already solved a body
                // or add them to the list of viable brains if they have not
                foreach (var viableBrain in viableBodyBrains.Take(maxSolutions))
                {
                    // If the brain has already solved the body maze and is in the list of viable brains,
                    // remove that brain from the pool of seed genomes (this is done because here, we're interested
                    // in getting unique brains and want to avoid an endless loop wherein the same viable brains
                    // are returned)
                    if (seedBrainPopulation.Select(x => x.Id).Contains(viableBrain.Id))
                        brainPopulation.Remove(viableBrain);
                    // Otherwise, add that brain to the list of viable brains
                    else
                    {
                        // Increment number of body solutions
                        bodySolutionCount[bodyGenome.Id]++;

                        // Add viable brain to the population
                        seedBrainPopulation.Add(viableBrain);
                    }
                }
            }

            return seedBrainPopulation;
        }

        /// <summary>
        ///     Ensure that the pre-evolved brain population facilitates the satisfaction of both their MC and the MC of the seed bodies.
        /// </summary>
        /// <param name="brainPopulation">The pre-evolved brain population.</param>
        /// <param name="bodyPopulation">The body population.</param>
        /// <param name="brainEvaluator">The brain evaluator.</param>
        /// <param name="bodyEvaluator">The body evaluator.</param>
        /// <returns>
        ///     Boolean flag indicating whether the seed brains meet their MC and facilitate satisfaction of the body population MC.
        /// </returns>
        protected static bool VerifyPreevolvedSeedPopulations(List<NeatGenome> brainPopulation,
            List<NeatGenome> bodyPopulation, IGenomeEvaluator<NeatGenome> brainEvaluator,
            IGenomeEvaluator<NeatGenome> bodyEvaluator)
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