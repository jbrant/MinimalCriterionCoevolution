namespace MazeGenomeGenerator
{
    /// <summary>
    ///     Runtime parameters that can be set upon application invocation.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     Indicates whether the mazes are to be generated (then subsuquently rendered as an image) or just read in from an
        ///     existing genome specification and rendered.
        /// </summary>
        GenerateMazes,

        /// <summary>
        ///     The number of path waypoints to be encoded in the maze.
        /// </summary>
        NumWaypoints,

        /// <summary>
        ///     The number of interior walls to be encoded in the maze.
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
        /// The maximum quadrant height of the maze being generated.
        /// </summary>
        MazeQuadrantHeight,
        
        /// <summary>
        /// The maximum quadrant width of the maze being generated.
        /// </summary>
        MazeQuadrantWidth,

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
        BitmapOutputBaseDirectory,

        /// <summary>
        ///     The path to the maze genome to render (only applicable when we're just rendering an existing maze genome instead of
        ///     generating the genome).
        /// </summary>
        MazeGenomeFile,

        /// <summary>
        ///     Indicates whether the maze genomes should be serialized to the same file or not.
        /// </summary>
        SingleGenomeOutputFile
    }
}