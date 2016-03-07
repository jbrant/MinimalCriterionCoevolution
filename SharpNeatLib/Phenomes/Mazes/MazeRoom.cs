#region

using System;

#endregion

namespace SharpNeat.Phenomes.Mazes
{
    public class MazeRoom
    {
        #region Constructors

        public MazeRoom(int x, int y, int width, int height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        #endregion

        #region Public Methods

        public Tuple<MazeRoom, MazeRoom> DivideRoom(int[,] grid, double unscaledWallLocation,
            double unscaledPassageLocation, bool isHorizontalDefaultOrientation)
        {
            // Short circuit if we can't subdivide anymore
            if (_width < MinimumWidth || _height < MinimumHeight)
                return null;

            // Determine orientation
            bool isHorizontal = DetermineWallOrientation(isHorizontalDefaultOrientation) == WallOrientation.Horizontal;

            // Determine starting location of wall
            // TODO: The wall location will be evolved
            int xWallLocation = _x +
                                (isHorizontal ? 0 : Math.Max(0, (int) ((_width*unscaledWallLocation) - MinimumWidth)));
            int yWallLocation = _y +
                                (isHorizontal ? Math.Max(0, (int) ((_height*unscaledWallLocation) - MinimumHeight)) : 0);

            // Determine the location of the passage
            // TODO: The passage location will be evolved
            int xPassageLocation = xWallLocation + (isHorizontal ? (int) (_width*unscaledPassageLocation) : 0);
            int yPassageLocation = yWallLocation + (isHorizontal ? 0 : (int) (_height*unscaledPassageLocation));

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
            MazeRoom newRoom1 = new MazeRoom(_x, _y, newWidth, newHeight);

            // Assign new x/y coordinates for bottom/right part of maze
            int offsetX = isHorizontal ? _x : xWallLocation + 1;
            int offsetY = isHorizontal ? yWallLocation + 1 : _y;

            // Calculate new width/height for bottom/right part of maze
            newWidth = isHorizontal ? _width : _x + _width - xWallLocation - 1;
            newHeight = isHorizontal ? _y + _height - yWallLocation - 1 : _height;

            // Recurse down bottom/right subfield
            MazeRoom newRoom2 = new MazeRoom(offsetX, offsetY, newWidth, newHeight);

            return new Tuple<MazeRoom, MazeRoom>(newRoom1, newRoom2);
        }

        #endregion

        #region Private Methods

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

        private const int MinimumWidth = 2;
        private const int MinimumHeight = 2;

        #endregion

        #region Instance Variables

        private readonly WallOrientation _bisectionOrientation;
        private readonly int _height;
        private readonly int _width;
        private readonly int _x;
        private readonly int _y;

        #endregion
    }
}