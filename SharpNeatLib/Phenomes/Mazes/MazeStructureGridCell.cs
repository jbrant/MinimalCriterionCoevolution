namespace SharpNeat.Phenomes.Mazes
{
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
        ///     Flag which indicates if cell is on the solution path.
        /// </summary>
        public bool IsOnPath { get; set; }

        /// <summary>
        ///     Flag which indicates if cell is on a path juncture (i.e. intersection of two, perpendicular path segments).
        /// </summary>
        public bool IsJuncture { get; set; }

        #endregion
    }
}