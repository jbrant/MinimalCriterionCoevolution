/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace SharpNeat.Domains
{
    /// <summary>
    ///     Defines a 2D point with integer cartesian coordinates.
    /// </summary>
    public struct IntPoint
    {
        /// <summary>
        ///     X-axis coordinate.
        /// </summary>
        public int X;

        /// <summary>
        ///     Y-axis coordinate.
        /// </summary>
        public int Y;

        #region Constructor

        /// <summary>
        ///     Construct point with the specified coordinates.
        ///     <param name="x">The x-coordinate in two-dimensiona space.</param>
        ///     <param name="y">The y-coordinate in two-dimensiona space.</param>
        /// </summary>
        public IntPoint(int x, int y)
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
        public void RotatePoint(double angle, IntPoint point)
        {
            // Decrement this point by the given point about which to rotate
            this -= point;

            // Perform the actual rotation
            X = (int) Math.Cos(angle)*X - (int) Math.Sin(angle)*Y;
            Y = (int) Math.Sin(angle)*X + (int) Math.Cos(angle)*Y;

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
            if (obj is IntPoint)
            {
                var p = (IntPoint) obj;
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
            return X + (17*Y);
        }

        #endregion

        #region Operators

        /// <summary>
        ///     Tests for equality between two points (which reduces to their x and y coordinates matching).
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>Whether or not the points are equivalent.</returns>
        public static bool operator ==(IntPoint a, IntPoint b)
        {
            return (a.X == b.X) && (a.Y == b.Y);
        }

        /// <summary>
        ///     Tests for inequality between two points (which reduces to either their x or y coordinates not matching).
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>Whether or not the points are *not* equivalent.</returns>
        public static bool operator !=(IntPoint a, IntPoint b)
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
        public static IntPoint operator -(IntPoint a, IntPoint b)
        {
            return new IntPoint(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        ///     Adds the two given points in a coordinate-wise fashion (i.e. x and y coordinates are summed independently).
        /// </summary>
        /// <param name="a">The augend point.</param>
        /// <param name="b">The addend point.</param>
        /// <returns>The point resulting from the arithmetic sum of the two given points.</returns>
        public static IntPoint operator +(IntPoint a, IntPoint b)
        {
            return new IntPoint(a.X + b.X, a.Y + b.Y);
        }

        #endregion

#pragma warning restore 1591

        #region Static Methods

        /// <summary>
        ///     Calculates the angle that the given point makes with the origin in radians.
        /// </summary>
        /// <param name="a">The point whose position to compare with the origin.</param>
        /// <returns>The angle (in radians) that the given point makes with the origin.</returns>
        public static double CalculateAngleFromOrigin(IntPoint a)
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
        public static int CalculateSquaredDistance(IntPoint a, IntPoint b)
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
        public static double CalculateEuclideanDistance(IntPoint a, IntPoint b)
        {
            double xDelta = (a.X - b.X);
            double yDelta = (a.Y - b.Y);
            return Math.Sqrt(xDelta*xDelta + yDelta*yDelta);
        }

        /// <summary>
        ///     Calculate Euclidean distance between two points.
        ///     <param name="a">The point in two-dimensional space.</param>
        ///     <param name="x">The x-coordinate in two-dimensiona space.</param>
        ///     <param name="y">The y-coordinate in two-dimensiona space.</param>
        ///     <returns>The Euclidean distance.</returns>
        /// </summary>
        public static double CalculateEuclideanDistance(IntPoint a, int x, int y)
        {
            double xDelta = (a.X - x);
            double yDelta = (a.Y - y);
            return Math.Sqrt(xDelta*xDelta + yDelta*yDelta);
        }

        /// <summary>
        ///     Calculate Manhattan distance between two points in two-dimensional space.
        /// </summary>
        /// <param name="a">The first point in two-dimensional space.</param>
        /// <param name="b">The second point in two-dimensional space.</param>
        /// <returns>The manhattan distance.</returns>
        public static double CalculateManhattanDistance(IntPoint a, IntPoint b)
        {
            double xDelta = Math.Abs(a.X - b.X);
            double yDelta = Math.Abs(a.Y - b.Y);
            return xDelta + yDelta;
        }

        /// <summary>
        ///     Calculate Manhattan distance between a point and x and y coordinates in two-dimensional space.
        /// </summary>
        /// <param name="a">The point in two-dimensional space.</param>
        /// <param name="x">The x-coordinate in two-dimensiona space.</param>
        /// <param name="y">The y-coordinate in two-dimensiona space.</param>
        /// <returns>The manhattan distance.</returns>
        public static double CalculateManhattanDistance(IntPoint a, int x, int y)
        {
            double xDelta = Math.Abs(a.X - x);
            double yDelta = Math.Abs(a.Y - y);
            return xDelta + yDelta;
        }

        #endregion
    }
}