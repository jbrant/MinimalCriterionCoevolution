using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Voxels;

namespace SharpNeat.Decoders.Voxel
{
    public class VoxelBrainDecoder : VoxelDecoder, IGenomeDecoder<NeatGenome, VoxelBrain>
    {
        #region Instance variables

        private readonly int _numConnections;

        #endregion
        
        #region Constructors

        public VoxelBrainDecoder(NetworkActivationScheme activationScheme, int x, int y, int z, int numConnections) : base(activationScheme,
            x, y, z)
        {
            _numConnections = numConnections;
        }

        #endregion

        #region IGenomeDecoder members

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
                        inputSignalArr[3] = _distanceMatrix[k, j, i]; // distance

                        // Reset from prior network activations
                        cppn.ResetState();

                        // Activate the network with the current inputs
                        cppn.Activate();

                        // Add synapse weights to the list of weights for the layer
                        // _numConnections is multiplied by 2 because for each connection, we have a flag indicating
                        // whether it is expressed in the phenotype, along with its weight
                        for (var idx = 0; idx < _numConnections*2; idx += 2)
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