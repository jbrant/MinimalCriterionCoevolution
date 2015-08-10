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
    }
}