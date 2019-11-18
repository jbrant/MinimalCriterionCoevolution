using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Voxels;

namespace SharpNeat.Decoders.Voxel
{
    /// <summary>
    ///     The voxel body decoder decodes a given CPPN genome into a CPPN graph structure, which queries each position on the
    ///     voxel substrate and produces voxel existence and material properties (i.e. passive or active) for each position in
    ///     the voxel lattice.
    /// </summary>
    public class VoxelBodyDecoder : VoxelDecoder, IGenomeDecoder<NeatGenome, VoxelBody>
    {
        #region Constructors

        /// <summary>
        ///     Constructor which accepts the chosen network activation scheme, along with the voxel body dimensions, and passes
        ///     them to the parent decoder.
        /// </summary>
        /// <param name="activationScheme">The CPPN activation scheme.</param>
        /// <param name="x">The length of the X-axis on the voxel lattice.</param>
        /// <param name="y">The length of the Y-axis on the voxel lattice.</param>
        /// <param name="z">The length of the Z-axis on the voxel lattice.</param>
        public VoxelBodyDecoder(NetworkActivationScheme activationScheme, int x, int y, int z) : base(activationScheme,
            x, y, z)
        {
        }

        #endregion

        /// <summary>
        ///     Decodes a given CPPN genome into the corresponding graph representation, then queries each position on the voxel
        ///     substrate to produce body voxels and their respective material types.
        /// </summary>
        /// <param name="genome">The CPPN genome to decode and query the voxel substrate.</param>
        /// <returns>The voxel structure given by querying the substrate with the decoded CPPN genome.</returns>
        public VoxelBody Decode(NeatGenome genome)
        {
            IList<IList<VoxelMaterial>> layerwiseVoxelMaterial = new List<IList<VoxelMaterial>>(Z);

            // Decode to CPPN
            var cppn = DecodeCppnMethod(genome);

            // Activate the CPPN for each voxel in the substrate
            for (var z = 0; z < Z; z++)
            {
                IList<VoxelMaterial> voxelMaterials = new List<VoxelMaterial>(X * Y);

                for (var y = 0; y < Y; y++)
                {
                    for (var x = 0; x < X; x++)
                    {
                        // Get references to CPPN input and output
                        var inputSignalArr = cppn.InputSignalArray;
                        var outputSignalArr = cppn.OutputSignalArray;

                        // Set the input values at the current voxel
                        inputSignalArr[0] = x; // X coordinate
                        inputSignalArr[1] = y; // Y coordinate
                        inputSignalArr[2] = z; // Z coordinate
                        inputSignalArr[3] = DistanceMatrix[x, y, z]; // distance

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