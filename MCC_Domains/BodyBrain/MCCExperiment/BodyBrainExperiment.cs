using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MCC_Domains.BodyBrain.MCCExperiment;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.Decoders.Voxel;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.Statistics;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain
{
    public class BodyBrainExperiment : BaseBodyBrainExperiment
    {
        #region Private methods

        /// <summary>
        ///     Randomly creates new voxel bodies on which to train initial controllers (brains).
        /// </summary>
        /// <param name="bodyGenomeFactory">The body genome factory.</param>
        /// <param name="bodyDecoder">The body decoder to convert genome representation into voxel structure.</param>
        /// <returns>List of voxel body genomes.</returns>
        private List<NeatGenome> GenerateBodies(IGenomeFactory<NeatGenome> bodyGenomeFactory,
            IGenomeDecoder<NeatGenome, VoxelBody> bodyDecoder)
        {
            var bodyGenomes = new List<NeatGenome>(BodySeedGenomeCount);

            // Continue generating new body genomes until there are enough to meet the seed count
            do
            {
                // Generate new body genome
                var bodyGenome = bodyGenomeFactory.CreateGenomeList(1, 0)[0];

                // Decode to voxel body
                var body = bodyDecoder.Decode(bodyGenome);

                // If body voxel contains the minimum amount of material and muscle voxels, add to the list
                if (body.ActiveTissueProportion >= SimulationProperties.MinPercentActive &&
                    body.FullProportion >= SimulationProperties.MinPercentMaterial)
                {
                    bodyGenomes.Add(bodyGenome);
                }
            } while (bodyGenomes.Count < BodySeedGenomeCount);

            return bodyGenomes;
        }

        #endregion

        #region Public Methods

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
            // Create the brain genome decoder
            IGenomeDecoder<NeatGenome, VoxelBrain> brainGenomeDecoder = new VoxelBrainDecoder(ActivationScheme,
                SimulationProperties.InitialXDimension, SimulationProperties.InitialYDimension,
                SimulationProperties.InitialZDimension, SimulationProperties.NumBrainConnections);

            // Create the body genome decoder
            IGenomeDecoder<NeatGenome, VoxelBody> bodyGenomeDecoder = new VoxelBodyDecoder(ActivationScheme,
                SimulationProperties.InitialXDimension, SimulationProperties.InitialYDimension,
                SimulationProperties.InitialZDimension);

            // If there are no body genomes, generate them
            var seedBodyPopulation = bodyGenomes != null && bodyGenomes.Any()
                ? bodyGenomes
                : GenerateBodies(bodyGenomeFactory, bodyGenomeDecoder);

            // If there are no brain genomes, pre-evolve them
            var seedBrainPopulation = brainGenomes != null && brainGenomes.Any()
                ? brainGenomes
                : EvolveSeedBrains(seedBodyPopulation, brainGenomeFactory, brainGenomeDecoder, bodyGenomeDecoder,
                    BrainSeedGenomeCount, ResourceLimit);

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

            // TODO: Add data loggers
            // Create the NEAT EA for brains
            AbstractEvolutionAlgorithm<NeatGenome> brainEvolutionAlgorithm =
                new QueueEvolutionAlgorithm<NeatGenome>(eaParams, new NeatAlgorithmStats(eaParams), null, null,
                    BrainBatchSize);

            // TODO: Add data loggers
            // Create the NEAT EA for bodies
            AbstractEvolutionAlgorithm<NeatGenome> bodyEvolutionAlgorithm =
                new QueueEvolutionAlgorithm<NeatGenome>(eaParams, new NeatAlgorithmStats(eaParams), null, null,
                    BodyBatchSize);

            // Create the brain phenome evaluator
            IPhenomeEvaluator<VoxelBrain, BehaviorInfo> brainEvaluator = new BrainEvaluator(SimulationProperties,
                MinAmbulationDistance, NumBodySuccessCriteria, Name, Run, ResourceLimit);

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
    }
}