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
                int[,] mazeSegments = new int[mazeHeight, mazeWidth];
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
                int[,] mazeSegments = mazeGrid.Grid;

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
            int[,] unscaledGrid = new int[genome.MazeBoundaryHeight, genome.MazeBoundaryWidth];
            int partitionCount = 0;

            // Queue up the first "room" (which will encompass the entirety of the maze grid)
            mazeRoomQueue.Enqueue(new MazeStructureRoom(0, 0, genome.MazeBoundaryWidth, genome.MazeBoundaryHeight));

            // Iterate through all of the genes, generating 
            foreach (MazeGene mazeGene in genome.GeneList)
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
    }
}