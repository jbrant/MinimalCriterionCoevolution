using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Voxels;

namespace SharpNeat.Decoders.Voxel
{
    /// <summary>
    ///     The voxel brain decoder decodes a given CPPN genome into a CPPN graph structure, which queries each position on the
    ///     voxel substrate and produces voxel-specific neurocontrollers.
    /// </summary>
    public class VoxelBrainDecoder : VoxelDecoder, IGenomeDecoder<NeatGenome, VoxelBrain>
    {
        #region Instance variables

        /// <summary>
        ///     The number of connections in the CPPN controller network.
        /// </summary>
        private readonly int _numConnections;

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructor which accepts the chosen network activation scheme, along with the voxel body dimensions and number of
        ///     voxel controller connections.
        /// </summary>
        /// <param name="activationScheme">The CPPN activation scheme.</param>
        /// <param name="x">The length of the X-axis on the voxel lattice.</param>
        /// <param name="y">The length of the Y-axis on the voxel lattice.</param>
        /// <param name="z">The length of the Z-axis on the voxel lattice.</param>
        /// <param name="numConnections">The number of connections in the voxel-specific neurocontrollers.</param>
        public VoxelBrainDecoder(NetworkActivationScheme activationScheme, int x, int y, int z, int numConnections) :
            base(activationScheme,
                x, y, z)
        {
            _numConnections = numConnections;
        }

        #endregion

        #region IGenomeDecoder members

        /// <summary>
        ///     Decodes a given CPPN genome into the corresponding graph representation, then queries each position on the voxel
        ///     substrate to produce a separate neural network controller for each voxel, along with its respective connection
        ///     weights.
        /// </summary>
        /// <param name="genome">The CPPN genome to decode and query the voxel substrate.</param>
        /// <returns>The per-voxel neural networks produced by querying the substrate with the decoded CPPN genome.</returns>
        public VoxelBrain Decode(NeatGenome genome)
        {
            IList<IList<double>> layerwiseBrainWeights = new List<IList<double>>(Z);

            // Decode to CPPN
            var cppn = DecodeCppnMethod(genome);

            // Activate the CPPN for each voxel in the substrate
            // (the z dimension is first because this defines the layers of the voxel structure -
            // x/y are reversed for consistency, though order doesn't matter here)
            for (var i = 0; i < Z; i++)
            {
                IList<double> layerWeights = new List<double>(X * Y);

                for (var j = 0; j < Y; j++)
                {
                    for (var k = 0; k < X; k++)
                    {
                        // Get references to CPPN input and output
                        var inputSignalArr = cppn.InputSignalArray;
                        var outputSignalArr = cppn.OutputSignalArray;

                        // Set the input values at the current voxel
                        inputSignalArr[0] = k; // X coordinate
                        inputSignalArr[1] = j; // Y coordinate
                        inputSignalArr[2] = i; // Z coordinate
                        inputSignalArr[3] = DistanceMatrix[k, j, i]; // distance

                        // Reset from prior network activations
                        cppn.ResetState();

                        // Activate the network with the current inputs
                        cppn.Activate();

                        // Add synapse weights to the list of weights for the layer
                        // _numConnections is multiplied by 2 because for each connection, we have a flag indicating
                        // whether it is expressed in the phenotype, along with its weight
                        for (var idx = 0; idx < _numConnections * 2; idx += 2)
                        {
                            // If connection is enabled, set weight to either -1 or 1 depending on polarization;
                            // otherwise, connection is disabled so set weight to 0
                            layerWeights.Add(outputSignalArr[idx] > 0 ? outputSignalArr[idx + 1] < 0 ? -1.0 : 1.0 : 0);
                        }
                    }
                }

                // Add layer weights to the overall list of weights for the brain
                layerwiseBrainWeights.Add(layerWeights);
            }

            // Construct and return voxel brain
            return new VoxelBrain(layerwiseBrainWeights, _numConnections, genome.Id);
        }

        #endregion
    }
}