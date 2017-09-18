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

        #endregion

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
        ///     Flag which indicates if cell has a north wall.
        /// </summary>
        public bool NorthWall { get; set; }

        /// <summary>
        ///     Flag which indicates if cell has a south wall.
        /// </summary>
        public bool SouthWall { get; set; }

        /// <summary>
        ///     Flag which indicates if cell has a east wall.
        /// </summary>
        public bool EastWall { get; set; }

        /// <summary>
        ///     Flag which indicates if cell has a west wall.
        /// </summary>
        public bool WestWall { get; set; }

        /// <summary>
        ///     Indicates membership on the solution path and the orientation of the path segment passing through the cell..
        /// </summary>
        public PathOrientation PathOrientation { get; set; }

        /// <summary>
        ///     Flag which indicates if cell is on a path juncture (i.e. intersection of two, perpendicular path segments).
        /// </summary>
        public bool IsJuncture { get; set; }

        #endregion
    }
}