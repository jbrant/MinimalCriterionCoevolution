namespace MazeExperimentSupportLib
{
    public struct MazeDiversityUnit
    {
        /// <summary>
        ///     The unique identifier of the first maze (which is the source maze being compared).
        /// </summary>
        public uint MazeId1 { get; }

        /// <summary>
        ///     The unique identifier of the second maze (the maze to which the first is being compared).
        /// </summary>
        public uint MazeId2 { get; }

        /// <summary>
        ///     The diversity score of maze 1 compared to maze 2 in terms of the distance between solution paths.
        /// </summary>
        public double MazeDiversityScore { get; }

        /// <summary>
        ///     Maze diversity unit constructor.
        /// </summary>
        /// <param name="mazeId1">The unique identifier of the first maze (which is the source maze being compared).</param>
        /// <param name="mazeId2">The unique identifier of the second maze (the maze to which the first is being compared).</param>
        /// <param name="mazeDiversityScore">
        ///     The diversity score of maze 1 compared to maze 2 in terms of the distance between
        ///     solution paths.
        /// </param>
        public MazeDiversityUnit(uint mazeId1, uint mazeId2, double mazeDiversityScore)
        {
            MazeId1 = mazeId1;
            MazeId2 = mazeId2;
            MazeDiversityScore = mazeDiversityScore;
        }
    }
}