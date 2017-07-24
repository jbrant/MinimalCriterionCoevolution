using System;

namespace SharpNeat.Domains
{
    /// <summary>
    ///     Defines a 2D line segment with 2D integer start and end points.
    /// </summary>
    public struct IntLine
    {
        /// <summary>
        ///     End point of the line segment.
        /// </summary>
        public readonly IntPoint End;

        /// <summary>
        ///     Start point of the line segment.
        /// </summary>
        public readonly IntPoint Start;

        #region Constructor

        /// <summary>
        ///     Construct line segment with the specified start and end points.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        public IntLine(IntPoint startPoint, IntPoint endPoint)
        {
            Start = startPoint;
            End = endPoint;
        }

        /// <summary>
        ///     Construct line segment with the specified start and end coordinates.
        /// </summary>
        /// <param name="x1">The start point X-coordinate.</param>
        /// <param name="y1">The start point Y-coordinate.</param>
        /// <param name="x2">The end point X-coordinate.</param>
        /// <param name="y2">The end point Y-coordinate.</param>
        public IntLine(int x1, int y1, int x2, int y2) : this(new IntPoint(x1, y1), new IntPoint(x2, y2))
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
            if (!(obj is IntLine)) return false;
            var line = (IntLine) obj;
            return Start.Equals(line.Start) && End.Equals(line.End);
        }

        /// <summary>
        ///     Gets the line segment hash code.
        /// </summary>
        /// <returns>The line segment hash code.</returns>
        public override int GetHashCode()
        {
            return Start.GetHashCode() + End.GetHashCode();
        }

        #endregion

        #region Operators

        /// <summary>
        ///     Tests for equality between two line segments (which reduces to their start and end points being equivalent).
        /// </summary>
        /// <param name="a">The first line segment.</param>
        /// <param name="b">The second line segment.</param>
        /// <returns>Whether or not the line segments are equivalent.</returns>
        public static bool operator ==(IntLine a, IntLine b)
        {
            return ((a.Start == b.Start) && (a.End == b.End) || (a.Start == b.End) && (a.End == b.Start));
        }

        /// <summary>
        ///     Tests for inequality between two line segments (which reduces to their start and end points not being equivalent).
        /// </summary>
        /// <param name="a">The first line segment.</param>
        /// <param name="b">The second line segment.</param>
        /// <returns>Whether or not the line segments are *not* equivalent.</returns>
        public static bool operator !=(IntLine a, IntLine b)
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
        public static IntLine operator -(IntLine a, IntLine b)
        {
            return new IntLine(a.Start - b.Start, a.End - b.End);
        }

        /// <summary>
        ///     Adds the two given line segments in a point-wise fashion (i.e. the start and end points are subtracted
        ///     indendently).
        /// </summary>
        /// <param name="a">The augend line segment.</param>
        /// <param name="b">The addend line segment.</param>
        /// <returns>The line segment resulting from the arithmetic sum of the two given line segments.</returns>
        public static IntLine operator +(IntLine a, IntLine b)
        {
            return new IntLine(a.Start + b.Start, a.End + b.End);
        }

        #endregion

#pragma warning restore 1591

        #region Static Methods

        /// <summary>
        ///     Calculates the midpoint of the given line segment.
        /// </summary>
        /// <param name="line">The line segment of which to find the midpoint.</param>
        /// <returns>The midpoint of the given line segment.</returns>
        public static IntPoint CalculateMidpoint(IntLine line)
        {
            // Half the sum of both the start and end X and Y coordinates
            var x = (line.Start.X + line.End.X)/2;
            var y = (line.Start.Y + line.End.Y)/2;

            // Return a new point based on those halved coordinates
            return new IntPoint(x, y);
        }

        /// <summary>
        ///     Calculates the intersection between two line segments.
        /// </summary>
        /// <param name="a">The first line segment.</param>
        /// <param name="b">The segment line segment.</param>
        /// <param name="intersectionFound">Whether or not the lines intersect.</param>
        /// <returns>The point of intersection between the two given line segments.</returns>
        public static IntPoint CalculateIntersection(IntLine a, IntLine b, out bool intersectionFound)
        {
            // Calculate the determinant's denominator
            var denominator = (a.Start.X - a.End.X)*(b.Start.Y - b.End.Y) - (a.Start.Y - a.End.Y)*(b.Start.X - b.End.X);

            if (denominator != 0)
            {
                // Calculate the determinants
                var xDeterminant = ((a.Start.X*a.End.Y - a.Start.Y*a.End.X)*(b.Start.X - b.End.X) -
                                    (a.Start.X - a.End.X)*(b.Start.X*b.End.Y - b.Start.Y*b.End.X))/denominator;
                var yDeterminant = ((a.Start.X*a.End.Y - a.Start.Y*a.End.X)*(b.Start.Y - b.End.Y) -
                                    (a.Start.Y - a.End.Y)*(b.Start.X*b.End.Y - b.Start.Y*b.End.X))/denominator;

                // Ensure that the intersection point actually lies within both line segments
                if (xDeterminant >= Math.Min(a.Start.X, a.End.X) && xDeterminant <= Math.Max(a.Start.X, a.End.X) &&
                    xDeterminant >= Math.Min(b.Start.X, b.End.X) && xDeterminant <= Math.Max(b.Start.X, b.End.X))
                {
                    intersectionFound = true;
                    return new IntPoint(xDeterminant, yDeterminant);
                }
            }

            // If the denominator came out to 0 or the point isn't within both 
            // line segments, then the lines don't intersect
            intersectionFound = false;
            return new IntPoint(0, 0);
        }

        /// <summary>
        ///     Calculates the squared shortest distance between a line segment and point.
        /// </summary>
        /// <param name="line">The line segment involved in the calculation.</param>
        /// <param name="point">The point involved in the calculation.</param>
        /// <returns>The shortest squared distance between the given line segment and point.</returns>
        public static int CalculateSquaredDistanceFromLineToPoint(IntLine line, IntPoint point)
        {
            // Calculate the projection of the given point onto the given line
            var numerator = (point.X - line.Start.X)*(line.End.X - line.Start.X) +
                            (point.Y - line.Start.Y)*(line.End.Y - line.Start.Y);
            var denominator = IntPoint.CalculateSquaredDistance(line.Start, line.End);
            double projection = numerator/denominator;

            // If the projection is beyond the segment, return the distance to 
            // either the start or end point on the line segment (whichever 
            // happens to be the shortest)
            if (projection < 0 || projection > 1)
            {
                var distanceToStart = IntPoint.CalculateSquaredDistance(line.Start, point);
                var distanceToEnd = IntPoint.CalculateSquaredDistance(line.End, point);
                return distanceToStart < distanceToEnd ? distanceToStart : distanceToEnd;
            }


            // Create a point on the line segment from which to measure the distance to the given point
            var segmentPoint = new IntPoint(line.Start.X + (int) projection*(line.End.X - line.Start.X),
                line.Start.Y + (int) projection*(line.End.Y - line.Start.Y));

            // Measure the distance from this point on the segment to the given point
            return IntPoint.CalculateSquaredDistance(segmentPoint, point);
        }

        /// <summary>
        ///     Calculates the euclidean shortest distance between a line segment and point.
        /// </summary>
        /// <param name="line">The line segment involved in the calculation.</param>
        /// <param name="point">The point involved in the calculation.</param>
        /// <returns>The shortest euclidean distance between the given line segment and point.</returns>
        public static double CalculateEuclideanDistanceFromLineToPoint(IntLine line, IntPoint point)
        {
            return Math.Sqrt(CalculateSquaredDistanceFromLineToPoint(line, point));
        }

        /// <summary>
        ///     Calculates the squared length of the given line segment.
        /// </summary>
        /// <param name="line">The line segment to measure.</param>
        /// <returns>The squared length of the line segment.</returns>
        public static int CalculateSquaredLineSegmentLength(IntLine line)
        {
            return IntPoint.CalculateSquaredDistance(line.Start, line.End);
        }

        /// <summary>
        ///     Calculates the length of the given line segment.
        /// </summary>
        /// <param name="line">The line segment to measure.</param>
        /// <returns>The length of the line segment.</returns>
        public static double CalculateLineSegmentLength(IntLine line)
        {
            return IntPoint.CalculateEuclideanDistance(line.Start, line.End);
        }

        #endregion
    }
}