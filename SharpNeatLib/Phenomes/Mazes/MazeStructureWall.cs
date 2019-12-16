namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     The maze wall encapsulates the start and end of a particular wall.  The maze itself can contain 0 or more maze
    ///     walls.
    /// </summary>
    public class MazeStructureWall
    {
        #region Constructors

        /// <summary>
        ///     Default constructor for a maze wall.
        /// </summary>
        public MazeStructureWall()
        {
        }

        /// <summary>
        ///     Constructor accepting the start and end coordinates of the new maze wall.
        /// </summary>
        /// <param name="xStart">X start coordinate.</param>
        /// <param name="yStart">Y start coordinate.</param>
        /// <param name="xEnd">X end coordinate.</param>
        /// <param name="yEnd">Y end coordinate.</param>
        public MazeStructureWall(int xStart, int yStart, int xEnd, int yEnd)
        {
            StartMazeStructurePoint = new MazeStructurePoint(xStart, yStart);
            EndMazeStructurePoint = new MazeStructurePoint(xEnd, yEnd);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Starting point of the maze wall.
        /// </summary>
        public MazeStructurePoint StartMazeStructurePoint { get; set; }

        /// <summary>
        ///     Ending point of the maze wall.
        /// </summary>
        public MazeStructurePoint EndMazeStructurePoint { get; set; }

        #endregion
    }
}