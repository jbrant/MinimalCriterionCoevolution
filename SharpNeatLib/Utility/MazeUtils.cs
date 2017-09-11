﻿#region

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
            int[] maxMazeResolutions = new int[numSamples];
            FastRandom rng = new FastRandom();

            // Fully partition maze space for the specified number of samples
            for (int curSample = 0; curSample < numSamples; curSample++)
            {
                Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>();
                MazeStructureGridCell[,] mazeSegments = new MazeStructureGridCell[mazeHeight, mazeWidth];
                int partitionCount = 0;

                // Queue up the first "room" (which will encompass the entirety of the maze grid)
                mazeRoomQueue.Enqueue(new MazeStructureRoom(0, 0, mazeWidth, mazeHeight));

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
            foreach (WallGene mazeGene in genome.WallGeneList)
            {
                // Make sure there are rooms left in the queue before attempting to dequeue and bisect
                if (mazeRoomQueue.Count > 0)
                {
                    // Dequeue a room and run division on it
                    Tuple<MazeStructureRoom, MazeStructureRoom> subRooms = mazeRoomQueue.Dequeue()
                        .DivideRoom(unscaledGrid, mazeGene.WallLocation,
                            mazeGene.PassageLocation,
                            mazeGene.OrientationSeed);

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
                    grid[heightIdx, widthIdx] = new MazeStructureGridCell(heightIdx, widthIdx);
                }
            }

            return grid;
        }

        public static void BuildMazeSolutionPath(MazeGenome genome)
        {
            // Starting location will always be at the top left corner
            Point2DInt startLocation = new Point2DInt(1, 1);

            // Ending location will always be at the bottom right corner
            Point2DInt targetLocation = new Point2DInt(genome.MazeBoundaryWidth - 1, genome.MazeBoundaryHeight - 1);

            // Initialize the grid
            MazeStructureGridCell[,] unscaledGrid = InitializeMazeGrid(genome.MazeBoundaryHeight,
                genome.MazeBoundaryWidth);

            for (int idx = 0; idx < genome.PathGeneList.Count; idx++)
            {
                // Get the previous point
                Point2DInt prevPoint = idx == 0 ? startLocation : genome.PathGeneList[idx - 1].JuncturePoint;
                PathGene curPath = genome.PathGeneList[idx];

                // Mark the cells in the horizontal component of the path
                if (IntersectionOrientation.Horizontal == curPath.Orientation)
                {
                    for (int hIdx = prevPoint.X; hIdx <= curPath.JuncturePoint.X; hIdx++)
                    {
                        unscaledGrid[curPath.JuncturePoint.Y, hIdx].IsOnPath = true;
                    }
                }
                // Mark the cells in the vertical component of the path
                else
                {
                    for (int vIdx = prevPoint.X; vIdx <= curPath.JuncturePoint.X; vIdx++)
                    {
                        unscaledGrid[vIdx, curPath.JuncturePoint.X].IsOnPath = true;
                    }
                }
            }

            foreach (var pathGene in genome.PathGeneList)
            {
                // TODO: Call method to connect genes
            }
        }
    }
}