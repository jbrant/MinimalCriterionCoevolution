namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     The orientation of the wall at the given location.  Note that a single cell can contain both a horizontal and
    ///     vertical wall.
    /// </summary>
    public enum WallOrientation
    {
        /// <summary>
        ///     Horizontal wall orientation.
        /// </summary>
        Horizontal = 1,

        /// <summary>
        ///     Vertical wall orientation.
        /// </summary>
        Vertical,

        /// <summary>
        ///     Both a horizontal and vertical wall orientation for intersecting walls.
        /// </summary>
        Both
    }
}