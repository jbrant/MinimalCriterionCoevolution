namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     Cardinal direction of the wall segment from the current position.  If the wall is to the right, the direction is
    ///     east.  If the wall is underneath (i.e. at the bottom of the cell), the direction is south.
    /// </summary>
    public enum WallDirection
    {
        /// <summary>
        ///     A wall direction is south if it is at the bottom of the current cell.
        /// </summary>
        South = 1,

        /// <summary>
        ///     A wall direction is east if it is on the righmost portion of the current cell.
        /// </summary>
        East
    }
}