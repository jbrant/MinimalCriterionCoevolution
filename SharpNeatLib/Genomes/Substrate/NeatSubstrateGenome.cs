using System.Collections.Generic;
using Redzen.Numerics.Distributions;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Network;

namespace SharpNeat.Genomes.Substrate
{
    /// <summary>
    ///     Encodes substrate mutation operation types.
    /// </summary>
    internal enum SubstrateMutation
    {
        IncreaseResolution = 0,
        DecreaseResolution
    }

    /// <summary>
    ///     The NEAT substrate genome encapsulates a NEAT genome that decodes to a CPPN, along with the substrate dimensions,
    ///     which are expanded (or the resolution is increased) through a substrate expansion mutation parameter.
    /// </summary>
    public class NeatSubstrateGenome : IGenome<NeatSubstrateGenome>, INetworkDefinition, ILoggable
    {
        #region Properties

        /// <summary>
        ///     Accessor property for genome factory.
        /// </summary>
        public NeatSubstrateGenomeFactory NeatSubstrateGenomeFactory
        {
            get => _neatSubstrateGenomeFactory;
            set
            {
                if (null != _neatSubstrateGenomeFactory)
                {
                    throw new SharpNeatException("NeatSubstrateGenome already has an assigned GenomeFactory.");
                }

                _neatSubstrateGenomeFactory = value;
            }
        }

        #endregion

        #region ILoggable methods

        /// <summary>
        ///     Returns NeatSubstrateGenome LoggableElements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The LoggableElements for NeatSubstrateGenome.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return _neatGenome.GetLoggableElements(logFieldEnableMap);
        }

        #endregion

        #region INetworkDefinition methods

        /// <summary>
        ///     Gets NetworkConnectivityData for the network.
        /// </summary>
        public NetworkConnectivityData GetConnectivityData()
        {
            return _neatGenome.GetConnectivityData();
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The NEAT substrate genome factory used for generating new NEAT substrate genomes.
        /// </summary>
        private NeatSubstrateGenomeFactory _neatSubstrateGenomeFactory;

        /// <summary>
        ///     The NEAT genome on which the NEAT substrate genome is based.
        /// </summary>
        private readonly NeatGenome _neatGenome;

        #endregion

        #region IGenome methods

        /// <summary>
        ///     Asexual reproduction.
        /// </summary>
        /// <param name="birthGeneration">
        ///     The current evolution algorithm generation.
        ///     Assigned to the new genome at its birth generation.
        /// </param>
        public NeatSubstrateGenome CreateOffspring(uint birthGeneration)
        {
            return _neatSubstrateGenomeFactory.Rng.NextDouble() < _neatSubstrateGenomeFactory
                       .NeatSubstrateGenomeParameters.ModifySubstrateResolutionProbability
                ? CreateSubstrateMutatedOffspring(birthGeneration)
                : CreateNeatMutatedOffspring(birthGeneration);
        }

        /// <summary>
        ///     Sexual reproduction.
        /// </summary>
        /// <param name="parent">The other parent genome (mates with the current genome).</param>
        /// <param name="birthGeneration">
        ///     The current evolution algorithm generation.
        ///     Assigned to the new genome at its birth generation.
        /// </param>
        public NeatSubstrateGenome CreateOffspring(NeatSubstrateGenome parent, uint birthGeneration)
        {
            NeatSubstrateGenome offspring;

            do
            {
                // Create a new NEAT genome through crossover
                var neatGenomeOffspring = _neatGenome.CreateOffspring(parent._neatGenome, birthGeneration);

                // Create a new voxel body genome wrapping the new NEAT genome
                // (the generator copy method is not called here because it would unnecessarily copy
                // NEAT genome contents twice)
                offspring = new NeatSubstrateGenome(_neatSubstrateGenomeFactory, neatGenomeOffspring, SubstrateX,
                    SubstrateY, SubstrateZ);
            } while (!_neatSubstrateGenomeFactory.IsGeneratedPhenomeValid(offspring));

            return offspring;
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Applies a substrate mutation by increasing or decreasing the substrate resolution.
        /// </summary>
        /// <param name="birthGeneration">
        ///     The current evolution algorithm generation. Assigned to the new genome at its birth
        ///     generation.
        /// </param>
        /// <returns>The mutated offspring.</returns>
        private NeatSubstrateGenome CreateSubstrateMutatedOffspring(uint birthGeneration)
        {
            NeatSubstrateGenome offspring;

            // TODO: Possibly allow expansion along randomly select axes

            // Create a new NEAT genome identical to the original
            var neatGenomeOffspring = _neatSubstrateGenomeFactory.NeatGenomeFactory.CreateGenomeCopy(_neatGenome,
                _neatSubstrateGenomeFactory.GenomeIdGenerator.NextId, birthGeneration);

            // Randomly pick the substrate mutation to be applied
            var outcome = (SubstrateMutation) DiscreteDistribution.Sample(_neatSubstrateGenomeFactory.Rng,
                _neatSubstrateGenomeFactory.NeatSubstrateGenomeParameters.RouletteWheelLayout);

            // Boundary check the substrate and apply relevant mutation overrides
            outcome = ApplySubstrateMutationOverrides(outcome);

            // Note that the generator copy method is not called because it would unnecessarily copy NEAT genome contents twice
            switch (outcome)
            {
                case SubstrateMutation.IncreaseResolution:
                    offspring = new NeatSubstrateGenome(_neatSubstrateGenomeFactory, neatGenomeOffspring,
                        SubstrateX + 1,
                        SubstrateY + 1,
                        SubstrateZ + 1);
                    break;
                case SubstrateMutation.DecreaseResolution:
                    offspring = new NeatSubstrateGenome(_neatSubstrateGenomeFactory, neatGenomeOffspring,
                        SubstrateX - 1,
                        SubstrateY - 1,
                        SubstrateZ - 1);
                    break;
                default:
                    throw new SharpNeatException(
                        $"NeatSubstrateGenome.CreateOffspring(): Unexpected outcome value [{outcome}]");
            }

            return offspring;
        }

        /// <summary>
        ///     Applies a NEAT mutation to produce a new offspring.
        /// </summary>
        /// <param name="birthGeneration">
        ///     The current evolution algorithm generation. Assigned to the new genome at its birth
        ///     generation.
        /// </param>
        /// <returns>The mutated offspring.</returns>
        private NeatSubstrateGenome CreateNeatMutatedOffspring(uint birthGeneration)
        {
            NeatSubstrateGenome offspring;

            // Continue creating/mutating new offspring until one that produces a valid phenotype is created
            do
            {
                // Create a new NEAT genome and mutate it
                var neatGenomeOffspring = _neatGenome.CreateOffspring(birthGeneration);

                // Create a new voxel body genome wrapping the new NEAT genome
                // (the generator copy method is not called here because it would unnecessarily copy
                // NEAT genome contents twice)
                offspring = new NeatSubstrateGenome(_neatSubstrateGenomeFactory, neatGenomeOffspring, SubstrateX,
                    SubstrateY, SubstrateZ);
            } while (!_neatSubstrateGenomeFactory.IsGeneratedPhenomeValid(offspring));

            return offspring;
        }

        private SubstrateMutation ApplySubstrateMutationOverrides(SubstrateMutation selectedMutation)
        {
            switch (selectedMutation)
            {
                // If the size of the current genome is already the smallest possible, only allow a resolution increase
                case SubstrateMutation.DecreaseResolution
                    when SubstrateX == _neatSubstrateGenomeFactory.DefaultSubstrateX ||
                         SubstrateY == _neatSubstrateGenomeFactory.DefaultSubstrateY ||
                         SubstrateZ == _neatSubstrateGenomeFactory.DefaultSubstrateZ:
                    return SubstrateMutation.IncreaseResolution;
                // If the substrate has reached maximum size along any of the three dimensions,
                // only a resolution decrease can be applied
                case SubstrateMutation.IncreaseResolution
                    when SubstrateX == _neatSubstrateGenomeFactory.MaxSubstrateResolution ||
                         SubstrateY == _neatSubstrateGenomeFactory.MaxSubstrateResolution ||
                         SubstrateZ == _neatSubstrateGenomeFactory.MaxSubstrateResolution:
                    return SubstrateMutation.DecreaseResolution;
                default:
                    return selectedMutation;
            }
        }

        #endregion

        #region NeatGenome properties

        /// <summary>
        ///     Gets the number of input neurons represented by the genome.
        /// </summary>
        public int InputNeuronCount => _neatGenome.InputNeuronCount;

        /// <summary>
        ///     Gets the number of output neurons represented by the genome.
        /// </summary>
        public int OutputNeuronCount => _neatGenome.OutputNeuronCount;

        /// <summary>
        ///     Gets the genome's list of neuron genes.
        /// </summary>
        public NeuronGeneList NeuronGeneList => _neatGenome.NeuronGeneList;

        /// <summary>
        ///     Gets the genome's list of connection genes.
        /// </summary>
        public ConnectionGeneList ConnectionGeneList => _neatGenome.ConnectionGeneList;

        #endregion

        #region INetworkDefinition properties

        /// <summary>
        ///     Gets the number of input nodes. This does not include the bias node which is always present.
        /// </summary>
        public int InputNodeCount => _neatGenome.InputNodeCount;

        /// <summary>
        ///     Gets the number of output nodes.
        /// </summary>
        public int OutputNodeCount => _neatGenome.OutputNodeCount;

        /// <summary>
        ///     Gets a bool flag that indicates if the network is acyclic.
        /// </summary>
        public bool IsAcyclic => _neatGenome.IsAcyclic;

        /// <summary>
        ///     Gets the network's activation function library. The activation function at each node is
        ///     represented by an integer ID, which refers to a function in this activation function library.
        /// </summary>
        public IActivationFunctionLibrary ActivationFnLibrary => _neatGenome.ActivationFnLibrary;

        /// <summary>
        ///     Gets the list of network nodes.
        /// </summary>
        public INodeList NodeList => _neatGenome.NodeList;

        /// <summary>
        ///     Gets the list of network connections.
        /// </summary>
        public IConnectionList ConnectionList => _neatGenome.ConnectionList;

        #endregion

        #region IGenome properties

        /// <summary>
        ///     Gets the genome's unique ID. IDs are unique across all genomes created from a single
        ///     IGenomeFactory and all ancestor genomes spawned from those genomes.
        /// </summary>
        public uint Id => _neatGenome.Id;

        /// <summary>
        ///     Gets or sets a specie index. This is the index of the species that the genome is in.
        ///     Implementing this is required only when using evolution algorithms that speciate genomes.
        /// </summary>
        public int SpecieIdx
        {
            get => _neatGenome.SpecieIdx;
            set => _neatGenome.SpecieIdx = value;
        }

        /// <summary>
        ///     Gets the generation that this genome was born/created in. Used to track genome age.
        /// </summary>
        public uint BirthGeneration => _neatGenome.BirthGeneration;

        /// <summary>
        ///     Gets evaluation information for the genome, including its fitness.
        /// </summary>
        public EvaluationInfo EvaluationInfo => _neatGenome.EvaluationInfo;

        /// <summary>
        ///     Gets a value that indicates the magnitude of a genome's complexity.
        ///     For a NeatGenome we return the number of connection genes since a neural network's
        ///     complexity is approximately proportional to the number of connections - the number of
        ///     neurons is less important and can be viewed as being a limit on the possible number of
        ///     connections.
        /// </summary>
        public double Complexity => _neatGenome.Complexity;

        /// <summary>
        ///     Gets a coordinate that represents the genome's position in the search space (also known
        ///     as the genetic encoding space). This allows speciation/clustering algorithms to operate on
        ///     an abstract cordinate data type rather than being coded against specific IGenome types.
        /// </summary>
        public CoordinateVector Position => _neatGenome.Position;

        /// <summary>
        ///     Gets or sets a cached phenome obtained from decodign the genome.
        ///     Genomes are typically decoded to Phenomes for evaluation. This property allows decoders to
        ///     cache the phenome in order to avoid decoding on each re-evaluation; However, this is optional.
        ///     The phenome in un-typed to prevent the class framework from becoming overly complex.
        /// </summary>
        public object CachedPhenome { get; set; }

        #endregion

        #region Substrate properties

        /// <summary>
        ///     The resolution of the substrate along the X dimension.
        /// </summary>
        public int SubstrateX { get; }

        /// <summary>
        ///     The resolution of the substrate along the Y dimension.
        /// </summary>
        public int SubstrateY { get; }

        /// <summary>
        ///     The resolution of the substrate along the Z dimension.
        /// </summary>
        public int SubstrateZ { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     NeatSubstrateGenome constructor.
        /// </summary>
        /// <param name="substrateGenomeFactory">The genome factory used for producing new NEAT substrate genomes.</param>
        /// <param name="id">The genome ID.</param>
        /// <param name="birthGeneration">The generation during which the genome was birthed.</param>
        /// <param name="neuronGeneList">The list of nodes in the network.</param>
        /// <param name="connectionGeneList">The list of connections in the network.</param>
        /// <param name="inputNeuronCount">The number of inputs.</param>
        /// <param name="outputNeuronCount">The number of outputs.</param>
        /// <param name="rebuildNeuronGeneConnectionInfo">Flag indicating whether to recreate the connection metadata.</param>
        public NeatSubstrateGenome(NeatSubstrateGenomeFactory substrateGenomeFactory, uint id, uint birthGeneration,
            NeuronGeneList neuronGeneList, ConnectionGeneList connectionGeneList, int inputNeuronCount,
            int outputNeuronCount, bool rebuildNeuronGeneConnectionInfo)
        {
            _neatGenome = new NeatGenome(substrateGenomeFactory.NeatGenomeFactory, id, birthGeneration, neuronGeneList,
                connectionGeneList, inputNeuronCount, outputNeuronCount, rebuildNeuronGeneConnectionInfo);
            _neatSubstrateGenomeFactory = substrateGenomeFactory;
        }

        /// <summary>
        ///     NeatSubstrateGenome copy constructor.
        /// </summary>
        /// <param name="copyFrom">The NEAT substrate genome whose properties to duplicate.</param>
        /// <param name="id">The genome ID of the newly created NEAT substrate genome.</param>
        /// <param name="birthGeneration">The generation during which the genome was birthed.</param>
        public NeatSubstrateGenome(NeatSubstrateGenome copyFrom, uint id, uint birthGeneration)
        {
            _neatGenome = new NeatGenome(copyFrom._neatGenome, id, birthGeneration);

            _neatSubstrateGenomeFactory = copyFrom._neatSubstrateGenomeFactory;
            SubstrateX = copyFrom.SubstrateX;
            SubstrateY = copyFrom.SubstrateY;
            SubstrateZ = copyFrom.SubstrateZ;
        }

        /// <summary>
        ///     NeatSubstrateGenome copy constructor.
        /// </summary>
        /// <param name="substrateGenomeFactory">The genome factory used for producing new NEAT substrate genomes.</param>
        /// <param name="neatGenome">The underlying NEAT genome which is leveraged to encode connection and node properties.</param>
        /// <param name="substrateX">The resolution of the substrate along the X dimension.</param>
        /// <param name="substrateY">The resolution of the substrate along the Y dimension.</param>
        /// <param name="substrateZ">The resolution of the substrate along the Z dimension.</param>
        public NeatSubstrateGenome(NeatSubstrateGenomeFactory substrateGenomeFactory, NeatGenome neatGenome,
            int substrateX,
            int substrateY, int substrateZ)
        {
            _neatGenome = neatGenome;
            _neatSubstrateGenomeFactory = substrateGenomeFactory;
            SubstrateX = substrateX;
            SubstrateY = substrateY;
            SubstrateZ = substrateZ;
        }

        #endregion
    }
}