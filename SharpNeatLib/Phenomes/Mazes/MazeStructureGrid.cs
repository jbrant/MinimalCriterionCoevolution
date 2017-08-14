#region

using System.Collections.Generic;

#endregion

namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     The maze grid struct encapsulates the unscaled, two-dimensional maze grid along with the number of partitions
    ///     slicing the maze into its component sub-spaces.
    /// </summary>
    public struct MazeStructureGrid
    {
        #region Constructors

        /// <summary>
        ///     Constructor which accepts a two-dimensional maze grid and the number of partitions slicing the maze into
        ///     sub-spaces along with the currently partition sub-spaces themselves.
        /// </summary>
        /// <param name="grid">The two-dimensional maze grid.</param>
        /// <param name="numPartitions">The number of sub-spaces into which the maze is sliced.</param>
        /// <param name="mazeRooms">The subspaces into which the maze is currently partition.</param>
        public MazeStructureGrid(int[,] grid, int numPartitions, List<MazeStructureRoom> mazeRooms)
        {
            Grid = grid;
            MazeRooms = mazeRooms;
            NumPartitions = numPartitions;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The unscaled maze grid.
        /// </summary>
        public int[,] Grid { get; }

        /// <summary>
        ///     The subspaces into which the maze is currently partition.
        /// </summary>
        public List<MazeStructureRoom> MazeRooms { get; }

        /// <summary>
        ///     The number of partitions slicing the maze into sub-spaces.
        /// </summary>
        public int NumPartitions { get; }

        #endregion
    }
}