namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     The maze wall encapsulates the start and end of a particular wall.  The maze itself can contain 0 or more maze
    ///     walls.
    /// </summary>
    public class MazeWall
    {
        #region Constructors

        /// <summary>
        ///     Default constructor for a maze wall.
        /// </summary>
        public MazeWall()
        {
        }

        /// <summary>
        ///     Constructor accepting the start and end coordinates of the new maze wall.
        /// </summary>
        /// <param name="xStart">X start coordinate.</param>
        /// <param name="yStart">Y start coordinate.</param>
        /// <param name="xEnd">X end coordinate.</param>
        /// <param name="yEnd">Y end coordinate.</param>
        public MazeWall(int xStart, int yStart, int xEnd, int yEnd)
        {
            StartMazePoint = new MazePoint(xStart, yStart);
            EndMazePoint = new MazePoint(xEnd, yEnd);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Starting point of the maze wall.
        /// </summary>
        public MazePoint StartMazePoint { get; set; }

        /// <summary>
        ///     Ending point of the maze wall.
        /// </summary>
        public MazePoint EndMazePoint { get; set; }

        #endregion
    }

    /// <summary>
    ///     The maze point struct contains the X and Y coordinates of the start or end of a maze wall.
    /// </summary>
    public struct MazePoint
    {
        /// <summary>
        ///     Constructor accept the X and Y coordinates of the new point.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public MazePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        ///     The X coordinate property.
        /// </summary>
        public int X { get; }

        /// <summary>
        ///     The Y coordinate property.
        /// </summary>
        public int Y { get; }
    }
}