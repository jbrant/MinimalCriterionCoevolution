namespace VoxelBodyGenerator
{
    /// <summary>
    ///     Runtime parameters that can be set upon application invocation.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     The length of the voxely body along all three dimensions (assumed to be square).
        /// </summary>
        BodySize,

        /// <summary>
        ///     The minimum proportion of muscle (active) voxels.
        /// </summary>
        MinMuscleProportion,

        /// <summary>
        ///     The minimum proportion of the voxely body that is filled with voxels (i.e. not empty space).
        /// </summary>
        MinFullProportion,

        /// <summary>
        ///     The path to the template file describing the default and empty fields for the voxelyze-specific body specification.
        /// </summary>
        BodyTemplateFilePath,

        /// <summary>
        ///     The number of bodies to generate.
        /// </summary>
        BodyCount,

        /// <summary>
        ///     The directory into which to write generated CPPN genomes.
        /// </summary>
        GenomeOutputDirectory,

        /// <summary>
        ///     The directory into which to write voxelyze-specific body configuration files.
        /// </summary>
        ConfigOutputDirectory
    }
}