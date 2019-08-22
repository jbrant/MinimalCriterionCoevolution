#region

using System;
using System.Collections.Generic;
using SharpNeat.Utility;

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
        /// <param name="genomeId">The unique identifier of the genome from which the phenotype was generated (optional).</param>
        public MazeStructure(int mazeWidth, int mazeHeight, int scaleMultiplier, uint genomeId = uint.MaxValue)
        {
            GenomeId = genomeId;
            Walls = new List<MazeStructureWall>();
            _mazeWidth = mazeWidth;
            _mazeHeight = mazeHeight;
            ScaleMultiplier = scaleMultiplier;

            // Set the scaled maze width and height
            ScaledMazeHeight = mazeHeight * scaleMultiplier;
            ScaledMazeWidth = mazeWidth * scaleMultiplier;

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
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId { get; }

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
        ///     The amount by which to scale up the size of the maze.
        /// </summary>
        public int ScaleMultiplier { get; }

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
        ///     The number of times an agent has used the maze for satisfying their MC (which is required to be considered viable
        ///     for persistence and reproduction). This is persisted on and carried through from the maze genotype.
        /// </summary>
        public int ViabilityUsageCount { get; set; }

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
            StartLocation = new MazeStructurePoint(ScaleMultiplier / 2, ScaleMultiplier / 2);

            // Set the target location to be in the bottom right corner of the maze
            TargetLocation = new MazeStructurePoint(ScaledMazeWidth - ScaleMultiplier / 2,
                ScaledMazeHeight - ScaleMultiplier / 2);
        }

        /// <summary>
        ///     Calculates the maximum number of time steps allotted to solve the maze based on the manhattan distance from the
        ///     starting location to the target location, respecting all obstructions.
        /// </summary>
        private void CalculateMaxTimesteps()
        {
            // Get the unscaled distance to the target location
            var unscaledDistance = MazeUtils.ComputeDistanceToTarget(MazeGrid, _mazeHeight, _mazeWidth);

            // Compute the maximum time steps by distributing the unscaled distance evenly across both dimensions 
            // (i.e. halving it) and multiplying by the scale multiplier for both dimensions
            // TODO: Need to experiment with polynomial timestep increase here
            MaxTimesteps = 2 * ScaleMultiplier * (unscaledDistance / 2);
        }

        /// <summary>
        ///     Parses the maze grid matrix in a row-wise manner, extracting the horizontal walls.
        /// </summary>
        /// <param name="mazeGridArray">The grid array which is being parsed.</param>
        private void ExtractHorizontalWalls(MazeStructureGridCell[,] mazeGridArray)
        {
            // Get all of the horizontal lines
            for (var curRow = 0; curRow < _mazeHeight; curRow++)
            {
                MazeStructureWall curHorizontalLine = null;

                for (var curCol = 0; curCol < _mazeWidth; curCol++)
                {
                    // Handle the start point of a line segment 
                    // (current cell is horizontal and a new horizontal line hasn't yet been established)
                    if (mazeGridArray[curRow, curCol].SouthWall && curHorizontalLine == null)
                    {
                        curHorizontalLine = new MazeStructureWall
                        {
                            StartMazeStructurePoint =
                                new MazeStructurePoint(curCol * ScaleMultiplier, (curRow + 1) * ScaleMultiplier)
                        };
                    }
                    // Otherwise, if we've been tracking a horizontal line and the current position contains neither a horizontal line segment 
                    // nor a combination of a horizontal and vertical line, then record this as the maze ending point and null out the current line
                    else if (mazeGridArray[curRow, curCol].SouthWall == false && curHorizontalLine != null)
                    {
                        curHorizontalLine.EndMazeStructurePoint = new MazeStructurePoint(curCol * ScaleMultiplier,
                            (curRow + 1) * ScaleMultiplier);
                        Walls.Add(curHorizontalLine);
                        curHorizontalLine = null;
                    }
                }

                // If we've reached the end of the current row but we're still tracking a horizontal line, then this must be its
                // end point since it can't protrude outside of the maze walls
                if (curHorizontalLine != null)
                {
                    curHorizontalLine.EndMazeStructurePoint = new MazeStructurePoint(_mazeWidth * ScaleMultiplier,
                        (curRow + 1) * ScaleMultiplier);
                    Walls.Add(curHorizontalLine);
                }
            }
        }

        /// <summary>
        ///     Parses the maze grid matrix in a column-wise manner, extracting the vertical walls.
        /// </summary>
        /// <param name="mazeGridArray">The grid array which is being parsed.</param>
        private void ExtractVerticalWalls(MazeStructureGridCell[,] mazeGridArray)
        {
            // Get all of the vertical lines
            for (var curCol = 0; curCol < _mazeWidth; curCol++)
            {
                MazeStructureWall curVerticalLine = null;

                for (var curRow = 0; curRow < _mazeHeight; curRow++)
                {
                    // Handle the start point of a line segment 
                    // (current cell is vertical and a new vertical line hasn't yet been established)
                    if (mazeGridArray[curRow, curCol].EastWall && curVerticalLine == null)
                    {
                        curVerticalLine = new MazeStructureWall
                        {
                            StartMazeStructurePoint =
                                new MazeStructurePoint((curCol + 1) * ScaleMultiplier, curRow * ScaleMultiplier)
                        };
                    }
                    // Otherwise, if we've been tracking a vertical line and the current position contains neither a vertical line segment 
                    // nor a combination of a horizontal and vertical line, then record this as the maze ending point and null out the current line
                    else if (mazeGridArray[curRow, curCol].EastWall == false && curVerticalLine != null)
                    {
                        curVerticalLine.EndMazeStructurePoint = new MazeStructurePoint((curCol + 1) * ScaleMultiplier,
                            Math.Max(1, curRow) * ScaleMultiplier);
                        Walls.Add(curVerticalLine);
                        curVerticalLine = null;
                    }
                }

                // If we've reached the end of the current row but we're still tracking a vertical line, then this must be its
                // end point since it can't protrude outside of the maze walls
                if (curVerticalLine != null)
                {
                    curVerticalLine.EndMazeStructurePoint = new MazeStructurePoint((curCol + 1) * ScaleMultiplier,
                        _mazeHeight * ScaleMultiplier);
                    Walls.Add(curVerticalLine);
                }
            }
        }

        #endregion

        #region Instance Variables

        // The maze dimensions
        private readonly int _mazeWidth;
        private readonly int _mazeHeight;

        #endregion
    }
}