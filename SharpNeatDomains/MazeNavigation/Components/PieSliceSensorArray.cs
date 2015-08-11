using System.Collections.Generic;

namespace SharpNeat.Domains.MazeNavigation.Components
{
    internal class PieSliceSensorArray
    {
        internal PieSliceSensorArray()
        {
            Radars = new List<Radar>(4);

            Radars.Add(new Radar(315, 405, 0));
            Radars.Add(new Radar(45, 135, 0));
            Radars.Add(new Radar(135, 225, 0));
            Radars.Add(new Radar(225, 315, 0));
        }

        internal List<Radar> Radars { get; }
    }
}