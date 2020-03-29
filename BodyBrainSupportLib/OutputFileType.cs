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
        ///     Body similarity measurements of bodies with equivalent size averaged over full run.
        /// </summary>
        RunSizeBodyDiversityData,

        /// <summary>
        ///     Body similarity measurements averaged over extant bodies in each batch.
        /// </summary>
        BatchBodyDiversityData,

        /// <summary>
        ///     Trajectory similarity measurements averaged over full run.
        /// </summary>
        RunTrajectoryDiversityData,

        /// <summary>
        ///     Trajectory similarity measurements for bodies with equivalent size averaged over full run.
        /// </summary>
        RunSizeTrajectoryDiversityData,

        /// <summary>
        ///     Trajectory similarity measurements averaged over resulting trajectories in each batch.
        /// </summary>
        BatchTrajectoryDiversityData
    }
}