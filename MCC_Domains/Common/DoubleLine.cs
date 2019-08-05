#region

using System;
using MCC_Domains.Utils;

#endregion

namespace MCC_Domains.Common
{
    /// <summary>
    ///     Defines a 2D line segment with 2D double-precision start and end points.
    /// </summary>
    public struct DoubleLine
    {
        /// <summary>
        ///     End point of the line segment.
        /// </summary>
        private readonly DoublePoint _end;

        /// <summary>
        ///     Start point of the line segment.
        /// </summary>
        private readonly DoublePoint _start;

        #region Constructor

        /// <summary>
        ///     Construct line segment with the specified start and end points.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        public DoubleLine(DoublePoint startPoint, DoublePoint endPoint)
        {
            _start = startPoint;
            _end = endPoint;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Construct line segment with the specified start and end coordinates.
        /// </summary>
        /// <param name="x1">The start point X-coordinate.</param>
        /// <param name="y1">The start point Y-coordinate.</param>
        /// <param name="x2">The end point X-coordinate.</param>
        /// <param name="y2">The end point Y-coordinate.</param>
        public DoubleLine(double x1, double y1, double x2, double y2)
            : this(new DoublePoint(x1, y1), new DoublePoint(x2, y2))
        {
        }

        #endregion

        // disable comment warnings for trivial public members.
#pragma warning disable 1591

        #region Overrides

        /// <summary>
        ///     Boolean comparator override.
        /// </summary>
        /// <param name="obj">The object against which to compare the line segment instance.</param>
        /// <returns>Whether or not the line segment and the given object are equivalent.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is DoubleLine)) return false;
            var line = (DoubleLine) obj;
            return _start.Equals(line._start) && _end.Equals(line._end);
        }

        /// <summary>
        ///     Gets the line segment hash code.
        /// </summary>
        /// <returns>The line segment hash code.</returns>
        public override int GetHashCode()
        {
            return _start.GetHashCode() + _end.GetHashCode();
        }

        #endregion

        #region Operators

        /// <summary>
        ///     Tests for equality between two line segments (which reduces to their start and end points being equivalent).
        /// </summary>
        /// <param name="a">The first line segment.</param>
        /// <param name="b">The second line segment.</param>
        /// <returns>Whether or not the line segments are equivalent.</returns>
        public static bool operator ==(DoubleLine a, DoubleLine b)
        {
            return ((a._start == b._start) && (a._end == b._end) || (a._start == b._end) && (a._end == b._start));
        }

        /// <summary>
        ///     Tests for inequality between two line segments (which reduces to their start and end points not being equivalent).
        /// </summary>
        /// <param name="a">The first line segment.</param>
        /// <param name="b">The second line segment.</param>
        /// <returns>Whether or not the line segments are *not* equivalent.</returns>
        public static bool operator !=(DoubleLine a, DoubleLine b)
        {
            return !(a == b);
        }

        /// <summary>
        ///     Subtracts the given line segments in a point-wise fashion (i.e. the start and end points are subtracted
        ///     independently).
        /// </summary>
        /// <param name="a">The minuend line segment.</param>
        /// <param name="b">The subtrahend line segment.</param>
        /// <returns>The line segment resulting from the arithmetic difference between the two given line segments.</returns>
        public static DoubleLine operator -(DoubleLine a, DoubleLine b)
        {
            return new DoubleLine(a._start - b._start, a._end - b._end);
        }

        /// <summary>
        ///     Adds the two given line segments in a point-wise fashion (i.e. the start and end points are subtracted
        ///     indendently).
        /// </summary>
        /// <param name="a">The augend line segment.</param>
        /// <param name="b">The addend line segment.</param>
        /// <returns>The line segment resulting from the arithmetic sum of the two given line segments.</returns>
        public static DoubleLine operator +(DoubleLine a, DoubleLine b)
        {
            return new DoubleLine(a._start + b._start, a._end + b._end);
        }

        #endregion

#pragma warning restore 1591

        #region Static Methods

        /// <summary>
        ///     Calculates the midpoint of the given line segment.
        /// </summary>
        /// <param name="line">The line segment of which to find the midpoint.</param>
        /// <returns>The midpoint of the given line segment.</returns>
        public static DoublePoint CalculateMidpoint(DoubleLine line)
        {
            // Half the sum of both the start and end X and Y coordinates
            var x = (line._start.X + line._end.X) / 2;
            var y = (line._start.Y + line._end.Y) / 2;

            // Return a new point based on those halved coordinates
            return new DoublePoint(x, y);
        }

        /// <summary>
        ///     Calculates the intersection between two line segments.
        /// </summary>
        /// <param name="a">The first line segment.</param>
        /// <param name="b">The segment line segment.</param>
        /// <param name="intersectionFound">Whether or not the lines intersect.</param>
        /// <returns>The point of intersection between the two given line segments.</returns>
        public static DoublePoint CalculateLineIntersection(DoubleLine a, DoubleLine b, out bool intersectionFound)
        {
            // Calculate the determinant's denominator
            var denominator = (a._start.X - a._end.X) * (b._start.Y - b._end.Y) -
                              (a._start.Y - a._end.Y) * (b._start.X - b._end.X);

            if (denominator != 0)
            {
                // Calculate the determinants
                var xDeterminant = ((a._start.X * a._end.Y - a._start.Y * a._end.X) * (b._start.X - b._end.X) -
                                    (a._start.X - a._end.X) * (b._start.X * b._end.Y - b._start.Y * b._end.X)) /
                                   denominator;
                var yDeterminant = ((a._start.X * a._end.Y - a._start.Y * a._end.X) * (b._start.Y - b._end.Y) -
                                    (a._start.Y - a._end.Y) * (b._start.X * b._end.Y - b._start.Y * b._end.X)) /
                                   denominator;

                // Ensure that the intersection point actually lies within both line segments
                if (MathUtils.AlmostGreaterThanOrEqual(xDeterminant, Math.Min(a._start.X, a._end.X)) &&
                    MathUtils.AlmostLessThanOrEqual(xDeterminant, Math.Max(a._start.X, a._end.X)) &&
                    MathUtils.AlmostGreaterThanOrEqual(xDeterminant, Math.Min(b._start.X, b._end.X)) &&
                    MathUtils.AlmostLessThanOrEqual(xDeterminant, Math.Max(b._start.X, b._end.X)) &&
                    MathUtils.AlmostGreaterThanOrEqual(yDeterminant, Math.Min(a._start.Y, a._end.Y)) &&
                    MathUtils.AlmostLessThanOrEqual(yDeterminant, Math.Max(a._start.Y, a._end.Y)) &&
                    MathUtils.AlmostGreaterThanOrEqual(yDeterminant, Math.Min(b._start.Y, b._end.Y)) &&
                    MathUtils.AlmostLessThanOrEqual(yDeterminant, Math.Max(b._start.Y, b._end.Y))
                )
                {
                    intersectionFound = true;
                    return new DoublePoint(xDeterminant, yDeterminant);
                }
            }

            // If the denominator came out to 0 or the point isn't within both 
            // line segments, then the lines don't intersect
            intersectionFound = false;
            return new DoublePoint(0, 0);
        }

        /// <summary>
        ///     Calculates the closest point on the line segment to the given point.
        /// </summary>
        /// <param name="line">The line segment on which to find the closest point.</param>
        /// <param name="point">The source point.</param>
        /// <returns>The closest point on the line segment.</returns>
        public static DoublePoint CalculateLineSegmentClosestPoint(DoubleLine line, DoublePoint point)
        {
            // Calculate the projection of the given point onto the given line
            var numerator = (point.X - line._start.X) * (line._end.X - line._start.X) +
                            (point.Y - line._start.Y) * (line._end.Y - line._start.Y);
            var denominator = DoublePoint.CalculateSquaredDistance(line._start, line._end);
            var projection = numerator / denominator;

            // Return the intersection point on the line segment
            return new DoublePoint(line._start.X + projection * (line._end.X - line._start.X),
                line._start.Y + projection * (line._end.Y - line._start.Y));
        }

        /// <summary>
        ///     Calculates the squared shortest distance between a line segment and point.
        /// </summary>
        /// <param name="line">The line segment involved in the calculation.</param>
        /// <param name="point">The point involved in the calculation.</param>
        /// <returns>The shortest squared distance between the given line segment and point.</returns>
        public static double CalculateSquaredDistanceFromLineToPoint(DoubleLine line, DoublePoint point)
        {
            // Calculate the projection of the given point onto the given line
            var numerator = (point.X - line._start.X) * (line._end.X - line._start.X) +
                            (point.Y - line._start.Y) * (line._end.Y - line._start.Y);
            var denominator = DoublePoint.CalculateSquaredDistance(line._start, line._end);
            var projection = numerator / denominator;

            // If the projection is beyond the segment, return the distance to 
            // either the start or end point on the line segment (whichever 
            // happens to be the shortest)
            if (projection < 0 || projection > 1)
            {
                var distanceToStart = DoublePoint.CalculateSquaredDistance(line._start, point);
                var distanceToEnd = DoublePoint.CalculateSquaredDistance(line._end, point);
                return distanceToStart < distanceToEnd ? distanceToStart : distanceToEnd;
            }


            // Create a point on the line segment from which to measure the distance to the given point
            var segmentPoint = new DoublePoint(line._start.X + projection * (line._end.X - line._start.X),
                line._start.Y + projection * (line._end.Y - line._start.Y));

            // Measure the distance from this point on the segment to the given point
            return DoublePoint.CalculateSquaredDistance(segmentPoint, point);
        }

        /// <summary>
        ///     Calculates the euclidean shortest distance between a line segment and point.
        /// </summary>
        /// <param name="line">The line segment involved in the calculation.</param>
        /// <param name="point">The point involved in the calculation.</param>
        /// <returns>The shortest euclidean distance between the given line segment and point.</returns>
        public static double CalculateEuclideanDistanceFromLineToPoint(DoubleLine line, DoublePoint point)
        {
            return Math.Sqrt(CalculateSquaredDistanceFromLineToPoint(line, point));
        }

        /// <summary>
        ///     Calculates the squared length of the given line segment.
        /// </summary>
        /// <param name="line">The line segment to measure.</param>
        /// <returns>The squared length of the line segment.</returns>
        public static double CalculateSquaredLineSegmentLength(DoubleLine line)
        {
            return DoublePoint.CalculateSquaredDistance(line._start, line._end);
        }

        /// <summary>
        ///     Calculates the length of the given line segment.
        /// </summary>
        /// <param name="line">The line segment to measure.</param>
        /// <returns>The length of the line segment.</returns>
        public static double CalculateLineSegmentLength(DoubleLine line)
        {
            return DoublePoint.CalculateEuclideanDistance(line._start, line._end);
        }

        #endregion
    }
}