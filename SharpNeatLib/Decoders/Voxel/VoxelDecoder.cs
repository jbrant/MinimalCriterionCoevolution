using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.NeuralNets.AcyclicNetwork;
using SharpNeat.Phenomes.NeuralNets.CyclicNetwork;
using SharpNeat.Phenomes.NeuralNets.FastCyclicNetwork;
using SharpNeat.Phenomes.Voxels;
using SharpNeat.Utility;

namespace SharpNeat.Decoders.Voxel
{
    public class VoxelDecoder
    {
        #region Instance variables

        private readonly NetworkActivationScheme _activationScheme;
        protected delegate IBlackBox DecodeGenome(NeatGenome genome);
        protected readonly DecodeGenome DecodeCppnMethod;

        protected readonly int X;
        protected readonly int Y;
        protected readonly int Z;

        protected readonly double[,,] _distanceMatrix;

        #endregion

        #region Constructors

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
            _distanceMatrix = VoxelUtils.ComputeVoxelDistanceMatrix(x, y, z);
        }

        #endregion
        
        #region Private methods

        private DecodeGenome GetDecodeMethod(NetworkActivationScheme activationScheme)
        {
            if(activationScheme.AcyclicNetwork)
            {
                return DecodeToFastAcyclicNetwork;
            }

            if(activationScheme.FastFlag)
            {
                return DecodeToFastCyclicNetwork;
            }
            return DecodeToCyclicNetwork;
        }

        private FastAcyclicNetwork DecodeToFastAcyclicNetwork(NeatGenome genome)
        {
            return FastAcyclicNetworkFactory.CreateFastAcyclicNetwork(genome);
        }

        private CyclicNetwork DecodeToCyclicNetwork(NeatGenome genome)
        {
            return CyclicNetworkFactory.CreateCyclicNetwork(genome, _activationScheme);
        }

        private FastCyclicNetwork DecodeToFastCyclicNetwork(NeatGenome genome)
        {
            return FastCyclicNetworkFactory.CreateFastCyclicNetwork(genome, _activationScheme);
        }

        #endregion
    }
}