#region

using System.Collections.Generic;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a behavior characterization based on the trajectory of an agent.  This reduces to capturing the position of
    ///     the agent in n-dimensional space at every time step.
    /// </summary>
    public class TrajectoryBehaviorCharacterization : IBehaviorCharacterization
    {
        /// <summary>
        ///     Allows the minimal criteria to be reversed such that only those who do *not* meet the minimal criteria are
        ///     considered viable.  This merely allows the instance MC itself to apply this functionality if internally enabled.
        /// </summary>
        private readonly bool _allowReverseCriteria;

        /// <summary>
        ///     The double array of behaviors.  Since this is a trajectory characterization, it will contain the position of the
        ///     agent for each time step.
        /// </summary>
        private readonly List<double> _behaviors;

        /// <summary>
        ///     The minimal criteria which the behavior must meet in order to be considered viable.
        /// </summary>
        private readonly IMinimalCriteria _minimalCriteria;

        /// <summary>
        ///     Default trajectory-behavior characterization constructor.
        /// </summary>
        public TrajectoryBehaviorCharacterization()
        {
            _behaviors = new List<double>();
        }

        /// <summary>
        ///     Trajectory behavior characterization constructor accepting a minimal criteria definition and a flag indicating
        ///     whether minimal criteria reversal should be allowed.
        /// </summary>
        /// <param name="minimalCriteria"></param>
        public TrajectoryBehaviorCharacterization(IMinimalCriteria minimalCriteria, bool allowReverseCriteria) : this()
        {
            _minimalCriteria = minimalCriteria;
            _allowReverseCriteria = allowReverseCriteria;
        }

        /// <summary>
        ///     Updates the behavior array.  This equates to appending the the end point to the existing behavior array, thus
        ///     establishing a measure of trajectory.
        /// </summary>
        /// <param name="newBehaviors">The new end point to append to the existing behavior state.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
            _behaviors.AddRange(newBehaviors);
        }

        /// <summary>
        ///     Evaluates whether the given behavior info meets the minimal criteria for this behavior characterization.
        /// </summary>
        /// <param name="behaviorInfo">The behavior info to evaluate.</param>
        /// <returns>
        ///     Boolean value indicating whether the given behavior info meets the minimal criteria for this behavior
        ///     characterization.
        /// </returns>
        public bool IsMinimalCriteriaSatisfied(BehaviorInfo behaviorInfo)
        {
            // If there is no minimal criteria, then by definition it has been met
            return _minimalCriteria?.DoesCharacterizationSatisfyMinimalCriteria(behaviorInfo, _allowReverseCriteria) ??
                   true;
        }

        /// <summary>
        ///     Converts behavior characterization to an array of doubles.
        /// </summary>
        /// <returns>Behavior characterization as an array of doubles.</returns>
        public double[] GetBehaviorCharacterizationAsArray()
        {
            return _behaviors.ToArray();
        }
    }
}