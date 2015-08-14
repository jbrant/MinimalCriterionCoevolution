using System;
using System.Collections.Generic;

namespace SharpNeat.Domains.MazeNavigation.Components
{
    internal class MazeNavigator
    {
        private const double MinSpeed = -3.0;
        private const double MaxSpeed = 3.0;
        private const double MinAngularVelocity = -3.0;
        private const double MaxAngularVelocity = 3.0;
        private const double AnnOutputScalingFactor = 0.5;
        internal static readonly double Radius = 8.0;

        public MazeNavigator(Point2D location)
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

        internal double Heading { get; private set; }
        internal double Speed { get; private set; }
        internal double AngularVelocity { get; private set; }
        internal Point2D Location { get; private set; }
        internal List<RangeFinder> RangeFinders { get; }
        internal PieSliceSensorArray RadarArray { get; }

        internal void Move(List<Line2D> walls)
        {
            // Compute angular velocity components
            var angularVelocityX = Math.Cos(MathUtils.toRadians(Heading)*Speed);
            var angularVelocityY = Math.Sin(MathUtils.toRadians(Heading)*Speed);

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
            var newLocation = new Point2D(angularVelocityX + Location.x, angularVelocityY + Location.y);

            // Move the navigator to the new location only if said movement does not
            // result in a wall collision
            if (IsCollision(newLocation, walls) == false)
            {
                Location = new Point2D(Location.x, Location.y);
            }

            // Update range finders and radar array
            UpdateRangeFinders(walls);
            RadarArray.UpdateRadarArray(Heading, Location);
        }

        private void UpdateRangeFinders(List<Line2D> walls)
        {
            // Update each range finder on the navigator
            foreach (var rangeFinder in RangeFinders)
            {
                rangeFinder.Update(walls, Heading, Location);
            }
        }

        private bool IsCollision(Point2D newLocation, List<Line2D> walls)
        {
            var doesCollide = false;

            // Iterate through all of the walls, determining if the traversal to the
            // newly proposed location will result in a collision
            foreach (var wall in walls)
            {
                // If the distance between the wall and the new location is less than
                // the radius of the navigator itself, then a collision will occur
                if (wall.distance(newLocation) < Radius)
                {
                    doesCollide = true;
                    break;
                }
            }

            return doesCollide;
        }

        internal double[] GetAnnInputs()
        {
            var annInputCnt = 0;

            // Create ANN input array with a separate input for each range finder and
            // radar, as well as an additional input for the bias
            var annInputs = new double[RangeFinders.Count + RadarArray.NumRadars + 1];

            // Set the bias
            annInputs[annInputCnt++] = 1;

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