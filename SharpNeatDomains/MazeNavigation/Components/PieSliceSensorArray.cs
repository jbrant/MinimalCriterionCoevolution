using System.Collections.Generic;

namespace SharpNeat.Domains.MazeNavigation.Components
{
    internal class PieSliceSensorArray
    {
        internal PieSliceSensorArray()
        {
            Radars = new List<Radar>(4)
            {
                new Radar(315, 405, 0),
                new Radar(45, 135, 0),
                new Radar(135, 225, 0),
                new Radar(225, 315, 0)
            };
        }

        internal List<Radar> Radars { get; }

        internal int NumRadars => Radars.Count;

        internal void UpdateRadarArray(double heading, Point2D location)
        {
            var target = new Point2D(location.x, location.y);

            // Rotate the target with respect to the heading of the navigator
            target.rotate(-heading, location);

            // Offset by the navigator's current location
            target.x -= location.x;
            target.y -= location.y;

            // Get the angle between the navigator and the target
            var navigatorTargetAngle = target.angle();

            // Update every radar in the array based on target alignment
            foreach (var radar in Radars)
            {
                radar.updateRadar(navigatorTargetAngle);
            }
        }

        internal double[] GetRadarOutputs()
        {
            double[] radarOutputs = new double[NumRadars];

            // Iterate through every radar in the array and collect its output
            for (int cnt = 0; cnt < NumRadars; cnt++)
            {
                radarOutputs[cnt] = Radars[cnt].Output;
            }

            return radarOutputs;
        }
    }
}