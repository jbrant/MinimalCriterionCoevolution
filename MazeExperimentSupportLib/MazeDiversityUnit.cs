namespace MazeExperimentSupportLib
{
    public struct MazeDiversityUnit
    {
        /// <summary>
        ///     The unique identifier of the maze whose solution path diversity is calculated.
        /// </summary>
        public uint MazeId { get; }

        /// <summary>
        ///     The diversity score of maze 1 compared to maze 2 in terms of the distance between solution paths.
        /// </summary>
        public double MazeDiversityScore { get; }

        /// <summary>
        ///     Maze diversity unit constructor.
        /// </summary>
        /// <param name="mazeId">The unique identifier of the maze whose solution path diversity is calculated.</param>
        /// <param name="mazeDiversityScore">
        ///     The diversity score of the given maze compared to the solution paths of all other mazes in the population.
        /// </param>
        public MazeDiversityUnit(uint mazeId, double mazeDiversityScore)
        {
            MazeId = mazeId;
            MazeDiversityScore = mazeDiversityScore;
        }
    }
}