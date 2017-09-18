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

        private static void MarkSolutionPath(MazeStructureGridCell[,] grid, Point2DInt startPoint, Point2DInt endPoint,
            IntersectionOrientation orientation)
        {
            if (IntersectionOrientation.Horizontal == orientation)
            {
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
            }
            // Mark path for vertical intersection
            else
            {
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
            }
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
                         grid[yCoord, startPoint.X].IsJuncture) &&
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
                         grid[endPoint.Y, xCoord].IsJuncture) && false == grid[endPoint.Y, xCoord].Equals(startPoint) &&
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
                         grid[startPoint.Y, xCoord].IsJuncture) &&
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
                         grid[yCoord, endPoint.X].IsJuncture) && false == grid[yCoord, endPoint.X].Equals(startPoint) &&
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
                    grid[heightIdx, widthIdx] = new MazeStructureGridCell(widthIdx, heightIdx);
                }
            }

            return grid;
        }

        public static MazeStructureGridCell[,] BuildMazeSolutionPath(MazeGenome genome)
        {
            var sortedPathGeneList = new List<PathGene>(genome.PathGeneList.Count);

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
                    genome.PathGeneList.OrderBy(g => g.JuncturePoint.Y)
                        .GroupBy(
                            g =>
                                GetUnscaledCoordinates(g.JuncturePoint, genome.RelativeCellWidth,
                                    genome.RelativeCellHeight).Y)
                        .Distinct()
                        .ToList())
            {
                // Sort in horizontal ascending order if this is the first row
                if (sortedPathGeneList.Count == 0)
                {
                    sortedPathGeneList.AddRange(verticalGroup.OrderBy(g => g.JuncturePoint.X));
                }
                else
                {
                    // Get vertical level of last sorted entry
                    int prevY =
                        GetUnscaledCoordinates(sortedPathGeneList.Last().JuncturePoint, genome.RelativeCellWidth,
                            genome.RelativeCellHeight).Y;

                    // If the max X-value for the previous row is greater than the max X-value for the current row,
                    // sort this row in descending order on the horizontal axis
                    sortedPathGeneList.AddRange(sortedPathGeneList.Where(
                        g =>
                            prevY ==
                            GetUnscaledCoordinates(g.JuncturePoint, genome.RelativeCellWidth,
                                genome.RelativeCellHeight).Y)
                        .Max(
                            h =>
                                GetUnscaledCoordinates(h.JuncturePoint, genome.RelativeCellWidth,
                                    genome.RelativeCellHeight).X) >
                                                verticalGroup.ToList()
                                                    .Max(
                                                        h =>
                                                            GetUnscaledCoordinates(h.JuncturePoint,
                                                                genome.RelativeCellWidth,
                                                                genome.RelativeCellHeight).X)
                        ? verticalGroup.OrderByDescending(g => g.JuncturePoint.X)
                        : verticalGroup.OrderBy(g => g.JuncturePoint.X));
                }
            }

            for (int idx = 0; idx <= genome.PathGeneList.Count; idx++)
            {
                // Get the previous point (if first iteration, previous point is the start location)
                Point2DInt prevPoint = idx == 0
                    ? startLocation
                    : GetUnscaledCoordinates(sortedPathGeneList[idx - 1].JuncturePoint, genome.RelativeCellWidth,
                        genome.RelativeCellHeight);

                // Get the current point (if last iteration, current point is the target location)
                Point2DInt curPoint = idx == genome.PathGeneList.Count
                    ? targetLocation
                    : GetUnscaledCoordinates(sortedPathGeneList[idx].JuncturePoint, genome.RelativeCellWidth,
                        genome.RelativeCellHeight);

                // Get the orientation
                IntersectionOrientation curOrientation = idx == genome.PathGeneList.Count
                    ? sortedPathGeneList[idx - 1].DefaultOrientation
                    : sortedPathGeneList[idx].DefaultOrientation;

                // Mark current juncture point
                unscaledGrid[curPoint.Y, curPoint.X].IsJuncture = true;

                // If there are no overlapping paths, use 
                if (DoesPathOverlap(unscaledGrid, prevPoint, curPoint, curOrientation) == false)
                {
                    MarkSolutionPath(unscaledGrid, prevPoint, curPoint, curOrientation);
                }
                else if (DoesPathOverlap(unscaledGrid, prevPoint, curPoint,
                    curOrientation == IntersectionOrientation.Horizontal
                        ? IntersectionOrientation.Vertical
                        : IntersectionOrientation.Horizontal) == false)
                {
                    MarkSolutionPath(unscaledGrid, prevPoint, curPoint,
                        curOrientation == IntersectionOrientation.Horizontal
                            ? IntersectionOrientation.Vertical
                            : IntersectionOrientation.Horizontal);
                }
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

        #endregion
    }
}