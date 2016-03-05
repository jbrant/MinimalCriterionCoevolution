using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazeGenerationTester.RecursiveDivision
{
    public class RecursiveDivisionMazeGenerationDepthFirst
    {
        private readonly Random _randomGenerator = new Random(123456);

        public RecursiveDivisionMazeGenerationDepthFirst(int width, int height)
        {
            Grid = new int[height, width];
        }

        public int[,] Grid { get; }

        public void Divide(int x, int y, int curWidth, int curHeight, Orientation orientation, int counter)
        {
            // Short-circuit recursion
            if (curWidth < 2 || curHeight < 2)
                return;

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
                    Grid[yWallLocation, xWallLocation] |= (int)perpendicularDirection;
                }

                // Increment the wall segment location by the appropriate directional components
                xWallLocation += xDirection;
                yWallLocation += yDirection;
            }

            // Calculate new width/height for top/left part of maze
            int newWidth = isHorizontal ? curWidth : xWallLocation - x + 1;
            int newHeight = isHorizontal ? yWallLocation - y + 1 : curHeight;

            // Recurse down top/left subfield
            Divide(x, y, newWidth, newHeight, MazeUtility.ChooseOrientation(newWidth, newHeight, _randomGenerator), counter);

            // Assign new x/y coordinates for bottom/right part of maze
            int offsetX = isHorizontal ? x : xWallLocation + 1;
            int offsetY = isHorizontal ? yWallLocation + 1 : y;

            // Calculate new width/height for bottom/right part of maze
            newWidth = isHorizontal ? curWidth : x + curWidth - xWallLocation - 1;
            newHeight = isHorizontal ? y + curHeight - yWallLocation - 1 : curHeight;

            // Recurse down bottom/right subfield
            Divide(offsetX, offsetY, newWidth, newHeight,
                MazeUtility.ChooseOrientation(newWidth, newHeight, _randomGenerator), counter);
        }
    }
}
