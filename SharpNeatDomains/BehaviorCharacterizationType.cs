using System;
using SharpNeat.Behaviors;
using SharpNeat.Core;

namespace SharpNeat.Domains
{
    /// <summary>
    ///     Defines the behavior characterization type to use (end point, trajectory, etc.) for a given experiment.
    /// </summary>
    public enum BehaviorCharacterizationType
    {
        /// <summary>
        ///     Indicates the end point behavior characterization type.
        /// </summary>
        EndPoint,

        /// <summary>
        ///     Indicates the trajectory behavior characterization type.
        /// </summary>
        Trajectory
    }

    /// <summary>
    ///     Provides utility methods for behavior characterizations.
    /// </summary>
    public static class BehaviorCharacterizationUtil
    {
        /// <summary>
        ///     Determines the appropriate behavior characterization type based on the given string value.
        /// </summary>
        /// <param name="strBehavioralCharacterization">The string-valued behavior characterization.</param>
        /// <returns>The behavior characterization domain type.</returns>
        public static BehaviorCharacterizationType ConvertStringToBehavioralCharacterization(
            String strBehavioralCharacterization)
        {
            if (BehaviorCharacterizationType.EndPoint.ToString()
                .Equals(strBehavioralCharacterization, StringComparison.InvariantCultureIgnoreCase))
            {
                return BehaviorCharacterizationType.EndPoint;
            }
            return BehaviorCharacterizationType.Trajectory;
        }

        /// <summary>
        ///     Creates a new behavior characterization based on the given behavior characterization type.
        /// </summary>
        /// <param name="behaviorCharacterizationTypeType">
        ///     The behavior charcterization type for which to create a new behavior
        ///     characterization.
        /// </param>
        /// <returns>An instantiated behavior characterization.</returns>
        public static IBehaviorCharacterization GenerateBehaviorCharacterization(
            BehaviorCharacterizationType behaviorCharacterizationTypeType)
        {
            switch (behaviorCharacterizationTypeType)
            {
                case BehaviorCharacterizationType.EndPoint:
                    return new EndPointBehaviorCharacterization();
                default:
                    return new TrajectoryBehaviorCharacterization();
            }
        }
    }
}