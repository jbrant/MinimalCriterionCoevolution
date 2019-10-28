using System.Collections.Generic;
using System.Linq;

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
        /// <summary>
        ///     The layer-wise voxels in the voxel structure.
        /// </summary>
        private readonly IList<IList<VoxelMaterial>> _voxels;

        /// <summary>
        ///     VoxelBody constructor.
        /// </summary>
        /// <param name="voxels">The layer-wise voxels in the voxel structure.</param>
        /// <param name="xlength">The length of the voxel structure along its X-axis.</param>
        /// <param name="ylength">The length of the voxel structure along its Y-axis.</param>
        /// <param name="zlength">The length of the voxel structure along its Z-axis.</param>
        /// <param name="genomeId">The ID of the genome from which the voxel body was generated.</param>
        public VoxelBody(IList<IList<VoxelMaterial>> voxels, int xlength, int ylength, int zlength, uint genomeId)
        {
            // Record voxel structure, number of voxels and voxels per dimension
            _voxels = voxels;
            Xlength = xlength;
            Ylength = ylength;
            Zlength = zlength;

            // Calculate the number of active and passive voxels and total voxels
            NumActiveVoxels = voxels.SelectMany(x => x).Count(x => x == VoxelMaterial.ActiveTissue);
            NumPassiveVoxels = voxels.SelectMany(x => x).Count(x => x == VoxelMaterial.PassiveTissue);
            NumVoxels = xlength * ylength * zlength;

            // Compute the proportion of the body that is composed of active/passive voxels and that is non-empty
            ActiveTissueProportion = (double) NumActiveVoxels / NumVoxels;
            PassiveTissueProportion = (double) NumPassiveVoxels / NumVoxels;
            FullProportion = (double) (NumActiveVoxels + NumPassiveVoxels) / NumVoxels;

            // Carry through the genome ID from the generate genome
            GenomeId = genomeId;
        }

        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId { get; }

        /// <summary>
        ///     The number of voxels contained in the voxel structure.
        /// </summary>
        public int NumVoxels { get; }

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
        public int Xlength { get; }

        /// <summary>
        ///     The length of the voxel structure along its Y-axis.
        /// </summary>
        public int Ylength { get; }

        /// <summary>
        ///     The length of the voxel structure along its Z-axis.
        /// </summary>
        public int Zlength { get; }

        /// <summary>
        ///     Returns the material codes (muscle or tissue) for the specified layer.
        /// </summary>
        /// <param name="layer">The layer index for which to retrieve component voxel materials.</param>
        /// <returns>The concatenated string of material codes for the given layer.</returns>
        public string GetLayerMaterialCodes(int layer)
        {
            return string.Join("", _voxels[layer].Select(x => (int) x));
        }
    }
}