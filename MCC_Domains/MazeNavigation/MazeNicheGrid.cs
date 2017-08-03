#region

using System.Collections.Generic;
using MCC_Domains.MazeNavigation.Components;
using SharpNeat.Domains;

#endregion

namespace MCC_Domains.MazeNavigation
{
    /// <summary>
    ///     Defines minimum/maximum boundaries on the provided maze walls as well as helper methods to map ending locations to
    ///     the appropriate niche.
    /// </summary>
    public struct MazeNicheGrid
    {
        /// <summary>
        ///     Density of grid points (used to scale ending position into appropriate niche grid location).
        /// </summary>
        private readonly int _gridDensity;

        /// <summary>
        ///     Maximum X point.
        /// </summary>
        private readonly double _maxXPoint;

        /// <summary>
        ///     Maximum Y point.
        /// </summary>
        private readonly double _maxYPoint;

        /// <summary>
        ///     Minimum X point.
        /// </summary>
        private readonly double _minXPoint;

        /// <summary>
        ///     Minimum Y point.
        /// </summary>
        private readonly double _minYPoint;

        /// <summary>
        ///     Constructor which initializes the maze boundary points.
        /// </summary>
        /// <param name="gridDensity">The density of the grid overlay.</param>
        /// <param name="mazeWalls">The collection of walls in the maze.</param>
        public MazeNicheGrid(int gridDensity, List<Wall> mazeWalls)
        {
            _gridDensity = gridDensity;

            // Initialize all min/max boundaries
            _minXPoint = 9999;
            _maxXPoint = 0;
            _minYPoint = 9999;
            _maxYPoint = 0;

            // Loop through each wall to determine the min/max X and Y boundaries
            foreach (Wall mazeWall in mazeWalls)
            {
                // Starting X point is less than current minimum X point
                if (mazeWall.WallLine.Start.X < _minXPoint)
                {
                    _minXPoint = mazeWall.WallLine.Start.X;
                }
                // Ending X point is less than current minimum X point
                else if (mazeWall.WallLine.End.X < _minXPoint)
                {
                    _minXPoint = mazeWall.WallLine.End.X;
                }
                // Starting X point is greater than current maximum X point
                else if (mazeWall.WallLine.Start.X > _maxXPoint)
                {
                    _maxXPoint = mazeWall.WallLine.Start.X;
                }
                // Ending X point is greater than current maximum X point
                else if (mazeWall.WallLine.End.X > _maxXPoint)
                {
                    _maxXPoint = mazeWall.WallLine.End.X;
                }
                // Starting Y point is less than current minimum Y point
                else if (mazeWall.WallLine.Start.Y < _minYPoint)
                {
                    _minYPoint = mazeWall.WallLine.Start.Y;
                }
                // Ending Y point is less than current minimum Y point
                else if (mazeWall.WallLine.End.Y < _minYPoint)
                {
                    _minYPoint = mazeWall.WallLine.End.Y;
                }
                // Starting Y point is greater than current maximum Y point
                else if (mazeWall.WallLine.Start.Y > _maxYPoint)
                {
                    _maxYPoint = mazeWall.WallLine.Start.Y;
                }
                // Ending Y point is greater than current maximum Y point
                else if (mazeWall.WallLine.End.Y > _maxYPoint)
                {
                    _maxYPoint = mazeWall.WallLine.End.Y;
                }
            }
        }

        /// <summary>
        ///     Calculates the niche in which the ending location resides.
        /// </summary>
        /// <param name="location">The ending location.</param>
        /// <returns>The integer ID of the niche in which the ending location resides.</returns>
        public int DetermineNicheId(DoublePoint location)
        {
            return _gridDensity*(int) (_gridDensity*(location.X - _minXPoint)/((0.01 + _maxXPoint) - _minXPoint)) +
                   (int) (_gridDensity*(location.Y - _minYPoint)/(_maxYPoint - _minYPoint));
        }
    }
}