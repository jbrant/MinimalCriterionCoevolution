#region

using System;

#endregion

namespace SharpNeat.Domains.MazeNavigation.Components
{
    /// <summary>
    ///     Defines the variant of maze environments to use (hard, medium, etc.) for a given maze navigation experiment.  Each
    ///     of these variants entail a certain environment configuration.
    /// </summary>
    public enum MazeVariant
    {
        /// <summary>
        ///     Indicates the medium maze variant.
        /// </summary>
        MediumMaze,

        /// <summary>
        ///     Indicates the hard maze variant.
        /// </summary>
        HardMaze,

        /// <summary>
        ///     Indicates an open-ended version of the medium maze.
        /// </summary>
        OpenEndedMediumMaze,

        /// <summary>
        ///     Indicates an open-ended version of the hard maze.
        /// </summary>
        OpenEndedHardMaze
    };

    /// <summary>
    ///     Provides utility methods for maze variants.
    /// </summary>
    public static class MazeVariantUtil
    {
        /// <summary>
        ///     Determines the appropriate maze variant based on the given string value.
        /// </summary>
        /// <param name="strMazeVariant">The string-valued maze variant.</param>
        /// <returns>The maze variant domain type.</returns>
        public static MazeVariant convertStringToMazeVariant(String strMazeVariant)
        {
            if ("MediumMaze".Equals(strMazeVariant, StringComparison.InvariantCultureIgnoreCase) ||
                "Medium Maze".Equals(strMazeVariant, StringComparison.InvariantCultureIgnoreCase))
            {
                return MazeVariant.MediumMaze;
            }
            if ("OpenEndedMediumMaze".Equals(strMazeVariant, StringComparison.InvariantCultureIgnoreCase) ||
                "Open Ended Medium Maze".Equals(strMazeVariant, StringComparison.InvariantCultureIgnoreCase))
            {
                return MazeVariant.OpenEndedMediumMaze;
            }
            if ("OpenEndedHardMaze".Equals(strMazeVariant, StringComparison.InvariantCultureIgnoreCase) ||
                "Open Ended Hard Maze".Equals(strMazeVariant, StringComparison.InvariantCultureIgnoreCase))
            {
                return MazeVariant.OpenEndedHardMaze;
            }
            return MazeVariant.HardMaze;
        }
    }
}