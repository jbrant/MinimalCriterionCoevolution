#region

using System.Collections.Generic;
using System.Diagnostics;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation
{
    /// <summary>
    ///     Sets up and runs the maze navigation simulation.
    /// </summary>
    /// <typeparam name="TTrialInfo">Information about a trial through the maze.</typeparam>
    public class MazeNavigationWorld<TTrialInfo> : ILoggable
    {
        #region Constructors

        /// <summary>
        ///     Creates the maze navigation world (environment) given the experiment parameters.
        /// </summary>
        /// <param name="walls">The walls in the maze environemnt.</param>
        /// <param name="mazeNicheGrid">Metadata about the grid of niches overlaying the maze.</param>
        /// <param name="navigatorLocation">The starting location of the maze navigator.</param>
        /// <param name="goalLocation">The location of the goal (target).</param>
        /// <param name="minSuccessDistance">The minimum distance from the target for the trial to be considered a success.</param>
        /// <param name="maxDistanceToTarget">The maximum distance from the target possible.</param>
        /// <param name="maxTimeSteps">The maximum number of time steps to run a given trial.</param>
        /// <param name="numBridgingApplications">The number of times to apply bridging during a given trial.</param>
        /// ///
        /// <param name="behaviorCharacterization">The behavior characterization for a navigator.</param>
        public MazeNavigationWorld(List<Wall> walls, MazeNicheGrid mazeNicheGrid, DoublePoint navigatorLocation,
            DoublePoint goalLocation,
            int minSuccessDistance,
            int maxDistanceToTarget, int maxTimeSteps,
            int numBridgingApplications,
            IBehaviorCharacterization behaviorCharacterization = null)
        {
            _walls = walls;
            _goalLocation = goalLocation;
            _minSuccessDistance = minSuccessDistance;
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimeSteps;
            _behaviorCharacterization = behaviorCharacterization;
            _numBridgingApplications = numBridgingApplications;
            _mazeNicheGrid = mazeNicheGrid;

            // Instantiate the navigator
            _navigator = new MazeNavigator(navigatorLocation);
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Runs a single time step of the maze navigator.
        /// </summary>
        /// <param name="agent">
        ///     The black box (neural network) that takes in the navigator sensors controls the navigator by
        ///     outputting the angular velocity and speed differentials based on those inputs.
        /// </param>
        /// <param name="isBridgingTimestep">Indicates whether bridging should be applied in the current timestep.</param>
        /// <param name="curBridgingApplications">
        ///     The current number of times bridging has been applied.  This value
        ///     will be incremented and the updated value used by the calling routine.
        /// </param>
        private void RunTimestep(IBlackBox agent, bool isBridgingTimestep, ref int curBridgingApplications)
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
            _navigator.Move(_walls, _goalLocation, isBridgingTimestep, ref curBridgingApplications);
        }

        #endregion

        #region Private members

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
        private readonly int _maxDistanceToTarget;

        /// <summary>
        ///     Maximum timesteps for the trial to run.
        /// </summary>
        private readonly int? _maxTimesteps;

        /// <summary>
        ///     Encapsulates the maze boundaries and supporting methods for mapping ending locations into the niche grid.
        /// </summary>
        private readonly MazeNicheGrid _mazeNicheGrid;

        /// <summary>
        ///     Minimum distance from the target for the evaluation to be considered a success.
        /// </summary>
        private readonly int _minSuccessDistance;

        /// <summary>
        ///     Reference to the navigator robot.
        /// </summary>
        private readonly MazeNavigator _navigator;

        /// <summary>
        ///     The number of times within a given trial to apply bridging "help".
        /// </summary>
        private readonly int _numBridgingApplications;

        /// <summary>
        ///     List of walls in the environment.
        /// </summary>
        private readonly List<Wall> _walls;

        #endregion

        #region Public methods

        /// <summary>
        ///     Returns MazeNavigationWorld LoggableElements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The LoggableElements for the MazeNavigationWorld.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.DistanceToTarget, GetDistanceToTarget()),
                new LoggableElement(EvaluationFieldElements.AgentXLocation, _navigator.Location.X),
                new LoggableElement(EvaluationFieldElements.AgentYLocation, _navigator.Location.Y)
            };
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
        /// <param name="searchType">The type of evaluation to perform (i.e. fitness, novelty, etc.).</param>
        /// <param name="goalReached">Indicates whether the goal has been reached.</param>
        /// <returns>The trial results (which will either be a fitness value or a behavior).</returns>
        public TTrialInfo RunTrial(IBlackBox agent, SearchType searchType, out bool goalReached)
        {
            ITrialInfo trialInfo;

            // Initialize the starting number of bridging applications
            int curBridgingApplications = 0;

            // Default the goal reached parameter to false
            goalReached = false;

            // Reset neural network
            agent.ResetState();

            // If this is a fitness evaluation, return the fitness score as the 
            // difference between the maximum target distance and the ending distance 
            // to the target
            if (searchType.Equals(SearchType.Fitness))
            {
                // Run for the given number of timesteps or until the goal is reached
                for (var curTimestep = 0; curTimestep < _maxTimesteps; curTimestep++)
                {
                    RunTimestep(agent, false, ref curBridgingApplications);

                    // If the goal has been reached, break out of the loop
                    if (GetDistanceToTarget() < _minSuccessDistance)
                    {
                        goalReached = true;
                        break;
                    }
                }

                double fitness = _maxDistanceToTarget - GetDistanceToTarget();
                trialInfo = new FitnessInfo(fitness, fitness);
            }
            // Otherwise, this is a behavioral evaluation, so return the ending 
            // location of the navigator
            else
            {
                // Run for the given number of timesteps or until the goal is reached
                for (var curTimestep = 0; curTimestep < _maxTimesteps; curTimestep++)
                {
                    RunTimestep(agent, (curBridgingApplications < _numBridgingApplications), ref curBridgingApplications);

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
                trialInfo = new BehaviorInfo(_behaviorCharacterization.GetBehaviorCharacterizationAsArray());
            }

            // TODO: Remove this
            double tempDistance = GetDistanceToTarget();
            if (tempDistance < 20)
            {
                Debug.WriteLine("Distance to goal: {0}", tempDistance);
            }

            // Determine the behavioral niche in which the navigator ended
            trialInfo.NicheId = _mazeNicheGrid.DetermineNicheId(_navigator.Location);

            return (TTrialInfo) trialInfo;
        }

        /// <summary>
        ///     Calculates the distance between the navigator's location and the goal location.
        /// </summary>
        /// <returns>The distance between the navigator and the goal.</returns>
        public double GetDistanceToTarget()
        {
            // Get the distance to the target based on the navigator's current location
            return DoublePoint.CalculateEuclideanDistance(_navigator.Location, _goalLocation);
        }

        #endregion
    }
}