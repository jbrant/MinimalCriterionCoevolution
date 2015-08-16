using System.Collections.Generic;

namespace SharpNeat.Domains.MazeNavigation.Components
{
    /// <summary>
    ///     Encapsulates a collection of radars laid out as four parts of a circular configuration.  A radar only fires when
    ///     the goal point is within its field-of-view.
    /// </summary>
    internal class PieSliceSensorArray
    {
        /// <summary>
        ///     Creates a new array of radars with the default fields of view.
        /// </summary>
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

        /// <summary>
        ///     The list of radars.
        /// </summary>
        internal List<Radar> Radars { get; }

        /// <summary>
        ///     The number of radars in the array.
        /// </summary>
        internal int NumRadars => Radars.Count;

        /// <summary>
        ///     Updates each radar in the array based on the given navigator heading and the goal location.
        /// </summary>
        /// <param name="heading">The heading of the navigator to which the radar array is attached.</param>
        /// <param name="location">The location of the goal.</param>
        internal void UpdateRadarArray(double heading, DoublePoint location)
        {
            var target = new DoublePoint(location.X, location.Y);

            // Rotate the target with respect to the heading of the navigator
            target.RotatePoint(-heading, location);

            // Offset by the navigator's current location
            target.X -= location.X;
            target.Y -= location.Y;

            // Get the angle between the navigator and the target
            var navigatorTargetAngle = DoublePoint.CalculateAngleFromOrigin(target);

            // Update every radar in the array based on target alignment
            foreach (var radar in Radars)
            {
                radar.updateRadar(navigatorTargetAngle);
            }
        }

        /// <summary>
        ///     Converts all of the radar outputs into a double array to be fed into the neural network.
        /// </summary>
        /// <returns>The double array of radars outputs.</returns>
        internal double[] GetRadarOutputs()
        {
            var radarOutputs = new double[NumRadars];

            // Iterate through every radar in the array and collect its output
            for (var cnt = 0; cnt < NumRadars; cnt++)
            {
                radarOutputs[cnt] = Radars[cnt].Output;
            }

            return radarOutputs;
        }
    }
}