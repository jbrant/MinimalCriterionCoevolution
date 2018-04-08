// This is a test of setting the file header.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeat.Utility
{
    /// <summary>
    ///     Contains utility methods for maze genomes.
    /// </summary>
    public static class MazeUtils
    {
        #region Internal helper methods

        /// <summary>
        ///     Determines the orientation (i.e. horizontal or vertical) of the given waypoint.
        /// </summary>
        /// <param name="prevWaypoint">The waypoint preceding the current waypoint on the trajectory.</param>
        /// <param name="curWaypoint">The waypoint for which the orientation is being determined.</param>
        /// <param name="nextWaypoint">The waypoint following the current waypoint on the trajectory.</param>
        /// <param name="defaultIntersectionOrientation">
        ///     The default orientation for the current waypoint (this can be overridden,
        ///     but ensures consistency in the event that either orientation can be assumed).
        /// </param>
        /// <param name="grid">The two-dimensional, n x n grid of maze cells.</param>
        /// <returns>The orientation (i.e. horizontal or vertical) of the given waypoint.</returns>
        private static IntersectionOrientation DetermineWaypointIntersectionOrientation(Point2DInt prevWaypoint,
            Point2DInt curWaypoint, Point2DInt nextWaypoint, IntersectionOrientation defaultIntersectionOrientation,
            MazeStructureGridCell[,] grid)
        {
            // Start with default intersection orientation specified on the path gene
            IntersectionOrientation intersectionOrientation = defaultIntersectionOrientation;

            // Handle vertical intersection exceptions         
            if (defaultIntersectionOrientation == IntersectionOrientation.Vertical)
            {
                // If current and previous waypoint are on the same row, vertical intersection is not possible
                if (curWaypoint.Y == prevWaypoint.Y)
                {
                    intersectionOrientation = IntersectionOrientation.Horizontal;
                }
                // If current waypoint is directly below and to the left of previous waypoint and previous waypoint
                // has a trajectory cell to its left, and next waypoint is on the next row, and current way point is
                // not on the left border, vertical intersection would cause overlapping trajectories
                else if (curWaypoint.Y == prevWaypoint.Y + 1 && curWaypoint.X < prevWaypoint.X &&
                         grid[prevWaypoint.Y, prevWaypoint.X - 1].PathDirection != PathDirection.None &&
                         curWaypoint.X > 0)
                {
                    intersectionOrientation = IntersectionOrientation.Horizontal;
                }
                // If current waypoint is below and to the right of previous waypoint and previous waypoint
                // has a trajectory cell to its right, vertical intersection would cause overlapping trajectories
                else if (curWaypoint.Y > prevWaypoint.Y && curWaypoint.X > prevWaypoint.X &&
                         grid[prevWaypoint.Y, prevWaypoint.X + 1].PathDirection != PathDirection.None)
                {
                    intersectionOrientation = IntersectionOrientation.Horizontal;
                }
            }
            // Handle horizontal intersection exceptions
            else
            {
                // If current waypoint is below and to the left of previous waypoint and next waypoint is on
                // the same row, horizontal intersection would cause overlapping trajectories
                if (curWaypoint.Y > prevWaypoint.Y && curWaypoint.X < prevWaypoint.X && curWaypoint.Y == nextWaypoint.Y &&
                    curWaypoint.X < nextWaypoint.X)
                {
                    intersectionOrientation = IntersectionOrientation.Vertical;
                }
                // If current waypoint is below and to the right of the previous waypoint, and next waypoint
                // is on the same row as the current waypoint but to the left of it, and the current waypoint 
                // is not the end point, then intersection must be vertical because the maze boundary prevents 
                // routing to the right of the current waypoint and intersecting horizontally
                if (curWaypoint.Y > prevWaypoint.Y && curWaypoint.Y == nextWaypoint.Y &&
                    curWaypoint.X >= prevWaypoint.X && curWaypoint.X > nextWaypoint.X &&
                    curWaypoint.Equals(nextWaypoint) == false)
                {
                    intersectionOrientation = IntersectionOrientation.Vertical;
                }
                // If current waypoint is directly below or below and to the left of the previous waypoint, and
                // next waypoint is on the same row and directyl below or to the right of the previous waypoint, 
                // and current waypoint is against the left maze boundary, then intersection must be vertical
                // because the maze boundary prevents routing to the left of the current waypoint and 
                // intersecting horizontally
                else if (curWaypoint.Y > prevWaypoint.Y && curWaypoint.Y == nextWaypoint.Y &&
                         curWaypoint.X <= prevWaypoint.X && nextWaypoint.X >= prevWaypoint.X && curWaypoint.X == 0)
                {
                    intersectionOrientation = IntersectionOrientation.Vertical;
                }
                // If current waypoint is below and to the right of the previous waypoint, and next waypoint is to
                // the left of the current waypoint and is on the last row, and the current waypoint is on the next
                // to the last row, then intersection must be vertical so that trajectory can back-track to intersect 
                // next waypoint without overlapping
                else if (curWaypoint.Y > prevWaypoint.Y && curWaypoint.X > prevWaypoint.X &&
                         curWaypoint.X > nextWaypoint.X && nextWaypoint.Y == grid.GetLength(0) - 1 &&
                         curWaypoint.Y == grid.GetLength(0) - 2)
                {
                    intersectionOrientation = IntersectionOrientation.Vertical;
                }
            }

            return intersectionOrientation;
        }

        /// <summary>
        ///     Marks the orientation and juncture location for each cell on a given solution path segment.
        /// </summary>
        /// <param name="grid">The two-dimensional, n x n grid of maze cells.</param>
        /// <param name="startPoint">The starting point on the solution path segment.</param>
        /// <param name="endPoint">The ending point on the solution path segment.</param>
        /// <param name="orientation">
        ///     The orientation (i.e. horizontal or vertical) of the solution path segment coming into the
        ///     ending point.
        /// </param>
        /// <param name="maxXPosition">The eastern-most (right-most) waypoint X-position.</param>
        /// ///
        /// <param name="maxYPosition">The southern-most (lowest) waypoint Y-position.</param>
        private static void MarkSolutionPathSegment(MazeStructureGridCell[,] grid, Point2DInt startPoint,
            Point2DInt endPoint, IntersectionOrientation orientation, int maxXPosition, int maxYPosition)
        {
            // Copy the start coordinate to track edge case trajectory adjustments
            Point2DInt curPoint = new Point2DInt(startPoint.X, startPoint.Y);

            if (IntersectionOrientation.Horizontal == orientation)
            {
                // Handle the case where the start waypoint has a vertical orientation and would otherwise overlap
                if ((endPoint.Y < curPoint.Y &&
                     grid[curPoint.Y - 1, curPoint.X].PathDirection != PathDirection.None) ||
                    (endPoint.Y > curPoint.Y &&
                     grid[curPoint.Y + 1, curPoint.X].PathDirection != PathDirection.None))
                {
                    // Set horizontal orientation on origin and one over to the right
                    grid[curPoint.Y, curPoint.X].PathDirection =
                        grid[curPoint.Y, curPoint.X + 1].PathDirection = PathDirection.East;

                    // Set juncture flag on new point
                    grid[curPoint.Y, curPoint.X + 1].IsJuncture = true;

                    // Move the starting X-coordinate 1 to the right
                    curPoint.X++;
                }

                // Handle the case where the start waypoint is below and to the left of the next waypoint 
                // (which would overlap existing components of the trajectory if it were allowed to ascend 
                // vertically then cut over to the right)
                if (endPoint.Y < curPoint.Y && endPoint.X > startPoint.X)
                {
                    // If there is an incoming westward trajectory, descend one unit
                    if (grid[curPoint.Y, curPoint.X + 1].PathDirection != PathDirection.None)
                    {
                        // Descend one unit
                        grid[curPoint.Y, curPoint.X].PathDirection =
                            grid[curPoint.Y + 1, curPoint.X].PathDirection = PathDirection.South;

                        // Set juncture flag on new point
                        grid[curPoint.Y + 1, curPoint.X].IsJuncture = true;

                        // Move the starting Y-coordinate 1 down
                        curPoint.Y++;
                    }

                    // Mark solution along x-axis until going two units to the east of the max x-coordinate
                    // (to avoid overlapping with trajectories in the space between the end point and the next rightmost point)
                    for (int xCoord = curPoint.X; xCoord <= maxXPosition + 2; xCoord++)
                    {
                        grid[curPoint.Y, xCoord].PathDirection = PathDirection.East;

                        // Update x-position
                        curPoint.X = xCoord;
                    }

                    // Set juncture flag on pivot point (as next move is vertical)
                    grid[curPoint.Y, curPoint.X].IsJuncture = true;
                }

                // Mark solution along y-axis, leaving X-location at previous point
                for (int yCoord = curPoint.Y;
                    curPoint.Y < endPoint.Y
                        ? yCoord <= endPoint.Y
                        : yCoord >= endPoint.Y;
                    yCoord += curPoint.Y < endPoint.Y ? 1 : -1)
                {
                    grid[yCoord, curPoint.X].PathDirection = curPoint.Y < endPoint.Y
                        ? PathDirection.South
                        : PathDirection.North;
                }

                // Mark solution along x-axis, with y-location at current point
                for (int xCoord = curPoint.X;
                    curPoint.X < endPoint.X
                        ? xCoord <= endPoint.X
                        : xCoord >= endPoint.X;
                    xCoord += curPoint.X < endPoint.X ? 1 : -1)
                {
                    grid[endPoint.Y, xCoord].PathDirection = curPoint.X < endPoint.X
                        ? PathDirection.East
                        : PathDirection.West;
                }

                // Set intermediate juncture point for vertical-horizontal transition
                if (curPoint.X != endPoint.X && curPoint.Y != endPoint.Y)
                {
                    grid[endPoint.Y, curPoint.X].IsJuncture = true;
                }
            }
            // Mark path for vertical intersection
            else
            {
                // Handles the case where the end waypoint is to the left of the start waypoint (which also means it's below) 
                // and has X-coordinate less than max X, and start waypoint has Y-coordinate less than max Y (intuition is that 
                // tracing to the left and down will overlap existing trajectories)
                if (endPoint.X < curPoint.X && endPoint.X < maxXPosition && curPoint.Y <= maxYPosition)
                {
                    // If there is an incoming vertical trajectory directly below, move one unit to the right before
                    // beginning downward traversal
                    if (grid[curPoint.Y + 1, curPoint.X].PathDirection != PathDirection.None)
                    {
                        // Set horizontal trajectory on origin and one to the right
                        grid[curPoint.Y, curPoint.X].PathDirection =
                            grid[curPoint.Y, curPoint.X + 1].PathDirection = PathDirection.East;

                        // Set juncture flag on new point
                        grid[curPoint.Y, curPoint.X + 1].IsJuncture = true;

                        // Move the start X-coordinate 1 to the right
                        curPoint.X++;
                    }

                    // Mark solution path along y-axis until going two below the max y-coordinate
                    // (to avoid overlapping with trajectories in the space between the end point and the next lowest point)
                    for (int yCoord = curPoint.Y; yCoord <= maxYPosition + 2; yCoord++)
                    {
                        grid[yCoord, curPoint.X].PathDirection = PathDirection.South;

                        // Update y-position
                        curPoint.Y = yCoord;
                    }

                    // Set juncture flag on pivot point (as next move is horizontal)
                    grid[curPoint.Y, curPoint.X].IsJuncture = true;
                }
                // Handle the case where the start waypoint has a horizontal orientation and would otherwise overlap
                else if ((endPoint.X < curPoint.X &&
                          grid[curPoint.Y, curPoint.X - 1].PathDirection != PathDirection.None) ||
                         (endPoint.X > curPoint.X &&
                          grid[curPoint.Y, curPoint.X + 1].PathDirection != PathDirection.None))
                {
                    // Set vertical orientation on origin and one down
                    grid[curPoint.Y, curPoint.X].PathDirection =
                        grid[curPoint.Y + 1, curPoint.X].PathDirection = PathDirection.South;

                    // Set juncture flag on new point
                    grid[curPoint.Y + 1, curPoint.X].IsJuncture = true;

                    // Move the starting Y-coordinate 1 down
                    curPoint.Y++;
                }

                // Mark solution along x-axis, with y-location at previous point
                for (int xCoord = curPoint.X;
                    curPoint.X < endPoint.X
                        ? xCoord <= endPoint.X
                        : xCoord >= endPoint.X;
                    xCoord += curPoint.X < endPoint.X ? 1 : -1)
                {
                    grid[curPoint.Y, xCoord].PathDirection = curPoint.X < endPoint.X
                        ? PathDirection.East
                        : PathDirection.West;
                }

                // Mark solution along y-axis, leaving X-location at current point
                for (int yCoord = curPoint.Y;
                    curPoint.Y < endPoint.Y
                        ? yCoord <= endPoint.Y
                        : yCoord >= endPoint.Y;
                    yCoord += curPoint.Y < endPoint.Y ? 1 : -1)
                {
                    grid[yCoord, endPoint.X].PathDirection = curPoint.Y < endPoint.Y
                        ? PathDirection.South
                        : PathDirection.North;
                }

                // Set intermediate juncture point for horizontal-vertical transition
                if (curPoint.X != endPoint.X && curPoint.Y != endPoint.Y)
                {
                    grid[curPoint.Y, endPoint.X].IsJuncture = true;
                }
            }

            // Set starting waypoint as juncture if incoming and outgoing path segments were perpendicular
            if ((grid[startPoint.Y, startPoint.X].PathOrientation == PathOrientation.Horizontal &&
                 ((startPoint.Y - 1 >= 0 &&
                   grid[startPoint.Y - 1, startPoint.X].PathOrientation == PathOrientation.Vertical) ||
                  (startPoint.Y + 1 < grid.GetLength(0) &&
                   grid[startPoint.Y + 1, startPoint.X].PathOrientation == PathOrientation.Vertical))) ||
                (grid[startPoint.Y, startPoint.X].PathOrientation == PathOrientation.Vertical &&
                 ((startPoint.X - 1 >= 0 &&
                   grid[startPoint.Y, startPoint.X - 1].PathOrientation == PathOrientation.Horizontal) ||
                  (startPoint.X + 1 < grid.GetLength(1) &&
                   grid[startPoint.Y, startPoint.X + 1].PathOrientation == PathOrientation.Horizontal))))
            {
                grid[startPoint.Y, startPoint.X].IsJuncture = true;
            }
        }

        /// <summary>
        ///     Builds list of submazes, partitioned via the interweaving solution trajectory.
        /// </summary>
        /// <param name="grid">The two-dimensional, n x n grid of maze cells.</param>
        /// <param name="mazeHeight">The height of the full maze.</param>
        /// <param name="mazeWidth">The width of the full maze.</param>
        /// <returns>List of submazes resulting from solution path.</returns>
        private static List<MazeStructureRoom> ExtractSubmazes(MazeStructureGridCell[,] grid, int mazeHeight,
            int mazeWidth)
        {
            List<MazeStructureRoom> subMazes = new List<MazeStructureRoom>();

            // Process mazes line-by-line
            for (int y = 0; y < mazeHeight; y++)
            {
                for (int x = 0; x < mazeWidth; x++)
                {
                    // If the trajectory does not intersect the current cell and it's not within an existing room, 
                    // then trace out room and add to list
                    if (grid[y, x].PathDirection == PathDirection.None &&
                        subMazes.Count(sm => sm.IsCellInRoom(grid[y, x])) == 0)
                    {
                        // Set room starting location
                        var roomStartX = x;
                        var roomStartY = y;
                        var roomEndX = roomStartX;
                        var roomEndY = roomStartY;
                        var obstructionLocated = false;

                        // Traverse to the right-most room edge
                        while (roomEndX + 1 < mazeWidth &&
                               grid[roomEndY, roomEndX + 1].PathDirection == PathDirection.None &&
                               subMazes.Count(sm => sm.IsCellInRoom(grid[roomEndY, roomEndX + 1])) == 0)
                        {
                            roomEndX++;
                        }

                        // Traverse to the bottom of the room until an obstruction is hit
                        while (obstructionLocated == false && roomEndY + 1 < mazeHeight)
                        {
                            // Check if the trajectory has changed shape (expanded eastward or westward) and close off the room if so
                            if ((roomEndX + 1 < mazeWidth &&
                                 grid[roomEndY + 1, roomEndX + 1].PathDirection == PathDirection.None) ||
                                (roomStartX - 1 > 0 &&
                                 grid[roomEndY + 1, roomEndX - 1].PathDirection == PathDirection.None))
                            {
                                obstructionLocated = true;
                            }
                            else
                            {
                                roomEndY++;

                                // Check to see if there is an intersecting path segment on the current row
                                for (int curX = roomStartX; curX <= roomEndX; curX++)
                                {
                                    // Set obstruction flag and decrement the row position
                                    if (grid[roomEndY, curX].PathDirection != PathDirection.None)
                                    {
                                        obstructionLocated = true;
                                        roomEndY--;
                                        break;
                                    }
                                }
                            }
                        }

                        // Add maze room
                        subMazes.Add(new MazeStructureRoom(roomStartX, roomStartY, roomEndX - roomStartX + 1,
                            roomEndY - roomStartY + 1));
                    }
                }
            }

            return subMazes;
        }

        /// <summary>
        ///     Moves along the trajectory and places boundaries between separate trajectory segments that happen to be adjacent
        ///     (i.e. within one unit of each other). This is to avoid having a big, open space (because there's not room to place
        ///     maze rooms), which would allow the navigator to simply cut diagonally through no obstructions and make the maze
        ///     artificially easier to solve despite having what should be a more complicated trajectory.
        /// </summary>
        /// <param name="grid">The two-dimensional, n x n grid of maze cells.</param>
        /// <param name="mazeHeight">The height of the full maze.</param>
        /// <param name="mazeWidth">The width of the full maze.</param>
        private static void MarkAdjacentTrajectoryBoundaries(MazeStructureGridCell[,] grid, int mazeHeight,
            int mazeWidth)
        {
            // Mark the starting position
            var curCell = grid[0, 0];
            var prevCell = curCell;

            // Trace through the trajectory until reaching the end, placing walls between 
            // adjacent trajectory segments
            do
            {
                // Handle adjacent trajectory to the north
                if (curCell.PathDirection != PathDirection.North && curCell.Y > 0 &&
                    grid[curCell.Y - 1, curCell.X] != prevCell &&
                    grid[curCell.Y - 1, curCell.X].PathDirection != PathDirection.None)
                {
                    grid[curCell.Y - 1, curCell.X].SouthWall = true;
                }

                // Handle adjacent trajectory to the south
                if (curCell.PathDirection != PathDirection.South && curCell.Y < mazeHeight - 1 &&
                    grid[curCell.Y + 1, curCell.X] != prevCell &&
                    grid[curCell.Y + 1, curCell.X].PathDirection != PathDirection.None)
                {
                    grid[curCell.Y, curCell.X].SouthWall = true;
                }

                // Handle adjacent trajectory to the west
                if (curCell.PathDirection != PathDirection.West && curCell.X > 0 &&
                    grid[curCell.Y, curCell.X - 1] != prevCell &&
                    grid[curCell.Y, curCell.X - 1].PathDirection != PathDirection.None)
                {
                    grid[curCell.Y, curCell.X - 1].EastWall = true;
                }

                // Handle adjacent trajectory to the east
                if (curCell.PathDirection != PathDirection.East && curCell.X < mazeWidth - 1 &&
                    grid[curCell.Y, curCell.X + 1] != prevCell &&
                    grid[curCell.Y, curCell.X + 1].PathDirection != PathDirection.None)
                {
                    grid[curCell.Y, curCell.X].EastWall = true;
                }

                // Update previous cell to point to the current cell
                prevCell = curCell;

                // Move east or west
                if (curCell.PathOrientation == PathOrientation.Horizontal)
                {
                    curCell = curCell.PathDirection == PathDirection.West
                        ? grid[curCell.Y, curCell.X - 1]
                        : grid[curCell.Y, curCell.X + 1];
                }
                // Move north or south
                else
                {
                    curCell = curCell.PathDirection == PathDirection.North
                        ? grid[curCell.Y - 1, curCell.X]
                        : grid[curCell.Y + 1, curCell.X];
                }
            } while (curCell.X != mazeWidth - 1 || curCell.Y != mazeHeight - 1);
        }

        /// <summary>
        ///     Determines whether a sub-maze to the right of the trajectory exists for the current row. The only scenario where a
        ///     right sub-maze would not exist would be the case in which the trajectory occupies the last column of the maze,
        ///     butting up against the right, outer wall.
        /// </summary>
        /// <param name="grid">The two-dimensional, n x n grid of maze cells.</param>
        /// <param name="row">The current maze row.</param>
        /// <param name="width">The width of the maze.</param>
        /// <param name="mazeStartPoint">
        ///     The point at which the sub-maze to the right of the trajectory begins (this is an output
        ///     parameter).
        /// </param>
        /// <returns>Boolean indicator of whether right sub-maze exists for the given row.</returns>
        private static bool IsRightSubmaze(MazeStructureGridCell[,] grid, int row, int width,
            out Point2DInt mazeStartPoint)
        {
            mazeStartPoint = new Point2DInt();

            // Loop through each cell in the row and find the right-most trajectory cell
            for (int pos = 0; pos < width; pos++)
            {
                if (grid[row, pos].PathDirection != PathDirection.None)
                {
                    // Set the start point to the cell to the right of the juncture
                    mazeStartPoint = new Point2DInt(pos + 1, row);
                }
            }

            // A valid starting location has been found if the X location is between the
            // first and last column
            return mazeStartPoint.X > 0 && mazeStartPoint.X < width;
        }

        /// <summary>
        ///     Determines whether the given row and column denote the end of the current sub-maze right of the trajectory.
        /// </summary>
        /// <param name="grid">The two-dimensional, n x n grid of maze cells.</param>
        /// <param name="row">The current maze row.</param>
        /// <param name="width">The width of the maze.</param>
        /// <param name="column">The current maze column.</param>
        /// <param name="mazeEndPoint">
        ///     The point at which the sub-maze to the right of the trajectory ends (this is an output
        ///     parameter).
        /// </param>
        /// <returns>Boolean indicator of whether current cell demarcates the end of the right sub-maze.</returns>
        private static bool IsRightMazeEndRow(MazeStructureGridCell[,] grid, int row, int column, int width,
            out Point2DInt mazeEndPoint)
        {
            mazeEndPoint = new Point2DInt();
            bool isEndRow = false;

            // Check for sub-maze to right of L-intersection (first) and inverted L-intersection (second)
            if (
                (grid[row, column].PathDirection != PathDirection.None &&
                 grid[row - 1, column].PathDirection == PathDirection.None) ||
                (grid[row, column - 1].PathDirection == PathDirection.None &&
                 grid[row - 1, column - 1].PathDirection != PathDirection.None))
            {
                mazeEndPoint = new Point2DInt(width - 1, row - 1);
                isEndRow = true;
            }

            return isEndRow;
        }

        /// <summary>
        ///     Determines the ending column of the sub-maze to the left of the trajectory for the given row.
        /// </summary>
        /// <param name="grid">The two-dimensional, n x n grid of maze cells.</param>
        /// <param name="row">The current maze row.</param>
        /// <returns>The column that demarcates the right-most boundary of the sub-maze to the left of the trajectory.</returns>
        private static int DetermineLeftSubmazeEndPosition(MazeStructureGridCell[,] grid, int row)
        {
            int pos = 0;

            // Increment until trajectory point cutting off the left submaze is found
            do
            {
                pos++;
            } while (grid[row, pos].PathDirection == PathDirection.None);

            // Return the difference between the intersecting trajectory point and the submaze start
            return pos - 1;
        }

        /// <summary>
        ///     Determines whether the given row and column denote the end of the current sub-maze left of the trajectory.
        /// </summary>
        /// <param name="grid">The two-dimensional, n x n grid of maze cells.</param>
        /// <param name="row">The current maze row.</param>
        /// <param name="subMazeEndPos">The end column of the sub-maze.</param>
        /// <param name="height">The height of the maze.</param>
        /// <param name="mazeEndPoint">
        ///     The point at which the sub-maze to the left of the trajectory ends (this is an output
        ///     parameter).
        /// </param>
        /// <returns>Boolean indicator of whether current cell demarcates the end of the left sub-maze.</returns>
        private static bool IsLeftMazeEndRow(MazeStructureGridCell[,] grid, int row, int subMazeEndPos, int height,
            out Point2DInt mazeEndPoint)
        {
            mazeEndPoint = new Point2DInt();
            bool isEndRow = false;

            // If this is the last row of the maze, it's the end position by default
            if (row == height - 1)
            {
                // If last row is beneath L-intersection on trajectory, set end position one
                // row above as there needs to be another room below the trajectory
                mazeEndPoint = grid[row, subMazeEndPos + 1].PathDirection == PathDirection.None
                    ? new Point2DInt(subMazeEndPos, row - 1)
                    : new Point2DInt(subMazeEndPos, row);
                isEndRow = true;
            }
            // Check for sub-maze to left of L-intersection (first) and inverted L-intersection (second)
            else if (grid[row, subMazeEndPos + 1].PathDirection == PathDirection.None ||
                     grid[row, subMazeEndPos].PathDirection != PathDirection.None)
            {
                mazeEndPoint = new Point2DInt(subMazeEndPos, row - 1);
                isEndRow = true;
            }

            return isEndRow;
        }

        /// <summary>
        ///     Builds a dictionary of sub-mazes and their associated internal walls, evenly distributing the walls specified in
        ///     the genome across each of the sub-mazes.
        /// </summary>
        /// <param name="subMazes">The sub-mazes in the overall maze that are induced by the solution trajectory.</param>
        /// <param name="wallGenes">The list of internal wall genes.</param>
        /// <returns>Dictionary containing associates between each sub-maze and its component internal walls.</returns>
        private static Dictionary<MazeStructureRoom, List<WallGene>> ExtractMazeWallMap(
            List<MazeStructureRoom> subMazes, IList<WallGene> wallGenes)
        {
            // Filter out mazes that are too narrow to support internal walls
            List<MazeStructureRoom> subMazesWithWalls = subMazes.Where(m => m.AreInternalWallsSupported()).ToList();

            // Filter out mazes that are too narrow to support internal walls and 
            // convert to dictionary of sub mazes and associated internal walls
            Dictionary<MazeStructureRoom, List<WallGene>> subMazeWallsMap =
                subMazes.Where(m => m.AreInternalWallsSupported()).ToDictionary(k => k, v => new List<WallGene>());

            // Evenly distribute internal walls among sub-mazes
            for (int wallCnt = 0; wallCnt < wallGenes.Count; wallCnt++)
            {
                subMazeWallsMap[subMazesWithWalls[wallCnt%subMazesWithWalls.Count]].Add(wallGenes[wallCnt]);
            }

            return subMazeWallsMap;
        }

        #endregion

        #region Helper methods        

        /// <summary>
        ///     Determines the maximum number of partitions possible given a partially partitioned maze genome as a starting point.
        /// </summary>
        /// <param name="mazeGenome">The partially complexified genome from which to start max partition estimation.</param>
        /// <returns>
        ///     The total number of partitions supported given the existing maze genome complexity/wall placement and dimensions.
        /// </returns>
        public static int DetermineMaxPartitions(MazeGenome mazeGenome)
        {
            int maxPartitions = 0;

            // Construct maze grid with solution path generated from connected waypoints
            MazeStructureGridCell[,] mazeGrid = BuildMazeSolutionPath(mazeGenome);

            // Extract the "sub-mazes" that are induced by the solution trajectory
            List<MazeStructureRoom> subMazes = ExtractSubmazes(mazeGrid, mazeGenome.MazeBoundaryHeight,
                mazeGenome.MazeBoundaryWidth);

            // Process all sub-mazes, iteratively bisecting the applicable maze room space
            foreach (var subMaze in subMazes)
            {
                Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>();

                // Mark boundaries for current submaze, including perpendicular opening next to first 
                // internal partition (if one exists)
                if (subMaze.AreInternalWallsSupported() && mazeGenome.WallGeneList.Count > 0)
                {
                    subMaze.MarkRoomBoundaries(mazeGrid, mazeGenome.WallGeneList[0].WallLocation,
                        mazeGenome.WallGeneList[0].OrientationSeed);
                }
                else
                {
                    subMaze.MarkRoomBoundaries(mazeGrid);
                }

                if (subMaze.AreInternalWallsSupported() && mazeGenome.WallGeneList.Count > 0)
                {
                    int loopIter = 0;

                    // Queue up the first "room" (which will encompass the entirety of the submaze grid)
                    mazeRoomQueue.Enqueue(subMaze);

                    // Make sure there are rooms left in the queue before attempting to dequeue and bisect
                    while (mazeRoomQueue.Count > 0)
                    {
                        // Dequeue a room and run division on it
                        Tuple<MazeStructureRoom, MazeStructureRoom> subRooms = mazeRoomQueue.Dequeue()
                            .DivideRoom(mazeGrid,
                                mazeGenome.WallGeneList[loopIter%mazeGenome.WallGeneList.Count].WallLocation,
                                mazeGenome.WallGeneList[loopIter%mazeGenome.WallGeneList.Count].PassageLocation,
                                mazeGenome.WallGeneList[loopIter%mazeGenome.WallGeneList.Count].OrientationSeed);

                        // Update max partitions to the max wall iteration depth in the submaze
                        maxPartitions = Math.Max(loopIter + 1, maxPartitions);

                        if (subRooms != null)
                        {
                            // Get the two resulting sub rooms and enqueue both of them
                            if (subRooms.Item1 != null) mazeRoomQueue.Enqueue(subRooms.Item1);
                            if (subRooms.Item2 != null) mazeRoomQueue.Enqueue(subRooms.Item2);
                        }

                        loopIter++;
                    }
                }
            }

            // Return the maximum number of partitions applied in a submaze
            return maxPartitions;
        }

        /// <summary>
        ///     Constructs maze sub-rooms around the path and distributes internal wall genes within said subrooms.
        /// </summary>
        /// <param name="genome">The maze genome containing internal wall genes.</param>
        /// <param name="mazeGrid">The two-dimensional, n x n grid of maze cells.</param>
        /// <returns>The maze structure grid with sub-mazes and internal walls appropriately placed.</returns>
        public static MazeStructureGrid BuildMazeStructureAroundPath(MazeGenome genome,
            MazeStructureGridCell[,] mazeGrid)
        {
            List<MazeStructureRoom> mazeRooms = new List<MazeStructureRoom>();
            int partitionCount = 0;

            // Extract the "sub-mazes" that are induced by the solution trajectory
            List<MazeStructureRoom> subMazes = ExtractSubmazes(mazeGrid, genome.MazeBoundaryHeight,
                genome.MazeBoundaryWidth);

            // Mark walls between trajectory segments that are adjacent
            MarkAdjacentTrajectoryBoundaries(mazeGrid, genome.MazeBoundaryHeight, genome.MazeBoundaryWidth);

            // Process all sub-mazes, iteratively bisecting the applicable maze room space
            foreach (var subMaze in subMazes)
            {
                Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>();

                // Mark boundaries for current submaze, including perpendicular opening next to first 
                // internal partition (if one exists)
                if (subMaze.AreInternalWallsSupported() && genome.WallGeneList.Count > 0)
                {
                    subMaze.MarkRoomBoundaries(mazeGrid, genome.WallGeneList[0].WallLocation,
                        genome.WallGeneList[0].OrientationSeed);
                }
                else
                {
                    subMaze.MarkRoomBoundaries(mazeGrid);
                }

                if (subMaze.AreInternalWallsSupported() && genome.WallGeneList.Count > 0)
                {
                    int loopIter = 0;

                    // Queue up the first "room" (which will encompass the entirety of the submaze grid)
                    mazeRoomQueue.Enqueue(subMaze);

                    // Make sure there are rooms left in the queue before attempting to dequeue and bisect
                    while (mazeRoomQueue.Count > 0)
                    {
                        // Dequeue a room and run division on it
                        Tuple<MazeStructureRoom, MazeStructureRoom> subRooms = mazeRoomQueue.Dequeue()
                            .DivideRoom(mazeGrid, genome.WallGeneList[loopIter%genome.WallGeneList.Count].WallLocation,
                                genome.WallGeneList[loopIter%genome.WallGeneList.Count].PassageLocation,
                                genome.WallGeneList[loopIter%genome.WallGeneList.Count].OrientationSeed);

                        if (subRooms != null)
                        {
                            // Increment the count of partitions
                            partitionCount++;

                            // Get the two resulting sub rooms and enqueue both of them
                            if (subRooms.Item1 != null) mazeRoomQueue.Enqueue(subRooms.Item1);
                            if (subRooms.Item2 != null) mazeRoomQueue.Enqueue(subRooms.Item2);
                        }

                        loopIter++;
                    }
                }
            }

            return new MazeStructureGrid(mazeGrid, partitionCount, mazeRooms);
        }

        /// <summary>
        ///     Decodes the given maze genome to an unscaled grid structure, indicating wall placement and orientation on a
        ///     cell-by-cell basis.
        /// </summary>
        /// <param name="genome">The genome to decode.</param>
        /// <returns>The unscaled, maze grid structure.</returns>
        public static MazeStructureGrid ConvertMazeGenomeToUnscaledStructure(MazeGenome genome)
        {
            Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>();
            MazeStructureGridCell[,] unscaledGrid = InitializeMazeGrid(genome.MazeBoundaryHeight,
                genome.MazeBoundaryWidth);
            int partitionCount = 0;

            // Queue up the first "room" (which will encompass the entirety of the maze grid)
            mazeRoomQueue.Enqueue(new MazeStructureRoom(0, 0, genome.MazeBoundaryWidth, genome.MazeBoundaryHeight));

            // Iterate through all of the genes, generating 
            foreach (WallGene wallGene in genome.WallGeneList)
            {
                // Make sure there are rooms left in the queue before attempting to dequeue and bisect
                if (mazeRoomQueue.Count > 0)
                {
                    // Dequeue a room and run division on it
                    Tuple<MazeStructureRoom, MazeStructureRoom> subRooms = mazeRoomQueue.Dequeue()
                        .DivideRoom(unscaledGrid, wallGene.WallLocation,
                            wallGene.PassageLocation,
                            wallGene.OrientationSeed);

                    if (subRooms != null)
                    {
                        // Increment the count of partitions
                        partitionCount++;

                        // Get the two resulting sub rooms and enqueue both of them
                        if (subRooms.Item1 != null) mazeRoomQueue.Enqueue(subRooms.Item1);
                        if (subRooms.Item2 != null) mazeRoomQueue.Enqueue(subRooms.Item2);
                    }
                }
            }

            // Construct and return the maze grid structure
            return new MazeStructureGrid(unscaledGrid, partitionCount, mazeRoomQueue.ToList());
        }

        /// <summary>
        ///     Flood fills from the starting location in the maze until the target location is reached. Each linked point has a
        ///     reference back to the point preceding it, allowing the full path to be traced back from the target point.
        /// </summary>
        /// <param name="mazeGrid">The grid of maze points.</param>
        /// <param name="mazeHeight">The height of the maze.</param>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <returns>The target point reached by the flood fill process, containing a pointer chain back to the starting location.</returns>
        public static MazeStructureLinkedPoint FloodFillToTarget(MazeStructureGrid mazeGrid, int mazeHeight,
            int mazeWidth)
        {
            MazeStructureLinkedPoint curPoint = null;

            // Setup grid to store maze structure points
            var pointGrid = new MazeStructureLinkedPoint[mazeHeight, mazeWidth];

            // Convert to grid of maze structure points
            for (int x = 0; x < mazeHeight; x++)
            {
                for (int y = 0; y < mazeWidth; y++)
                {
                    pointGrid[x, y] = new MazeStructureLinkedPoint(x, y);
                }
            }

            // Define queue in which to store cells as they're discovered and visited
            Queue<MazeStructureLinkedPoint> cellQueue = new Queue<MazeStructureLinkedPoint>(pointGrid.Length);

            // Define a list to store visited cells in the order they were visited
            List<MazeStructureLinkedPoint> visitedCells = new List<MazeStructureLinkedPoint>();

            // Enqueue the starting location
            cellQueue.Enqueue(new MazeStructureLinkedPoint(pointGrid[0, 0]));

            // Iterate through maze cells, dequeueing each and determining the distance to their reachable neighbors
            // until the target location is reached and we have the shortest distance to it
            while (cellQueue.Count > 0)
            {
                // Get the next element in the queue
                curPoint = cellQueue.Dequeue();

                // Exit if target reached
                if (curPoint.X == mazeHeight - 1 && curPoint.Y == mazeWidth - 1)
                {
                    break;
                }

                // Handle cells in each cardinal direction

                // North
                if (0 != curPoint.X && mazeGrid.Grid[curPoint.X - 1, curPoint.Y].SouthWall == false &&
                    visitedCells.Contains(pointGrid[curPoint.X - 1, curPoint.Y]) == false)
                {
                    cellQueue.Enqueue(new MazeStructureLinkedPoint(pointGrid[curPoint.X - 1, curPoint.Y], curPoint));
                    visitedCells.Add(pointGrid[curPoint.X - 1, curPoint.Y]);
                }

                // East
                if (mazeWidth > curPoint.Y + 1 && mazeGrid.Grid[curPoint.X, curPoint.Y].EastWall == false &&
                    visitedCells.Contains(pointGrid[curPoint.X, curPoint.Y + 1]) == false)
                {
                    cellQueue.Enqueue(new MazeStructureLinkedPoint(pointGrid[curPoint.X, curPoint.Y + 1], curPoint));
                    visitedCells.Add(pointGrid[curPoint.X, curPoint.Y + 1]);
                }

                // South
                if (mazeHeight > curPoint.X + 1 && mazeGrid.Grid[curPoint.X, curPoint.Y].SouthWall == false &&
                    visitedCells.Contains(pointGrid[curPoint.X + 1, curPoint.Y]) == false)
                {
                    cellQueue.Enqueue(new MazeStructureLinkedPoint(pointGrid[curPoint.X + 1, curPoint.Y], curPoint));
                    visitedCells.Add(pointGrid[curPoint.X + 1, curPoint.Y]);
                }

                // West
                if (0 != curPoint.Y && mazeGrid.Grid[curPoint.X, curPoint.Y - 1].EastWall == false &&
                    visitedCells.Contains(pointGrid[curPoint.X, curPoint.Y - 1]) == false)
                {
                    cellQueue.Enqueue(new MazeStructureLinkedPoint(pointGrid[curPoint.X, curPoint.Y - 1], curPoint));
                    visitedCells.Add(pointGrid[curPoint.X, curPoint.Y - 1]);
                }
            }

            return curPoint;
        }

        /// <summary>
        ///     Computes the (unscaled) distance from the starting location in the maze grid to the target location.
        /// </summary>
        /// <param name="mazeGrid">The grid of maze points.</param>
        /// <param name="mazeHeight">The height of the maze.</param>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <returns>Distance from starting location to the ending location.</returns>
        public static int ComputeDistanceToTarget(MazeStructureGrid mazeGrid, int mazeHeight, int mazeWidth)
        {
            int distance = 0;

            // Get target point with links back to origin
            var targetLinkedPoint = FloodFillToTarget(mazeGrid, mazeHeight, mazeWidth);

            // Walk the references back to the origin and compute the number of steps
            while (targetLinkedPoint != null)
            {
                // Increment distance
                distance++;

                // Set the previous point
                targetLinkedPoint = targetLinkedPoint.PrevPoint;
            }

            return distance;
        }

        /// <summary>
        ///     Creates a two-dimensional array (grid) of empty maze grid cells.
        /// </summary>
        /// <param name="height">Height of the grid.</param>
        /// <param name="width">Width of the grid.</param>
        /// <returns>Grid initialized with empty cells.</returns>
        public static MazeStructureGridCell[,] InitializeMazeGrid(int height, int width)
        {
            // Create the two-dimensional grid
            MazeStructureGridCell[,] grid =
                new MazeStructureGridCell[height, width];

            // Iterate through and create an empty cell for each position
            for (int heightIdx = 0; heightIdx < height; heightIdx++)
            {
                for (int widthIdx = 0; widthIdx < width; widthIdx++)
                {
                    grid[heightIdx, widthIdx] = new MazeStructureGridCell(widthIdx, heightIdx,
                        heightIdx == 0 && widthIdx == 0, heightIdx == height - 1 && widthIdx == width - 1);
                }
            }

            return grid;
        }

        /// <summary>
        ///     Constructs the maze solution trajectory based on the waypoints defined in the maze genome.
        /// </summary>
        /// <param name="genome">The maze genome.</param>
        /// <returns>The maze grid cells with cells in the path trajectory identified.</returns>
        public static MazeStructureGridCell[,] BuildMazeSolutionPath(MazeGenome genome)
        {
            // Starting location will always be at the top left corner
            Point2DInt startLocation = new Point2DInt(0, 0);

            // Ending location will always be at the bottom right corner
            Point2DInt targetLocation = new Point2DInt(genome.MazeBoundaryWidth - 1, genome.MazeBoundaryHeight - 1);

            // Initialize the grid
            MazeStructureGridCell[,] unscaledGrid = InitializeMazeGrid(genome.MazeBoundaryHeight,
                genome.MazeBoundaryWidth);

            for (int idx = 0; idx <= genome.PathGeneList.Count; idx++)
            {
                // Get the previous point (if first iteration, previous point is the start location)
                Point2DInt prevPoint = idx == 0
                    ? startLocation
                    : genome.PathGeneList[idx - 1].Waypoint;

                // Get the current point (if last iteration, current point is the target location)
                Point2DInt curPoint = idx == genome.PathGeneList.Count
                    ? targetLocation
                    : genome.PathGeneList[idx].Waypoint;

                // Set the waypoint intersection orientation
                IntersectionOrientation curOrientation = curPoint.Equals(targetLocation)
                    ? genome.PathGeneList[idx - 1].DefaultOrientation
                    : genome.PathGeneList[idx].DefaultOrientation;

                // Mark current waypoint
                unscaledGrid[curPoint.Y, curPoint.X].IsWayPoint = true;

                // Denote the resulting solution path on the grid
                MarkSolutionPathSegment(unscaledGrid, prevPoint, curPoint, curOrientation,
                    idx > 0 ? genome.PathGeneList.Take(idx).Max(p => p.Waypoint.X) : prevPoint.X,
                    idx > 0 ? genome.PathGeneList.Take(idx).Max(p => p.Waypoint.Y) : prevPoint.Y);
            }

            return unscaledGrid;
        }

        /// <summary>
        ///     Converts relative, decimal coordinates to unscaled integer coordinates.
        /// </summary>
        /// <param name="relativeCoordinates">The relative coordinate pair on the path gene.</param>
        /// <param name="relativeCellWidth">The relative cell width.</param>
        /// <param name="relativeCellHeight">The relative cell height.</param>
        /// <returns>The integer coordinates on the unscaled grid.</returns>
        public static Point2DInt GetUnscaledCoordinates(Point2DDouble relativeCoordinates, double relativeCellWidth,
            double relativeCellHeight)
        {
            return new Point2DInt(Convert.ToInt32(Math.Floor(relativeCoordinates.X/relativeCellWidth)),
                Convert.ToInt32(Math.Floor(relativeCoordinates.Y/relativeCellHeight)));
        }

        /// <summary>
        ///     Iterates through every cell of the maze grid and determines the number of waypoints that are within the
        ///     "neighborhood" of that cell.
        /// </summary>
        /// <param name="waypoints">The list of waypoints (i.e. path genes).</param>
        /// <param name="mazeHeight">The unscaled maze height.</param>
        /// <param name="mazeWidth">The unscaled maze width.</param>
        /// <param name="neighborhoodRadius">The radius of a maze cell neighborhood in which to search for nearby waypoints.</param>
        /// <returns>Dictionary containing each non-waypoint cell and the number of waypoints within that cell's neighborhood.</returns>
        public static Dictionary<Point2DInt, int> ComputeCellNeighborCounts(IList<PathGene> waypoints, int mazeHeight,
            int mazeWidth, int neighborhoodRadius)
        {
            Dictionary<Point2DInt, int> cellNeighborCounts = new Dictionary<Point2DInt, int>(mazeHeight*mazeWidth);

            // Iterate through each grid cell and compute number of waypoints in neighborhood
            // Note that cells which are themselves waypoints are discarded
            for (int curHeight = 0; curHeight < mazeHeight; curHeight++)
            {
                for (int curWidth = 0; curWidth < mazeWidth; curWidth++)
                {
                    // Skip points that already contain a waypoint
                    if (waypoints.Any(p => p.Waypoint.X == curWidth && p.Waypoint.Y == curHeight))
                        continue;

                    // Count waypoints in cell neighborhood
                    cellNeighborCounts.Add(new Point2DInt(curWidth, curHeight),
                        waypoints.Count(p => (p.Waypoint.X >= Math.Max(curWidth - neighborhoodRadius, 0) &&
                                              p.Waypoint.X <= Math.Min(curWidth + neighborhoodRadius, mazeWidth - 1)) &&
                                             (p.Waypoint.Y >= Math.Max(curHeight - neighborhoodRadius, 0) &&
                                              p.Waypoint.Y <= Math.Min(curHeight + neighborhoodRadius, mazeHeight - 1))));
                }
            }

            return cellNeighborCounts;
        }

        /// <summary>
        ///     Ensures that the proposed waypoint location is within the maze boundaries and does not overlap
        ///     with other waypoints or with the start/end location (which are in the upper-left and lower right cells of the maze
        ///     respectively). Specifically, the following validity checks are performed:
        ///     1. Checks X and Y minimum and maximum points are in the horizontal and vertical boundaries of the maze
        ///     respectively.
        ///     2. Checks that proposed location does not overlap existing waypoints.
        ///     3. Checks that proposed location does not overlap start location or target location.
        ///     4. Checks that proposed location is higher than the next to the last row, or that the proposed location is on the
        ///     next to the last row but its x-position is less than or equal to existing points on the last row, or that the
        ///     proposed location is on the last row but its x-position is greater than or equal to existing points on the next to
        ///     the last row. This is to prevent trajectory overlaps.
        /// </summary>
        /// <param name="genome">The maze genome undergoing mutation.</param>
        /// <param name="waypointLocation">The proposed waypoint.</param>
        /// <param name="genomeId">
        ///     The unique ID (innovation ID) of the path gene. This is used to determine whether the mutation
        ///     results in a valid ordering.
        /// </param>
        /// <returns>Boolean indicating whether the given point is valid per the maze boundary constraints.</returns>
        public static bool IsValidWaypointLocation(MazeGenome genome,
            Point2DInt waypointLocation, uint genomeId)
        {
            return
                // Check that x-coordinate is at-or-above minimum maze width
                waypointLocation.X >= 0 &&

                // Check that x-coordinate is within maze width boundary
                waypointLocation.X < genome.MazeBoundaryWidth - 1 &&

                // Check that y-coordinate is at-or-above minimum maze height
                waypointLocation.Y >= 0 &&

                // Check that y-coordinate is within maze height boundary and is not on the last row
                waypointLocation.Y < genome.MazeBoundaryHeight - 1 &&

                // Check that proposed waypoint does not overlap start position
                waypointLocation.Equals(new Point2DInt(0, 0)) == false &&

                // Check that proposed waypoint does not overlap target position
                waypointLocation.Equals(new Point2DInt(genome.MazeBoundaryWidth - 1, genome.MazeBoundaryHeight - 1)) ==
                false &&

                // Check that waypoints that were added earlier are still at least two units above or to the left of mutated waypoint. 
                // This is because waypoints are connected in the order in which they're added to the genome, and to 
                // avoid overlaps, waypoints are only added below or to the right of pre-existing waypoints.
                ((genome.PathGeneList.Any(g => g.InnovationId < genomeId && g.Waypoint.X + 1 >= waypointLocation.X) ==
                  false &&
                  (genome.PathGeneList.Any(g => g.Waypoint.Y == waypointLocation.Y) == false)) ||
                 (genome.PathGeneList.Any(g => g.InnovationId < genomeId && g.Waypoint.Y + 1 >= waypointLocation.Y) ==
                  false &&
                  (genome.PathGeneList.Any(g => g.Waypoint.X == waypointLocation.X) == false))) &&

                // Check that new waypoint does not overlap existing trajectory
                BuildMazeSolutionPath(genome)[waypointLocation.Y, waypointLocation.X].PathDirection ==
                PathDirection.None
                ;
        }

        #endregion
    }
}