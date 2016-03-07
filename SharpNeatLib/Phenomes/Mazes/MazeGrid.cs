#region

using System;
using System.Collections.Generic;

#endregion

namespace SharpNeat.Phenomes.Mazes
{
    public class MazeGrid
    {
        #region Constructors

        public MazeGrid(int mazeWidth, int mazeHeight, int scaleMultiplier)
        {
            Walls = new List<MazeWall>();
            _mazeWidth = mazeWidth;
            _mazeHeight = mazeHeight;
            _scaleMultiplier = scaleMultiplier;

            // Initialize bounding walls
            GenerateBorderWalls();
        }

        #endregion

        #region Properties

        public List<MazeWall> Walls { get; }

        #endregion

        #region Public Methods

        public void ConvertGridArrayToWalls(int[,] mazeGridArray)
        {
            // Extract all of the horizontal walls
            ExtractHorizontalWalls(mazeGridArray);

            // Extract all of the vertical walls
            ExtractVerticalWalls(mazeGridArray);
        }

        #endregion

        #region Private Methods

        private void GenerateBorderWalls()
        {
            // Add bottom border
            Walls.Add(new MazeWall(0, 0, (_mazeWidth*_scaleMultiplier), 0));

            // Add left border
            Walls.Add(new MazeWall(0, 0, 0, (_mazeHeight*_scaleMultiplier)));

            // Add right border
            Walls.Add(new MazeWall((_mazeWidth*_scaleMultiplier), 0, (_mazeWidth*_scaleMultiplier),
                (_mazeHeight*_scaleMultiplier)));

            // Add top border
            Walls.Add(new MazeWall(0, (_mazeHeight*_scaleMultiplier), (_mazeWidth*_scaleMultiplier),
                (_mazeHeight*_scaleMultiplier)));
        }

        private void ExtractHorizontalWalls(int[,] mazeGridArray)
        {
            // Get all of the horizontal lines
            for (int curRow = 0; curRow < _mazeHeight; curRow++)
            {
                MazeWall curHorizontalLine = null;

                for (int curCol = 0; curCol < _mazeWidth; curCol++)
                {
                    // Handle the start point of a line segment 
                    // (current cell is horizontal and a new horizontal line hasn't yet been established)
                    if ((mazeGridArray[curRow, curCol] == (int) WallOrientation.Horizontal ||
                         mazeGridArray[curRow, curCol] == (int) WallOrientation.Both) && curHorizontalLine == null)
                    {
                        curHorizontalLine = new MazeWall
                        {
                            StartPoint = new Point(curCol*_scaleMultiplier, (curRow + 1)*_scaleMultiplier)
                        };
                    }
                    else if (mazeGridArray[curRow, curCol] != (int) WallOrientation.Horizontal &&
                             mazeGridArray[curRow, curCol] != (int) WallOrientation.Both && curHorizontalLine != null)
                    {
                        curHorizontalLine.EndPoint = new Point(curCol*_scaleMultiplier, (curRow + 1)*_scaleMultiplier);
                        Walls.Add(curHorizontalLine);
                        curHorizontalLine = null;
                    }
                }

                if (curHorizontalLine != null)
                {
                    curHorizontalLine.EndPoint = new Point(_mazeWidth*_scaleMultiplier,
                        (curRow + 1)*_scaleMultiplier);
                    Walls.Add(curHorizontalLine);
                }
            }
        }

        private void ExtractVerticalWalls(int[,] mazeGridArray)
        {
            // Get all of the vertical lines
            for (int curCol = 0; curCol < _mazeWidth; curCol++)
            {
                MazeWall curVerticalLine = null;

                for (int curRow = 0; curRow < _mazeHeight; curRow++)
                {
                    if ((mazeGridArray[curRow, curCol] == (int) WallOrientation.Vertical ||
                         mazeGridArray[curRow, curCol] == (int) WallOrientation.Both) && curVerticalLine == null)
                    {
                        curVerticalLine = new MazeWall
                        {
                            StartPoint =
                                new Point((curCol + 1)*_scaleMultiplier, curRow*_scaleMultiplier)
                        };
                    }
                    else if (mazeGridArray[curRow, curCol] != (int) WallOrientation.Vertical &&
                             mazeGridArray[curRow, curCol] != (int) WallOrientation.Both && curVerticalLine != null)
                    {
                        curVerticalLine.EndPoint = new Point((curCol + 1)*_scaleMultiplier,
                            Math.Max(1, curRow)*_scaleMultiplier);
                        Walls.Add(curVerticalLine);
                        curVerticalLine = null;
                    }
                }

                if (curVerticalLine != null)
                {
                    curVerticalLine.EndPoint = new Point((curCol + 1)*_scaleMultiplier, _mazeHeight*_scaleMultiplier);
                    Walls.Add(curVerticalLine);
                }
            }
        }

        #endregion

        #region Instance Variables

        private readonly int _mazeWidth;
        private readonly int _mazeHeight;
        private readonly int _scaleMultiplier;

        #endregion
    }
}