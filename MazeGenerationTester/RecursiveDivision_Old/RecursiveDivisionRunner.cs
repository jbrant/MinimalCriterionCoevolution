#region

using System;
using System.Diagnostics;

#endregion

namespace MazeGenerationTester
{
    internal class RecursiveDivisionRunner
    {
        private static void Main(string[] args)
        {
            RecursiveDivision recursiveDivision = new RecursiveDivision(30, 30);

            // Kick off algorithm
            recursiveDivision.MakeMaze();

            recursiveDivision.PrintMaze();
        }

        private class RecursiveDivision
        {
            // NOTE: THESE ARE FOR DISPLAY ONLY
            private static readonly char MAZE_WALL = '#';
            private static readonly char MAZE_PATH = ' ';
            private readonly int act_rows;
            // NOTE: THESE ARE FOR DISPLAY ONLY
            private readonly char[,] board;
            private readonly int cols;
            private readonly int rows;
            private int act_cols;

            public RecursiveDivision(int row, int col)
            {
                // Initialize instance variables
                rows = row*2 + 1;
                cols = col*2 + 1;
                act_rows = row;
                act_cols = col;
                board = new char[rows, cols];

                // Start with an empty maze
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        board[i, j] = MAZE_PATH;
                    }
                }

                // Record characters for left and right outer walls
                for (int i = 0; i < rows; i++)
                {
                    board[i, 0] = MAZE_WALL;
                    board[i, cols - 1] = MAZE_WALL;
                }

                // Record characters for top and bottom outer walls
                for (int i = 0; i < cols; i++)
                {
                    board[0, i] = MAZE_WALL;
                    board[rows - 1, i] = MAZE_WALL;
                }
            }

            // This is sort of a wrapper
            public void MakeMaze()
            {
                MakeMaze(0, cols - 1, 0, rows - 1);
                MakeOpenings();
            }

            // This is what actually creates the maze
            private void MakeMaze(int left, int right, int top, int bottom)
            {
                // Why aren't these absolute values?
                int width = right - left;
                int height = bottom - top;

                // The below is true if there is room for division of both height and width
                if (width > 2 && height > 2)
                {
                    // If the subfield is wider than it is tall, we want to vertically bisect it
                    if (width > height)
                    {
                        DivideVertical(left, right, top, bottom);
                    }

                    // If the subfield is taller than it is wide, we want to horizontally bisect it
                    else if (height > width)
                    {
                        DivideHorizontal(left, right, top, bottom);
                    }

                    // Otherwise, the dimensions are equal, so randomly choose bisection orientation
                    else
                    {
                        Random random = new Random();
                        bool chooseVertical = random.Next(100) >= 50;

                        if (chooseVertical)
                        {
                            DivideVertical(left, right, top, bottom);
                        }
                        else
                        {
                            DivideHorizontal(left, right, top, bottom);
                        }
                    }
                }

                // The below applies to the case where there's sufficient room horizontally but not vertically
                else if (width > 2 && height <= 2)
                {
                    // Since the area is naturally wider than it is tall, we bisect vertically
                    DivideVertical(left, right, top, bottom);
                }

                // The below applies to the case where there's sufficient room vertically but not horizontally
                else if (width <= 2 && height > 2)
                {
                    // Since the area is naturally taller than it is wide, we bisect horizontally
                    DivideHorizontal(left, right, top, bottom);
                }
            }

            private void DivideVertical(int left, int right, int top, int bottom)
            {
                Random random = new Random();

                // Locate a random point at which to begin the bisection (note that this must be even)
                // TODO: This is part of what will be evolved
                int divide = left + 2 + random.Next((right - left - 1)/2)*2;

                // Ensure that division point is even
                if (divide % 2 == 1)
                {
                    divide++;
                }

                // Draw a vertical separator (line) at every row for the given column at which division is being performed
                for (int i = top; i < bottom; i++)
                {
                    board[i, divide] = MAZE_WALL;
                }

                // Choose a random integer between the top and bottom and clear that cell
                // TODO: This will differ in a couple of ways during evolution
                // TODO: 1. The point at which the cleared space begins will be evolved
                // TODO: 2. The first division needs to be handled as a special case wherein division is potentially made in middle of wall
                // TODO:    (though this may actually be permissible for other bisecting walls as well since both sides would effectively be attached to something)
                int clearSpace = top + random.Next((bottom - top)/2)*2 + 1;

                // Clear a cell at the given row "clear space" on the bisection line
                // TODO: This really only clears a single cell - we may want larger gaps than this
                board[clearSpace, divide] = MAZE_PATH;

                // Make maze in the left subfield
                MakeMaze(left, divide, top, bottom);

                // Make maze in the right subfield
                MakeMaze(divide, right, top, bottom);
            }

            private void DivideHorizontal(int left, int right, int top, int bottom)
            {
                Random random = new Random();

                // Locate a random point at which to begin the bisection (note that this must be even)
                // TODO: This is part of what will be evolved
                int divide = top + 2 + random.Next((bottom - top - 1)/2)*2;

                // Ensure that division point is even
                if (divide%2 == 1)
                {
                    divide++;
                }

                // Draw a horizontal separator (line) at every row for the given row at which division is being performed
                for (int i = left; i < right; i++)
                {
                    board[divide, i] = MAZE_WALL;
                }

                // Choose a random integer between the left and right and clear that cell
                // TODO: This will differ in a couple of ways during evolution
                // TODO: 1. The point at which the cleared space begins will be evolved
                // TODO: 2. The first division needs to be handled as a special case wherein division is potentially made in middle of wall
                // TODO:    (though this may actually be permissible for other bisecting walls as well since both sides would effectively be attached to something)
                int clearSpace = left + random.Next((right - left)/2)*2 + 1;

                // Clear a cell at the given row "clear space" on the bisection line
                // TODO: This really only clears a single cell - we may want larger gaps than this
                board[divide, clearSpace] = MAZE_PATH;

                // Make maze in the top subfield
                MakeMaze(left, right, top, divide);

                // Make maze in the bottom subfield
                MakeMaze(left, right, divide, bottom);
            }

            private void MakeOpenings()
            {
                Random rand = new Random();
                Random rand2 = new Random();

                // Create a random location for the entrance and exit
                // TODO: Perhaps this should be also incorporated into the evolved variant for determining start and goal
                int entranceRow = rand.Next(act_rows - 1)*2 + 1;
                int exitRow = rand2.Next(act_rows - 1)*2 + 1;

                // Clear that space on the maze output
                board[entranceRow, 0] = MAZE_PATH;
                board[exitRow, cols - 1] = MAZE_PATH;
            }

            public void PrintMaze()
            {
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        Debug.Write(board[i, j]);
                    }
                    Debug.WriteLine("");
                }
            }

            public char[,] GetMaze()
            {
                return board;
            }
        }
    }
}