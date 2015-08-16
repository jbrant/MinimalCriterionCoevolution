using System;
using System.Collections.Generic;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    internal class MazeNavigationWorld
    {
        /// <summary>
        ///     Location of the goal.
        /// </summary>
        private readonly DoublePoint _goalLocation;

        private readonly int? _maxDistanceToTarget;
        private readonly int? _maxTimesteps;
        private readonly int? _minSuccessDistance;
        private readonly MazeNavigator _navigator;
        private readonly List<DoubleLine> _walls;

        public MazeNavigationWorld(MazeVariant mazeVariant = MazeVariant.MediumMaze, int? minSuccessDistance = 5,
            int? maxDistanceToTarget = 300,
            int? maxTimeSteps = 400)
        {
            _minSuccessDistance = minSuccessDistance;
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimeSteps;

            switch (mazeVariant)
            {
                // Setup appropriate parameters for medium maze
                case MazeVariant.MediumMaze:
                    // Initialize the navigator at the appropriate starting location
                    _navigator = new MazeNavigator(new DoublePoint(30, 22));

                    // Initialize the goal location
                    _goalLocation = new DoublePoint(270, 100);

                    // Define all of the maze walls
                    _walls = new List<DoubleLine>(11)
                    {
                        new DoubleLine(293, 7, 289, 130),
                        new DoubleLine(289, 130, 6, 134),
                        new DoubleLine(6, 134, 8, 5),
                        new DoubleLine(8, 5, 292, 7),
                        new DoubleLine(241, 130, 58, 65),
                        new DoubleLine(114, 7, 73, 42),
                        new DoubleLine(130, 91, 107, 46),
                        new DoubleLine(196, 8, 139, 51),
                        new DoubleLine(219, 122, 182, 63),
                        new DoubleLine(267, 9, 214, 63),
                        new DoubleLine(271, 129, 237, 88)
                    };
                    break;

                // Setup appropriate parameters for hard maze
                case MazeVariant.HardMaze:
                    // Initialize the navigator at the appropriate starting location
                    _navigator = new MazeNavigator(new DoublePoint(36, 184));

                    // Initialize the goal location
                    _goalLocation = new DoublePoint(31, 20);

                    // Define all of the maze walls
                    _walls = new List<DoubleLine>(13)
                    {
                        new DoubleLine(41, 5, 3, 8),
                        new DoubleLine(3, 8, 4, 49),
                        new DoubleLine(4, 49, 57, 53),
                        new DoubleLine(4, 49, 7, 202),
                        new DoubleLine(7, 202, 195, 198),
                        new DoubleLine(195, 198, 186, 8),
                        new DoubleLine(186, 8, 39, 5),
                        new DoubleLine(56, 54, 56, 157),
                        new DoubleLine(57, 106, 158, 162),
                        new DoubleLine(77, 201, 108, 164),
                        new DoubleLine(6, 80, 33, 121),
                        new DoubleLine(192, 146, 87, 91),
                        new DoubleLine(56, 55, 133, 30)
                    };
                    break;
            }
        }

        public double RunTrial(IBlackBox agent)
        {
            // Reset neural network
            agent.ResetState();

            // Run for the given number of timesteps or until the goal is reached
            for (var curTimestep = 0; curTimestep < _maxTimesteps; curTimestep++)
            {
                // Reset the ANN input array
                agent.InputSignalArray.Reset();

                // Get the ANN input values
                var annInputs = _navigator.GetAnnInputs();

                // Set the inputs on the input signal array
                for (var annInputIndex = 0; annInputIndex < annInputs.Length; annInputIndex++)
                {
                    agent.InputSignalArray[annInputIndex] = annInputs[annInputIndex];
                }

                // Activate the network
                agent.Activate();

                // Decode the ANN output and update agent state
                _navigator.TranslateAndApplyAnnOutputs(agent.OutputSignalArray[0], agent.OutputSignalArray[1]);

                // Move the navigator to the new position (i.e. execute a single timestep)
                _navigator.Move(_walls);

                // Compute the new distance to the target (if the distance is less than 1,
                // we've solved the maze anyways but don't want to divide by 0 or
                // artificially inflate the fitness)
                var distanceToTarget = Math.Max(GetDistanceToTarget(), 1);

                // If the goal has been reached, break out of the loop
                if (distanceToTarget < _minSuccessDistance)
                {
                    break;
                }
            }

            // Return the fitness score as the difference between the maximum target distance
            // and the ending distance to the target
            return (double) _maxDistanceToTarget - GetDistanceToTarget();
        }

        private double GetDistanceToTarget()
        {
            // Get the distance to the target based on the navigator's current location
            return DoublePoint.CalculateEuclideanDistance(_navigator.Location, _goalLocation);
        }
    }
}