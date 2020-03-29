namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     The maze point class contains the X and Y coordinates of a location in the maze.
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

        #region Method overrides

        /// <summary>
        ///     Performs equality comparison based on the X/Y position of the current point.
        /// </summary>
        /// <param name="obj">The point against which to compare.</param>
        /// <returns>Whether or not this point and the given point are equal.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            // If this is a MazeStructureGridCell, compare cartesian coordinates directly
            if (obj.GetType() == typeof(MazeStructureGridCell))
                return X == ((MazeStructureGridCell) obj).X && Y == ((MazeStructureGridCell) obj).Y;

            return obj.GetType() == GetType() && Equals((MazeStructurePoint) obj);
        }

        /// <summary>
        ///     Performs equality comparison based on the X/Y position of the two given points.
        /// </summary>
        /// <param name="point1">The first point to compare.</param>
        /// <param name="point2">The second point to compare.</param>
        /// <returns>Whether or not the two points are equal.</returns>
        public static bool operator ==(MazeStructurePoint point1, MazeStructurePoint point2)
        {
            if (ReferenceEquals(point1, point2)) return true;
            if (ReferenceEquals(point1, null)) return false;
            if (ReferenceEquals(point2, null)) return false;
            return point1.X == point2.X && point1.Y == point2.Y;
        }

        /// <summary>
        ///     Performs inequality comparison based on the X/Y position of the two given points.
        /// </summary>
        /// <param name="point1">The first point to compare.</param>
        /// <param name="point2">The second point to compare.</param>
        /// <returns>Whether the two points are unequal.</returns>
        public static bool operator !=(MazeStructurePoint point1, MazeStructurePoint point2)
        {
            return !(point1 == point2);
        }

        /// <summary>
        ///     Computes unique hash based on the X/Y points.
        /// </summary>
        /// <returns>The point hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        #endregion
    }

    /// <summary>
    ///     The maze linked point class contains the X and Y coordinates of of a point that was traversed to reach this point
    ///     (used as part of linked list for determining shortest path through maze).
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
            var point = obj as MazeStructureLinkedPoint;

            // False if null
            if (ReferenceEquals(point, null)) return false;

            // Return true if the component properties match
            return X == point.X && Y == point.Y && PrevPoint == point.PrevPoint;
        }

        /// <summary>
        ///     Computes unique hash based on the X/Y points and the hash of the previously visited point.
        /// </summary>
        /// <returns>The linked point hash code.</returns>
        public override int GetHashCode()
        {
            var hash = 13;
            hash = hash * 7 + X.GetHashCode();
            hash = hash * 7 + Y.GetHashCode();
            hash = hash * 7 + PrevPoint.GetHashCode();
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