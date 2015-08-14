using System;

namespace SharpNeat.Domains.MazeNavigation.Components
{
    public enum MazeVariant
    {
        MEDIUM_MAZE,

        HARD_MAZE
    };

    public static class MazeVariantUtl
    {
        public static MazeVariant convertStringToMazeVariant(String strMazeVariant)
        {
            if (MazeVariant.MEDIUM_MAZE.ToString().Equals(strMazeVariant, StringComparison.InvariantCultureIgnoreCase))
            {
                return MazeVariant.MEDIUM_MAZE;
            }
            return MazeVariant.HARD_MAZE;
        }
    }
}