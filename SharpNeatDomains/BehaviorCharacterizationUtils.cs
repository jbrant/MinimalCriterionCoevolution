#region

using System;
using SharpNeat.Behaviors;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Domains
{
    /// <summary>
    ///     Defines the behavior characterization type to use (end point, trajectory, etc.) for a given experiment.
    /// </summary>
    public enum BehaviorCharacterizationUtils
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
    ///     Defines the minimal criteria type to use (euclidean location, etc.) for a given experiment.
    /// </summary>
    public enum MinimalCriteriaType
    {
        /// <summary>
        ///     Indicates the euclidean location minimal criteria.
        /// </summary>
        EuclideanLocation
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
        public static BehaviorCharacterizationUtils ConvertStringToBehavioralCharacterization(
            String strBehavioralCharacterization)
        {
            if (BehaviorCharacterizationUtils.EndPoint.ToString()
                .Equals(strBehavioralCharacterization, StringComparison.InvariantCultureIgnoreCase))
            {
                return BehaviorCharacterizationUtils.EndPoint;
            }
            return BehaviorCharacterizationUtils.Trajectory;
        }

        /// <summary>
        ///     Determines the appropriate minimal criteria type based on the given string value.
        /// </summary>
        /// <param name="strMinimalCriteria">The string-valued minimal criteria.</param>
        /// <returns>The minimal criteria domain type.</returns>
        public static MinimalCriteriaType ConvertStringToMinimalCriteria(String strMinimalCriteria)
        {
            // TODO: Doesn't really make sense until additional minimal criteria types are added
            if (MinimalCriteriaType.EuclideanLocation.ToString()
                .Equals(strMinimalCriteria, StringComparison.InvariantCultureIgnoreCase))
            {
                return MinimalCriteriaType.EuclideanLocation;
            }
            return MinimalCriteriaType.EuclideanLocation;
        }

        /// <summary>
        ///     Creates a new behavior characterization based on the given string-valued behavior characterization.
        /// </summary>
        /// <param name="strBehaviorCharacterization">
        ///     String representation of the behavior charcterization type for which to create a new behavior characterization.
        /// </param>
        /// <returns>An instantiated behavior characterization.</returns>
        public static IBehaviorCharacterization GenerateBehaviorCharacterization(
            String strBehaviorCharacterization)
        {
            switch (ConvertStringToBehavioralCharacterization(strBehaviorCharacterization))
            {
                case BehaviorCharacterizationUtils.EndPoint:
                    return new EndPointBehaviorCharacterization();
                default:
                    return new TrajectoryBehaviorCharacterization();
            }
        }

        /// <summary>
        ///     Creates a new behavior characterization based on the given behavior characterization type.
        /// </summary>
        /// <param name="behaviorCharacterizationType">
        ///     The behavior charcterization type for which to create a new behavior
        ///     characterization.
        /// </param>
        /// <returns>An instantiated behavior characterization.</returns>
        public static IBehaviorCharacterization GenerateBehaviorCharacterization(
            BehaviorCharacterizationUtils behaviorCharacterizationType)
        {
            switch (behaviorCharacterizationType)
            {
                case BehaviorCharacterizationUtils.EndPoint:
                    return new EndPointBehaviorCharacterization();
                default:
                    return new TrajectoryBehaviorCharacterization();
            }
        }
    }
}