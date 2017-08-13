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

        #region Public Methods

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
        /// <param name="isHorizontalDefaultOrientation">Indicator for whether the dividing line is horizontal or vertical.</param>
        /// <returns>The two subfields that were created as a result of the subfield division.</returns>
        public Tuple<MazeStructureRoom, MazeStructureRoom> DivideRoom(int[,] grid, double unscaledWallLocation,
            double unscaledPassageLocation, bool isHorizontalDefaultOrientation)
        {            
            // Determine orientation
            bool isHorizontal = DetermineWallOrientation(isHorizontalDefaultOrientation) == WallOrientation.Horizontal;

            // Determine starting location of wall
            // TODO: The wall location will be evolved
            int xWallLocation = _x +
                                (isHorizontal ? 0 : Math.Max(0, (int) ((_width - MinimumWidth)*unscaledWallLocation)));
            int yWallLocation = _y +
                                (isHorizontal ? Math.Max(0, (int) ((_height - MinimumHeight)*unscaledWallLocation)) : 0);

            // Determine the location of the passage 
            // (location can be no further out than width or height minus 1 to prevent passage starting at the wall end point)
            // TODO: The passage location will be evolved
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
            WallDirection perpendicularDirection = isHorizontal ? WallDirection.South : WallDirection.East;

            // Notate where all of the wall segments are in the current wall
            for (int curWallCell = 0; curWallCell < wallLength; curWallCell++)
            {
                // If the current cell isn't in the defined passage, place a wall segment there
                if (xWallLocation != xPassageLocation || yWallLocation != yPassageLocation)
                {
                    // Bitwise or with perpendicular direction to get appropriate wall
                    grid[yWallLocation, xWallLocation] |= (int) perpendicularDirection;
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

        #region Constants

        // The minimum width and height of the maze
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