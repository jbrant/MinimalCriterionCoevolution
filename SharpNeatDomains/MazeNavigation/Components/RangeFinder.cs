using System;
using System.Collections.Generic;

namespace SharpNeat.Domains.MazeNavigation.Components
{
    internal class RangeFinder
    {
        internal RangeFinder(double range, double angle, double output)
        {
            Range = range;
            Angle = angle;
            Output = output;
        }

        internal double Range { get; private set; }
        internal double Angle { get; }
        internal double Output { get; set; }

        internal void Update(List<Line2D> walls, double heading, Point2D location)
        {
            // Convert rangefinder angle to radians
            var radianAngle = MathUtils.toRadians(Angle);

            // Project a point from the navigator location outward
            var projectedPoint = new Point2D(location.x + Math.Cos(radianAngle)*Range,
                location.y + Math.Sin(radianAngle)*Range);

            //  Rotate the point based on the navigator's heading
            projectedPoint.rotate(heading, location);

            // Create a line segment from the navigator's current location to the 
            // projected point
            var projectedLine = new Line2D(location, projectedPoint);

            // Initialize the range to the maximum range of the range finder sensor
            var adjustedRange = Range;

            foreach (var wall in walls)
            {
                // Initialize the intersection indicator to false
                var intersectionFound = false;

                // Get the intersection point between wall and projected trajectory
                // (if one exists)
                var wallIntersectionPoint = wall.intersection(projectedLine, out intersectionFound);

                // If trajectory intersects with a wall, adjust the range to the point
                // of intersection (as the range finder cannot penetrate walls)
                if (intersectionFound)
                {
                    // Get the distance from the wall
                    var wallRange = wallIntersectionPoint.distance(location);

                    // If the current wall range is shorter than the current adjusted range,
                    // update the adjusted range to the shorter value
                    if (wallRange < adjustedRange)
                    {
                        adjustedRange = wallRange;
                    }
                }
            }

            // Update the range finder range to be the adjusted range
            Range = adjustedRange;
        }
    }
}