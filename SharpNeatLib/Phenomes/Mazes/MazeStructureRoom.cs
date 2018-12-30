#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpNeat.Utility;

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

        #region Enums        

        /// <summary>
        ///     Identifies the cardinal direction of the maze room opening.
        /// </summary>
        private enum RoomOpeningDirection
        {
            North,
            South,
            West,
            East
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Converts the relative wall position within the maze room space to cartesian coordinates, indicating the starting
        ///     position of the wall.
        /// </summary>
        /// <param name="unscaledWallLocation">The encoded position (between 0 and 1) of the wall within the maze room.</param>
        /// <param name="isHorizontal">Flag indicating whether the wall is horizontal or not (i.e. vertical).</param>
        /// <returns>The cartesian coordinates of the wall start location.</returns>
        private Point2DInt GetWallLocation(double unscaledWallLocation, bool isHorizontal)
        {
            return new Point2DInt(
                _x + (isHorizontal ? 0 : Math.Max(0, (int) ((_width - MinimumWidth + 1) * unscaledWallLocation))),
                _y + (isHorizontal ? Math.Max(0, (int) ((_height - MinimumHeight + 1) * unscaledWallLocation)) : 0));
        }

        /// <summary>
        ///     Converts the relative passage position to cartesian coordinates, indicating the position of the passage within the
        ///     given wall.
        /// </summary>
        /// <param name="wallLocation">The cartesian coordinates of the wall start location.</param>
        /// <param name="unscaledPassageLocation">The encoded position (between 0 and 1) of the passage within the wall.</param>
        /// <param name="isHorizontal">Flag indicating whether the wall is horizontal or not (i.e. vertical).</param>
        /// <returns>The cartesian coordinates of the passage within the wall.</returns>
        private Point2DInt GetPassageLocation(Point2DInt wallLocation, double unscaledPassageLocation,
            bool isHorizontal)
        {
            return new Point2DInt(
                wallLocation.X + (isHorizontal ? Math.Min((_width - 1), (int) (_width * unscaledPassageLocation)) : 0),
                wallLocation.Y + (isHorizontal ? 0 : Math.Min(_height - 1, (int) (_height * unscaledPassageLocation))));
        }

        /// <summary>
        ///     Computes the distance between the given X/Y reference locations and the path segment due north of that location. If
        ///     the path is not intersected, integer max value is returned.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="verticalReferencePosition">The Y-component of the starting position within the maze.</param>
        /// <param name="horizontalReferencePosition">The X-component of the starting position within the maze.</param>
        /// <returns>The distance between the reference position and the intersecting path segment.</returns>
        private int ComputeNorthPathDistance(MazeStructureGridCell[,] grid, int verticalReferencePosition,
            int horizontalReferencePosition)
        {
            for (var curPosition = verticalReferencePosition - 1; curPosition >= 0; curPosition--)
            {
                if (grid[curPosition, horizontalReferencePosition].PathOrientation != PathOrientation.None)
                {
                    return verticalReferencePosition - curPosition;
                }
            }

            return int.MaxValue;
        }

        /// <summary>
        ///     Computes the distance between the given X/Y reference locations and the path segment due south of that location. If
        ///     the path is not intersected, integer max value is returned.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="verticalReferencePosition">The Y-component of the starting position within the maze.</param>
        /// <param name="horizontalReferencePosition">The X-component of the starting position within the maze.</param>
        /// <param name="mazeHeight">
        ///     The height of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <returns>The distance between the reference position and the intersecting path segment.</returns>
        private int ComputeSouthPathDistance(MazeStructureGridCell[,] grid, int verticalReferencePosition,
            int horizontalReferencePosition, int mazeHeight)
        {
            for (var curPosition = verticalReferencePosition + _height; mazeHeight > curPosition; curPosition++)
            {
                if (grid[curPosition, horizontalReferencePosition].PathOrientation != PathOrientation.None)
                {
                    return curPosition - (verticalReferencePosition + _height - 1);
                }
            }

            return int.MaxValue;
        }

        /// <summary>
        ///     Computes the distance between the given X/Y reference locations and the path segment due west of that location. If
        ///     the path is not intersected, integer max value is returned.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="verticalReferencePosition">The Y-component of the starting position within the maze.</param>
        /// <param name="horizontalReferencePosition">The X-component of the starting position within the maze.</param>
        /// <returns>The distance between the reference position and the intersecting path segment.</returns>
        private int ComputeWestPathDistance(MazeStructureGridCell[,] grid, int verticalReferencePosition,
            int horizontalReferencePosition)
        {
            for (var curPosition = horizontalReferencePosition - 1; curPosition >= 0; curPosition--)
            {
                if (grid[verticalReferencePosition, curPosition].PathOrientation != PathOrientation.None)
                {
                    return horizontalReferencePosition - curPosition;
                }
            }

            return int.MaxValue;
        }

        /// <summary>
        ///     Computes the distance between the given X/Y reference locations and the path segment due east of that location. If
        ///     the path is not intersected, integer max value is returned.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="verticalReferencePosition">The Y-component of the starting position within the maze.</param>
        /// <param name="horizontalReferencePosition">The X-component of the starting position within the maze.</param>
        /// <param name="mazeWidth">
        ///     The width of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <returns>The distance between the reference position and the intersecting path segment.</returns>
        private int ComputeEastPathDistance(MazeStructureGridCell[,] grid, int verticalReferencePosition,
            int horizontalReferencePosition, int mazeWidth)
        {
            for (var curPosition = horizontalReferencePosition + _width; mazeWidth > curPosition; curPosition++)
            {
                if (grid[verticalReferencePosition, curPosition].PathOrientation != PathOrientation.None)
                {
                    return curPosition - (horizontalReferencePosition + _width - 1);
                }
            }

            return int.MaxValue;
        }

        /// <summary>
        ///     Computes the distance between the wall/passage location and the nearest path segment in the given cardinal
        ///     direction.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="wallLocation">The cartesian coordinates of the wall start location.</param>
        /// <param name="passageLocation">The cartesian coordinates of the passage location.</param>
        /// <param name="openingDirection">The cardinal direction of the candidate room opening.</param>
        /// <param name="mazeHeight">
        ///     The height of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <param name="mazeWidth">
        ///     The width of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <param name="isHorizontal">Flag indicating whether the wall is horizontal or not (i.e. vertical).</param>
        /// <returns>
        ///     The distance between the wall/passage location and the nearest intersecting path segment in the given cardinal
        ///     direction.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Exception thrown for invalid cardinal direction.</exception>
        private int ComputePathDistance(MazeStructureGridCell[,] grid, Point2DInt wallLocation,
            Point2DInt passageLocation, RoomOpeningDirection openingDirection, int mazeHeight, int mazeWidth,
            bool isHorizontal)
        {
            int pathDistance;

            switch (openingDirection)
            {
                case RoomOpeningDirection.North:
                    pathDistance = isHorizontal
                        ? ComputeNorthPathDistance(grid, _y, passageLocation.X)
                        : ComputeNorthPathDistance(grid, wallLocation.Y, wallLocation.X);
                    break;
                case RoomOpeningDirection.South:
                    pathDistance = isHorizontal
                        ? ComputeSouthPathDistance(grid, _y, passageLocation.X, mazeHeight)
                        : ComputeSouthPathDistance(grid, wallLocation.Y, wallLocation.X, mazeHeight);
                    break;
                case RoomOpeningDirection.West:
                    pathDistance = isHorizontal
                        ? ComputeWestPathDistance(grid, wallLocation.Y, wallLocation.X)
                        : ComputeWestPathDistance(grid, passageLocation.Y, _x);
                    break;
                case RoomOpeningDirection.East:
                    pathDistance = isHorizontal
                        ? ComputeEastPathDistance(grid, wallLocation.Y, wallLocation.X, mazeWidth)
                        : ComputeEastPathDistance(grid, passageLocation.Y, _x, mazeWidth);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(openingDirection), openingDirection, null);
            }

            return pathDistance;
        }

        /// <summary>
        ///     Computes the distances between the wall/passage location and the nearest path segment in all of the given cardinal
        ///     directions.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="wallLocation">The cartesian coordinates of the wall start location.</param>
        /// <param name="passageLocation">The cartesian coordinates of the passage location.</param>
        /// <param name="isHorizontal">Flag indicating whether the wall is horizontal or not (i.e. vertical).</param>
        /// <param name="mazeHeight">
        ///     The height of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <param name="mazeWidth">
        ///     The width of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <returns>
        ///     The distance between the wall/passage location and the nearest intersecting path segments in each cardinal
        ///     direction.
        /// </returns>
        private IDictionary<RoomOpeningDirection, int> ComputePathDistances(MazeStructureGridCell[,] grid,
            Point2DInt wallLocation, Point2DInt passageLocation, bool isHorizontal, int mazeHeight, int mazeWidth)
        {
            IDictionary<RoomOpeningDirection, int> directionalPathDistances =
                new Dictionary<RoomOpeningDirection, int>(Enum.GetNames(typeof(RoomOpeningDirection)).Length);

            foreach (var direction in Enum.GetValues(typeof(RoomOpeningDirection)).Cast<RoomOpeningDirection>())
            {
                directionalPathDistances.Add(direction,
                    ComputePathDistance(grid, wallLocation, passageLocation, direction, mazeHeight, mazeWidth,
                        isHorizontal));
            }

            return directionalPathDistances;
        }

        /// <summary>
        ///     Determines the cardinal direction of the room opening that would be nearest to a path segment in that direction. No
        ///     bisecting wall/passage alignment is taken into account - this simply searches from the corner of the maze room.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="mazeHeight">
        ///     The height of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <param name="mazeWidth">
        ///     The width of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <returns>The direction of the path opening </returns>
        private RoomOpeningDirection GetRoomOpeningDirection(MazeStructureGridCell[,] grid, int mazeHeight,
            int mazeWidth)
        {
            var directionalPathDistances = new Dictionary<RoomOpeningDirection, int>
            {
                {RoomOpeningDirection.North, ComputeNorthPathDistance(grid, _y, _x)},
                {RoomOpeningDirection.South, ComputeSouthPathDistance(grid, _y, _x, mazeHeight)},
                {RoomOpeningDirection.West, ComputeWestPathDistance(grid, _y, _x)},
                {RoomOpeningDirection.East, ComputeEastPathDistance(grid, _y, _x, mazeWidth)}
            };

            return directionalPathDistances.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
        }

        /// <summary>
        ///     Determines the cardinal direction of the room opening that would be nearest to a path segment in that direction,
        ///     and aligning with the position of the first bisecting wall and passage.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="wallLocation">The cartesian coordinates of the wall start location.</param>
        /// <param name="passageLocation">The cartesian coordinates of the passage location.</param>
        /// <param name="isHorizontal">Flag indicating whether the wall is horizontal or not (i.e. vertical).</param>
        /// <param name="mazeHeight">
        ///     The height of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <param name="mazeWidth">
        ///     The width of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <returns>The cardinal direction of the room opening nearest to a path segment.</returns>
        private RoomOpeningDirection GetRoomOpeningDirection(MazeStructureGridCell[,] grid, Point2DInt wallLocation,
            Point2DInt passageLocation, bool isHorizontal, int mazeHeight, int mazeWidth)
        {
            // Get the path distance for each cardinal direction
            var directionalPathDistances =
                ComputePathDistances(grid, wallLocation, passageLocation, isHorizontal, mazeHeight, mazeWidth);

            // Return the direction of shortest distance
            return directionalPathDistances.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
        }

        /// <summary>
        ///     Marks the room entry (i.e. removes a boundary) at the location determined to be nearest to a path segment and
        ///     aligns with the first bisecting wall.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="unscaledWallLocation">The encoded position (between 0 and 1) of the wall within the maze room.</param>
        /// <param name="unscaledPassageLocation">The encoded position (between 0 and 1) of the passage within the wall.</param>
        /// <param name="isHorizontal">Flag indicating whether the wall is horizontal or not (i.e. vertical).</param>
        /// <param name="mazeHeight">
        ///     The height of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <param name="mazeWidth">
        ///     The width of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Exception thrown for invalid cardinal direction.</exception>
        private void MarkRoomEntry(MazeStructureGridCell[,] grid, double unscaledWallLocation,
            double unscaledPassageLocation, bool isHorizontal, int mazeHeight, int mazeWidth)
        {
            // Determine starting location of wall and passage within wall
            var wallLocation = GetWallLocation(unscaledWallLocation, isHorizontal);
            var passageLocation = GetPassageLocation(wallLocation, unscaledPassageLocation, isHorizontal);

            // Get the direction with shortest distance to the path
            var roomOpeningDirection =
                GetRoomOpeningDirection(grid, wallLocation, passageLocation, isHorizontal, mazeHeight, mazeWidth);

            // Clear a room opening based on the computed direction and the orientation of the first dividing wall
            switch (roomOpeningDirection)
            {
                case RoomOpeningDirection.North:
                    if (isHorizontal)
                        grid[_y - 1, passageLocation.X].SouthWall = false;
                    else
                        grid[wallLocation.Y - 1, wallLocation.X].SouthWall = false;
                    break;
                case RoomOpeningDirection.South:
                    if (isHorizontal)
                        grid[_y + _height - 1, passageLocation.X].SouthWall = false;
                    else
                        grid[wallLocation.Y + _height - 1, wallLocation.X].SouthWall = false;
                    break;
                case RoomOpeningDirection.West:
                    if (isHorizontal)
                        grid[wallLocation.Y, _x - 1].EastWall = false;
                    else
                        grid[passageLocation.Y, _x - 1].EastWall = false;
                    break;
                case RoomOpeningDirection.East:
                    if (isHorizontal)
                        grid[wallLocation.Y, _x + _width - 1].EastWall = false;
                    else
                        grid[passageLocation.Y, _x + _width - 1].EastWall = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Marks the room entry (i.e. removes a boundary) at the location determined to be nearest to a path segment. No
        ///     bisecting wall is taken into account when determining such a position, search start from a corner of the maze room.
        ///     As such, this method is used for rooms that are not large enough to support any interior walls.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="mazeHeight">
        ///     The height of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <param name="mazeWidth">
        ///     The width of the containing maze (used as an upper bounding when searching for path
        ///     intersection).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Exception thrown for invalid cardinal direction.</exception>
        private void MarkRoomEntry(MazeStructureGridCell[,] grid, int mazeHeight, int mazeWidth)
        {
            // Get the direction with shortest distance to the path
            var roomOpeningDirection = GetRoomOpeningDirection(grid, mazeHeight, mazeWidth);

            // Clear a room opening at the room edge with shortest distance to the path 
            switch (roomOpeningDirection)
            {
                case RoomOpeningDirection.North:
                    grid[_y - 1, _x].SouthWall = false;
                    break;
                case RoomOpeningDirection.South:
                    grid[_y + _height - 1, _x].SouthWall = false;
                    break;
                case RoomOpeningDirection.West:
                    grid[_y, _x - 1].EastWall = false;
                    break;
                case RoomOpeningDirection.East:
                    grid[_y, _x + _width - 1].EastWall = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Marks outer boundaries of maze room.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        public void TraceRoomEnclosure(MazeStructureGridCell[,] grid)
        {
            // Mark horizontal north and south boundaries
            for (var x = _x; x < _x + _width; x++)
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
            for (var y = _y; y < _y + _height; y++)
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
                //grid[_y, _x == 0 ? _x + _width - 1 : _x - 1].EastWall = false;
                MarkRoomEntry(grid, grid.GetLength(1), grid.GetLength(0));
            }
        }

        /// <summary>
        ///     Marks outer boundaries of maze room and inserts room entry in the corner nearest to a path segment.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="mazeHeight">The height of the containing maze.</param>
        /// <param name="mazeWidth">The width of the containing maze.</param>
        public void MarkRoomBoundaries(MazeStructureGridCell[,] grid, int mazeHeight, int mazeWidth)
        {
            // Mark the fully enclosed sub-maze boundaries
            TraceRoomEnclosure(grid);

            // Mark the entry (opening) into the maze room based on corner of room closest to path
            MarkRoomEntry(grid, mazeHeight, mazeWidth);
        }

        /// <summary>
        ///     Marks outer boundaries of maze room and inserts room entry perpendicular to first internal wall.
        /// </summary>
        /// <param name="grid">The matrix of maze cells.</param>
        /// <param name="unscaledWallLocation">The relative location of the first internal dividing wall.</param>
        /// <param name="unscaledPassageLocation">
        ///     The relative location of the passage (opening) with the first internal dividing
        ///     wall.
        /// </param>
        /// <param name="isHorizontal">Indicator of whether the first internal wall is horizontal or vertical.</param>
        /// <param name="mazeHeight">The height of the containing maze.</param>
        /// <param name="mazeWidth">The width of the containing maze.</param>
        public void MarkRoomBoundaries(MazeStructureGridCell[,] grid, double unscaledWallLocation,
            double unscaledPassageLocation,
            bool isHorizontal, int mazeHeight, int mazeWidth)
        {
            // Mark the fully enclosed sub-maze boundaries
            TraceRoomEnclosure(grid);

            // Mark the entry (opening) into the maze room based on first bisecting wall position
            MarkRoomEntry(grid, unscaledWallLocation, unscaledPassageLocation, isHorizontal, mazeHeight, mazeWidth);
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
            // Ensure that wall location does not reach 1 
            // (otherwise, this may cause room to not split and result in an endless loop)
            unscaledWallLocation = Math.Min(999999e-6, unscaledWallLocation);

            // Determine wall and passage starting location
            // (passage location can be no further out than width or height minus 1 to prevent passage starting at the wall end point)
            var wallLocation = GetWallLocation(unscaledWallLocation, isHorizontal);
            var passageLocation = GetPassageLocation(wallLocation, unscaledPassageLocation, isHorizontal);

            // Determine wall directional components
            var xDirection = isHorizontal ? 1 : 0;
            var yDirection = isHorizontal ? 0 : 1;

            // Determine length of wall (equivalent to the length of the subfield in the given direction)
            var wallLength = isHorizontal ? _width : _height;

            // Notate where all of the wall segments are in the current wall
            for (var curWallCell = 0; curWallCell < wallLength; curWallCell++)
            {
                // If the current cell isn't in the defined passage, place a wall segment there
                if (wallLocation.X != passageLocation.X || wallLocation.Y != passageLocation.Y)
                {
                    // Add wall segment to cell based on perpendicular direction
                    if (isHorizontal)
                    {
                        grid[wallLocation.Y, wallLocation.X].SouthWall = true;
                    }
                    else
                    {
                        grid[wallLocation.Y, wallLocation.X].EastWall = true;
                    }
                }

                // Increment the wall segment location by the appropriate directional components
                wallLocation.X += xDirection;
                wallLocation.Y += yDirection;
            }

            // Calculate new width/height for top/left part of maze
            var newWidth = isHorizontal ? _width : wallLocation.X - _x + 1;
            var newHeight = isHorizontal ? wallLocation.Y - _y + 1 : _height;

            // Recurse down top/left subfield
            var newRoom1 = (newWidth >= MinimumWidth && newHeight >= MinimumHeight)
                ? new MazeStructureRoom(_x, _y, newWidth, newHeight)
                : null;

            // Assign new x/y coordinates for bottom/right part of maze
            var offsetX = isHorizontal ? _x : wallLocation.X + 1;
            var offsetY = isHorizontal ? wallLocation.Y + 1 : _y;

            // Calculate new width/height for bottom/right part of maze
            newWidth = isHorizontal ? _width : _x + _width - wallLocation.X - 1;
            newHeight = isHorizontal ? _y + _height - wallLocation.Y - 1 : _height;

            // Recurse down bottom/right subfield
            var newRoom2 = (newWidth >= MinimumWidth && newHeight >= MinimumHeight)
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