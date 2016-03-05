#region

using System;
using MazeGenerationTester.RecursiveDivision;

#endregion

namespace MazeGenerationTester
{
    public class RecursiveDivisionMazeGenerationExecutor
    {
        private const int Width = 20;
        private const int Height = 20;
        private const bool isBreadthFirst = true;
        private static readonly Random RandomNumGenerator = new Random(12345678);

        public static void Main(string[] args)
        {
            if (isBreadthFirst)
            {
                int numIterations = 400;
                int imageScaleMultiplier = 15;

                RecursiveMazeGenerationBreadthFirst mazeGenerationBreadthFirst =
                    new RecursiveMazeGenerationBreadthFirst(Width, Height, RandomNumGenerator);

                mazeGenerationBreadthFirst.RunBreadthFirstGeneration(numIterations);

                MazeUtility.DisplayMaze(mazeGenerationBreadthFirst.Grid);

                MazeUtility.PrintBitmapMaze(
                    MazeUtility.ExtractLineSegments(mazeGenerationBreadthFirst.Grid, imageScaleMultiplier),
                    Width*imageScaleMultiplier, Height*imageScaleMultiplier);
            }
            else
            {
                RecursiveDivisionMazeGenerationDepthFirst mazeGenerationDepthFirst =
                    new RecursiveDivisionMazeGenerationDepthFirst(Width, Height);

                // Begin division
                mazeGenerationDepthFirst.Divide(0, 0, Width, Height,
                    MazeUtility.ChooseOrientation(Width, Height, RandomNumGenerator), 0);

                // Print the maze
                MazeUtility.DisplayMaze(mazeGenerationDepthFirst.Grid);

                //Utility.PrintBitmapMaze(Utility.ExtractLineSegments(mazeGenerationDepthFirst.Grid), Width, Height);
            }
        }
    }
}