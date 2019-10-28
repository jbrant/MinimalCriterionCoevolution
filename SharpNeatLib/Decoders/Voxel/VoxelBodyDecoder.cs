using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Voxels;

namespace SharpNeat.Decoders.Voxel
{
    public class VoxelBodyDecoder : VoxelDecoder, IGenomeDecoder<NeatGenome, VoxelBody>
    {
        #region Constructors

        public VoxelBodyDecoder(NetworkActivationScheme activationScheme, int x, int y, int z) : base(activationScheme,
            x, y, z)
        {
        }

        #endregion

        public VoxelBody Decode(NeatGenome genome)
        {
            IList<IList<VoxelMaterial>> layerwiseVoxelMaterial = new List<IList<VoxelMaterial>>(Z);

            // Decode to CPPN
            var cppn = DecodeCppnMethod(genome);

            // Activate the CPPN for each voxel in the substrate
            for (var i = 0; i < Z; i++)
            {
                IList<VoxelMaterial> voxelMaterials = new List<VoxelMaterial>(X * Y);

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

                        // Add material if voxel is enabled, otherwise set to 0 to indicate missing voxel
                        voxelMaterials.Add(outputSignalArr[0] > 0
                            ? outputSignalArr[1] > 0 ? VoxelMaterial.ActiveTissue : VoxelMaterial.PassiveTissue
                            : 0);
                    }
                }

                // Add layer materials to the overall list of materials for the body
                layerwiseVoxelMaterial.Add(voxelMaterials);
            }

            // Construct and return voxel body
            return new VoxelBody(layerwiseVoxelMaterial, X, Y, Z, genome.Id);
        }
    }
}