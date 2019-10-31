using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Utility;

namespace MCC_Domains.BodyBrain.MCCExperiment
{
    /// <summary>
    ///     Encapsulates the results of a simulated trial in terms of final location and distance traveled.
    /// </summary>
    public struct SimulationResults : ILoggable
    {
        /// <summary>
        ///     The ending location of the simulated robot.
        /// </summary>
        public readonly Point2DDouble Location;

        /// <summary>
        ///     The distance traversed by the simulated robot.
        /// </summary>
        public readonly double Distance;

        /// <summary>
        ///     The amount of simulation time expended while executing the trial.
        /// </summary>
        public readonly double SimulationTime;

        /// <summary>
        ///     SimulationResults constructor.
        /// </summary>
        /// <param name="xPos">The x-component of the ending location.</param>
        /// <param name="yPos">The y-component of the ending location.</param>
        /// <param name="distance">The distance traversed.</param>
        /// <param name="simTime">The amount of simulation time expended.</param>
        public SimulationResults(double xPos, double yPos, double distance, double simTime)
        {
            Location = new Point2DDouble(xPos, yPos);
            Distance = distance;
            SimulationTime = simTime;
        }

        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return new List<LoggableElement>
            {
                logFieldEnableMap?.ContainsKey(EvaluationFieldElements.DistanceToTarget) == true &&
                logFieldEnableMap[EvaluationFieldElements.DistanceToTarget]
                    ? new LoggableElement(EvaluationFieldElements.DistanceToTarget, Distance)
                    : null,
                logFieldEnableMap?.ContainsKey(EvaluationFieldElements.AgentXLocation) == true &&
                logFieldEnableMap[EvaluationFieldElements.AgentXLocation]
                    ? new LoggableElement(EvaluationFieldElements.AgentXLocation, Location.X)
                    : null,
                logFieldEnableMap?.ContainsKey(EvaluationFieldElements.AgentYLocation) == true &&
                logFieldEnableMap[EvaluationFieldElements.AgentYLocation]
                    ? new LoggableElement(EvaluationFieldElements.AgentYLocation, Location.Y)
                    : null,
                logFieldEnableMap?.ContainsKey(EvaluationFieldElements.SimTime) == true &&
                logFieldEnableMap[EvaluationFieldElements.SimTime]
                    ? new LoggableElement(EvaluationFieldElements.SimTime, SimulationTime)
                    : null,
            };
        }
    }
}