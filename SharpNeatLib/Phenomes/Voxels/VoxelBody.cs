using System.Collections.Generic;
using System.Linq;
using SharpNeat.Utility;

namespace SharpNeat.Phenomes.Voxels
{
    /// <summary>
    ///     Specifies voxel material types and their ordinal value (which is aligned with the material ID specified in the
    ///     generated voxelyze configuration file).
    /// </summary>
    public enum VoxelMaterial
    {
        /// <summary>
        ///     Indicates that there is no material specified for the voxel position.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Passive material type that does not independently deform, but is not necessarily rigid and can therefore undergo
        ///     deformation via contact with adjacent cells.
        /// </summary>
        PassiveTissue = 1,

        /// <summary>
        ///     Active material type that expands and contracts (i.e. muscle).
        /// </summary>
        ActiveTissue = 3
    }

    /// <summary>
    ///     Encapsulates material properties of voxels forming a voxel structure, and the dimensions and composition of that
    ///     structure.
    /// </summary>
    public class VoxelBody
    {
        #region Constructor

        /// <summary>
        ///     VoxelBody constructor.
        /// </summary>
        /// <param name="cppn">The CPPN with substrate dimensions coding for the voxel body.</param>
        /// <param name="substrateResUpscale">The amount by which to increase the preset resolution.</param>
        public VoxelBody(IBlackBoxSubstrate cppn, int substrateResUpscale = 0)
        {
            // Activate CPPN for all positions on the substrate to get the voxel materials
            _voxelMaterials = ExtractVoxelMaterials(cppn, substrateResUpscale);

            // Copy off voxel dimensions
            LengthX = cppn.CppnSubstrateResolution.X + substrateResUpscale;
            LengthY = cppn.CppnSubstrateResolution.Y + substrateResUpscale;
            LengthZ = cppn.CppnSubstrateResolution.Z + substrateResUpscale;

            // Calculate the number of active and passive voxels and total voxels
            NumActiveVoxels = _voxelMaterials.SelectMany(x => x).Count(x => x == VoxelMaterial.ActiveTissue);
            NumPassiveVoxels = _voxelMaterials.SelectMany(x => x).Count(x => x == VoxelMaterial.PassiveTissue);
            NumMaterialVoxels = NumActiveVoxels + NumPassiveVoxels;
            NumVoxels = LengthX * LengthY * LengthZ;

            // Compute the proportion of the body that is composed of active/passive voxels and that is non-empty
            ActiveTissueProportion = (double) NumActiveVoxels / NumMaterialVoxels;
            PassiveTissueProportion = (double) NumPassiveVoxels / NumMaterialVoxels;
            FullProportion = (double) NumMaterialVoxels / NumVoxels;

            // Carry through the genome ID from the generate genome
            GenomeId = cppn.GenomeId;

            // Build voxel material lookup
            _voxelMaterialLookup = new VoxelMaterial[LengthX, LengthY, LengthZ];
            for (var z = 0; z < LengthZ; z++)
            {
                for (var y = 0; y < LengthY; y++)
                {
                    for (var x = 0; x < LengthX; x++)
                    {
                        _voxelMaterialLookup[x, y, z] = _voxelMaterials[z][y * LengthX + x];
                    }
                }
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Activates the CPPN for every position on the substrate to output the material and returns the layer-wise material
        ///     for each voxel.
        /// </summary>
        /// <param name="cppn">The CPPN with substrate dimensions coding for the voxel body.</param>
        /// <param name="substrateResUpscale">The amount by which to increase the preset resolution.</param>
        /// <returns>The layer-wise material for each voxel</returns>
        private IList<IList<VoxelMaterial>> ExtractVoxelMaterials(IBlackBoxSubstrate cppn, int substrateResUpscale)
        {
            var substrateRes = substrateResUpscale > 0
                ? new SubstrateResolution(cppn.CppnSubstrateResolution.X + substrateResUpscale,
                    cppn.CppnSubstrateResolution.Y + substrateResUpscale,
                    cppn.CppnSubstrateResolution.Y + substrateResUpscale)
                : cppn.CppnSubstrateResolution;

            IList<IList<VoxelMaterial>> layerwiseVoxelMaterial = new List<IList<VoxelMaterial>>(substrateRes.Z);

            // Compute distance to centroid for each voxel in the body
            var distanceMatrix = VoxelUtils.ComputeVoxelDistanceMatrix(substrateRes.X, substrateRes.Y, substrateRes.Z);

            // Normalize each position along each of three axes
            var xAxisNorm = VoxelUtils.NormalizeAxis(substrateRes.X);
            var yAxisNorm = VoxelUtils.NormalizeAxis(substrateRes.Y);
            var zAxisNorm = VoxelUtils.NormalizeAxis(substrateRes.Z);
            
            // Activate the CPPN for each voxel in the substrate
            for (var z = 0; z < substrateRes.Z; z++)
            {
                IList<VoxelMaterial> voxelMaterials = new List<VoxelMaterial>(substrateRes.X * substrateRes.Y);

                for (var y = 0; y < substrateRes.Y; y++)
                {
                    for (var x = 0; x < substrateRes.X; x++)
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

                        // Add material if voxel is enabled, otherwise set to 0 to indicate missing voxel
                        voxelMaterials.Add(outputSignalArr[0] > 0
                            ? outputSignalArr[1] > 0 ? VoxelMaterial.ActiveTissue : VoxelMaterial.PassiveTissue
                            : 0);
                    }
                }

                // Add layer materials to the overall list of materials for the body
                layerwiseVoxelMaterial.Add(voxelMaterials);
            }

            return layerwiseVoxelMaterial;
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The layer-wise voxel materials in the voxel structure.
        /// </summary>
        private readonly IList<IList<VoxelMaterial>> _voxelMaterials;

        /// <summary>
        ///     The 3D voxel array for rapid lookup.
        /// </summary>
        private readonly VoxelMaterial[,,] _voxelMaterialLookup;

        #endregion

        #region Public methods

        /// <summary>
        ///     Returns the material codes (muscle or tissue) for the specified layer.
        /// </summary>
        /// <param name="layer">The layer index for which to retrieve component voxel materials.</param>
        /// <returns>The concatenated string of material codes for the given layer.</returns>
        public string GetLayerMaterialCodes(int layer)
        {
            return string.Join("", _voxelMaterials[layer].Select(x => (int) x));
        }

        /// <summary>
        ///     Returns the material code (muscle, tissue or none) at the specified location.
        /// </summary>
        /// <param name="x">The x-location.</param>
        /// <param name="y">The y-location.</param>
        /// <param name="z">The z-location.</param>
        /// <returns>The material code at the 3D intersection.</returns>
        public VoxelMaterial GetMaterialAtLocation(int x, int y, int z)
        {
            return x < LengthX && y < LengthY && z < LengthZ ? _voxelMaterialLookup[x, y, z] : VoxelMaterial.None;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId { get; }

        /// <summary>
        ///     The number of voxels contained in the voxel structure.
        /// </summary>
        public int NumVoxels { get; }

        /// <summary>
        ///     The number of non-empty voxels (i.e. voxels containing passive or active material) in the voxel structure.
        /// </summary>
        public int NumMaterialVoxels { get; }

        /// <summary>
        ///     The number of active voxels contained in the voxel structure.
        /// </summary>
        public int NumActiveVoxels { get; }

        /// <summary>
        ///     The number of passive voxels contained in the voxel structure.
        /// </summary>
        public int NumPassiveVoxels { get; }

        /// <summary>
        ///     The overall proportion of active tissue in the voxel structure.
        /// </summary>
        public double ActiveTissueProportion { get; }

        /// <summary>
        ///     The overall proportion of passive tissue in the voxel structure.
        /// </summary>
        public double PassiveTissueProportion { get; }

        /// <summary>
        ///     The proportion of the voxel structure that contains material (i.e. is not empty).
        /// </summary>
        public double FullProportion { get; }

        /// <summary>
        ///     The length of the voxel structure along its X-axis.
        /// </summary>
        public int LengthX { get; }

        /// <summary>
        ///     The length of the voxel structure along its Y-axis.
        /// </summary>
        public int LengthY { get; }

        /// <summary>
        ///     The length of the voxel structure along its Z-axis.
        /// </summary>
        public int LengthZ { get; }

        #endregion
    }
}