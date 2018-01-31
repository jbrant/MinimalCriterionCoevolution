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

        private static IntersectionOrientation DetermineWaypointIntersectionOrientation(Point2DInt prevWaypoint,
            Point2DInt curWaypoint, Point2DInt nextWaypoint, IntersectionOrientation prevIntersectionOrientation,
            IntersectionOrientation defaultIntersectionOrientation, MazeStructureGridCell[,] grid)
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
                         grid[prevWaypoint.Y, prevWaypoint.X - 1].PathOrientation != PathOrientation.None &&
                         curWaypoint.X > 0)
                {
                    intersectionOrientation = IntersectionOrientation.Horizontal;
                }
                // If current waypoint is below and to the right of previous waypoint and previous waypoint
                // has a trajectory cell to its right, vertical intersection would cause overlapping trajectories
                else if (curWaypoint.Y > prevWaypoint.Y && curWaypoint.X > prevWaypoint.X &&
                         grid[prevWaypoint.Y, prevWaypoint.X + 1].PathOrientation != PathOrientation.None)
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
        /// <param name="nextPoint">The next point after the solution path segment end point.</param>
        /// <param name="orientation">
        ///     The orientation (i.e. horizontal or vertical) of the solution path segment coming into the
        ///     ending point.
        /// </param>
        /// <param name="isStartPointEasterlyTrajectory">
        ///     Flag indicating whether start point is on an easterly trajectory (used to
        ///     determine non-standard trajectory modification for intersecting first point in last row).
        /// </param>
        private static void MarkSolutionPathSegment(MazeStructureGridCell[,] grid, Point2DInt startPoint,
            Point2DInt endPoint, Point2DInt nextPoint, IntersectionOrientation orientation,
            bool isStartPointEasterlyTrajectory)
        {
            if (IntersectionOrientation.Horizontal == orientation)
            {
                // If end point is below and less than the start point, but has a next point 
                // that's on the opposite side of the start point (or vice versa), mark solution 
                // path one unit to the left or right of the end point respectively so that the 
                // trajectory doesn't descend into the middle of two points on the same row
                if (nextPoint.Y == endPoint.Y && startPoint.Y < endPoint.Y &&
                    ((startPoint.X > endPoint.X && startPoint.X <= nextPoint.X) ||
                     (startPoint.X < endPoint.X && startPoint.X >= nextPoint.X)))
                {
                    Point2DInt curPoint = startPoint;

                    for (int xCoord = startPoint.X;
                        startPoint.X > endPoint.X && startPoint.X < nextPoint.X
                            ? xCoord >= endPoint.X - 1
                            : xCoord <= endPoint.X + 1;
                        xCoord += startPoint.X > endPoint.X && startPoint.X <= nextPoint.X ? -1 : 1)
                    {
                        grid[startPoint.Y, xCoord].PathOrientation = PathOrientation.Horizontal;
                        curPoint.X = xCoord;
                    }

                    // Set intermediate juncture point for horizontal-vertical transition
                    grid[curPoint.Y, curPoint.X].IsJuncture = true;

                    // Set the start point to the point resulting from the above path realignment
                    startPoint = curPoint;
                }

                // Mark solution along y-axis, leaving X-location at previous point
                for (int yCoord = startPoint.Y;
                    startPoint.Y < endPoint.Y
                        ? yCoord <= endPoint.Y
                        : yCoord >= endPoint.Y;
                    yCoord += startPoint.Y < endPoint.Y ? 1 : -1)
                {
                    grid[yCoord, startPoint.X].PathOrientation = PathOrientation.Vertical;
                }

                // Mark solution along x-axis, with y-location at current point
                for (int xCoord = startPoint.X;
                    startPoint.X < endPoint.X
                        ? xCoord <= endPoint.X
                        : xCoord >= endPoint.X;
                    xCoord += startPoint.X < endPoint.X ? 1 : -1)
                {
                    grid[endPoint.Y, xCoord].PathOrientation = PathOrientation.Horizontal;
                }

                // Set intermediate juncture point for vertical-horizontal transition
                if (startPoint.X != endPoint.X && startPoint.Y != endPoint.Y)
                {
                    grid[endPoint.Y, startPoint.X].IsJuncture = true;
                }
            }
            // Mark path for vertical intersection
            else
            {
                // If start point is to the right of end point and it has trajectory points to the west of it,
                // it must first descend one cell so that trajectory can intersect the end point vertically
                // without overlapping
                if (startPoint.X > endPoint.X &&
                    grid[startPoint.Y, startPoint.X - 1].PathOrientation != PathOrientation.None)
                {
                    // Set start point outgoing orientation to vertical
                    grid[startPoint.Y, startPoint.X].PathOrientation = PathOrientation.Vertical;

                    // Move one cell down
                    startPoint.Y++;

                    // Mark intersection of cell below as vertical and set it as juncture
                    grid[startPoint.Y, startPoint.X].PathOrientation = PathOrientation.Vertical;
                    grid[startPoint.Y, startPoint.X].IsJuncture = true;
                }

                // Mark solution along x-axis, with y-location at previous point
                for (int xCoord = startPoint.X;
                    startPoint.X < endPoint.X
                        ? xCoord <= endPoint.X
                        : xCoord >= endPoint.X;
                    xCoord += startPoint.X < endPoint.X ? 1 : -1)
                {
                    grid[startPoint.Y, xCoord].PathOrientation = PathOrientation.Horizontal;
                }

                // Mark solution along y-axis, leaving X-location at current point
                for (int yCoord = startPoint.Y;
                    startPoint.Y < endPoint.Y
                        ? yCoord <= endPoint.Y
                        : yCoord >= endPoint.Y;
                    yCoord += startPoint.Y < endPoint.Y ? 1 : -1)
                {
                    grid[yCoord, endPoint.X].PathOrientation = PathOrientation.Vertical;
                }

                // Set intermediate juncture point for horizontal-vertical transition
                if (startPoint.X != endPoint.X && startPoint.Y != endPoint.Y)
                {
                    grid[startPoint.Y, endPoint.X].IsJuncture = true;
                }
            }

            // Set starting waypoint as juncture if incoming and outgoing path segments were perpendicular
            if ((grid[startPoint.Y, startPoint.X].PathOrientation == PathOrientation.Horizontal &&
                 (startPoint.Y - 1 >= 0 &&
                  grid[startPoint.Y - 1, startPoint.X].PathOrientation == PathOrientation.Vertical)) ||
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
            MazeStructureGridCell roomStartCell = null;
            bool inRoom = false;
            bool isLeftSubmazeTaller = false;
            //int y = 0;


            // Handle left submazes
            Point2DInt subMazeStartPoint = new Point2DInt();
            Point2DInt subMazeEndPoint;
            int leftSubmazeEndPos = 0;

            // Process left sub-maze rooms
            for (int y = 0; y < mazeHeight; y++)
            {
                // Mark start of new sub-maze if there are no path obstructions
                if (inRoom == false && y != 0 && grid[y, 0].PathOrientation == PathOrientation.None)
                {
                    subMazeStartPoint = new Point2DInt(0, y);
                    leftSubmazeEndPos = DetermineLeftSubmazeEndPosition(grid, subMazeStartPoint.Y);

                    // If this is the last row, add the one unit height maze and leave in-room indicator false
                    if (y == mazeHeight - 1)
                    {
                        subMazes.Add(new MazeStructureRoom(subMazeStartPoint.X, subMazeStartPoint.Y,
                            leftSubmazeEndPos + 1, 1));
                    }
                    // Otherwise, set in room indicator
                    else
                    {
                        inRoom = true;
                    }
                }
                // Add current room if we've reached its endpoint
                else if (inRoom && IsLeftMazeEndRow(grid, y, leftSubmazeEndPos, mazeHeight, out subMazeEndPoint))
                {
                    subMazes.Add(new MazeStructureRoom(subMazeStartPoint.X, subMazeStartPoint.Y, subMazeEndPoint.X + 1,
                        subMazeEndPoint.Y - subMazeStartPoint.Y + 1));

                    // Back y up to the maze endpoint if its less than the current iteration
                    if (y > subMazeEndPoint.Y)
                    {
                        y -= y - subMazeEndPoint.Y;
                    }

                    inRoom = false;
                }
            }

            // Process right sub-maze rooms
            for (int y = 0; y < mazeHeight; y++)
            {
                // Mark the start of new right sub-maze (contingent on the trajectory being left 
                // of the last column, as that would leave no room for a right sub-maze)
                if (inRoom == false && IsRightSubmaze(grid, y, mazeWidth, out subMazeStartPoint))
                {
                    inRoom = true;
                }
                //else if (inRoom && grid[y, subMazeStartPoint.X].PathOrientation != PathOrientation.None)
                else if (inRoom &&
                         IsRightMazeEndRow(grid, y, mazeHeight, mazeWidth, subMazeStartPoint.X, out subMazeEndPoint))
                {
                    subMazes.Add(new MazeStructureRoom(subMazeStartPoint.X, subMazeStartPoint.Y,
                        mazeWidth - subMazeStartPoint.X, subMazeEndPoint.Y - subMazeStartPoint.Y + 1));

                    // Back y up to the maze endpoint if its less than the current iteration
                    y--;

                    inRoom = false;
                }
            }

            // Process walls enclosing vertically adjacent trajectory paths
            MazeStructureGridCell prevGridCell = grid[0, 0];
            MazeStructureGridCell curGridCell = grid[0, 0];

            do
            {
                if (curGridCell.IsJuncture || curGridCell.IsStartCell)
                {
                    prevGridCell = curGridCell;

                    if (curGridCell.PathOrientation == PathOrientation.Vertical)
                    {
                        curGridCell = grid[curGridCell.Y + 1, curGridCell.X];
                    }
                    else
                    {
                        // Check to see if part of the trajectory passes directly underneath
                        if (curGridCell.Y < mazeHeight - 1 &&
                            grid[curGridCell.Y + 1, curGridCell.X].PathOrientation != PathOrientation.None)
                        {
                            grid[curGridCell.Y, curGridCell.X].SouthWall = true;
                        }

                        if (curGridCell.X > 0 &&
                            grid[curGridCell.Y, curGridCell.X - 1].PathOrientation != PathOrientation.None)
                        {
                            curGridCell = grid[curGridCell.Y, curGridCell.X - 1];
                        }
                        else
                        {
                            curGridCell = grid[curGridCell.Y, curGridCell.X + 1];
                        }
                    }
                }
                else if (curGridCell.PathOrientation == PathOrientation.Horizontal)
                {
                    if (curGridCell.Y < mazeHeight - 1 &&
                        grid[curGridCell.Y + 1, curGridCell.X].PathOrientation != PathOrientation.None)
                    {
                        grid[curGridCell.Y, curGridCell.X].SouthWall = true;
                    }

                    if (prevGridCell.X < curGridCell.X)
                    {
                        prevGridCell = curGridCell;
                        curGridCell = grid[curGridCell.Y, curGridCell.X + 1];
                    }
                    else
                    {
                        prevGridCell = curGridCell;
                        curGridCell = grid[curGridCell.Y, curGridCell.X - 1];
                    }
                }
                else
                {
                    prevGridCell = curGridCell;
                    curGridCell = grid[curGridCell.Y + 1, curGridCell.X];
                }
            } while (curGridCell.X != mazeWidth - 1 || curGridCell.Y != mazeHeight - 1);

            return subMazes;
        }

        private static bool IsRightSubmaze(MazeStructureGridCell[,] grid, int row, int width,
            out Point2DInt mazeStartPoint)
        {
            mazeStartPoint = new Point2DInt();

            // Loop through each cell in the row and find the right-most trajectory cell
            for (int pos = 0; pos < width; pos++)
            {
                if (grid[row, pos].PathOrientation != PathOrientation.None)
                {
                    // Set the start point to the cell to the right of the juncture
                    mazeStartPoint = new Point2DInt(pos + 1, row);
                }
            }

            // A valid starting location has been found if the X location is between the
            // first and last column
            return mazeStartPoint.X > 0 && mazeStartPoint.X < width;
        }

        private static bool IsRightMazeEndRow(MazeStructureGridCell[,] grid, int row, int height, int width, int column,
            out Point2DInt mazeEndPoint)
        {
            mazeEndPoint = new Point2DInt();
            bool isEndRow = false;

            // Check for sub-maze to right of L-intersection (first) and inverted L-intersection (second)
            if (
                (grid[row, column].PathOrientation != PathOrientation.None &&
                 grid[row - 1, column].PathOrientation == PathOrientation.None) ||
                (grid[row, column - 1].PathOrientation == PathOrientation.None &&
                 grid[row - 1, column - 1].PathOrientation != PathOrientation.None))
            {
                mazeEndPoint = new Point2DInt(width - 1, row - 1);
                isEndRow = true;
            }

            return isEndRow;
        }

        private static int DetermineLeftSubmazeEndPosition(MazeStructureGridCell[,] grid, int row)
        {
            int pos = 0;

            // Increment until trajectory point cutting off the left submaze is found
            do
            {
                pos++;
            } while (grid[row, pos].PathOrientation == PathOrientation.None);

            // Return the difference between the intersecting trajectory point and the submaze start
            return pos - 1;
        }

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
                mazeEndPoint = grid[row, subMazeEndPos + 1].PathOrientation == PathOrientation.None
                    ? new Point2DInt(subMazeEndPos, row - 1)
                    : new Point2DInt(subMazeEndPos, row);
                isEndRow = true;
            }
            // Check for sub-maze to left of L-intersection (first) and inverted L-intersection (second)
            else if (grid[row, subMazeEndPos + 1].PathOrientation == PathOrientation.None ||
                     grid[row, subMazeEndPos].PathOrientation != PathOrientation.None)
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

        private static bool DoesPathOverlap(MazeStructureGridCell[,] grid, Point2DInt startPoint, Point2DInt endPoint,
            IntersectionOrientation orientation)
        {
            if (IntersectionOrientation.Horizontal == orientation)
            {
                // Check for path overlaps when traversing y-direction
                for (int yCoord = startPoint.Y;
                    startPoint.Y < endPoint.Y
                        ? yCoord <= endPoint.Y
                        : yCoord >= endPoint.Y;
                    yCoord += startPoint.Y < endPoint.Y ? 1 : -1)
                {
                    if ((PathOrientation.None != grid[yCoord, startPoint.X].PathOrientation ||
                         grid[yCoord, startPoint.X].IsWayPoint) &&
                        false == grid[yCoord, startPoint.X].Equals(startPoint) &&
                        false == grid[yCoord, startPoint.X].Equals(endPoint))
                    {
                        return true;
                    }
                }

                // Check for path overlaps when traversing x-direction
                for (int xCoord = startPoint.X;
                    startPoint.X < endPoint.X
                        ? xCoord <= endPoint.X
                        : xCoord >= endPoint.X;
                    xCoord += startPoint.X < endPoint.X ? 1 : -1)
                {
                    if ((PathOrientation.None != grid[endPoint.Y, xCoord].PathOrientation ||
                         grid[endPoint.Y, xCoord].IsWayPoint) && false == grid[endPoint.Y, xCoord].Equals(startPoint) &&
                        false == grid[endPoint.Y, xCoord].Equals(endPoint))
                    {
                        return true;
                    }
                }
            }
            else if (IntersectionOrientation.Vertical == orientation)
            {
                // Check for path overlaps when traversing x-direction
                for (int xCoord = startPoint.X;
                    startPoint.X < endPoint.X
                        ? xCoord <= endPoint.X
                        : xCoord >= endPoint.X;
                    xCoord += startPoint.X < endPoint.X ? 1 : -1)
                {
                    if ((PathOrientation.None != grid[startPoint.Y, xCoord].PathOrientation ||
                         grid[startPoint.Y, xCoord].IsWayPoint) &&
                        false == grid[startPoint.Y, xCoord].Equals(startPoint) &&
                        false == grid[startPoint.Y, xCoord].Equals(endPoint))
                    {
                        return true;
                    }
                }

                // Check for path overlaps when traversing y-direction
                for (int yCoord = startPoint.Y;
                    startPoint.Y < endPoint.Y
                        ? yCoord <= endPoint.Y
                        : yCoord >= endPoint.Y;
                    yCoord += startPoint.Y < endPoint.Y ? 1 : -1)
                {
                    if ((PathOrientation.None != grid[yCoord, endPoint.X].PathOrientation ||
                         grid[yCoord, endPoint.X].IsWayPoint) && false == grid[yCoord, endPoint.X].Equals(startPoint) &&
                        false == grid[yCoord, endPoint.X].Equals(endPoint))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Helper methods

        /// <summary>
        ///     Averages out the number of partitions possible given the evolved maze dimensions.
        /// </summary>
        /// <param name="mazeHeight">The height of the maze.</param>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <param name="numSamples">
        ///     The number of sample mazes to attempt partitioning out to get a representative average
        ///     (defaults to 2,000).
        /// </param>
        /// <returns>The average number of partitions supportable within the given maze dimensions.</returns>
        public static int DetermineMaxPartitions(int mazeHeight, int mazeWidth, int numSamples = 2000)
        {
            var maxMazeResolutions = new int[numSamples];
            var rng = new FastRandom();

            // Fully partition maze space for the specified number of samples
            for (var curSample = 0; curSample < numSamples; curSample++)
            {
                var mazeRoomQueue = new Queue<MazeStructureRoom>();
                var mazeSegments = new MazeStructureGridCell[mazeHeight, mazeWidth];
                var partitionCount = 0;

                // Queue up the first "room" (which will encompass the entirety of the maze grid)
                mazeRoomQueue.Enqueue(new MazeStructureRoom(0, 0, mazeWidth, mazeHeight));

                // Iterate until there are no more available sub fields in the queue
                while (mazeRoomQueue.Count > 0)
                {
                    // Dequeue a room and run division on it
                    var subRooms = mazeRoomQueue.Dequeue()
                        .DivideRoom(mazeSegments, rng.NextDoubleNonZero(),
                            rng.NextDoubleNonZero(),
                            rng.NextDoubleNonZero() > 0.5);

                    if (subRooms != null)
                    {
                        // Increment the count of partitions
                        partitionCount++;

                        // Get the two resulting sub rooms and enqueue both of them
                        if (subRooms.Item1 != null) mazeRoomQueue.Enqueue(subRooms.Item1);
                        if (subRooms.Item2 != null) mazeRoomQueue.Enqueue(subRooms.Item2);
                    }
                }

                // Record the total number of maze partitions
                maxMazeResolutions[curSample] = partitionCount;
            }

            // Return the average number of partitions associated with a fully partitioned maze
            return (int) maxMazeResolutions.Average(partition => partition);
        }

        /// <summary>
        ///     Averages out the number of partitions possible given a partially partitioned maze genome as a starting point. The
        ///     higher the maze complexity, the more exact the max partitions estimate will be. If the maze is only one wall away
        ///     from max complexity, the return value will be exact.
        /// </summary>
        /// <param name="mazeGenome">The partially complexified genome from which to start max partition estimation.</param>
        /// <param name="numSamples">
        ///     The number of sample mazes to attempt partitioning out to get a representative average
        ///     (defaults to 2,000).
        /// </param>
        /// <returns>
        ///     The average number of partitions supportable given the existing maze genome complexity/wall placement and
        ///     dimensions.
        /// </returns>
        public static int DetermineMaxPartitions(MazeGenome mazeGenome, int numSamples = 2000)
        {
            int[] maxMazeResolutions = new int[numSamples];
            FastRandom rng = new FastRandom();

            // First call maze grid conversion method to decode existing genome
            MazeStructureGrid mazeGrid = ConvertMazeGenomeToUnscaledStructure(mazeGenome);

            // Fully partition the remainder maze space for the specified number of samples
            for (int curSample = 0; curSample < numSamples; curSample++)
            {
                // Seed the queue with existing maze sub-spaces
                Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>(mazeGrid.MazeRooms);

                // Initialize the partition count and grid to the current number of partitions and current maze grid respectively
                int partitionCount = mazeGrid.NumPartitions;
                MazeStructureGridCell[,] mazeSegments = mazeGrid.Grid;

                // Iterate until there are no more available sub fields in the queue
                while (mazeRoomQueue.Count > 0)
                {
                    // Dequeue a room and run division on it
                    Tuple<MazeStructureRoom, MazeStructureRoom> subRooms = mazeRoomQueue.Dequeue()
                        .DivideRoom(mazeSegments, rng.NextDoubleNonZero(),
                            rng.NextDoubleNonZero(),
                            rng.NextDoubleNonZero() > 0.5);

                    if (subRooms != null)
                    {
                        // Increment the count of partitions
                        partitionCount++;

                        // Get the two resulting sub rooms and enqueue both of them
                        if (subRooms.Item1 != null) mazeRoomQueue.Enqueue(subRooms.Item1);
                        if (subRooms.Item2 != null) mazeRoomQueue.Enqueue(subRooms.Item2);
                    }
                }

                // Record the total number of maze partitions
                maxMazeResolutions[curSample] = partitionCount;
            }

            // Return the average number of partitions associated with a fully partitioned maze
            return (int) maxMazeResolutions.Average(partition => partition);
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

            // Build map of sub-mazes and their assigned internal walls
            Dictionary<MazeStructureRoom, List<WallGene>> subMazeWallsMap = ExtractMazeWallMap(subMazes,
                genome.WallGeneList);

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

                // TODO: This whole thing needs to be updated to iterate through wall gene list and loop back around as necessary
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
            var sortedPathGeneList = new List<PathGene>(genome.PathGeneList.Count);

            // List indexed by vertical position, indicating trajectory direction for that row
            bool[] easterlyRowTrajectories = new bool[genome.MazeBoundaryHeight];

            // Starting location will always be at the top left corner
            Point2DInt startLocation = new Point2DInt(0, 0);

            // Ending location will always be at the bottom right corner
            Point2DInt targetLocation = new Point2DInt(genome.MazeBoundaryWidth - 1, genome.MazeBoundaryHeight - 1);

            // Initialize the grid
            MazeStructureGridCell[,] unscaledGrid = InitializeMazeGrid(genome.MazeBoundaryHeight,
                genome.MazeBoundaryWidth);

            // Order points vertically and then horizontally (hoping for a winding trajectory with this ordering)
            foreach (
                var verticalGroup in
                    genome.PathGeneList.OrderBy(g => g.Waypoint.Y)
                        .GroupBy(g => g.Waypoint.Y).Distinct().ToList())
            {
                // Sort in horizontal ascending order if this is the first row
                if (sortedPathGeneList.Count == 0)
                {
                    sortedPathGeneList.AddRange(verticalGroup.OrderBy(g => g.Waypoint.X));

                    // Set easterly orientation for first row
                    easterlyRowTrajectories[sortedPathGeneList.First().Waypoint.Y] = true;
                }
                else
                {
                    // Get last waypoint in previous row
                    Point2DInt prevPoint = sortedPathGeneList.Last().Waypoint;

                    // Get left-most point in current row
                    Point2DInt leftmostCurPoint = verticalGroup.OrderBy(g => g.Waypoint.X).First().Waypoint;

                    // Get right-most point in current row
                    Point2DInt rightmostCurPoint = verticalGroup.OrderByDescending(g => g.Waypoint.X).First().Waypoint;

                    // If this is not the last row and either the max X-value for the previous row is greater 
                    // than or equal to the max X-value for the current row OR if the max X-value for the 
                    // previous row is between the min and max X-values on the current row AND there's only 
                    // one row separating the two, then sort the current row in descending order on the horizontal 
                    // axis
                    if (verticalGroup.Key < genome.MazeBoundaryHeight - 1 &&
                        (prevPoint.X >= rightmostCurPoint.X ||
                         (prevPoint.X > leftmostCurPoint.X && prevPoint.X <= rightmostCurPoint.X &&
                          easterlyRowTrajectories[prevPoint.Y])))
                    {
                        sortedPathGeneList.AddRange(verticalGroup.OrderByDescending(g => g.Waypoint.X));
                        easterlyRowTrajectories[leftmostCurPoint.Y] = false;
                    }
                    // Otherwise, sort in ascending order
                    else
                    {
                        sortedPathGeneList.AddRange(verticalGroup.OrderBy(g => g.Waypoint.X));
                        easterlyRowTrajectories[leftmostCurPoint.Y] = true;
                    }
                }
            }

            for (int idx = 0; idx <= genome.PathGeneList.Count; idx++)
            {
                // Get the previous point (if first iteration, previous point is the start location)
                Point2DInt prevPoint = idx == 0
                    ? startLocation
                    : sortedPathGeneList[idx - 1].Waypoint;

                // Get the current point (if last iteration, current point is the target location)
                Point2DInt curPoint = idx == genome.PathGeneList.Count
                    ? targetLocation
                    : sortedPathGeneList[idx].Waypoint;

                // Get the next point (if exists)
                Point2DInt nextPoint = idx + 1 >= genome.PathGeneList.Count
                    ? targetLocation
                    : sortedPathGeneList[idx + 1].Waypoint;

                // TODO: This may be problematic if the waypoint is on the left side and has a horizontal orientation
                // Get the orientation
                IntersectionOrientation curOrientation = DetermineWaypointIntersectionOrientation(prevPoint, curPoint,
                    nextPoint,
                    idx == 0
                        ? sortedPathGeneList[idx].DefaultOrientation
                        : sortedPathGeneList[idx - 1].DefaultOrientation,
                    idx == genome.PathGeneList.Count
                        ? sortedPathGeneList[idx - 1].DefaultOrientation
                        : sortedPathGeneList[idx].DefaultOrientation, unscaledGrid);

                // Modify intersection orientation on path gene to denote updated orientation
                if (idx < genome.PathGeneList.Count)
                {
                    sortedPathGeneList[idx].DefaultOrientation = curOrientation;
                }

                // Mark current waypoint
                unscaledGrid[curPoint.Y, curPoint.X].IsWayPoint = true;

                // If there are no overlapping paths, use 
                MarkSolutionPathSegment(unscaledGrid, prevPoint, curPoint, nextPoint, curOrientation,
                    easterlyRowTrajectories[prevPoint.Y]);
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
        
        #endregion
    }
}