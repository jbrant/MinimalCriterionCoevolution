using System.Collections.Generic;
using System.Linq;

namespace SharpNeat.Phenomes.Voxels
{
    public class VoxelBrain
    {
        private readonly IList<IList<double>> _voxelCellNetworkWeights;

        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId { get; }

        public int NumConnections { get; }

        public VoxelBrain(IList<IList<double>> voxelCellNetworkWeights, int numConnections, uint genomeId)
        {
            _voxelCellNetworkWeights = voxelCellNetworkWeights;

            // Set the number of connections in the brain neural network
            NumConnections = numConnections;

            // Carry through the genome ID from the generate genome
            GenomeId = genomeId;
        }

        public string GetLayerSynapseWeights(int layer)
        {
            return string.Join(",", _voxelCellNetworkWeights[layer].Select(x => x));
        }
    }
}