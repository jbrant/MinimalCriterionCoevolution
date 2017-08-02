#region

using System;
using System.Collections.Generic;

#endregion

namespace SharpNeat.Domains.MazeNavigation.Components
{
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
        internal static readonly double Radius = 8.0;

        /// <summary>
        ///     Creates a MazeNavigator with the given starting location.  Also sets up the range finders and radar array for the
        ///     navigator.
        /// </summary>
        /// <param name="location">The starting location of the navigator.</param>
        public MazeNavigator(DoublePoint location)
        {
            Heading = 0;
            Speed = 0;
            AngularVelocity = 0;

            Location = location;

            RangeFinders = new List<RangeFinder>(6)
            {
                new RangeFinder(100, -180, 0),
                new RangeFinder(100, -90, 0),
                new RangeFinder(100, -45, 0),
                new RangeFinder(100, 0, 0),
                new RangeFinder(100, 45, 0),
                new RangeFinder(100, 90, 0)                
            };

            RadarArray = new PieSliceSensorArray();
        }

        /// <summary>
        ///     The directional heading of the navigator in degrees.
        /// </summary>
        public double Heading { get; set; }

        /// <summary>
        ///     The Speed of the navigator (in units per timestep).
        /// </summary>
        public double Speed { get; set; }

        /// <summary>
        ///     The angular velocity of the navigator.
        /// </summary>
        public double AngularVelocity { get; set; }

        /// <summary>
        ///     The current location of the navigator (this is simply a cartesian coordinate).
        /// </summary>
        public DoublePoint Location { get; private set; }

        /// <summary>
        ///     The list of range finder sensors attached to the navigator.
        /// </summary>
        public List<RangeFinder> RangeFinders { get; }

        /// <summary>
        ///     The array of pie slice radars attached to the navigator.
        /// </summary>
        public PieSliceSensorArray RadarArray { get; }

        /// <summary>
        ///     Moves the navigator to a new location based on its heading, speed, and angular velocity.  The point to which it
        ///     moves is also dictated by the presence of walls that might be obstructing its path.
        /// </summary>
        /// <param name="walls">The list of walls in the environment.</param>
        /// <param name="targetLocation">The location of the target (goal).</param>
        /// <param name="applyBridging">Whether or not to apply bridging "help".</param>
        /// <param name="curBridgingApplications">
        ///     The current number of times bridging been applied.  This value
        ///     will be incremented and the updated value used by the calling routine.
        /// </param>
        public void Move(List<Wall> walls, DoublePoint targetLocation, bool applyBridging,
            ref int curBridgingApplications)
        {
            // Compute angular velocity components
            var angularVelocityX = Math.Cos(MathUtils.toRadians(Heading))*Speed;
            var angularVelocityY = Math.Sin(MathUtils.toRadians(Heading))*Speed;

            // Set the new heading by incrementing by the angular velocity
            Heading += AngularVelocity;

            // If the navigator's resulting heading is greater than 360 degrees,
            // it has performed more than a complete rotation, so subtract 360 
            // degrees to have a valid heading
            if (Heading > 360)
            {
                Heading -= 360;
            }
            // On the other hand, if the heading is negative, the same has happened
            // in the other direction.  So add 360 degrees
            else if (Heading < 0)
            {
                Heading += 360;
            }

            // Determine the new location, incremented by the X and Y component velocities
            var newLocation = new DoublePoint(angularVelocityX + Location.X, angularVelocityY + Location.Y);

            // Declare variable in which to store the colliding wall in the event of a potential collision
            Wall collidingWall;

            // Move the navigator to the new location only if said movement does not
            // result in a wall collision
            if (IsCollision(newLocation, walls, out collidingWall) == false)
            {
                Location = new DoublePoint(newLocation.X, newLocation.Y);
            }
            // Otherwise, if there was a collision and the bridging indicator is set, calculate an 
            // adjusted heading based on the properties of the wall with which the navigator collided 
            // and the side of that wall that it hit
            else if (applyBridging)
            {
                Heading = collidingWall.CalculateAdjustedHeading(Heading, Location);
                curBridgingApplications++;
            }

            // Update range finders and radar array
            UpdateRangeFinders(walls);
            RadarArray.UpdateRadarArray(Heading, Location, targetLocation);
        }

        /// <summary>
        ///     Updates the output of each of the range finders based on the proximity of walls in its respective direction.
        /// </summary>
        /// <param name="walls">The list of walls in the environment.</param>
        private void UpdateRangeFinders(List<Wall> walls)
        {
            // Update each range finder on the navigator
            foreach (var rangeFinder in RangeFinders)
            {
                rangeFinder.Update(walls, Heading, Location);
            }
        }

        /// <summary>
        ///     Determines whether the newly proposed location will result in a collision.
        /// </summary>
        /// <param name="newLocation">The location to which the navigator is prepped to move.</param>
        /// <param name="walls">The list of walls in the environment.</param>
        /// <param name="collidingWall">Output parameter recording the wall at which the collision would occur.</param>
        /// <returns>Whether or not the proposed move will result in a collision.</returns>
        private bool IsCollision(DoublePoint newLocation, List<Wall> walls, out Wall collidingWall)
        {
            var doesCollide = false;
            collidingWall = null;

            // Iterate through all of the walls, determining if the traversal to the
            // newly proposed location will result in a collision
            foreach (Wall wall in walls)
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
            var annInputs = new double[RangeFinders.Count + RadarArray.NumRadars];

            // Get the output of every range finder
            foreach (var rangeFinder in RangeFinders)
            {
                annInputs[annInputCnt++] = rangeFinder.Output/rangeFinder.Range;
            }

            // Get the output of every radar
            foreach (var radarOutput in RadarArray.GetRadarOutputs())
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
            AngularVelocity += (rotationQuantity - AnnOutputScalingFactor);
            Speed += (propulsionQuantity - AnnOutputScalingFactor);

            // Impose navigator speed constraints
            if (Speed > MaxSpeed)
            {
                Speed = MaxSpeed;
            }
            else if (Speed < MinSpeed)
            {
                Speed = MinSpeed;
            }

            // Impose navigator angular velocity constraints
            if (AngularVelocity > MaxAngularVelocity)
            {
                AngularVelocity = MaxAngularVelocity;
            }
            else if (AngularVelocity < MinAngularVelocity)
            {
                AngularVelocity = MinAngularVelocity;
            }
        }
    }
}