namespace BodyBrainSupportLib
{
    /// <summary>
    ///     Encapsulates the average difference between the current voxel body and the rest of the population by comparing
    ///     material properties at individual cells.
    /// </summary>
    public class BodyDiversityUnit
    {
        /// <summary>
        ///     BodyDiversityUnit constructor.
        /// </summary>
        /// <param name="bodyId">The voxel body genome ID.</param>
        /// <param name="bodySize">The size of the voxel body grid.</param>
        /// <param name="avgVoxelDiff">
        ///     The average number of overall voxel mismatches (including empty voxels) compared to other
        ///     bodies in the population.
        /// </param>
        /// <param name="avgVoxelMaterialDiff">
        ///     The average number of voxel material mismatches compared to other bodies in the
        ///     population.
        /// </param>
        /// <param name="avgVoxelActiveMaterialDiff">
        ///     The average number of active voxel mismatches compared to other bodies in the
        ///     population.
        /// </param>
        /// <param name="avgVoxelPassiveMaterialDiff">
        ///     The average number of passive voxel mismatches compared to other bodies in
        ///     the population.
        /// </param>
        public BodyDiversityUnit(uint bodyId, int bodySize, double avgVoxelDiff, double avgVoxelMaterialDiff,
            double avgVoxelActiveMaterialDiff, double avgVoxelPassiveMaterialDiff)
        {
            BodyId = bodyId;
            BodySize = bodySize;
            AvgVoxelDiff = avgVoxelDiff;
            AvgVoxelMaterialDiff = avgVoxelMaterialDiff;
            AvgVoxelActiveMaterialDiff = avgVoxelActiveMaterialDiff;
            AvgVoxelPassiveMaterialDiff = avgVoxelPassiveMaterialDiff;
        }

        /// <summary>
        ///     The voxel body genome ID.
        /// </summary>
        public uint BodyId { get; }

        /// <summary>
        ///     The size of the voxel body.
        /// </summary>
        public int BodySize { get; }

        /// <summary>
        ///     The average number of overall voxel mismatches (including empty voxels) compared to other bodies in the population.
        /// </summary>
        public double AvgVoxelDiff { get; }

        /// <summary>
        ///     The average number of voxel material mismatches compared to other bodies in the population.
        /// </summary>
        public double AvgVoxelMaterialDiff { get; }

        /// <summary>
        ///     The average number of active voxel mismatches compared to other bodies in the population.
        /// </summary>
        public double AvgVoxelActiveMaterialDiff { get; }

        /// <summary>
        ///     The average number of passive voxel mismatches compared to other bodies in the population.
        /// </summary>
        public double AvgVoxelPassiveMaterialDiff { get; }
    }
}