using System.Collections.Generic;
using System.Linq;

namespace SharpNeat.Phenomes.Voxels
{
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

    public class VoxelBody
    {
        private readonly IList<IList<VoxelMaterial>> _voxels;

        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId { get; }
        
        public int NumVoxels { get; }
        public int NumActiveVoxels { get; }
        public int NumTissueVoxels { get; }
        public double ActiveTissueProportion { get; }
        public double PassiveTissueProportion { get; }
        public double FullProportion { get; }

        public int Xlength { get; }

        public int Ylength { get; }

        public int Zlength { get; }

        public VoxelBody(IList<IList<VoxelMaterial>> voxels, int xlength, int ylength, int zlength, uint genomeId)
        {
            // Record voxel structure, number of voxels and voxels per dimension
            _voxels = voxels;
            Xlength = xlength;
            Ylength = ylength;
            Zlength = zlength;

            // Calculate the number of active and passive voxels and total voxels
            NumActiveVoxels = voxels.SelectMany(x => x).Count(x => x == VoxelMaterial.ActiveTissue);
            NumTissueVoxels = voxels.SelectMany(x => x).Count(x => x == VoxelMaterial.PassiveTissue);
            NumVoxels = xlength * ylength * zlength;

            // Compute the proportion of the body that is composed of active/passive voxels and that is non-empty
            ActiveTissueProportion = (double) NumActiveVoxels / NumVoxels;
            PassiveTissueProportion = (double) NumTissueVoxels / NumVoxels;
            FullProportion = (double) (NumActiveVoxels + NumTissueVoxels) / NumVoxels;
            
            // Carry through the genome ID from the generate genome
            GenomeId = genomeId;
        }

        public string GetLayerMaterialCodes(int layer)
        {
            return string.Join("", _voxels[layer].Select(x => (int) x));
        }
    }
}