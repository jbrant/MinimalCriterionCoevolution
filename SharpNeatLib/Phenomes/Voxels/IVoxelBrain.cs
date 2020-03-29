namespace SharpNeat.Phenomes.Voxels
{
    /// <summary>
    ///     Interface for voxel brain, which can be represented by phase-offset controller or neural network.
    /// </summary>
    public interface IVoxelBrain
    {
        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        uint GenomeId { get; }

        /// <summary>
        ///     Returns the comma-delimited, voxel-specific control values for the specified layer.
        /// </summary>
        /// <param name="layer">The layer index for which to retrieve the voxel-specific control values.</param>
        /// <returns>The comma-delimited string of voxel-specific control values in the given layer.</returns>
        string GetFlattenedLayerData(int layer);
    }
}