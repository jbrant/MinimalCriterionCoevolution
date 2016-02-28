#region

using System;
using System.Diagnostics;

#endregion

namespace MazeGenerationTester
{
    public enum Direction
    {
        South = 1,
        East
    }

    public enum Orientation
    {
        Horizontal = 1,
        Vertical
    }


    public class RecursiveDivisionMazeGenerationExecutor
    {
        private const int Width = 10;
        private const int Height = 10;

        public static void Main(string[] args)
        {
            RecursiveDivisionMazeGeneration mazeGeneration = new RecursiveDivisionMazeGeneration(Width, Height);

            // Begin division
            mazeGeneration.Divide(0, 0, Width, Height, mazeGeneration.ChooseOrientation(Width, Height));
        }
    }

    public class RecursiveDivisionMazeGeneration
    {
        private readonly int[,] _grid;
        private readonly Random _randomGenerator = new Random(1234);

        public RecursiveDivisionMazeGeneration(int width, int height)
        {
            _grid = new int[height, width];
        }

        public void Divide(int x, int y, int curWidth, int curHeight, Orientation orientation)
        {
            // Short-circuit recursion
            if (curWidth < 2 || curHeight < 2)
                return;

            // Print current state of maze
            DisplayMaze();

            // Determine orientation
            bool isHorizontal = orientation == Orientation.Horizontal;

            // Determine starting location of wall
            // TODO: This will be evolved
            int xWallLocation = x + (isHorizontal ? 0 : _randomGenerator.Next(curWidth - 2));
            int yWallLocation = y + (isHorizontal ? _randomGenerator.Next(curHeight - 2) : 0);

            // Determine the location of the passage
            // TODO: This along with passage length will be evolved
            int xPassageLocation = xWallLocation + (isHorizontal ? _randomGenerator.Next(curWidth) : 0);
            int yPassageLocation = yWallLocation + (isHorizontal ? 0 : _randomGenerator.Next(curHeight));

            // Determine wall directional components
            int xDirection = isHorizontal ? 1 : 0;
            int yDirection = isHorizontal ? 0 : 1;

            // Determine length of wall (equivalent to the length of the subfield in the given direction)
            int wallLength = isHorizontal ? curWidth : curHeight;

            // Determine perpendicular direction
            Direction perpendicularDirection = isHorizontal ? Direction.South : Direction.East;

            // Notate where all of the wall segments are in the current wall
            for (int curWallCell = 0; curWallCell < wallLength; curWallCell++)
            {
                // If the current cell isn't in the defined passage, place a wall segment there
                if (xWallLocation != xPassageLocation || yWallLocation != yPassageLocation)
                {
                    // Bitwise or with perpendicular direction to get appropriate wall
                    _grid[yWallLocation, xWallLocation] |= (int) perpendicularDirection;
                }

                // Increment the wall segment location by the appropriate directional components
                xWallLocation += xDirection;
                yWallLocation += yDirection;
            }

            // Calculate new width/height for top/left part of maze
            int newWidth = isHorizontal ? curWidth : xWallLocation - x + 1;
            int newHeight = isHorizontal ? yWallLocation - y + 1 : curHeight;

            // Recurse down top/left subfield
            Divide(x, y, newWidth, newHeight, ChooseOrientation(newWidth, newHeight));

            // Assign new x/y coordinates for bottom/right part of maze
            int offsetX = isHorizontal ? x : xWallLocation + 1;
            int offsetY = isHorizontal ? yWallLocation + 1 : y;

            // Calculate new width/height for bottom/right part of maze
            newWidth = isHorizontal ? curWidth : x + curWidth - xWallLocation - 1;
            newHeight = isHorizontal ? y + curHeight - yWallLocation - 1 : curHeight;

            // Recurse down bottom/right subfield
            Divide(offsetX, offsetY, newWidth, newHeight, ChooseOrientation(newWidth, newHeight));
        }

        public void DisplayMaze()
        {
            // Write space at top border
            Debug.Write(" ");

            // Write top border itself 
            // (note that this is doubled for display purposes - to have both '|' and '_' character in same "cell")
            for (int i = 0; i < (_grid.GetLength(0)*2 - 1); i++)
            {
                Debug.Write("_");
            }

            // Write new line
            Debug.WriteLine("");

            // Actually iterate through the rows of the maze grid
            for (int curRowNum = 0; curRowNum < _grid.GetLength(0); curRowNum++)
            {
                // Write left border for the current row
                Debug.Write("|");

                // Iterate through each cell (column) in the current row
                for (int curColNum = 0; curColNum < _grid.GetLength(1); curColNum++)
                {
                    // We're at the bottom if 1 + current row is greater than or equal to the height
                    bool isBottom = curRowNum + 1 >= _grid.GetLength(0);

                    // Southern wall is enabled if one is specified in the grid or this is the bottom row
                    bool isSouthernWall = (_grid[curRowNum, curColNum] & (int) Direction.South) != 0 || isBottom;

                    // Enable the southern wall at the second half of the current cell if its still part of the same line
                    // (this prevents gaps due to doubling the number of columns for display purposes)
                    bool isInternalSouthernWall = curColNum + 1 < _grid.GetLength(1) &&
                                                  (_grid[curRowNum, curColNum + 1] & (int) Direction.South) != 0 ||
                                                  isBottom;

                    // Eastern wall is enabled if specified on the cell or if this is the last column in the grid
                    bool isEasternWall = (_grid[curRowNum, curColNum] & (int) Direction.East) != 0 ||
                                         curColNum + 1 >= _grid.GetLength(1);

                    Debug.Write(isSouthernWall ? "_" : " ");
                    Debug.Write(isEasternWall ? "|" : (isSouthernWall && isInternalSouthernWall) ? "_" : " ");
                }

                // Write new line
                Debug.WriteLine("");
            }
        }

        public Orientation ChooseOrientation(int width, int height)
        {
            if (width < height)
            {
                return Orientation.Horizontal;
            }
            if (height < width)
            {
                return Orientation.Vertical;
            }
            return _randomGenerator.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
        }
    }
}