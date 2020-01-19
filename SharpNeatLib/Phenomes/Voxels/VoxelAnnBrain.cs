using System.Collections.Generic;
using System.Linq;
using SharpNeat.Phenomes;
using SharpNeat.Utility;

namespace SharpNeat.Phenomes.Voxels
{
    /// <summary>
    ///     Encapsulates neural network weights for voxel brains corresponding to each voxel in a voxel body.
    /// </summary>
    public class VoxelAnnBrain : IVoxelBrain
    {
        #region Constructor

        /// <summary>
        ///     VoxelBrain constructor for ANN-based brain.
        /// </summary>
        /// <param name="cppn">The CPPN coding for the voxel brain.</param>
        /// <param name="substrateX">The substrate resolution along the X dimension.</param>
        /// <param name="substrateY">The substrate resolution along the Y dimension.</param>
        /// <param name="substrateZ">The substrate resolution along the Z dimension.</param>
        /// <param name="numConnections">The number of connections in the voxel-specific neurocontrollers.</param>
        public VoxelAnnBrain(IBlackBox cppn, int substrateX, int substrateY, int substrateZ, int numConnections)
        {
            // Activate CPPN for all positions on the substrate to get the per-voxel controller weights
            _voxelCellNetworkWeights =
                ExtractVoxelNetworkWeights(cppn, substrateX, substrateY, substrateZ, numConnections);

            // Set the number of connections in the brain neural network
            NumConnections = numConnections;

            // Carry through the genome ID from the generate genome
            GenomeId = cppn.GenomeId;
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Activates the CPPN for every position on the substrate to output the connection presence/weights for every
        ///     connection in each voxel neurocontroller.
        /// </summary>
        /// <param name="cppn">The CPPN coding for the voxel brain.</param>
        /// <param name="substrateX">The substrate resolution along the X dimension.</param>
        /// <param name="substrateY">The substrate resolution along the Y dimension.</param>
        /// <param name="substrateZ">The substrate resolution along the Z dimension.</param>
        /// <param name="numConnections">The number of connections in the voxel-specific neurocontrollers.</param>
        /// <returns>The layer-wise neurocontroller weights for each voxel.</returns>
        private IList<IList<double>> ExtractVoxelNetworkWeights(IBlackBox cppn, int substrateX, int substrateY,
            int substrateZ, int numConnections)
        {
            IList<IList<double>> layerwiseBrainWeights = new List<IList<double>>(substrateZ);

            // Compute distance to centroid for each voxel in the body
            var distanceMatrix = VoxelUtils.ComputeVoxelDistanceMatrix(substrateX, substrateY, substrateZ);
            
            // Normalize each position along each of three axes
            var xAxisNorm = VoxelUtils.NormalizeAxis(substrateX);
            var yAxisNorm = VoxelUtils.NormalizeAxis(substrateY);
            var zAxisNorm = VoxelUtils.NormalizeAxis(substrateZ);

            // Activate the CPPN for each voxel in the substrate
            // (the z dimension is first because this defines the layers of the voxel structure -
            // x/y are reversed for consistency, though order doesn't matter here)
            for (var z = 0; z < substrateZ; z++)
            {
                IList<double> layerWeights = new List<double>(substrateX * substrateY);

                for (var y = 0; y < substrateY; y++)
                {
                    for (var x = 0; x < substrateX; x++)
                    {
                        // Get references to CPPN input and output
                        var inputSignalArr = cppn.InputSignalArray;
                        var outputSignalArr = cppn.OutputSignalArray;

                        // Set the input values at the current voxel
                        inputSignalArr[0] = xAxisNorm[x]; // X coordinate
                        inputSignalArr[1] = yAxisNorm[y]; // Y coordinate
                        inputSignalArr[2] = zAxisNorm[z]; // Z coordinate
                        inputSignalArr[3] = distanceMatrix[x, y, z]; // distance

                        // Reset from prior network activations
                        cppn.ResetState();

                        // Activate the network with the current inputs
                        cppn.Activate();

                        // Add synapse weights to the list of weights for the layer
                        // _numConnections is multiplied by 2 because for each connection, we have a flag indicating
                        // whether it is expressed in the phenotype, along with its weight
                        for (var idx = 0; idx < numConnections * 2; idx += 2)
                        {
                            // if this is the last two connections to the ANN output node, they are always enabled
                            if (idx / 2 >= numConnections - NumOutputNodeConnections)
                            {
                                layerWeights.Add(outputSignalArr[idx] < 0 ? -1.0 : 1.0);
                            }
                            else
                            {
                                // If connection is enabled, set weight to either -1 or 1 depending on polarization;
                                // otherwise, connection is disabled so set weight to 0
                                layerWeights.Add(outputSignalArr[idx] > 0
                                    ? outputSignalArr[idx + 1] < 0 ? -1.0 : 1.0
                                    : 0);
                            }
                        }
                    }
                }

                // Add layer weights to the overall list of weights for the brain
                layerwiseBrainWeights.Add(layerWeights);
            }

            return layerwiseBrainWeights;
        }

        #endregion

        #region Public methods
        
        /// <summary>
        ///     Returns the layer connection (synapse) weights for the specified layer.
        /// </summary>
        /// <param name="layer">The layer index for which to retrieve the controller connection (synapse) weights.</param>
        /// <returns>
        ///     The comma-delimited string of connection (synapse) weights for all voxel-specific controllers in the given
        ///     layer.
        /// </returns>
        public string GetFlattenedLayerData(int layer)
        {
            return string.Join(",", _voxelCellNetworkWeights[layer].Select(x => x));
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The layer-wise list of weights for each voxel in the voxel body.
        /// </summary>
        private readonly IList<IList<double>> _voxelCellNetworkWeights;

        /// <summary>
        ///     The number of connections inbound to the output node.
        /// </summary>
        private const int NumOutputNodeConnections = 2;

        #endregion

        #region Properties

        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId { get; }

        
        /// <summary>
        ///     The number of connections in a given voxel-specific controller.
        /// </summary>
        public int NumConnections { get; }

        #endregion
    }
}