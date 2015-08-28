using System;

// ReSharper disable NonReadonlyMemberInGetHashCode

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace SharpNeat.Domains
{
    /// <summary>
    ///     Defines a 2D point with double-precision cartesian coordinates.
    /// </summary>
    public struct DoublePoint
    {
        /// <summary>
        ///     X-axis coordinate.
        /// </summary>
        public double X;

        /// <summary>
        ///     Y-axis coordinate.
        /// </summary>
        public double Y;

        #region Constructor

        /// <summary>
        ///     Construct point with the specified coordinates.
        ///     <param name="x">The x-coordinate in two-dimensiona space.</param>
        ///     <param name="y">The y-coordinate in two-dimensiona space.</param>
        /// </summary>
        public DoublePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        #endregion

        #region Instance Methods

        /// <summary>
        ///     Rotates the currrent point around another point at a given angle from the origin.
        /// </summary>
        /// <param name="angle">The angle from the origin.</param>
        /// <param name="point">The point about which to rotate.</param>
        public void RotatePoint(double angle, DoublePoint point)
        {
            // Decrement this point by the given point about which to rotate
            this -= point;

            // Perform the actual rotation
            X = Math.Cos(angle)*X - Math.Sin(angle)*Y;
            Y = Math.Sin(angle)*X + Math.Cos(angle)*Y;

            // Add the coordinates that had been offset back in
            this += point;
        }

        #endregion

        // disable comment warnings for trivial public members.
#pragma warning disable 1591

        #region Overrides

        /// <summary>
        ///     Boolean comparator override.
        /// </summary>
        /// <param name="obj">The object against which to compare the point instance.</param>
        /// <returns>Whether or not the point and the given object are equivalent.</returns>
        public override bool Equals(object obj)
        {
            if (obj is DoublePoint)
            {
                var p = (DoublePoint) obj;
                return (X == p.X) && (Y == p.Y);
            }
            return false;
        }

        /// <summary>
        ///     Gets the point hash code.
        /// </summary>
        /// <returns>The point hash code.</returns>
        public override int GetHashCode()
        {
            return (int) (X + (17*Y));
        }

        #endregion

        #region Operators

        /// <summary>
        ///     Tests for equality between two points (which reduces to their x and y coordinates matching).
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>Whether or not the points are equivalent.</returns>
        public static bool operator ==(DoublePoint a, DoublePoint b)
        {
            return (a.X == b.X) && (a.Y == b.Y);
        }

        /// <summary>
        ///     Tests for inequality between two points (which reduces to either their x or y coordinates not matching).
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>Whether or not the points are *not* equivalent.</returns>
        public static bool operator !=(DoublePoint a, DoublePoint b)
        {
            return (a.X != b.X) || (a.Y != b.Y);
        }

        /// <summary>
        ///     Subtracts the two given points in a coordinate-wise fashion (i.e. x and y coordinates are subtracted
        ///     independently).
        /// </summary>
        /// <param name="a">The minuend point.</param>
        /// <param name="b">The subtrahend point.</param>
        /// <returns>The point resulting from the arithmetic difference between the two given points.</returns>
        public static DoublePoint operator -(DoublePoint a, DoublePoint b)
        {
            return new DoublePoint(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        ///     Adds the two given points in a coordinate-wise fashion (i.e. x and y coordinates are summed independently).
        /// </summary>
        /// <param name="a">The augend point.</param>
        /// <param name="b">The addend point.</param>
        /// <returns>The point resulting from the arithmetic sum of the two given points.</returns>
        public static DoublePoint operator +(DoublePoint a, DoublePoint b)
        {
            return new DoublePoint(a.X + b.X, a.Y + b.Y);
        }

        #endregion

#pragma warning restore 1591

        #region Static Methods

        /// <summary>
        ///     Calculates the angle that the given point makes with the origin in radians.
        /// </summary>
        /// <param name="a">The point whose position to compare with the origin.</param>
        /// <returns>The angle (in radians) that the given point makes with the origin.</returns>
        public static double CalculateAngleFromOrigin(DoublePoint a)
        {
            // If we're both X and Y are zero, the point is at the origin, but consider 
            // this to be 90 degrees (pi/2 radians)
            if (a.X == 0)
            {
                if (a.Y == 0)
                {
                    return Math.PI/2;
                }

                // If only X is 0 but Y isn't, we still can't calculate the slope so 
                // consider this to be 270 degrees (3pi/2 radians)
                return (Math.PI*3)/2;
            }

            // Calculate the slope (this would just be Y/X since it's compared to the 
            // origin) and take the arc tangent (which yields the angle in radians)
            var angle = Math.Atan(a.Y/a.X);

            // If the X coordinate is positive, just return the calculated angle
            if (a.X > 0)
                return angle;

            // Otherwise, return the angle plus 180 degrees (pi radians)
            return angle + Math.PI;
        }

        /// <summary>
        ///     Calculate the squared distance between two points.
        /// </summary>
        /// <param name="a">The first point in two-dimensional space.</param>
        /// <param name="b">The second point in two-dimensional space.</param>
        /// <returns>The squared distance.</returns>
        public static double CalculateSquaredDistance(DoublePoint a, DoublePoint b)
        {
            var xDelta = a.X - b.X;
            var yDelta = a.Y - b.Y;
            return xDelta*xDelta + yDelta*yDelta;
        }

        /// <summary>
        ///     Calculate Euclidean distance between two points.
        /// </summary>
        /// <param name="a">The first point in two-dimensional space.</param>
        /// <param name="b">The second point in two-dimensional space.</param>
        /// <returns>The Euclidean distance.</returns>
        public static double CalculateEuclideanDistance(DoublePoint a, DoublePoint b)
        {
            var xDelta = (a.X - b.X);
            var yDelta = (a.Y - b.Y);
            return Math.Sqrt(xDelta*xDelta + yDelta*yDelta);
        }

        /// <summary>
        ///     Calculate Euclidean distance between two points.
        ///     <param name="a">The point in two-dimensional space.</param>
        ///     <param name="x">The x-coordinate in two-dimensiona space.</param>
        ///     <param name="y">The y-coordinate in two-dimensiona space.</param>
        ///     <returns>The Euclidean distance.</returns>
        /// </summary>
        public static double CalculateEuclideanDistance(DoublePoint a, int x, int y)
        {
            var xDelta = (a.X - x);
            var yDelta = (a.Y - y);
            return Math.Sqrt(xDelta*xDelta + yDelta*yDelta);
        }

        /// <summary>
        ///     Calculate Manhattan distance between two points in two-dimensional space.
        /// </summary>
        /// <param name="a">The first point in two-dimensional space.</param>
        /// <param name="b">The second point in two-dimensional space.</param>
        /// <returns>The manhattan distance.</returns>
        public static double CalculateManhattanDistance(DoublePoint a, DoublePoint b)
        {
            var xDelta = Math.Abs(a.X - b.X);
            var yDelta = Math.Abs(a.Y - b.Y);
            return xDelta + yDelta;
        }

        /// <summary>
        ///     Calculate Manhattan distance between a point and x and y coordinates in two-dimensional space.
        /// </summary>
        /// <param name="a">The point in two-dimensional space.</param>
        /// <param name="x">The x-coordinate in two-dimensiona space.</param>
        /// <param name="y">The y-coordinate in two-dimensiona space.</param>
        /// <returns>The manhattan distance.</returns>
        public static double CalculateManhattanDistance(DoublePoint a, int x, int y)
        {
            var xDelta = Math.Abs(a.X - x);
            var yDelta = Math.Abs(a.Y - y);
            return xDelta + yDelta;
        }

        #endregion
    }
}