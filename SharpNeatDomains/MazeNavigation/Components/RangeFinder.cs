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

        internal double Range { get; }
        internal double Angle { get; }
        internal double Output { get; set; }
    }
}