#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace SharpNeat.Phenomes.Mazes
{
    /// <summary>
    ///     The maze structure encapsulates the entire maze, including borders and all of the walls.  It can be scaled up from
    ///     the original resolution used during evolution.
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

            // Set the scaled maze width and height
            ScaledMazeHeight = mazeHeight*scaleMultiplier;
            ScaledMazeWidth = mazeWidth*scaleMultiplier;

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
        /// <param name="mazeGrid"></param>
        public void ConvertGridArrayToWalls(MazeStructureGrid mazeGrid)
        {
            MazeGrid = mazeGrid;

            // Extract all of the horizontal walls
            ExtractHorizontalWalls(mazeGrid.Grid);

            // Extract all of the vertical walls
            ExtractVerticalWalls(mazeGrid.Grid);

            // Calculate the maximum number of allotted time steps based on the maze structure
            CalculateMaxTimesteps();
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

        /// <summary>
        ///     The scaled height of the maze.
        /// </summary>
        public int ScaledMazeHeight { get; }

        /// <summary>
        ///     The scaled width of the maze.
        /// </summary>
        public int ScaledMazeWidth { get; }

        /// <summary>
        ///     The unscaled maze cell matrix.
        /// </summary>
        public MazeStructureGrid MazeGrid { get; private set; }

        /// <summary>
        ///     The maximum number of timesteps allotted to solve the maze.
        /// </summary>
        public int MaxTimesteps { get; private set; }

        /// <summary>
        ///     The number of partitions bisecting maze sub-spaces. A partition could be either one or two walls (depending on
        ///     whether the passage is adjacent to a maze bounding wall.
        /// </summary>
        public int NumPartitions { get; set; }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Generates the walls that bound the maze (i.e. the borders).
        /// </summary>
        private void GenerateBorderWalls()
        {
            // Add bottom border
            Walls.Add(new MazeStructureWall(0, 0, ScaledMazeWidth, 0));

            // Add left border
            Walls.Add(new MazeStructureWall(0, 0, 0, ScaledMazeHeight));

            // Add right border
            Walls.Add(new MazeStructureWall(ScaledMazeWidth, 0, ScaledMazeWidth, ScaledMazeHeight));

            // Add top border
            Walls.Add(new MazeStructureWall(0, ScaledMazeHeight, ScaledMazeWidth, ScaledMazeHeight));
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
            TargetLocation = new MazeStructurePoint(ScaledMazeWidth - (_scaleMultiplier/2),
                ScaledMazeHeight - (_scaleMultiplier/2));
        }

        /// <summary>
        ///     Calculates the maximum number of time steps allotted to solve the maze based on the manhattan distance from the
        ///     starting location to the target location, respecting all obstructions.
        /// </summary>
        private void CalculateMaxTimesteps()
        {
            // Setup grid to store maze structure points
            var pointGrid = new MazeStructurePoint[_mazeHeight, _mazeWidth];

            // Convert to grid of maze structure points
            for (int x = 0; x < _mazeHeight; x++)
            {
                for (int y = 0; y < _mazeWidth; y++)
                {
                    pointGrid[x, y] = new MazeStructurePoint(x, y);
                }
            }

            // Define queue in which to store cells as they're discovered and visited
            Queue<MazeStructurePoint> cellQueue = new Queue<MazeStructurePoint>(pointGrid.Length);

            // Define a dictionary to store the distances between each visited cell and the starting location
            Dictionary<MazeStructurePoint, int> visitedCellDistances = new Dictionary<MazeStructurePoint, int>();

            // Enqueue the starting location
            cellQueue.Enqueue(pointGrid[0, 0]);

            // Add starting location to visited cells with 0 distance
            visitedCellDistances.Add(pointGrid[0, 0], 0);

            // Iterate through maze cells, dequeueing each and determining the distance to their reachable neighbors
            // until the target location is reached and we have the shortest distance to it
            while (cellQueue.Count > 0)
            {
                // Get the next element in the queue
                var curPoint = cellQueue.Dequeue();

                // Exit if target reached
                if (curPoint.X == _mazeHeight - 1 && curPoint.Y == _mazeWidth - 1)
                {
                    break;
                }
                // Every adjacent vertex is a distance of 1 away
                int curDistance = visitedCellDistances[curPoint] + 1;

                // Handle cells in each cardinal direction

                // North
                if (0 != curPoint.X && (int) WallOrientation.Horizontal != MazeGrid.Grid[curPoint.X - 1, curPoint.Y] &&
                    (int) WallOrientation.Both != MazeGrid.Grid[curPoint.X - 1, curPoint.Y] &&
                    visitedCellDistances.ContainsKey(pointGrid[curPoint.X - 1, curPoint.Y]) == false)
                {
                    cellQueue.Enqueue(pointGrid[curPoint.X - 1, curPoint.Y]);
                    visitedCellDistances.Add(pointGrid[curPoint.X - 1, curPoint.Y], curDistance);
                }

                // East
                if (_mazeWidth > curPoint.Y + 1 &&
                    (int) WallOrientation.Vertical != MazeGrid.Grid[curPoint.X, curPoint.Y] &&
                    (int) WallOrientation.Both != MazeGrid.Grid[curPoint.X, curPoint.Y] &&
                    visitedCellDistances.ContainsKey(pointGrid[curPoint.X, curPoint.Y + 1]) == false)
                {
                    cellQueue.Enqueue(pointGrid[curPoint.X, curPoint.Y + 1]);
                    visitedCellDistances.Add(pointGrid[curPoint.X, curPoint.Y + 1], curDistance);
                }

                // South
                if (_mazeHeight > curPoint.X + 1 &&
                    (int) WallOrientation.Horizontal != MazeGrid.Grid[curPoint.X, curPoint.Y] &&
                    (int) WallOrientation.Both != MazeGrid.Grid[curPoint.X, curPoint.Y] &&
                    visitedCellDistances.ContainsKey(pointGrid[curPoint.X + 1, curPoint.Y]) == false)
                {
                    cellQueue.Enqueue(pointGrid[curPoint.X + 1, curPoint.Y]);
                    visitedCellDistances.Add(pointGrid[curPoint.X + 1, curPoint.Y], curDistance);
                }

                // West
                if (0 != curPoint.Y && (int) WallOrientation.Vertical != MazeGrid.Grid[curPoint.X, curPoint.Y - 1] &&
                    (int) WallOrientation.Both != MazeGrid.Grid[curPoint.X, curPoint.Y - 1] &&
                    visitedCellDistances.ContainsKey(pointGrid[curPoint.X, curPoint.Y - 1]) == false)
                {
                    cellQueue.Enqueue(pointGrid[curPoint.X, curPoint.Y - 1]);
                    visitedCellDistances.Add(pointGrid[curPoint.X, curPoint.Y - 1], curDistance);
                }
            }

            // Get the unscaled distance to the target location
            int unscaledDistance =
                visitedCellDistances.Single(cd => (_mazeHeight - 1) == cd.Key.X && (_mazeWidth - 1) == cd.Key.Y).Value;

            // Compute the maximum time steps by distributing the unscalled distance evenly across both dimensions 
            // (i.e. halving it) and multiplying by the scale multiplier for both dimensions
            // TODO: Need to experiment with polynomial timestep increase here
            MaxTimesteps = 2*(_scaleMultiplier*(unscaledDistance/2));
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
                            StartMazeStructurePoint =
                                new MazeStructurePoint(curCol*_scaleMultiplier, (curRow + 1)*_scaleMultiplier)
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