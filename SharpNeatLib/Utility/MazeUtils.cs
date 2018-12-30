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
                // Handle the case where the start waypoint is below and to the left of the next waypoint 
                // (which would overlap existing components of the trajectory if it were allowed to ascend 
                // vertically then cut over to the right)
                if (endPoint.Y < curPoint.Y && endPoint.X > curPoint.X)
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
                // Handle the case where the start waypoint has a vertical orientation and would otherwise overlap
                else if (endPoint.Y > curPoint.Y && grid[curPoint.Y + 1, curPoint.X].PathDirection != PathDirection.None)
                {
                    // Set horizontal orientation on origin and one over to the right
                    grid[curPoint.Y, curPoint.X].PathDirection =
                        grid[curPoint.Y, curPoint.X + 1].PathDirection = PathDirection.East;

                    // Set juncture flag on new point
                    grid[curPoint.Y, curPoint.X + 1].IsJuncture = true;

                    // Move the starting X-coordinate 1 to the right
                    curPoint.X++;
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
                // Handles the case where the end waypoint is to the left of and below the start waypoint (intuition is that 
                // tracing to the left and down will overlap existing trajectories)
                if (endPoint.X < curPoint.X && endPoint.Y > curPoint.Y)
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
                else if (endPoint.X > curPoint.X && grid[curPoint.Y, curPoint.X + 1].PathDirection != PathDirection.None)
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
        private static IEnumerable<MazeStructureRoom> ExtractSubmazes(MazeStructureGridCell[,] grid, int mazeHeight,
            int mazeWidth, int maxSubmazeHeight = 5, int maxSubmazeWidth = 5)
        {
            var subMazes = new List<MazeStructureRoom>();

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
                                 grid[roomEndY + 1, roomStartX - 1].PathDirection == PathDirection.None))
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

                        // Compute the height and width of the submaze
                        var submazeHeight = roomEndY - roomStartY + 1;
                        var submazeWidth = roomEndX - roomStartX + 1;                        

                        // If submaze exceeds width and height constraints AND is greater than 1 unit in both height
                        // and width, split into equal-sized (or as close to equal as possible) quadrants
                        if ((submazeWidth > maxSubmazeWidth || submazeHeight > maxSubmazeHeight) && submazeWidth > 1 && submazeHeight > 1)
                        {
                            // Determine the quadrant sizes in which to break up the submaze based on the minimum of
                            // the height/width of the current submaze vs. the max allowable height/width
                            var quadrantHeight = Math.Min(submazeHeight, maxSubmazeHeight);
                            var quadrantWidth = Math.Min(submazeWidth, maxSubmazeWidth);

                            // If submaze is both taller and wider than allowable height/width, split into multiple
                            // quadrants stacked both horizontally and vertically
                            if (submazeHeight > maxSubmazeHeight && submazeWidth > maxSubmazeWidth)
                            {
                                for (var yQuadPos = roomStartY; yQuadPos < submazeHeight + roomStartY; yQuadPos += quadrantHeight)
                                {
                                    for (var xQuadPos = roomStartX; xQuadPos < submazeWidth + roomStartX; xQuadPos += quadrantWidth)
                                    {
                                        subMazes.Add(new MazeStructureRoom(xQuadPos, yQuadPos,
                                            xQuadPos + quadrantWidth > roomEndX + 1
                                                ? quadrantWidth - (xQuadPos + quadrantWidth - roomEndX) + 1
                                                : quadrantWidth,
                                            yQuadPos + quadrantHeight > roomEndY + 1
                                                ? quadrantHeight - (yQuadPos + quadrantHeight - roomEndY) + 1
                                                : quadrantHeight));
                                    }
                                }
                            }
                            // If submaze is taller than allowable height, split into quadrants stacked vertically
                            else if (submazeHeight > maxSubmazeHeight)
                            {
                                for (var yQuadPos = roomStartY; yQuadPos < submazeHeight + roomStartY; yQuadPos += quadrantHeight)
                                {
                                    subMazes.Add(new MazeStructureRoom(roomStartX, yQuadPos, roomEndX - roomStartX + 1,
                                        yQuadPos + quadrantHeight > roomEndY + 1
                                            ? quadrantHeight - (yQuadPos + quadrantHeight - roomEndY) + 1
                                            : quadrantHeight));
                                }
                            }
                            // Otherwise, submaze is wider than allowable width, so split into quadrants stacked horizontally
                            else
                            {
                                for (var xQuadPos = roomStartX; xQuadPos < submazeWidth + roomStartX; xQuadPos += quadrantWidth)
                                {
                                    subMazes.Add(new MazeStructureRoom(xQuadPos, roomStartY,
                                        xQuadPos + quadrantWidth > roomEndX + 1
                                            ? quadrantWidth - (xQuadPos + quadrantWidth - roomEndX) + 1
                                            : quadrantWidth, roomEndY - roomStartY + 1));
                                }
                            }
                        }
                        else
                        {
                            // Add maze room
                            subMazes.Add(new MazeStructureRoom(roomStartX, roomStartY, roomEndX - roomStartX + 1,
                                roomEndY - roomStartY + 1));
                        }                        
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
            var maxPartitions = 0;
            var loopIter = 0;
            var wallGeneIdx = 0;

            // Construct maze grid with solution path generated from connected waypoints
            MazeStructureGridCell[,] mazeGrid = BuildMazeSolutionPath(mazeGenome);

            // Extract the "sub-mazes" that are induced by the solution trajectory
            IEnumerable<MazeStructureRoom> subMazes = ExtractSubmazes(mazeGrid, mazeGenome.MazeBoundaryHeight,
                mazeGenome.MazeBoundaryWidth);

            // Process all sub-mazes, iteratively bisecting the applicable maze room space
            foreach (var subMaze in subMazes)
            {
                Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>();

                // Get the current wall gene index
                wallGeneIdx = loopIter++ % mazeGenome.WallGeneList.Count;
                
                // Mark boundaries for current submaze, including perpendicular opening next to first 
                // internal partition (if one exists)
                if (subMaze.AreInternalWallsSupported() && mazeGenome.WallGeneList.Count > 0)
                {
                    subMaze.MarkRoomBoundaries(mazeGrid, mazeGenome.WallGeneList[wallGeneIdx].WallLocation,
                        mazeGenome.WallGeneList[wallGeneIdx].PassageLocation,
                        mazeGenome.WallGeneList[wallGeneIdx].OrientationSeed, mazeGenome.MazeBoundaryHeight, mazeGenome.MazeBoundaryWidth);
                }
                else
                {
                    subMaze.MarkRoomBoundaries(mazeGrid, mazeGenome.MazeBoundaryHeight, mazeGenome.MazeBoundaryWidth);
                }

                if (subMaze.AreInternalWallsSupported() && mazeGenome.WallGeneList.Count > 0)
                {
                    // Queue up the first "room" (which will encompass the entirety of the submaze grid)
                    mazeRoomQueue.Enqueue(subMaze);

                    // Make sure there are rooms left in the queue before attempting to dequeue and bisect
                    while (mazeRoomQueue.Count > 0)
                    {
                        // Get the next wall gene index
                        wallGeneIdx = loopIter++ % mazeGenome.WallGeneList.Count;
                        
                        // Dequeue a room and run division on it
                        Tuple<MazeStructureRoom, MazeStructureRoom> subRooms = mazeRoomQueue.Dequeue()
                            .DivideRoom(mazeGrid, mazeGenome.WallGeneList[wallGeneIdx].WallLocation,
                                mazeGenome.WallGeneList[wallGeneIdx].PassageLocation,
                                mazeGenome.WallGeneList[wallGeneIdx].OrientationSeed);

                        if (subRooms != null)
                        {
                            // Get the two resulting sub rooms and enqueue both of them
                            if (subRooms.Item1 != null) mazeRoomQueue.Enqueue(subRooms.Item1);
                            if (subRooms.Item2 != null) mazeRoomQueue.Enqueue(subRooms.Item2);
                        }
                    }
                }
            }

            // Return the maximum number of partitions applied in a submaze
            return loopIter - 1;
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
            var mazeRooms = new List<MazeStructureRoom>();
            var partitionCount = 0;
            var loopIter = 0;
            var wallGeneIdx = 0;

            // Extract the "sub-mazes" that are induced by the solution trajectory
            IEnumerable<MazeStructureRoom> subMazes = ExtractSubmazes(mazeGrid, genome.MazeBoundaryHeight,
                genome.MazeBoundaryWidth);

            // Mark walls between trajectory segments that are adjacent
            MarkAdjacentTrajectoryBoundaries(mazeGrid, genome.MazeBoundaryHeight, genome.MazeBoundaryWidth);

            // Process all sub-mazes, iteratively bisecting the applicable maze room space
            foreach (var subMaze in subMazes)
            {
                Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>();

                // Get the current wall gene index
                wallGeneIdx = loopIter++ % genome.WallGeneList.Count;
                
                // Mark boundaries for current submaze, including perpendicular opening next to first 
                // internal partition (if one exists)
                if (subMaze.AreInternalWallsSupported() && genome.WallGeneList.Count > 0)
                {
                    subMaze.MarkRoomBoundaries(mazeGrid, genome.WallGeneList[wallGeneIdx].WallLocation,
                        genome.WallGeneList[wallGeneIdx].PassageLocation,
                        genome.WallGeneList[wallGeneIdx].OrientationSeed, genome.MazeBoundaryHeight, genome.MazeBoundaryWidth);
                }
                else
                {
                    subMaze.MarkRoomBoundaries(mazeGrid, genome.MazeBoundaryHeight, genome.MazeBoundaryWidth);
                }

                if (subMaze.AreInternalWallsSupported() && genome.WallGeneList.Count > 0)
                {                    
                    // Queue up the first "room" (which will encompass the entirety of the submaze grid)
                    mazeRoomQueue.Enqueue(subMaze);

                    // Make sure there are rooms left in the queue before attempting to dequeue and bisect
                    while (mazeRoomQueue.Count > 0)
                    {
                        // Get the next wall gene index
                        wallGeneIdx = loopIter++ % genome.WallGeneList.Count;
                        
                        // Dequeue a room and run division on it
                        Tuple<MazeStructureRoom, MazeStructureRoom> subRooms = mazeRoomQueue.Dequeue()
                            .DivideRoom(mazeGrid, genome.WallGeneList[wallGeneIdx].WallLocation,
                                genome.WallGeneList[wallGeneIdx].PassageLocation,
                                genome.WallGeneList[wallGeneIdx].OrientationSeed);

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
        ///     Constructs the maze solution trajectory based on the given list of path genes (waypoints) and maze dimensions.
        /// </summary>
        /// <param name="pathGenes">The list of path (waypoint) genes.</param>
        /// <param name="mazeHeight">The height of the maze.</param>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <returns>The maze grid cells with cells in the path trajectory identified.</returns>
        public static MazeStructureGridCell[,] BuildMazeSolutionPath(IList<PathGene> pathGenes, int mazeHeight,
            int mazeWidth)
        {
            // Starting location will always be at the top left corner
            Point2DInt startLocation = new Point2DInt(0, 0);

            // Ending location will always be at the bottom right corner
            Point2DInt targetLocation = new Point2DInt(mazeWidth - 1, mazeHeight - 1);

            // Initialize the grid
            MazeStructureGridCell[,] unscaledGrid = InitializeMazeGrid(mazeHeight,
                mazeWidth);

            for (int idx = 0; idx <= pathGenes.Count; idx++)
            {
                // Get the previous point (if first iteration, previous point is the start location)
                Point2DInt prevPoint = idx == 0
                    ? startLocation
                    : pathGenes[idx - 1].Waypoint;

                // Get the current point (if last iteration, current point is the target location)
                Point2DInt curPoint = idx == pathGenes.Count
                    ? targetLocation
                    : pathGenes[idx].Waypoint;

                // Set the waypoint intersection orientation
                IntersectionOrientation curOrientation = curPoint.Equals(targetLocation)
                    ? pathGenes[idx - 1].DefaultOrientation
                    : pathGenes[idx].DefaultOrientation;

                // Mark current waypoint
                unscaledGrid[curPoint.Y, curPoint.X].IsWayPoint = true;

                // Denote the resulting solution path on the grid
                MarkSolutionPathSegment(unscaledGrid, prevPoint, curPoint, curOrientation,
                    idx > 0 ? pathGenes.Take(idx).Max(p => p.Waypoint.X) : prevPoint.X,
                    idx > 0 ? pathGenes.Take(idx).Max(p => p.Waypoint.Y) : prevPoint.Y);
            }

            return unscaledGrid;
        }

        /// <summary>
        ///     Constructs the maze solution trajectory based on the waypoints defined in the maze genome.
        /// </summary>
        /// <param name="genome">The maze genome.</param>
        /// <returns>The maze grid cells with cells in the path trajectory identified.</returns>
        public static MazeStructureGridCell[,] BuildMazeSolutionPath(MazeGenome genome)
        {
            return BuildMazeSolutionPath(genome.PathGeneList, genome.MazeBoundaryHeight, genome.MazeBoundaryWidth);
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
        /// <param name="geneId">
        ///     The unique ID (innovation ID) of the path gene. This is used to determine whether the mutation
        ///     results in a valid ordering.
        /// </param>
        /// <param name="waypointOrientation">The orientation of the proposed waypoint.</param>
        /// <returns>Boolean indicating whether the given point is valid per the maze boundary constraints.</returns>
        public static bool IsValidWaypointLocation(MazeGenome genome,
            Point2DInt waypointLocation, uint geneId, IntersectionOrientation waypointOrientation)
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
                ((genome.PathGeneList.Any(g => g.InnovationId < geneId && g.Waypoint.X + 1 >= waypointLocation.X) ==
                  false &&
                  (genome.PathGeneList.Any(g => g.Waypoint.Y == waypointLocation.Y) == false)) ||
                 (genome.PathGeneList.Any(g => g.InnovationId < geneId && g.Waypoint.Y + 1 >= waypointLocation.Y) ==
                  false &&
                  (genome.PathGeneList.Any(g => g.Waypoint.X == waypointLocation.X) == false))) &&

                // Check that new waypoint does not induce a trajectory overlap nor does it cause other waypoints to not be visited
                IsTrajectoryValid(genome.PathGeneList, geneId, waypointLocation, waypointOrientation,
                    genome.MazeBoundaryHeight, genome.MazeBoundaryWidth)
                ;
        }

        /// <summary>
        ///     Incorporates the candidate waypoint into the list of existing waypoints, builds out the resulting trajectory, and
        ///     walks that trajectory checking for overlaps. Also ensures that all waypoints in the maze are visited.
        /// </summary>
        /// <param name="waypoints">The existing list of path (waypoint) genes.</param>
        /// <param name="candidateWaypointId">The gene ID of the candidate waypoint.</param>
        /// <param name="candidateWaypointLocation">The cartesian coordinates of the candidate waypoint.</param>
        /// <param name="candidateWaypointOrientation">The orientation (i.e. vertical/horizontal) of the candidate waypoint.</param>
        /// <param name="mazeHeight">The unscaled maze height.</param>
        /// <param name="mazeWidth">The unscaled maze width.</param>
        /// <returns>Boolean indicator of whether the trajectory overlaps upon itself.</returns>
        public static bool IsTrajectoryValid(IList<PathGene> waypoints, uint candidateWaypointId,
            Point2DInt candidateWaypointLocation, IntersectionOrientation candidateWaypointOrientation, int mazeHeight,
            int mazeWidth)
        {
            bool isOverlap = false;

            // Make a copy of the wayoint list
            var modifiedWaypoints = new List<PathGene>(waypoints);

            // Create new path gene
            var candidatePathGene = new PathGene(candidateWaypointId, candidateWaypointLocation,
                candidateWaypointOrientation);

            // Replace the existing waypoint if matching waypoint ID already exists, otherwise
            // append new candidate waypoint to the list
            if (modifiedWaypoints.Any(g => g.InnovationId == candidateWaypointId))
            {
                modifiedWaypoints[
                    modifiedWaypoints.IndexOf(modifiedWaypoints.Single(g => g.InnovationId == candidateWaypointId))] =
                    candidatePathGene;
            }
            else
            {
                modifiedWaypoints.Add(candidatePathGene);
            }

            // Create list of visited waypoints
            List<Point2DInt> visitedWaypointLocations = new List<Point2DInt>();

            // Get all waypoints in the maze
            var mazeWaypointLocations = waypoints.Select(pg => new Point2DInt(pg.Waypoint.X, pg.Waypoint.Y)).ToList();

            // Build out maze grid
            var mazeGrid = BuildMazeSolutionPath(modifiedWaypoints, mazeHeight, mazeWidth);

            // Setup the start/end points
            var startCell = mazeGrid[0, 0];
            var endCell = mazeGrid[mazeGrid.GetLength(0) - 1, mazeGrid.GetLength(1) - 1];
            var curCell = startCell;
            var prevCell = curCell;

            // Walk the trajectory
            do
            {
                // If the current cell is a waypoint, add to the list of visited waypoints
                if (curCell.IsWayPoint)
                {
                    visitedWaypointLocations.Add(new Point2DInt(curCell.X, curCell.Y));
                }

                // If direction of current and previous cell don't match and neither are junctures, 
                // this means there's been a trajectory overlap so set overlap flag to true and break
                if (curCell.PathDirection != prevCell.PathDirection && curCell.IsJuncture == false &&
                    prevCell.IsJuncture == false)
                {
                    isOverlap = true;
                }
                else if (curCell.PathOrientation == PathOrientation.Vertical)
                {
                    prevCell = curCell;
                    curCell = curCell.PathDirection == PathDirection.North
                        ? mazeGrid[curCell.Y - 1, curCell.X]
                        : mazeGrid[curCell.Y + 1, curCell.X];
                }
                else
                {
                    prevCell = curCell;
                    curCell = curCell.PathDirection == PathDirection.West
                        ? mazeGrid[curCell.Y, curCell.X - 1]
                        : mazeGrid[curCell.Y, curCell.X + 1];
                }
            } while (!curCell.Equals(endCell) && isOverlap == false);

            // Get any unvisited waypoints
            var unvisitedWaypoints = mazeWaypointLocations.Except(visitedWaypointLocations);

            return isOverlap == false && !unvisitedWaypoints.Any();
        }

        #endregion
    }
}