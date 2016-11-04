namespace MazeExperimentSuppotLib
{
    /// <summary>
    ///     Output file types for various supported analyses.
    /// </summary>
    public enum OutputFileType
    {
        /// <summary>
        ///     Navigator/Maze simulation data.
        /// </summary>
        NavigatorMazeEvaluationData,

        /// <summary>
        ///     All points visited in agent trajectory.
        /// </summary>
        TrajectoryData,

        /// <summary>
        ///     Trajectory diversity score data.
        /// </summary>
        TrajectoryDiversityData,

        /// <summary>
        ///     Novelty search comparison data.
        /// </summary>
        NoveltySearchComparisonData
    }
}