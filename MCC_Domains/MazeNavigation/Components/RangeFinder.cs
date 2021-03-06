﻿#region

using System;
using System.Collections.Generic;
using MCC_Domains.Common;
using MCC_Domains.Utils;

#endregion

namespace MCC_Domains.MazeNavigation.Components
{
    /// <summary>
    ///     Defines a range finder that indicates the distance to the nearest obstacle.
    /// </summary>
    public class RangeFinder
    {
        /// <summary>
        ///     The angle of the range finder with respect to the navigator.
        /// </summary>
        private readonly double _angle;

        /// <summary>
        ///     Creates a new range finder with the given default range, angle (with respect to the navigator orientation) and
        ///     output (which changes based on proximity to an obstacle).
        /// </summary>
        /// <param name="range">The default range of the range finder.</param>
        /// <param name="angle">The angle of the range finder with respect to the navigator.</param>
        /// <param name="output">The output of the range finder (which is the adjusted range based on obstacles in its trajectory).</param>
        internal RangeFinder(double range, double angle, double output)
        {
            Range = range;
            _angle = angle;
            Output = output;
        }

        /// <summary>
        ///     The default range finder range (which is the maximum range that the range finder sensor is capable of).
        /// </summary>
        internal double Range { get; }

        /// <summary>
        ///     The output of the range finder (scaled by the distance from the nearest obstacle).
        /// </summary>
        internal double Output { get; private set; }

        /// <summary>
        ///     Updates each of the range finders based on obstacles along their trajectory.  This amounts to setting the output of
        ///     the range finder to either the distance to the nearest obstacle or, if there are no obstacles in its path, to the
        ///     maximum distance of the range finder.
        /// </summary>
        /// <param name="walls">The list of walls in the environment.</param>
        /// <param name="heading">The heading of the navigator (in degrees).</param>
        /// <param name="location">The location of the navigator in the environment.</param>
        internal void Update(IList<Wall> walls, double heading, DoublePoint location)
        {
            // Convert rangefinder angle to radians
            var radianAngle = MathUtils.ToRadians(_angle);

            // Project a point from the navigator location outward
            var projectedPoint = new DoublePoint(location.X + Math.Cos(radianAngle) * Range,
                location.Y + Math.Sin(radianAngle) * Range);

            //  Rotate the point based on the navigator's heading
            projectedPoint.RotatePoint(heading, location);

            // Create a line segment from the navigator's current location to the 
            // projected point
            var projectedLine = new DoubleLine(location, projectedPoint);

            // Initialize the range to the maximum range of the range finder sensor
            var adjustedRange = Range;

            foreach (var wall in walls)
            {
                // Get the intersection point between wall and projected trajectory
                // (if one exists)
                var wallIntersectionPoint = DoubleLine.CalculateLineIntersection(wall.WallLine, projectedLine,
                    out var intersectionFound);

                // Skip to next wall if there's no intersection for current one
                if (!intersectionFound) continue;

                // Otherwise, if trajectory intersects with a wall, adjust the range to the point
                // of intersection (as the range finder cannot penetrate walls)

                // Get the distance from the wall
                var wallRange = DoublePoint.CalculateEuclideanDistance(wallIntersectionPoint, location);

                // If the current wall range is shorter than the current adjusted range,
                // update the adjusted range to the shorter value
                if (wallRange < adjustedRange)
                {
                    adjustedRange = wallRange;
                }
            }

            // Update the range finder range to be the adjusted range
            Output = adjustedRange;
        }
    }
}