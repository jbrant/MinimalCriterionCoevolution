namespace BodyBrainSupportLib
{
    /// <summary>
    ///     Output file types for body/brain post-hoc analyses
    /// </summary>
    public enum OutputFileType
    {
        /// <summary>
        ///     Verbose body/brain simulation log data.
        /// </summary>
        SimulationLogData,

        /// <summary>
        ///     CPPN upscale upscale result data.
        /// </summary>
        UpscaleResultData,

        /// <summary>
        ///     Body similarity measurements averaged over full run.
        /// </summary>
        RunBodyDiversityData,

        /// <summary>
        ///     Body similarity measurements averaged over extant bodies in each batch.
        /// </summary>
        BatchBodyDiversityData
    }
}