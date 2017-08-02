namespace MazeExperimentSupportLib
{
    /// <summary>
    ///     Encapsulates a single measure of trajectory diversity.
    /// </summary>
    public struct TrajectoryDiversityUnit
    {
        /// <summary>
        ///     Trajectory diversity unit constructor.
        /// </summary>
        /// <param name="agentId">The unique agent identifier.</param>
        /// <param name="mazeId">The unique maze identifier.</param>
        /// <param name="intraMazeDiversityScore">
        ///     The diversity score of the trajectory as compared to the trajectory of other
        ///     agents through the same maze.
        /// </param>
        /// <param name="interMazeDiversityScore">
        ///     The diversity score of the trajectory as compared to the trajectory of other
        ///     agents through different mazes.
        /// </param>
        /// <param name="globalDiversityScore">
        ///     The diversity score of the trajectory as compared to the trajectory of other agents
        ///     through any other maze (including the current one).
        /// </param>
        public TrajectoryDiversityUnit(int agentId, int mazeId, double intraMazeDiversityScore,
            double interMazeDiversityScore, double globalDiversityScore)
        {
            AgentId = agentId;
            MazeId = mazeId;
            IntraMazeDiversityScore = intraMazeDiversityScore;
            InterMazeDiversityScore = interMazeDiversityScore;
            GlobalDiversityScore = globalDiversityScore;
        }

        /// <summary>
        ///     The unique agent identifier.
        /// </summary>
        public int AgentId { get; }

        /// <summary>
        ///     The unique maze identifier
        /// </summary>
        public int MazeId { get; }

        /// <summary>
        ///     The diversity score of the trajectory as compared to the trajectory of other agents through the same maze.
        /// </summary>
        public double IntraMazeDiversityScore { get; }

        /// <summary>
        ///     The diversity score of the trajectory as compared to the trajectory of other agents through different mazes.
        /// </summary>
        public double InterMazeDiversityScore { get; }

        /// <summary>
        ///     The diversity score of the trajectory as compared to the trajectory of other agents through any other maze
        ///     (including the current one).
        /// </summary>
        public double GlobalDiversityScore { get; }
    }
}