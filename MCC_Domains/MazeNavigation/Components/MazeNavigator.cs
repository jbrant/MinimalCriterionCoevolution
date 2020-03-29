#region

using System;
using System.Collections.Generic;
using MCC_Domains.Common;
using MCC_Domains.Utils;

#endregion

namespace MCC_Domains.MazeNavigation.Components
{
    /// <summary>
    ///     Encapsulates properties of the maze navigator agent.
    /// </summary>
    public class MazeNavigator
    {
        /// <summary>
        ///     The minimum speed of the navigator.
        /// </summary>
        private const double MinSpeed = -3.0;

        /// <summary>
        ///     The maximum speed of the navigator.
        /// </summary>
        private const double MaxSpeed = 3.0;

        /// <summary>
        ///     The minimum angular velocity of the navigator.
        /// </summary>
        private const double MinAngularVelocity = -3.0;

        /// <summary>
        ///     The maximum angular velocity of the navigator.
        /// </summary>
        private const double MaxAngularVelocity = 3.0;

        /// <summary>
        ///     The value by which to scale ANN outputs (amount to subtracting this from the output).
        /// </summary>
        private const double AnnOutputScalingFactor = 0.5;

        /// <summary>
        ///     The radius of the navigator.
        /// </summary>
        private const double Radius = 8.0;

        /// <summary>
        ///     Creates a MazeNavigator with the given starting location.  Also sets up the range finders and radar array for the
        ///     navigator.
        /// </summary>
        /// <param name="location">The starting location of the navigator.</param>
        public MazeNavigator(DoublePoint location)
        {
            _heading = 0;
            _speed = 0;
            _angularVelocity = 0;

            Location = location;

            _rangeFinders = new List<RangeFinder>(6)
            {
                new RangeFinder(100, -180, 0),
                new RangeFinder(100, -90, 0),
                new RangeFinder(100, -45, 0),
                new RangeFinder(100, 0, 0),
                new RangeFinder(100, 45, 0),
                new RangeFinder(100, 90, 0)
            };

            _radarArray = new PieSliceSensorArray();
        }

        /// <summary>
        ///     The directional heading of the navigator in degrees.
        /// </summary>
        private double _heading;

        /// <summary>
        ///     The Speed of the navigator (in units per timestep).
        /// </summary>
        private double _speed;

        /// <summary>
        ///     The angular velocity of the navigator.
        /// </summary>
        private double _angularVelocity;

        /// <summary>
        ///     The current location of the navigator (this is simply a cartesian coordinate).
        /// </summary>
        public DoublePoint Location { get; private set; }

        /// <summary>
        ///     The list of range finder sensors attached to the navigator.
        /// </summary>
        private readonly List<RangeFinder> _rangeFinders;

        /// <summary>
        ///     The array of pie slice radars attached to the navigator.
        /// </summary>
        private readonly PieSliceSensorArray _radarArray;

        /// <summary>
        ///     Moves the navigator to a new location based on its heading, speed, and angular velocity.  The point to which it
        ///     moves is also dictated by the presence of walls that might be obstructing its path.
        /// </summary>
        /// <param name="walls">The list of walls in the environment.</param>
        /// <param name="targetLocation">The location of the target (goal).</param>
        public void Move(List<Wall> walls, DoublePoint targetLocation)
        {
            // Compute angular velocity components
            var angularVelocityX = Math.Cos(MathUtils.ToRadians(_heading)) * _speed;
            var angularVelocityY = Math.Sin(MathUtils.ToRadians(_heading)) * _speed;

            // Set the new heading by incrementing by the angular velocity
            _heading += _angularVelocity;

            // If the navigator's resulting heading is greater than 360 degrees,
            // it has performed more than a complete rotation, so subtract 360 
            // degrees to have a valid heading
            if (_heading > 360)
            {
                _heading -= 360;
            }
            // On the other hand, if the heading is negative, the same has happened
            // in the other direction.  So add 360 degrees
            else if (_heading < 0)
            {
                _heading += 360;
            }

            // Determine the new location, incremented by the X and Y component velocities
            var newLocation = new DoublePoint(angularVelocityX + Location.X, angularVelocityY + Location.Y);

            // Move the navigator to the new location only if said movement does not
            // result in a wall collision
            if (IsCollision(newLocation, walls, out var collidingWall) == false)
            {
                Location = new DoublePoint(newLocation.X, newLocation.Y);
            }

            // Update range finders and radar array
            UpdateRangeFinders(walls);
            _radarArray.UpdateRadarArray(_heading, Location, targetLocation);
        }

        /// <summary>
        ///     Updates the output of each of the range finders based on the proximity of walls in its respective direction.
        /// </summary>
        /// <param name="walls">The list of walls in the environment.</param>
        private void UpdateRangeFinders(List<Wall> walls)
        {
            // Update each range finder on the navigator
            foreach (var rangeFinder in _rangeFinders)
            {
                rangeFinder.Update(walls, _heading, Location);
            }
        }

        /// <summary>
        ///     Determines whether the newly proposed location will result in a collision.
        /// </summary>
        /// <param name="newLocation">The location to which the navigator is prepped to move.</param>
        /// <param name="walls">The list of walls in the environment.</param>
        /// <param name="collidingWall">Output parameter recording the wall at which the collision would occur.</param>
        /// <returns>Whether or not the proposed move will result in a collision.</returns>
        private static bool IsCollision(DoublePoint newLocation, IList<Wall> walls, out Wall collidingWall)
        {
            var doesCollide = false;
            collidingWall = null;

            // Iterate through all of the walls, determining if the traversal to the
            // newly proposed location will result in a collision
            foreach (var wall in walls)
            {
                // If the distance between the wall and the new location is less than
                // the radius of the navigator itself, then a collision will occur
                if (!(DoubleLine.CalculateEuclideanDistanceFromLineToPoint(wall.WallLine, newLocation) < Radius))
                    continue;

                // If we made it here, there is a collision, so set the flag, record the colliding wall, and break
                doesCollide = true;
                collidingWall = wall;

                break;
            }

            return doesCollide;
        }

        /// <summary>
        ///     Gathers all of the range finder, radar, and bias outputs into a double array to be fed directly into the neural
        ///     network.
        /// </summary>
        /// <returns>A double array that is to be used as the ANN input signal array.</returns>
        internal double[] GetAnnInputs()
        {
            var annInputCnt = 0;

            // Create ANN input array with a separate input for each range finder and
            // radar, as well as an additional input for the bias
            var annInputs = new double[_rangeFinders.Count + _radarArray.NumRadars];

            // Get the output of every range finder
            foreach (var rangeFinder in _rangeFinders)
            {
                annInputs[annInputCnt++] = rangeFinder.Output / rangeFinder.Range;
            }

            // Get the output of every radar
            foreach (var radarOutput in _radarArray.GetRadarOutputs())
            {
                annInputs[annInputCnt++] = radarOutput;
            }

            return annInputs;
        }

        /// <summary>
        ///     Updates the navigator state based on the output of the neureal network.  There are two outputs, which increment or
        ///     decrement the navigator's angular velocity and speed.
        /// </summary>
        /// <param name="rotationQuantity">
        ///     The rotation quantity output by the network which affects the navigator's angular
        ///     velocity.
        /// </param>
        /// <param name="propulsionQuantity">The propulsion quantity output by the network which affects the navigator's speed.</param>
        internal void TranslateAndApplyAnnOutputs(double rotationQuantity, double propulsionQuantity)
        {
            // Adjust the angular velocity and speed based on the neural net outputs
            _angularVelocity += (rotationQuantity - AnnOutputScalingFactor);
            _speed += (propulsionQuantity - AnnOutputScalingFactor);

            // Impose navigator speed constraints
            if (_speed > MaxSpeed)
            {
                _speed = MaxSpeed;
            }
            else if (_speed < MinSpeed)
            {
                _speed = MinSpeed;
            }

            // Impose navigator angular velocity constraints
            if (_angularVelocity > MaxAngularVelocity)
            {
                _angularVelocity = MaxAngularVelocity;
            }
            else if (_angularVelocity < MinAngularVelocity)
            {
                _angularVelocity = MinAngularVelocity;
            }
        }
    }
}