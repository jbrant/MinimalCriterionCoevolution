#region

using System.Collections.Generic;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a behavior characterization based on the end-point of an agent.  As such, the behavior is simply defined as
    ///     a coordinate in n-dimensional space.
    /// </summary>
    public class EndPointBehaviorCharacterization : IBehaviorCharacterization
    {
        /// <summary>
        ///     Allows the minimal criteria to be reversed such that only those who do *not* meet the minimal criteria are
        ///     considered viable.  This merely allows the instance MC itself to apply this functionality if internally enabled.
        /// </summary>
        private readonly bool _allowReverseCriteria;

        /// <summary>
        ///     The minimal criteria which the behavior must meet in order to be considered viable.
        /// </summary>
        private readonly IMinimalCriteria _minimalCriteria;

        /// <summary>
        ///     The double array of behaviors.  Since this is an end-point characterization, it should only contain the number of
        ///     elements equivalent to the dimensionality of the state space.
        /// </summary>
        private List<double> _behaviors;

        /// <summary>
        ///     Default end-point behavior characterization constructor.
        /// </summary>
        public EndPointBehaviorCharacterization()
        {
        }

        /// <summary>
        ///     End-point behavior characterization constructor accepting a minimal criteria definition and a flag indicating
        ///     whether minimal criteria reversal should be allowed.
        /// </summary>
        /// <param name="minimalCriteria"></param>
        /// <param name="allowReverseCriteria">Flag indicating whether minimal criteria reversal should be allowed.</param>
        public EndPointBehaviorCharacterization(IMinimalCriteria minimalCriteria, bool allowReverseCriteria)
        {
            _minimalCriteria = minimalCriteria;
            _allowReverseCriteria = allowReverseCriteria;
        }

        /// <summary>
        ///     Updates the behavior array.  This equates to simply replacing the behavior array with the coordinates of the new
        ///     end point.
        /// </summary>
        /// <param name="newBehaviors">The new end point with which to characterize the behavior state.</param>
        public void UpdateBehaviors(List<double> newBehaviors)
        {
            // Overwrite the existing behavior array with the current location
            _behaviors = newBehaviors;
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