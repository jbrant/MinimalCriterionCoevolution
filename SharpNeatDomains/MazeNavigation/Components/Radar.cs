namespace SharpNeat.Domains.MazeNavigation.Components
{
    internal class Radar
    {
        internal Radar(double minFieldOfViewAngle, double maxFieldOfViewAngle, double output)
        {
            MinFieldOfViewAngle = minFieldOfViewAngle;
            MaxFieldOfViewAngle = maxFieldOfViewAngle;
            Output = output;
        }

        internal double MinFieldOfViewAngle { get; }
        internal double MaxFieldOfViewAngle { get; }
        internal double Output { get; set; }

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