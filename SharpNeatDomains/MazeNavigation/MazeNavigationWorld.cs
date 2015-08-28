using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    public class MazeNavigationWorld<TTrialInfo>
    {
        /// <summary>
        ///     Characterization to use for capturing navigator behavior.
        /// </summary>
        private readonly IBehaviorCharacterization _behaviorCharacterization;

        /// <summary>
        ///     Location of the goal.
        /// </summary>
        private readonly DoublePoint _goalLocation;

        /// <summary>
        ///     Maximum distance to the target (i.e. goal location).
        /// </summary>
        private readonly int? _maxDistanceToTarget;

        /// <summary>
        ///     Maximum timesteps for the trial to run.
        /// </summary>
        private readonly int? _maxTimesteps;

        /// <summary>
        ///     Minimum distance from the target for the evaluation to be considered a success.
        /// </summary>
        private readonly int? _minSuccessDistance;

        /// <summary>
        ///     Reference to the navigator robot.
        /// </summary>
        private readonly MazeNavigator _navigator;

        /// <summary>
        ///     List of walls in the environment.
        /// </summary>
        private readonly List<DoubleLine> _walls;

        /// <summary>
        ///     Creates the maze navigation world (environment) given the experiment parameters.
        /// </summary>
        /// <param name="mazeVariant">The maze variant to utilize (i.e. medium maze, hard maze, etc.).</param>
        /// <param name="minSuccessDistance">The minimum distance from the target for the trial to be considered a success.</param>
        /// <param name="maxDistanceToTarget">The maximum distance from the target possible.</param>
        /// <param name="maxTimeSteps">The maximum number of time steps to run a given trial.</param>
        /// <param name="behaviorCharacterization">The behavior characterization for a navigator.</param>
        public MazeNavigationWorld(MazeVariant mazeVariant = MazeVariant.MediumMaze, int? minSuccessDistance = 5,
            int? maxDistanceToTarget = 300,
            int? maxTimeSteps = 400, IBehaviorCharacterization behaviorCharacterization = null)
        {
            _minSuccessDistance = minSuccessDistance;
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimeSteps;
            _behaviorCharacterization = behaviorCharacterization;

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
                        //new DoubleLine(114, 7, 83, 32),
                        new DoubleLine(130, 91, 107, 46),
                        //new DoubleLine(130, 91, 117, 56),
                        new DoubleLine(196, 8, 139, 51),
                        //new DoubleLine(196, 8, 149, 41),
                        new DoubleLine(219, 122, 182, 63),
                        //new DoubleLine(219, 122, 192, 73),
                        new DoubleLine(267, 9, 214, 63),
                        //new DoubleLine(267, 9, 224, 53),
                        new DoubleLine(271, 129, 237, 88)
                        //new DoubleLine(271, 129, 247, 98)
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

        /// <summary>
        ///     Runs a maze navigation trial.  This involves, for every timestep, activating the network with the radar/sensor
        ///     inputs, updating the navigator with the network output (i.e. changing the angular velocity and speed), moving the
        ///     navigator to the next position based on those updates, and finally returning the trial information, whether that be
        ///     fitness-based or behavior-based.
        /// </summary>
        /// <param name="agent">
        ///     The black box (neural network) that takes in the navigator sensors controls the navigator by
        ///     outputting the angular velocity and speed differentials based on those inputs.
        /// </param>
        /// <param name="evaluationType">The type of evaluation to perform (i.e. fitness, novelty, etc.).</param>
        /// <returns>The trial results (which will either be a fitness value or a behavior).</returns>
        public TTrialInfo RunTrial(IBlackBox agent, EvaluationType evaluationType, out bool goalReached)
        {
            ITrialInfo trialInfo;

            // Default the goal reached parameter to false
            goalReached = false;

            // Reset neural network
            agent.ResetState();

            // If this is a fitness evaluation, return the fitness score as the 
            // difference between the maximum target distance and the ending distance 
            // to the target
            if (evaluationType.Equals(EvaluationType.Fitness))
            {
                // Run for the given number of timesteps or until the goal is reached
                for (var curTimestep = 0; curTimestep < _maxTimesteps; curTimestep++)
                {
                    RunTimestep(agent);

                    // If the goal has been reached, break out of the loop
                    if (GetDistanceToTarget() < _minSuccessDistance)
                    {
                        goalReached = true;
                        break;
                    }
                }

                var fitness = (double) _maxDistanceToTarget - GetDistanceToTarget();
                trialInfo = new FitnessInfo(fitness, fitness);
            }
            // Otherwise, this is a behavioral evaluation, so return the ending 
            // location of the navigator
            else
            {
                // Run for the given number of timesteps or until the goal is reached
                for (var curTimestep = 0; curTimestep < _maxTimesteps; curTimestep++)
                {
                    RunTimestep(agent);
                    _behaviorCharacterization.UpdateBehaviors(new List<double>
                    {
                        _navigator.Location.X,
                        _navigator.Location.Y
                    });

                    // If the goal has been reached, break out of the loop
                    if (GetDistanceToTarget() < _minSuccessDistance)
                    {
                        goalReached = true;
                        break;
                    }
                }

                // Extract the behavior info object
                trialInfo = new BehaviorInfo(_behaviorCharacterization.Behaviors.ToArray());
            }

            // TODO: Remove this
            double tempDistance = GetDistanceToTarget();
            if (tempDistance < 40)
            {
                Debug.WriteLine("Distance to goal: {0}", tempDistance);
            }

            return (TTrialInfo) trialInfo;
        }

        /// <summary>
        ///     Runs a single time step of the maze navigator.
        /// </summary>
        /// <param name="agent">
        ///     The black box (neural network) that takes in the navigator sensors controls the navigator by
        ///     outputting the angular velocity and speed differentials based on those inputs.
        /// </param>
        private void RunTimestep(IBlackBox agent)
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
        }

        /// <summary>
        ///     Calculates the distance between the navigator's location and the goal location.
        /// </summary>
        /// <returns>The distance between the navigator and the goal.</returns>
        private double GetDistanceToTarget()
        {
            // Get the distance to the target based on the navigator's current location
            return DoublePoint.CalculateEuclideanDistance(_navigator.Location, _goalLocation);
        }
    }
}