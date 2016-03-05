#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

#endregion

namespace MazeGenerationTester.RecursiveDivision
{
    public static class MazeUtility
    {
        public static Orientation ChooseOrientation(int width, int height, Random randomNumGenerator)
        {
            if (width < height)
            {
                return Orientation.Horizontal;
            }
            if (height < width)
            {
                return Orientation.Vertical;
            }
            return randomNumGenerator.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
        }

        public static void DisplayMaze(int[,] grid)
        {
            // Write space at top border
            Debug.Write(" ");

            // Write top border itself 
            // (note that this is doubled for display purposes - to have both '|' and '_' character in same "cell")
            for (int i = 0; i < (grid.GetLength(0)*2 - 1); i++)
            {
                Debug.Write("_");
            }

            // Write new line
            Debug.WriteLine("");

            // Actually iterate through the rows of the maze grid
            for (int curRowNum = 0; curRowNum < grid.GetLength(0); curRowNum++)
            {
                // Write left border for the current row
                Debug.Write("|");

                // Iterate through each cell (column) in the current row
                for (int curColNum = 0; curColNum < grid.GetLength(1); curColNum++)
                {
                    // We're at the bottom if 1 + current row is greater than or equal to the height
                    bool isBottom = curRowNum + 1 >= grid.GetLength(0);

                    // Southern wall is enabled if one is specified in the grid or this is the bottom row
                    bool isSouthernWall = (grid[curRowNum, curColNum] & (int) Direction.South) != 0 || isBottom;

                    // Enable the southern wall at the second half of the current cell if its still part of the same line
                    // (this prevents gaps due to doubling the number of columns for display purposes)
                    bool isInternalSouthernWall = curColNum + 1 < grid.GetLength(1) &&
                                                  (grid[curRowNum, curColNum + 1] & (int) Direction.South) != 0 ||
                                                  isBottom;

                    // Eastern wall is enabled if specified on the cell or if this is the last column in the grid
                    bool isEasternWall = (grid[curRowNum, curColNum] & (int) Direction.East) != 0 ||
                                         curColNum + 1 >= grid.GetLength(1);

                    Debug.Write(isSouthernWall ? "_" : " ");
                    Debug.Write(isEasternWall ? "|" : (isSouthernWall && isInternalSouthernWall) ? "_" : " ");
                }

                // Write new line
                Debug.WriteLine("");
            }
        }

        public static List<Line2D> ExtractLineSegments(int[,] grid, int scaleMultiplier)
        {
            List<Line2D> lineSegments = new List<Line2D>();

            // First, establish the borders
            lineSegments.Add(new Line2D(0, 0, (grid.GetLength(1))*scaleMultiplier, 0)); // bottom border
            lineSegments.Add(new Line2D(0, 0, 0, (grid.GetLength(0))*scaleMultiplier)); // left border
            // right border
            lineSegments.Add(new Line2D((grid.GetLength(1))*scaleMultiplier, 0, (grid.GetLength(1))*scaleMultiplier,
                (grid.GetLength(0))*scaleMultiplier));
            // top border            
            lineSegments.Add(new Line2D(0, (grid.GetLength(0))*scaleMultiplier, (grid.GetLength(1))*scaleMultiplier,
                (grid.GetLength(0))*scaleMultiplier));

            // Get all of the horizontal lines
            for (int curRow = 0; curRow < grid.GetLength(0); curRow++)
            {
                Line2D curHorizontalLine = null;

                for (int curCol = 0; curCol < grid.GetLength(1); curCol++)
                {
                    // Handle the start point of a line segment 
                    // (current cell is horizontal and a new horizontal line hasn't yet been established)
                    if ((grid[curRow, curCol] == (int) Orientation.Horizontal ||
                         grid[curRow, curCol] == (int) Orientation.Both) && curHorizontalLine == null)
                    {
                        curHorizontalLine = new Line2D
                        {
                            StartPoint = new Point(curCol*scaleMultiplier, (curRow + 1)*scaleMultiplier)
                        };
                    }
                    else if (grid[curRow, curCol] != (int) Orientation.Horizontal &&
                             grid[curRow, curCol] != (int) Orientation.Both && curHorizontalLine != null)
                    {
                        curHorizontalLine.EndPoint = new Point(curCol*scaleMultiplier, (curRow + 1)*scaleMultiplier);
                        lineSegments.Add(curHorizontalLine);
                        curHorizontalLine = null;
                    }
                }

                if (curHorizontalLine != null)
                {
                    curHorizontalLine.EndPoint = new Point(grid.GetLength(1)*scaleMultiplier,
                        (curRow + 1)*scaleMultiplier);
                    lineSegments.Add(curHorizontalLine);
                }
            }

            // Get all of the vertical lines
            for (int curCol = 0; curCol < grid.GetLength(1); curCol++)
            {
                Line2D curVerticalLine = null;

                for (int curRow = 0; curRow < grid.GetLength(0); curRow++)
                {
                    if ((grid[curRow, curCol] == (int) Orientation.Vertical ||
                         grid[curRow, curCol] == (int) Orientation.Both) && curVerticalLine == null)
                    {
                        curVerticalLine = new Line2D
                        {
                            StartPoint =
                                new Point((curCol + 1)*scaleMultiplier, curRow*scaleMultiplier)
                        };
                    }
                    else if (grid[curRow, curCol] != (int) Orientation.Vertical &&
                             grid[curRow, curCol] != (int) Orientation.Both && curVerticalLine != null)
                    {
                        curVerticalLine.EndPoint = new Point((curCol + 1)*scaleMultiplier,
                            Math.Max(1, curRow)*scaleMultiplier);
                        lineSegments.Add(curVerticalLine);
                        curVerticalLine = null;
                    }
                }

                if (curVerticalLine != null)
                {
                    curVerticalLine.EndPoint = new Point((curCol + 1)*scaleMultiplier, grid.GetLength(0)*scaleMultiplier);
                    lineSegments.Add(curVerticalLine);
                }
            }

            return lineSegments;
        }

        public static void PrintBitmapMaze(List<Line2D> lines, int mazeWidth, int mazeHeight)
        {
            Pen blackPen = new Pen(Color.Black, 0.0001f);
            Bitmap mazeBitmap = new Bitmap(mazeWidth + 1, mazeHeight + 1);

            // Fill with white
            using (Graphics graphics = Graphics.FromImage(mazeBitmap))
            {
                Rectangle imageSize = new Rectangle(0, 0, mazeWidth + 1, mazeHeight + 1);
                graphics.FillRectangle(Brushes.White, imageSize);
                graphics.FillEllipse(Brushes.Green, 5, 5, 5, 5);
                graphics.FillEllipse(Brushes.Red, mazeWidth - 10, mazeHeight - 10, 5, 5);
            }

            foreach (Line2D line in lines)
            {
                using (Graphics graphics = Graphics.FromImage(mazeBitmap))
                {
                    graphics.DrawLine(blackPen, line.StartPoint, line.EndPoint);
                }
            }

            mazeBitmap.Save("RecursiveDivision_TestImage.bmp");
        }
    }
}