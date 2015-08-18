using System;

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
        HardMaze
    };

    /// <summary>
    ///     Provides utility methods for maze variants.
    /// </summary>
    public static class MazeVariantUtl
    {
        /// <summary>
        ///     Determines the appropriate maze variant based on the given string value.
        /// </summary>
        /// <param name="strMazeVariant">The string-valued maze variant.</param>
        /// <returns>The maze variant domain type.</returns>
        public static MazeVariant convertStringToMazeVariant(String strMazeVariant)
        {
            if (MazeVariant.MediumMaze.ToString().Equals(strMazeVariant, StringComparison.InvariantCultureIgnoreCase))
            {
                return MazeVariant.MediumMaze;
            }
            return MazeVariant.HardMaze;
        }
    }
}