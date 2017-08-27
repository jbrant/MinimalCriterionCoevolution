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

    /// <summary>
    ///     The maze point class contains the X and Y coordinates of the start or end of a maze wall.
    /// </summary>
    public class MazeStructurePoint
    {
        #region Constructor

        /// <summary>
        ///     Constructor accepts the X and Y coordinates of the new point.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public MazeStructurePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The X coordinate property.
        /// </summary>
        public int X { get; }

        /// <summary>
        ///     The Y coordinate property.
        /// </summary>
        public int Y { get; }

        #endregion
    }

    /// <summary>
    ///     The maze linked point class contains the X and Y coordinates of the start or end of a maze wall and the point that
    ///     was traversed to reach this point (used as part of linked list for determining shortest path through maze).
    /// </summary>
    public class MazeStructureLinkedPoint : MazeStructurePoint
    {
        #region Properties

        /// <summary>
        ///     The previous point by which the current point is reached.
        /// </summary>
        public MazeStructureLinkedPoint PrevPoint { get; }

        #endregion

        #region Method overrides

        /// <summary>
        ///     Performs equality comparison based on the X/Y position of the current point and the previous point.
        /// </summary>
        /// <param name="obj">The object against which to compare.</param>
        /// <returns>Whether or not this object and the given object are equal.</returns>
        public override bool Equals(object obj)
        {
            // Attempt to coerce to appropriate type and return false if coercion fails
            MazeStructureLinkedPoint point = obj as MazeStructureLinkedPoint;
            if (point == null)
            {
                return false;
            }

            // Return true if the component properties match
            return (X == point.X && Y == point.Y && PrevPoint == point.PrevPoint);
        }

        /// <summary>
        ///     Computes unique hash based on the X/Y points and the hash of the previously visited point.
        /// </summary>
        /// <returns>The linked point hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash*7) + X.GetHashCode();
            hash = (hash*7) + Y.GetHashCode();
            hash = (hash*7) + PrevPoint.GetHashCode();
            return hash;
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Constructor accepts the X and Y coordinates of the new point along with the previous maze point through which this
        ///     point was reached.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="prevPoint">The previous point through which this point was reached.</param>
        public MazeStructureLinkedPoint(int x, int y, MazeStructureLinkedPoint prevPoint = null) : base(x, y)
        {
            PrevPoint = prevPoint;
        }

        /// <summary>
        ///     Constructor accepts a reference to the base maze point along with the previous maze point through which this point
        ///     was reached.
        /// </summary>
        /// <param name="mazePoint">The base maze point with the x and y coordinates.</param>
        /// <param name="prevPoint">The previous point through which this point was reached.</param>
        public MazeStructureLinkedPoint(MazeStructurePoint mazePoint, MazeStructureLinkedPoint prevPoint = null)
            : this(mazePoint.X, mazePoint.Y, prevPoint)
        {
        }

        #endregion
    }
}