﻿#region

using System.Collections.Generic;
using MCC_Domains.Common;

#endregion

namespace MCC_Domains.MazeNavigation.Components
{
    /// <summary>
    ///     Encapsulates a collection of radars laid out as four parts of a circular configuration.  A radar only fires when
    ///     the goal point is within its field-of-view.
    /// </summary>
    public class PieSliceSensorArray
    {
        /// <summary>
        ///     The list of radars.
        /// </summary>
        private readonly List<Radar> _radars;

        /// <summary>
        ///     Creates a new array of radars with the default fields of view.
        /// </summary>
        internal PieSliceSensorArray()
        {
            _radars = new List<Radar>(4)
            {
                new Radar(315, 405, 0),
                new Radar(45, 135, 0),
                new Radar(135, 225, 0),
                new Radar(225, 315, 0)
            };
        }

        /// <summary>
        ///     The number of radars in the array.
        /// </summary>
        internal int NumRadars => _radars.Count;

        /// <summary>
        ///     Updates each radar in the array based on the given navigator heading and the goal location.
        /// </summary>
        /// <param name="heading">The heading of the navigator to which the radar array is attached.</param>
        /// <param name="location">The location of the goal.</param>
        /// <param name="targetLocation">The location of the target (goal).</param>
        internal void UpdateRadarArray(double heading, DoublePoint location, DoublePoint targetLocation)
        {
            var target = targetLocation;

            // Rotate the target with respect to the heading of the navigator
            target.RotatePoint(-heading, location);

            // Offset by the navigator's current location
            target.X -= location.X;
            target.Y -= location.Y;

            // Get the angle between the navigator and the target
            var navigatorTargetAngle = DoublePoint.CalculateAngleFromOrigin(target);

            // Update every radar in the array based on target alignment
            foreach (var radar in _radars)
            {
                radar.UpdateRadar(navigatorTargetAngle);
            }
        }

        /// <summary>
        ///     Converts all of the radar outputs into a double array to be fed into the neural network.
        /// </summary>
        /// <returns>The double array of radars outputs.</returns>
        internal IEnumerable<double> GetRadarOutputs()
        {
            var radarOutputs = new double[NumRadars];

            // Iterate through every radar in the array and collect its output
            for (var cnt = 0; cnt < NumRadars; cnt++)
            {
                radarOutputs[cnt] = _radars[cnt].Output;
            }

            return radarOutputs;
        }
    }
}