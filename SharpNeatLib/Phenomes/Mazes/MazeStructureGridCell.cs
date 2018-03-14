#region

using SharpNeat.Utility;

#endregion

namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     Indicates whether grid cell is on solution path segment and, if so, the orientation of that segment.
    /// </summary>
    public enum PathOrientation
    {
        /// <summary>
        ///     Grid cell is not on solution path.
        /// </summary>
        None,

        /// <summary>
        ///     Grid cell is on solution path and part of a horizontal path segment.
        /// </summary>
        Horizontal,

        /// <summary>
        ///     Grid cell is on solution path and part of a vertical path segment.
        /// </summary>
        Vertical
    }

    /// <summary>
    ///     The maze structure grid cell encapsulates the cartesian coordinates of a maze grid cell, the presence of each of
    ///     its four potential bounding walls, and whether that cell is on the maze solution path.
    /// </summary>
    public class MazeStructureGridCell
    {
        #region Public methods

        /// <summary>
        ///     Compares current grid cell to point.
        /// </summary>
        /// <param name="point">The point of comparison.</param>
        /// <returns>Where the grid cell and the point represent the same location.</returns>
        public bool Equals(Point2DInt point)
        {
            return point.X == X && point.Y == Y;
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructor which accepts cartesian coordinates of grid cell.
        /// </summary>
        /// <param name="x">The x-coordinate of the cell.</param>
        /// <param name="y">The y-coordinate of the cell.</param>
        public MazeStructureGridCell(int x, int y)
        {
            X = x;
            Y = y;
            PathOrientation = PathOrientation.None;
        }

        /// <summary>
        ///     Constructor which accepts cartesian coordinates of grid cell along with two flags indicating whether it is the
        ///     start cell or end cell respectively.
        /// </summary>
        /// <param name="x">The x-coordinate of the cell.</param>
        /// <param name="y">The y-coordinate of the cell.</param>
        /// <param name="isStartCell">Flag indicating whether this cell is the start cell.</param>
        /// <param name="isEndCell">Flag indicating whether this cell is the end cell.</param>
        public MazeStructureGridCell(int x, int y, bool isStartCell, bool isEndCell) : this(x, y)
        {
            IsStartCell = isStartCell;
            IsEndCell = isEndCell;
        }

        #endregion

        #region Instance variables

        /// <summary>
        /// Internal flag which indicates if cell has a south wall (which is the cell horizontal wall).
        /// </summary>
        private bool _southWall;

        /// <summary>
        /// Internal flag which indicates if cell has a east wall (which is the cell vertical wall).
        /// </summary>
        private bool _eastWall;

        #endregion

        #region Properties

        /// <summary>
        ///     The x-coordinate of the grid cell.
        /// </summary>
        public int X { get; }

        /// <summary>
        ///     The y-coordinate of the grid cell.
        /// </summary>
        public int Y { get; }

        /// <summary>
        ///     Flag which indicates if cell has a south wall (which is the cell horizontal wall).
        /// </summary>
        public bool SouthWall
        {
            get
            {
                return _southWall;
            }

            set
            {
                // Mark the south cell boundary as processed
                IsHorizontalBoundaryProcessed = true;

                _southWall = value;
            }
        }

        /// <summary>
        ///     Flag which indicates if cell has a east wall (which is the cell vertical wall).
        /// </summary>
        public bool EastWall
        {
            get
            {
                return _eastWall;
            }

            set
            {
                // Mark the east cell boundary as processed 
                IsVerticalBoundaryProcessed = true;

                _eastWall = value;
            }
        }

        /// <summary>
        ///     Indicates membership on the solution path and the orientation of the path segment passing through the cell..
        /// </summary>
        public PathOrientation PathOrientation { get; set; }

        /// <summary>
        ///     Flag which indicates if cell is on a path juncture (i.e. intersection of two perpendicular path components).
        /// </summary>
        public bool IsJuncture { get; set; }

        /// <summary>
        ///     Flag which indicates if cell is a waypoint (i.e. genome-encoded point demarcating two path segments).
        /// </summary>
        public bool IsWayPoint { get; set; }

        /// <summary>
        ///     Flag which indicates if this cell is the starting cell in the maze.
        /// </summary>
        public bool IsStartCell { get; }

        /// <summary>
        ///     Flag which indicates if this cell is the ending (target) cell in the maze.
        /// </summary>
        public bool IsEndCell { get; }

        public bool IsVerticalBoundaryProcessed { get; set; }

        public bool IsHorizontalBoundaryProcessed { get; set; }

        #endregion
    }
}