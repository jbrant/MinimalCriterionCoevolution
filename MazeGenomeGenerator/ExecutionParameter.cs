﻿namespace MazeGenomeGenerator
{
    /// <summary>
    ///     Runtime parameters that can be set upon application invocation.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     The number of interior walls to be encoded in the maz.
        /// </summary>
        NumWalls,

        /// <summary>
        ///     The number of sample mazes to generate.
        /// </summary>
        NumMazes,

        /// <summary>
        ///     The directory into which to output the maze genomes.
        /// </summary>
        MazeGenomeOutputBaseDirectory,

        /// <summary>
        ///     The height of the maze being generated.
        /// </summary>
        MazeHeight,

        /// <summary>
        ///     The width of the maze being generated.
        /// </summary>
        MazeWidth,

        /// <summary>
        ///     The scale by which to scale out the maze (i.e. the multiplier factor for enlarging the maze structure).
        /// </summary>
        MazeScaleFactor,

        /// <summary>
        ///     Whether or not to output a bitmap image of the generated maze.
        /// </summary>
        OutputMazeBitmap,

        /// <summary>
        ///     The base directory into which to write the bitmap image trajectories.
        /// </summary>
        BitmapOutputBaseDirectory
    }
}