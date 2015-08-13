using System.Collections.Generic;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    internal class MazeNavigationWorld
    {
        private static readonly int MinSuccessDistance = 5;
        private static readonly int MaxDistanceToTarget = 300;

        /// <summary>
        ///     Location of the goal.
        /// </summary>
        private Point2D _goalLocation;

        private bool _isGoalReached;

        private int _maxTimesteps;

        private MazeNavigator _navigator;

        public MazeNavigationWorld(MazeVariant mazeVariant, int maxTimeSteps = 400)
        {

            _maxTimesteps = maxTimeSteps;

            // Initialize goal reached status to false
            _isGoalReached = false;

            if (mazeVariant == MazeVariant.MEDIUM_MAZE)
            {
                //TODO: Implement navigator with initial location and heading in the medium maze

                //TODO: Implement medium maze walls
            }
            else if (mazeVariant == MazeVariant.HARD_MAZE)
            {
                //TODO: Implement navigator with initial location and heading in the hard maze

                //TODO: Implement hard maze walls
            }
        }

        public List<Line2D> Walls { get; private set; }

        public bool RunTrial(IBlackBox agent)
        {
            
            // Reset neural network
            agent.ResetState();

            // Run for the given number of timesteps or until the goal is reached
            for (int curTimestep = 0; curTimestep < _maxTimesteps; curTimestep++)
            {
                //TODO: Activate the network here

                // Reset the ANN input array
                agent.InputSignalArray.Reset();

                // Get the ANN input values
                double[] annInputs = _navigator.GetAnnInputs();

                // Set the inputs on the input signal array
                for (int annInputIndex = 0; annInputIndex < annInputs.Length; annInputIndex++)
                {
                    agent.InputSignalArray[annInputIndex] = annInputs[annInputIndex];
                }

                // Activate the network
                agent.Activate();

                //TODO: Decode the network outputs here

                //TODO: Move the navigator here
            }

            return false;
        }
    }
}