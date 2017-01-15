#region

using System;

#endregion

namespace SharpNeat.Domains
{
    /// <summary>
    ///     Common utility methods used in experiment calculations.
    /// </summary>
    public static class MathUtils
    {
        private const double ROUNDING_ERROR = 1e-12;

        /// <summary>
        ///     Converts a given in angle (in degrees) to radians.
        /// </summary>
        /// <param name="angleInDegrees">The angle in degrees.</param>
        /// <returns>The corresponding value in radians.</returns>
        public static double toRadians(double angleInDegrees)
        {
            return (Math.PI/180)*angleInDegrees;
        }

        /// <summary>
        ///     Converts a given angle (in radians) to degrees.
        /// </summary>
        /// <param name="angleInRadians">The angle in radians.</param>
        /// <returns>The corresponding angle in degrees.</returns>
        public static double toDegrees(double angleInRadians)
        {
            return (180/Math.PI)*angleInRadians;
        }

        /// <summary>
        ///     Compares two double-precision floating point values to see if the first is greater than the last and allowing for a
        ///     small margin of rounding error in the equality comparison.
        /// </summary>
        /// <param name="item1">The first item to compare.</param>
        /// <param name="item2">The second item to compare.</param>
        /// <returns>Whether the first is greater than or equal to the last.</returns>
        public static bool AlmostGreaterThanOrEqual(double item1, double item2)
        {
            bool result = false;

            // Check if values are equal
            if (Math.Abs(item1 - item2) <= ROUNDING_ERROR)
            {
                result = true;
            }
            // Check if the first is greater than the last
            else if (item1 > item2)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        ///     Compares two double-precision floating point values to see if the first is less than the last and allowing for a
        ///     small margin of rounding error in the equality comparison.
        /// </summary>
        /// <param name="item1">The first item to compare.</param>
        /// <param name="item2">The second item to compare.</param>
        /// <returns>Whether the first is less than or equal to the last.</returns>
        public static bool AlmostLessThanOrEqual(double item1, double item2)
        {
            bool result = false;

            // Check if values are equal
            if (Math.Abs(item1 - item2) <= ROUNDING_ERROR)
            {
                result = true;
            }
            // Check if the first is less than the last
            else if (item1 < item2)
            {
                result = true;
            }

            return result;
        }
    }
}