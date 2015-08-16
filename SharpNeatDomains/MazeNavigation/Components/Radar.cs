namespace SharpNeat.Domains.MazeNavigation.Components
{
    /// <summary>
    ///     Defines a single pie slice radar that fires when the goal point is within its field-of-view.
    /// </summary>
    internal class Radar
    {
        /// <summary>
        ///     Creeates a radar with the given min/max field-of-view (in degrees) and output (which will be 0 by default).
        /// </summary>
        /// <param name="minFieldOfViewAngle">The minimum field-of-view of the radar (in degrees).</param>
        /// <param name="maxFieldOfViewAngle">The maximum field-of-view of the radar (in degrees).</param>
        /// <param name="output">The initial output value of the radar.</param>
        internal Radar(double minFieldOfViewAngle, double maxFieldOfViewAngle, double output)
        {
            MinFieldOfViewAngle = minFieldOfViewAngle;
            MaxFieldOfViewAngle = maxFieldOfViewAngle;
            Output = output;
        }

        /// <summary>
        ///     The minimum field-of-view (in degrees).
        /// </summary>
        internal double MinFieldOfViewAngle { get; }

        /// <summary>
        ///     The maximum field-of-view (in degrees).
        /// </summary>
        internal double MaxFieldOfViewAngle { get; }

        /// <summary>
        ///     The output value of the radar.
        /// </summary>
        internal double Output { get; set; }

        /// <summary>
        ///     Updates the radar output based on whether the goal location is within its field-of-view.
        /// </summary>
        /// <param name="goalAngle">The angle of the goal location with respect to the navigator.</param>
        internal void updateRadar(double goalAngle)
        {
            // Initialize the radar output to 0 by default (meaning that the vector
            // formed between the target and the navigator does not fall within the
            // radar field-of-view)
            Output = 0;

            // If the angle falls within the field-of-view of the radar, activate the
            // radar output
            if (goalAngle >= MinFieldOfViewAngle && goalAngle < MaxFieldOfViewAngle)
            {
                Output = 1;
            }
            // Otherwise, add 360 and perform the same comparison to handle negative
            // angles
            else if (goalAngle + 360 >= MinFieldOfViewAngle && goalAngle + 360 < MaxFieldOfViewAngle)
            {
                Output = 1;
            }
        }
    }
}