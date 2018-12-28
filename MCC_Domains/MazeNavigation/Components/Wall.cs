using MCC_Domains.Common;

namespace MCC_Domains.MazeNavigation.Components
{
    /// <summary>
    ///     Encapsulates a wall (2D line segment) abstraction along with supporting metadata regarding heading adjustments
    ///     given approach from different directions.
    /// </summary>
    public class Wall
    {
        #region Wall Properties

        /// <summary>
        ///     The 2D line segment representing the start and end point of the wall.
        /// </summary>
        public DoubleLine WallLine { get; }

        #endregion

        #region Instance methods

        /// <summary>
        ///     Calculates the adjusted heading based on the current position in relation to the colliding wall.
        /// </summary>
        /// <param name="currentHeading">The current heading of the navigator.</param>
        /// <param name="currentPosition">The current position of the navigator.</param>
        /// <returns>The updated navigator heading.</returns>
        public double CalculateAdjustedHeading(double currentHeading, DoublePoint currentPosition)
        {
            double adjustedHeading = 0;

            // Calculate the closest point on the colliding wall from the current position
            DoublePoint closestPointOnWall = DoubleLine.CalculateLineSegmentClosestPoint(WallLine, currentPosition);

            // Adjust the heading based on whether the navigator is approaching from the left or the right
            adjustedHeading = currentPosition.X < closestPointOnWall.X
                ? currentHeading + _leftApproachAdjustment
                : currentHeading + _rightApproachAdjustment;

            // If the navigator's resulting heading is greater than 360 degrees,
            // it has performed more than a complete rotation, so subtract 360 
            // degrees to have a valid heading
            if (adjustedHeading > 360)
            {
                adjustedHeading -= 360;
            }
            // On the other hand, if the heading is negative, the same has happened
            // in the other direction.  So add 360 degrees
            else if (adjustedHeading < 0)
            {
                adjustedHeading += 360;
            }

            return adjustedHeading;
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Wall constructor which accepts online the line definition.
        /// </summary>
        /// <param name="wallLine">The line representing a wall in the maze.</param>
        public Wall(DoubleLine wallLine) : this(wallLine, 0, 0, 0)
        {
        }

        /// <summary>
        ///     Wall constructor which accepts the wall line as well as heading adjustments.
        /// </summary>
        /// <param name="wallLine">The 2D line segment representing the start and end point of the wall.</param>
        /// <param name="leftApproachAdjustmentCoefficient">
        ///     The direction in which to adjust the heading given an approach from the
        ///     west.
        /// </param>
        /// <param name="rightApproachAdjustmentCoefficient">
        ///     The direction in which to adjust the heading given an approach from the
        ///     east.
        /// </param>
        /// <param name="headingAdjustmentMagnitude">The "amount" (magnitude) in degrees by which the heading should be adjusted.</param>
        public Wall(DoubleLine wallLine, int leftApproachAdjustmentCoefficient, int rightApproachAdjustmentCoefficient,
            int headingAdjustmentMagnitude)
        {
            WallLine = wallLine;
            _leftApproachAdjustment = leftApproachAdjustmentCoefficient*headingAdjustmentMagnitude;
            _rightApproachAdjustment = rightApproachAdjustmentCoefficient*headingAdjustmentMagnitude;
        }

        #endregion

        #region Wall instance variables

        /// <summary>
        ///     The heading adjustment (in degrees) given an approach from the west.
        /// </summary>
        private readonly int _leftApproachAdjustment;

        /// <summary>
        ///     The heading adjustment (in degrees) given an approach from the east.
        /// </summary>
        private readonly int _rightApproachAdjustment;

        #endregion
    }
}