using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.NeuralNets.AcyclicNetwork;
using SharpNeat.Phenomes.NeuralNets.CyclicNetwork;
using SharpNeat.Phenomes.NeuralNets.FastCyclicNetwork;
using SharpNeat.Utility;

namespace SharpNeat.Decoders.Voxel
{
    /// <summary>
    ///     The voxel decoder translates a given CPPN genome into its phenotypic representation - either a 3D voxel lattice
    ///     composed of multiple voxels with varying material, or a voxel controller, either for each cell individually or for
    ///     the structure as a whole.
    /// </summary>
    public class VoxelDecoder
    {
        #region Constructors

        /// <summary>
        ///     The VoxelDecoder constructor.
        /// </summary>
        /// <param name="activationScheme">The CPPN activation scheme.</param>
        /// <param name="x">The length of the X-axis on the voxel lattice.</param>
        /// <param name="y">The length of the Y-axis on the voxel lattice.</param>
        /// <param name="z">The length of the Z-axis on the voxel lattice.</param>
        protected VoxelDecoder(NetworkActivationScheme activationScheme, int x, int y, int z)
        {
            _activationScheme = activationScheme;

            // Pre-determine which decode routine to use based on the activation scheme.
            DecodeCppnMethod = GetDecodeMethod(activationScheme);

            // Set dimensions of voxel structure
            X = x;
            Y = y;
            Z = z;

            // Compute per-voxel distance matrix
            DistanceMatrix = VoxelUtils.ComputeVoxelDistanceMatrix(x, y, z);
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
        protected delegate IBlackBox DecodeGenome(NeatGenome genome);

        /// <summary>
        ///     Reference to the method for decoding a CPPN genome into its component graph representation.
        /// </summary>
        protected readonly DecodeGenome DecodeCppnMethod;

        /// <summary>
        ///     The length of the X-axis on the voxel lattice.
        /// </summary>
        protected readonly int X;

        /// <summary>
        ///     The length of the Y-axis on the voxel lattice.
        /// </summary>
        protected readonly int Y;

        /// <summary>
        ///     The length of the Z-axis on the voxel lattice.
        /// </summary>
        protected readonly int Z;

        /// <summary>
        ///     The distance between each voxel and the center-of-mass of the voxel structure (which is equivalent to the geometric
        ///     centroid of the structure given that we assume consistent, per-voxel mass regardless of voxel material).
        /// </summary>
        protected readonly double[,,] DistanceMatrix;

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
        /// <param name="genome">The CPPN genome to decode.</param>
        /// <returns>The decoded, acyclic graph structure.</returns>
        private FastAcyclicNetwork DecodeToFastAcyclicNetwork(NeatGenome genome)
        {
            return FastAcyclicNetworkFactory.CreateFastAcyclicNetwork(genome);
        }

        /// <summary>
        ///     Interprets the genome as a cyclic network and decodes to the corresponding graph.
        /// </summary>
        /// <param name="genome">The CPPN genome to decode.</param>
        /// <returns>The decoded, recurrent graph structure.</returns>
        private CyclicNetwork DecodeToCyclicNetwork(NeatGenome genome)
        {
            return CyclicNetworkFactory.CreateCyclicNetwork(genome, _activationScheme);
        }

        /// <summary>
        ///     Interprets the genome as a cyclic network and decodes to the corresponding graph using heuristics intended to
        ///     accelerate inference.
        /// </summary>
        /// <param name="genome">The CPPN genome to decode.</param>
        /// <returns>The decoded, recurrent graph structure.</returns>
        private FastCyclicNetwork DecodeToFastCyclicNetwork(NeatGenome genome)
        {
            return FastCyclicNetworkFactory.CreateFastCyclicNetwork(genome, _activationScheme);
        }

        #endregion
    }
}