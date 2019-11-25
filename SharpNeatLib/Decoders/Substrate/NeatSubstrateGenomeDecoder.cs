using SharpNeat.Core;
using SharpNeat.Genomes.Substrate;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.CPPNs;
using SharpNeat.Phenomes.NeuralNets.AcyclicNetwork;
using SharpNeat.Phenomes.NeuralNets.CyclicNetwork;
using SharpNeat.Phenomes.NeuralNets.FastCyclicNetwork;

namespace SharpNeat.Decoders.Substrate
{
    /// <summary>
    ///     The NEAT substrate genome decoder decodes a given NEAT substrate genome into a graph structure and embeds it along
    ///     with the given substrate resolution that it is to query.
    /// </summary>
    public class NeatSubstrateGenomeDecoder : IGenomeDecoder<NeatSubstrateGenome, IBlackBoxSubstrate>
    {
        #region Constructor

        /// <summary>
        ///     Constructor which accepts the chosen network activation scheme and passes to the parent decoder.
        /// </summary>
        /// <param name="activationScheme">The activation scheme.</param>
        public NeatSubstrateGenomeDecoder(NetworkActivationScheme activationScheme)
        {
            _activationScheme = activationScheme;

            // Pre-determine which decode routine to use based on the activation scheme.
            _decodeMethod = GetDecodeMethod(activationScheme);
        }

        #endregion

        #region IGenomeDecoder methods

        /// <summary>
        ///     Decodes a given NEAT substrate genome into the corresponding graph representation and packages with the assigned
        ///     substrate resolution.
        /// </summary>
        /// <param name="genome">The NEAT substrate genome to decode and convert into a graph representation.</param>
        /// <returns>The graph representation packaged with a specification of the target substrate resolution.</returns>
        public IBlackBoxSubstrate Decode(NeatSubstrateGenome genome)
        {
            // Decode to CPPN
            var cppn = _decodeMethod(genome);

            // Construct and return CPPN with the corresponding substrate resolution
            return new CppnSubstrate(cppn,
                new SubstrateResolution(genome.SubstrateX, genome.SubstrateY, genome.SubstrateZ));
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The selected activation scheme for the CPPN (this will typically be feed-forward)
        /// </summary>
        private readonly NetworkActivationScheme _activationScheme;

        /// <summary>
        ///     The method for decoding a CPPN genome into its component graph representation.
        /// </summary>
        /// <param name="genome">The CPPN genome to decode.</param>
        private delegate IBlackBox DecodeGenome(NeatSubstrateGenome genome);

        /// <summary>
        ///     Reference to the method for decoding a CPPN genome into its component graph representation.
        /// </summary>
        private readonly DecodeGenome _decodeMethod;

        #endregion

        #region Private methods

        /// <summary>
        ///     Returns the appropriate genome decoding method based on the given activation scheme.
        /// </summary>
        /// <param name="activationScheme">The network activation scheme which will dictate the selected decoding method.</param>
        /// <returns>The activation-aligned decoding method.</returns>
        private DecodeGenome GetDecodeMethod(NetworkActivationScheme activationScheme)
        {
            if (activationScheme.AcyclicNetwork)
            {
                return DecodeToFastAcyclicNetwork;
            }

            if (activationScheme.FastFlag)
            {
                return DecodeToFastCyclicNetwork;
            }

            return DecodeToCyclicNetwork;
        }

        /// <summary>
        ///     Interprets the genome as an acyclic network and decodes to the corresponding graph.
        /// </summary>
        /// <param name="genome">The NEAT substrate genome to decode.</param>
        /// <returns>The decoded, acyclic graph structure.</returns>
        private FastAcyclicNetwork DecodeToFastAcyclicNetwork(NeatSubstrateGenome genome)
        {
            return FastAcyclicNetworkFactory.CreateFastAcyclicNetwork(genome);
        }

        /// <summary>
        ///     Interprets the genome as a cyclic network and decodes to the corresponding graph.
        /// </summary>
        /// <param name="genome">The NEAT substrate genome to decode.</param>
        /// <returns>The decoded, recurrent graph structure.</returns>
        private CyclicNetwork DecodeToCyclicNetwork(NeatSubstrateGenome genome)
        {
            return CyclicNetworkFactory.CreateCyclicNetwork(genome, _activationScheme);
        }

        /// <summary>
        ///     Interprets the genome as a cyclic network and decodes to the corresponding graph using heuristics intended to
        ///     accelerate inference.
        /// </summary>
        /// <param name="genome">The NEAT substrate genome to decode.</param>
        /// <returns>The decoded, recurrent graph structure.</returns>
        private FastCyclicNetwork DecodeToFastCyclicNetwork(NeatSubstrateGenome genome)
        {
            return FastCyclicNetworkFactory.CreateFastCyclicNetwork(genome, _activationScheme);
        }

        #endregion
    }
}