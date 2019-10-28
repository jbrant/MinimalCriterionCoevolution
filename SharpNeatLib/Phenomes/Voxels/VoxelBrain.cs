using System.Collections.Generic;
using System.Linq;

namespace SharpNeat.Phenomes.Voxels
{
    /// <summary>
    ///     Encapsulates neural network weights for voxel brains corresponding to each voxel in a voxel body.
    /// </summary>
    public class VoxelBrain
    {
        /// <summary>
        ///     The layer-wise list of weights for each voxel in the voxel body.
        /// </summary>
        private readonly IList<IList<double>> _voxelCellNetworkWeights;

        /// <summary>
        ///     VoxelBrain constructor.
        /// </summary>
        /// <param name="voxelCellNetworkWeights">The layer-wise list of weights for each voxel in the voxel body.</param>
        /// <param name="numConnections">The number of connections in a given voxel-specific controller.</param>
        /// <param name="genomeId">The unique identifier of the genome from which the phenotype was generated.</param>
        public VoxelBrain(IList<IList<double>> voxelCellNetworkWeights, int numConnections, uint genomeId)
        {
            _voxelCellNetworkWeights = voxelCellNetworkWeights;

            // Set the number of connections in the brain neural network
            NumConnections = numConnections;

            // Carry through the genome ID from the generate genome
            GenomeId = genomeId;
        }

        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId { get; }

        /// <summary>
        ///     The number of connections in a given voxel-specific controller.
        /// </summary>
        public int NumConnections { get; }

        /// <summary>
        ///     Returns the layer connection (synapse) weights for the specified layer.
        /// </summary>
        /// <param name="layer">The layer index for which to retrieve the controller connection (synapse) weights.</param>
        /// <returns>
        ///     The comma-delimited string of connection (synapse) weights for all voxel-specific controllers in the given
        ///     layer.
        /// </returns>
        public string GetLayerSynapseWeights(int layer)
        {
            return string.Join(",", _voxelCellNetworkWeights[layer].Select(x => x));
        }
    }
}