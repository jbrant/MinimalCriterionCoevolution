#region

using System;
using System.Collections.Generic;

#endregion

namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     The maze structure encapsulates the entire maze, including borders and all of the walls.  It can be scaled up from the
    ///     original resolution used during evolution.
    /// </summary>
    public class MazeStructure
    {
        #region Constructors

        /// <summary>
        ///     Constructor which accepts the dimensions of the resulting maze along with the multiplier indicating the scale
        ///     increase (or decrease).
        /// </summary>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <param name="mazeHeight">The height of the maze.</param>
        /// <param name="scaleMultiplier">The multiplier dictating the increase (or decrease) in maze size.</param>
        public MazeStructure(int mazeWidth, int mazeHeight, int scaleMultiplier)
        {
            Walls = new List<MazeStructureWall>();
            _mazeWidth = mazeWidth;
            _mazeHeight = mazeHeight;
            _scaleMultiplier = scaleMultiplier;

            // Initialize bounding walls
            GenerateBorderWalls();

            // Calculate the starting and ending points
            CalculateStartEndPoints();
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Converts the maze space matrix (which indicates what type of wall is at each intersection in the 2D maze) to a list
        ///     of horizontal and vertical walls.
        /// </summary>
        /// <param name="mazeGridArray"></param>
        public void ConvertGridArrayToWalls(int[,] mazeGridArray)
        {
            MazeArray = mazeGridArray;

            // Extract all of the horizontal walls
            ExtractHorizontalWalls(mazeGridArray);

            // Extract all of the vertical walls
            ExtractVerticalWalls(mazeGridArray);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The list of walls in the maze.
        /// </summary>
        public List<MazeStructureWall> Walls { get; }

        /// <summary>
        ///     The starting location of a maze navigator.
        /// </summary>
        public MazeStructurePoint StartLocation { get; private set; }

        /// <summary>
        ///     The target/goal of a maze navigator.
        /// </summary>
        public MazeStructurePoint TargetLocation { get; private set; }

        public int[,] MazeArray { get; private set; }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Generates the walls that bound the maze (i.e. the borders).
        /// </summary>
        private void GenerateBorderWalls()
        {
            // Add bottom border
            Walls.Add(new MazeStructureWall(0, 0, (_mazeWidth*_scaleMultiplier), 0));

            // Add left border
            Walls.Add(new MazeStructureWall(0, 0, 0, (_mazeHeight*_scaleMultiplier)));

            // Add right border
            Walls.Add(new MazeStructureWall((_mazeWidth*_scaleMultiplier), 0, (_mazeWidth*_scaleMultiplier),
                (_mazeHeight*_scaleMultiplier)));

            // Add top border
            Walls.Add(new MazeStructureWall(0, (_mazeHeight*_scaleMultiplier), (_mazeWidth*_scaleMultiplier),
                (_mazeHeight*_scaleMultiplier)));
        }

        /// <summary>
        ///     Calculates the location of the starting and ending locations in the maze.
        /// </summary>
        private void CalculateStartEndPoints()
        {
            // Set the starting location to be in the top left corner of the maze, half the scale multiplier
            // (this guarantees there will be no intersecting walls)
            StartLocation = new MazeStructurePoint(_scaleMultiplier/2, _scaleMultiplier/2);

            // Set the target location to be in the bottom right corner of the maze
            TargetLocation = new MazeStructurePoint(_mazeWidth - (_scaleMultiplier/2), _mazeHeight - (_scaleMultiplier/2));
        }

        /// <summary>
        ///     Parses the maze grid matrix in a row-wise manner, extracting the horizontal walls.
        /// </summary>
        /// <param name="mazeGridArray">The grid array which is being parsed.</param>
        private void ExtractHorizontalWalls(int[,] mazeGridArray)
        {
            // Get all of the horizontal lines
            for (int curRow = 0; curRow < _mazeHeight; curRow++)
            {
                MazeStructureWall curHorizontalLine = null;

                for (int curCol = 0; curCol < _mazeWidth; curCol++)
                {
                    // Handle the start point of a line segment 
                    // (current cell is horizontal and a new horizontal line hasn't yet been established)
                    if ((mazeGridArray[curRow, curCol] == (int) WallOrientation.Horizontal ||
                         mazeGridArray[curRow, curCol] == (int) WallOrientation.Both) && curHorizontalLine == null)
                    {
                        curHorizontalLine = new MazeStructureWall
                        {
                            StartMazeStructurePoint = new MazeStructurePoint(curCol*_scaleMultiplier, (curRow + 1)*_scaleMultiplier)
                        };
                    }
                    // Otherwise, if we've been tracking a horizontal line and the current position contains neither a horizontal line segment 
                    // nor a combination of a horizontal and vertical line, then record this as the maze ending point and null out the current line
                    else if (mazeGridArray[curRow, curCol] != (int) WallOrientation.Horizontal &&
                             mazeGridArray[curRow, curCol] != (int) WallOrientation.Both && curHorizontalLine != null)
                    {
                        curHorizontalLine.EndMazeStructurePoint = new MazeStructurePoint(curCol*_scaleMultiplier,
                            (curRow + 1)*_scaleMultiplier);
                        Walls.Add(curHorizontalLine);
                        curHorizontalLine = null;
                    }
                }

                // If we've reached the end of the current row but we're still tracking a horizontal line, then this must be its
                // end point since it can't protrude outside of the maze walls
                if (curHorizontalLine != null)
                {
                    curHorizontalLine.EndMazeStructurePoint = new MazeStructurePoint(_mazeWidth*_scaleMultiplier,
                        (curRow + 1)*_scaleMultiplier);
                    Walls.Add(curHorizontalLine);
                }
            }
        }

        /// <summary>
        ///     Parses the maze grid matrix in a column-wise manner, extracting the vertical walls.
        /// </summary>
        /// <param name="mazeGridArray">The grid array which is being parsed.</param>
        private void ExtractVerticalWalls(int[,] mazeGridArray)
        {
            // Get all of the vertical lines
            for (int curCol = 0; curCol < _mazeWidth; curCol++)
            {
                MazeStructureWall curVerticalLine = null;

                for (int curRow = 0; curRow < _mazeHeight; curRow++)
                {
                    // Handle the start point of a line segment 
                    // (current cell is vertical and a new vertical line hasn't yet been established)
                    if ((mazeGridArray[curRow, curCol] == (int) WallOrientation.Vertical ||
                         mazeGridArray[curRow, curCol] == (int) WallOrientation.Both) && curVerticalLine == null)
                    {
                        curVerticalLine = new MazeStructureWall
                        {
                            StartMazeStructurePoint =
                                new MazeStructurePoint((curCol + 1)*_scaleMultiplier, curRow*_scaleMultiplier)
                        };
                    }
                    // Otherwise, if we've been tracking a vertical line and the current position contains neither a vertical line segment 
                    // nor a combination of a horizontal and vertical line, then record this as the maze ending point and null out the current line
                    else if (mazeGridArray[curRow, curCol] != (int) WallOrientation.Vertical &&
                             mazeGridArray[curRow, curCol] != (int) WallOrientation.Both && curVerticalLine != null)
                    {
                        curVerticalLine.EndMazeStructurePoint = new MazeStructurePoint((curCol + 1)*_scaleMultiplier,
                            Math.Max(1, curRow)*_scaleMultiplier);
                        Walls.Add(curVerticalLine);
                        curVerticalLine = null;
                    }
                }

                // If we've reached the end of the current row but we're still tracking a vertical line, then this must be its
                // end point since it can't protrude outside of the maze walls
                if (curVerticalLine != null)
                {
                    curVerticalLine.EndMazeStructurePoint = new MazeStructurePoint((curCol + 1)*_scaleMultiplier,
                        _mazeHeight*_scaleMultiplier);
                    Walls.Add(curVerticalLine);
                }
            }
        }

        #endregion

        #region Instance Variables

        // The maze dimensions
        private readonly int _mazeWidth;
        private readonly int _mazeHeight;

        // The amount by which to scale up the size of the maze
        private readonly int _scaleMultiplier;

        #endregion
    }
}