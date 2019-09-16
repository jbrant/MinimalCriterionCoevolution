#region

using System.Collections.Generic;
using MCC_Domains.Common;
using MCC_Domains.MazeNavigation.Components;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace MCC_Domains.MazeNavigation
{
    /// <inheritdoc />
    /// <summary>
    ///     Sets up and runs the maze navigation simulation.
    /// </summary>
    public class MazeNavigationWorld : ILoggable
    {
        #region Private methods

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
            _navigator.Move(_walls, _goalLocation);
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates the maze navigation world (environment) given the experiment parameters.
        /// </summary>
        /// <param name="walls">The walls in the maze environment.</param>
        /// <param name="navigatorLocation">The starting location of the maze navigator.</param>
        /// <param name="goalLocation">The location of the goal (target).</param>
        /// <param name="minSuccessDistance">The minimum distance from the target for the trial to be considered a success.</param>
        /// <param name="maxDistanceToTarget">The maximum distance from the target possible.</param>
        /// <param name="maxTimeSteps">The maximum number of time steps to run a given trial.</param>
        /// <param name="behaviorCharacterization">The behavior characterization for a navigator.</param>
        public MazeNavigationWorld(List<Wall> walls, DoublePoint navigatorLocation,
            DoublePoint goalLocation,
            int minSuccessDistance,
            int maxDistanceToTarget, int maxTimeSteps,
            IBehaviorCharacterization behaviorCharacterization = null)
        {
            _walls = walls;
            _goalLocation = goalLocation;
            _minSuccessDistance = minSuccessDistance;
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimeSteps;
            _behaviorCharacterization = behaviorCharacterization;

            // Instantiate the navigator
            _navigator = new MazeNavigator(navigatorLocation);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates the maze navigation world, omitting the maximum distance to the target (required for
        ///     fitness-based evaluations).
        /// </summary>
        /// <param name="walls">The walls in the maze environemnt.</param>
        /// <param name="navigatorLocation">The starting location of the maze navigator.</param>
        /// <param name="goalLocation">The location of the goal (target).</param>
        /// <param name="minSuccessDistance">The minimum distance from the target for the trial to be considered a success.</param>
        /// <param name="maxTimeSteps">The maximum number of time steps to run a given trial.</param>
        /// <param name="behaviorCharacterization">The behavior characterization for a navigator.</param>
        public MazeNavigationWorld(List<Wall> walls, DoublePoint navigatorLocation, DoublePoint goalLocation,
            int minSuccessDistance, int maxTimeSteps, IBehaviorCharacterization behaviorCharacterization)
            : this(
                walls, navigatorLocation, goalLocation, minSuccessDistance, 0, maxTimeSteps, behaviorCharacterization)
        {
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
        ///     Minimum distance from the target for the evaluation to be considered a success.
        /// </summary>
        private readonly int _minSuccessDistance;

        /// <summary>
        ///     Reference to the navigator robot.
        /// </summary>
        private readonly MazeNavigator _navigator;

        /// <summary>
        ///     List of walls in the environment.
        /// </summary>
        private readonly List<Wall> _walls;

        /// <summary>
        ///     Number of timesteps executed by trial.
        /// </summary>
        private int _timestep;

        #endregion

        #region Public methods

        /// <inheritdoc />
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
                logFieldEnableMap?.ContainsKey(EvaluationFieldElements.DistanceToTarget) == true &&
                logFieldEnableMap[EvaluationFieldElements.DistanceToTarget]
                    ? new LoggableElement(EvaluationFieldElements.DistanceToTarget, GetDistanceToTarget())
                    : null,
                logFieldEnableMap?.ContainsKey(EvaluationFieldElements.AgentXLocation) == true &&
                logFieldEnableMap[EvaluationFieldElements.AgentXLocation]
                    ? new LoggableElement(EvaluationFieldElements.AgentXLocation, _navigator.Location.X)
                    : null,
                logFieldEnableMap?.ContainsKey(EvaluationFieldElements.AgentYLocation) == true &&
                logFieldEnableMap[EvaluationFieldElements.AgentYLocation]
                    ? new LoggableElement(EvaluationFieldElements.AgentYLocation, _navigator.Location.Y)
                    : null
            };
        }

        /// <summary>
        ///     Runs a maze navigation trial.  This involves, for every timestep, activating the network with the radar/sensor
        ///     inputs, updating the navigator with the network output (i.e. changing the angular velocity and speed), moving the
        ///     navigator to the next position based on those updates, and finally returning the trial information, which for a
        ///     fitness-based trial, is the final distance to the target.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="goalReached"></param>
        /// <returns>The final distance to the target (which is interpreted as the fitness).</returns>
        public double RunFitnessTrial(IBlackBox agent, out bool goalReached)
        {
            // Default the goal reached parameter to false
            goalReached = false;

            // Reset neural network
            agent.ResetState();

            // Run for the given number of timesteps or until the goal is reached
            for (_timestep = 0; _timestep < _maxTimesteps; _timestep++)
            {
                RunTimestep(agent);

                // If the goal has been reached, break out of the loop
                if (GetDistanceToTarget() < _minSuccessDistance)
                {
                    goalReached = true;
                    break;
                }
            }

            // Compute fitness as final distance from target location
            var fitness = _maxDistanceToTarget - GetDistanceToTarget();

            return fitness;
        }

        /// <summary>
        ///     Runs a maze navigation trial.  This involves, for every timestep, activating the network with the radar/sensor
        ///     inputs, updating the navigator with the network output (i.e. changing the angular velocity and speed), moving the
        ///     navigator to the next position based on those updates, and finally returning the trial information, which for a
        ///     behavior-based trial is given by the specific attributes captured by the selected behavior characterization (e.g.
        ///     the position at every time step, the final position, the position sampled at a subset of the time steps, etc.).
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="goalReached"></param>
        /// <returns>The final distance to the target (which is interpreted as the fitness).</returns>
        public double[] RunBehaviorTrial(IBlackBox agent, out bool goalReached)
        {
            // Default the goal reached parameter to false
            goalReached = false;

            // Reset neural network
            agent.ResetState();

            // Run for the given number of timesteps or until the goal is reached
            for (_timestep = 0; _timestep < _maxTimesteps; _timestep++)
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
            var behavior = _behaviorCharacterization.GetBehaviorCharacterizationAsArray();

            return behavior;
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

        /// <summary>
        ///     Gets the number of timesteps for which the maze simulation executed.
        /// </summary>
        /// <returns>The number of timesteps for which the maze simulation executed.</returns>
        public int GetSimulationTimesteps()
        {
            return _timestep;
        }

        #endregion
    }
}