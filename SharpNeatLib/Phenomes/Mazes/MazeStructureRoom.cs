#region

using System;

#endregion

namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     The maze room encapsulates a single room or "subfield" within the maze.  This room can then be further subdivided
    ///     until the minimum allowed resolution is reached.
    /// </summary>
    public class MazeStructureRoom
    {
        #region Constructors

        /// <summary>
        ///     Constructor which takes the origin X and Y coordinates as well as the width and height dimensions of the maze or
        ///     subfield.
        /// </summary>
        /// <param name="x">The horizontal component of the starting coordinate.</param>
        /// <param name="y">The vertical component of the starting coordinate.</param>
        /// <param name="width">The width of the maze/subfield.</param>
        /// <param name="height">The height of the maze/subfield.</param>
        public MazeStructureRoom(int x, int y, int width, int height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Determines whether the dividing wall should be horizontal or vertical based on the comparitive width and height of
        ///     the subfield under consideration.
        /// </summary>
        /// <param name="isHorizontalDefaultOrientation">
        ///     Each gene in the genome carries an indicator of preferred orientation.  If
        ///     the dimensions are equal (i.e. the subfield is a square), this randomly selected orientation will be used.
        /// </param>
        /// <returns>The orientation of the dividing wall.</returns>
        private WallOrientation DetermineWallOrientation(bool isHorizontalDefaultOrientation)
        {
            // Adopt random orientation seed set on genome
            WallOrientation bisectionOrientation = isHorizontalDefaultOrientation
                ? WallOrientation.Horizontal
                : WallOrientation.Vertical;

            // Determine the orientation of the separator wall within the room
            if (_width < _height)
            {
                bisectionOrientation = WallOrientation.Horizontal;
            }
            else if (_height < _width)
            {
                bisectionOrientation = WallOrientation.Vertical;
            }

            return bisectionOrientation;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Marks outer boundaries of maze room.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        public void MarkRoomBoundaries(MazeStructureGridCell[,] grid)
        {
            // Mark horizontal north and south boundaries
            for (int x = _x; x < _x + _width; x++)
            {
                // Mark north horizontal boundary
                if (_y > 0 && grid[_y - 1, x].IsHorizontalBoundaryProcessed == false)
                {
                    grid[_y - 1, x].SouthWall = true;
                }

                // Mark south horizontal boundary
                if (grid[_y + _height - 1, x].IsHorizontalBoundaryProcessed == false)
                {
                    grid[_y + _height - 1, x].SouthWall = true;
                }
            }

            // Mark vertical east and west boundaries
            for (int y = _y; y < _y + _height; y++)
            {
                // Mark west vertical boundary
                if (_x > 0 && grid[y, _x - 1].IsVerticalBoundaryProcessed == false)
                {
                    grid[y, _x - 1].EastWall = true;
                }

                // Mark east vertical boundary
                if (grid[y, _x + _width - 1].IsVerticalBoundaryProcessed == false)
                {
                    grid[y, _x + _width - 1].EastWall = true;
                }
            }

            // If the height is such that the maze can't support any internal walls, 
            // open up the side that is not against the maze boundary
            if (_height < MinimumHeight || _width < MinimumWidth)
            {
                grid[_y, _x == 0 ? _x + _width - 1 : _x - 1].EastWall = false;
            }
            // Otherwise, if the width is such that the maze can't support any internal walls, 
            // open up the side that is not against the maze boundary
            /*else if (_width < MinimumWidth)
            {
                grid[_y == 0 ? _y + _height - 1 : _y - 1, _x].SouthWall = false;
            }*/
        }

        /// <summary>
        ///     Marks outer boundaries of maze room and inserts passage perpendicular to first internal wall.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="unscaledWallLocation">The relative location of the first internal dividing wall.</param>
        /// <param name="isHorizontal">Indicator of whether the first internal wall is horizontal or vertical.</param>
        public void MarkRoomBoundaries(MazeStructureGridCell[,] grid, double unscaledWallLocation,
            bool isHorizontal)
        {
            // Mark the fully enclosed sub-maze boundaries
            MarkRoomBoundaries(grid);

            // Determine starting location of wall
            int xWallLocation = _x +
                                (isHorizontal
                                    ? 0
                                    : Math.Max(0, (int) ((_width - MinimumWidth + 1)*unscaledWallLocation)));
            int yWallLocation = _y +
                                (isHorizontal
                                    ? Math.Max(0, (int) ((_height - MinimumHeight + 1)*unscaledWallLocation))
                                    : 0);

            // Determine perpendicular direction
            WallDirection perpendicularDirection = isHorizontal ? WallDirection.East : WallDirection.South;

            // Insert horizontal gap either above or below the sub-maze
            if (perpendicularDirection == WallDirection.South)
            {
                grid[yWallLocation == 0 ? _height + yWallLocation - 1 : yWallLocation - 1, xWallLocation].SouthWall =
                    false;
            }
            // Insert vertical gap either to the left or right of the sub-maze
            else
            {
                grid[yWallLocation, xWallLocation == 0 ? _width + xWallLocation - 1 : xWallLocation - 1].EastWall =
                    false;
            }
        }

        /// <summary>
        ///     Divides the maze or subfield (room) into two component subfields based on the given wall location and passage
        ///     location (which are evolved out of context and must be scaled to the dimensions of the containing subfield).
        /// </summary>
        /// <param name="grid">
        ///     The matrix of the current genotypic maze state and the structural components (horizontal/vertical
        ///     walls, both, or none) that are at each possible position.
        /// </param>
        /// <param name="unscaledWallLocation">
        ///     The location of the new dividing wall.  This is a real number between 0 and 1 which
        ///     must be normalized to the appropriate range for the containing subfield.
        /// </param>
        /// <param name="unscaledPassageLocation">
        ///     The location of the new dividing wall passage.  This is a real number between 0
        ///     and 1 which must be normalized to the appropriate range for the containing subfield.
        /// </param>
        /// <param name="isHorizontal">Indicator for whether the dividing line is horizontal or vertical.</param>
        /// <returns>The two subfields that were created as a result of the subfield division.</returns>
        public Tuple<MazeStructureRoom, MazeStructureRoom> DivideRoom(MazeStructureGridCell[,] grid,
            double unscaledWallLocation,
            double unscaledPassageLocation, bool isHorizontal)
        {
            // Determine starting location of wall
            int xWallLocation = _x +
                                (isHorizontal
                                    ? 0
                                    : Math.Max(0, (int) ((_width - MinimumWidth + 1)*unscaledWallLocation)));
            int yWallLocation = _y +
                                (isHorizontal
                                    ? Math.Max(0, (int) ((_height - MinimumHeight + 1)*unscaledWallLocation))
                                    : 0);

            // Determine the location of the passage 
            // (location can be no further out than width or height minus 1 to prevent passage starting at the wall end point)
            int xPassageLocation = xWallLocation +
                                   (isHorizontal ? Math.Min((_width - 1), (int) (_width*unscaledPassageLocation)) : 0);
            int yPassageLocation = yWallLocation +
                                   (isHorizontal ? 0 : Math.Min(_height - 1, (int) (_height*unscaledPassageLocation)));

            // Determine wall directional components
            int xDirection = isHorizontal ? 1 : 0;
            int yDirection = isHorizontal ? 0 : 1;

            // Determine length of wall (equivalent to the length of the subfield in the given direction)
            int wallLength = isHorizontal ? _width : _height;

            // Determine perpendicular direction
            WallDirection wallDirection = isHorizontal ? WallDirection.South : WallDirection.East;

            // Notate where all of the wall segments are in the current wall
            for (int curWallCell = 0; curWallCell < wallLength; curWallCell++)
            {
                // If the current cell isn't in the defined passage, place a wall segment there
                if (xWallLocation != xPassageLocation || yWallLocation != yPassageLocation)
                {
                    // Add wall segment to cell based on perpendicular direction
                    if (wallDirection == WallDirection.East)
                    {
                        grid[yWallLocation, xWallLocation].EastWall = true;
                    }
                    else
                    {
                        grid[yWallLocation, xWallLocation].SouthWall = true;
                    }
                }

                // Increment the wall segment location by the appropriate directional components
                xWallLocation += xDirection;
                yWallLocation += yDirection;
            }

            // Calculate new width/height for top/left part of maze
            int newWidth = isHorizontal ? _width : xWallLocation - _x + 1;
            int newHeight = isHorizontal ? yWallLocation - _y + 1 : _height;

            // Recurse down top/left subfield
            MazeStructureRoom newRoom1 = (newWidth >= MinimumWidth && newHeight >= MinimumHeight)
                ? new MazeStructureRoom(_x, _y, newWidth, newHeight)
                : null;

            // Assign new x/y coordinates for bottom/right part of maze
            int offsetX = isHorizontal ? _x : xWallLocation + 1;
            int offsetY = isHorizontal ? yWallLocation + 1 : _y;

            // Calculate new width/height for bottom/right part of maze
            newWidth = isHorizontal ? _width : _x + _width - xWallLocation - 1;
            newHeight = isHorizontal ? _y + _height - yWallLocation - 1 : _height;

            // Recurse down bottom/right subfield
            MazeStructureRoom newRoom2 = (newWidth >= MinimumWidth && newHeight >= MinimumHeight)
                ? new MazeStructureRoom(offsetX, offsetY, newWidth, newHeight)
                : null;

            return new Tuple<MazeStructureRoom, MazeStructureRoom>(newRoom1, newRoom2);
        }

        /// <summary>
        ///     Reports whether maze is of a size sufficient for supporting internal walls. This means that both the height and
        ///     width of the maze are greater than one unit.
        /// </summary>
        /// <returns>Boolean value indicating whether the maze is of sufficient size to support internal walls.</returns>
        public bool AreInternalWallsSupported()
        {
            return _height > 1 && _width > 1;
        }

        /// <summary>
        ///     Determines whether the given cell lies within the boundaries of the maze room.
        /// </summary>
        /// <param name="cell">The cell whose position within or outside of the maze room is to be determined.</param>
        /// <returns>Flag indicating whether the cell is inside the boundaries of the room or not.</returns>
        public bool IsCellInRoom(MazeStructureGridCell cell)
        {
            return cell.X >= _x && cell.X < _x + _width && cell.Y >= _y && cell.Y < _y + _height;
        }

        #endregion

        #region Constants

        // The minimum width and height of the maze (if supporting internal walls)
        private const int MinimumWidth = 2;
        private const int MinimumHeight = 2;

        #endregion

        #region Instance Variables

        // The dimensions of the maze or subfield
        private readonly int _height;
        private readonly int _width;

        //  The starting X and Y position of the maze of subfield
        private readonly int _x;
        private readonly int _y;

        #endregion
    }
}