#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Wrapper class for behavior values.
    /// </summary>
    public class BehaviorInfo : IPhenomeEvaluationInfo
    {
        #region Constructors

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public BehaviorInfo()
        {
            TrialData = new List<TrialInfo>();
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Stores information about the individual simulation trials that comprise a behavioral evaluation.
        /// </summary>
        public IList<TrialInfo> TrialData { get; }

        /// <summary>
        ///     Flag indicating whether the behaviors satisfy the minimal criteria.  This flag is set after evaluation.
        /// </summary>
        public bool DoesBehaviorSatisfyMinimalCriteria { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        ///     Calculates the behavioral distance between two simulation trials.
        /// </summary>
        /// <param name="behavior1Trials">The first simulation trial in the distance calculation.</param>
        /// <param name="behavior2Trials">The second simulation trial in the distance calculation.</param>
        /// <returns>A measure of the behavioral distance.</returns>
        public static double CalculateDistance(IList<TrialInfo> behavior1Trials, IList<TrialInfo> behavior2Trials)
        {
            // Make sure there is only one simulation trial to compare
            if (behavior1Trials.Skip(1).Any() || behavior2Trials.Skip(1).Any())
            {
                throw new SharpNeatException(
                    $"Cannot compute behavioral distance between behavior characterizations with more than one representative trial (behavior 1 trials: [{behavior1Trials.Count}], behavior 2 trials: [{behavior2Trials.Count}]).");
            }

            // Extract trials from the two behaviors
            var trial1 = behavior1Trials[0];
            var trial2 = behavior2Trials[0];

            // Ensure we're comparing evaluations against the same paired genome
            if (trial1.PairedGenomeId != trial2.PairedGenomeId)
            {
                throw new SharpNeatException(
                    $"Cannot compare behavior characterizations on different mazes (maze 1 ID [{trial1.PairedGenomeId}], maze 2 ID: [{trial2.PairedGenomeId}])");
            }

            // Calculate the difference between the behavior double-precision arrays
            return CalculateDistance(trial1.Behaviors, trial2.Behaviors);
        }

        /// <summary>
        ///     Calculates the distance between two (double-precision) behaviors.
        /// </summary>
        /// <param name="behavior1">The first behavior in the distance calculation.</param>
        /// <param name="behavior2">The second behavior in the distance calculation.</param>
        /// <returns>A measure of the behavioral distance.</returns>
        public static double CalculateDistance(double[] behavior1, double[] behavior2)
        {
            if (behavior1.Length != behavior2.Length)
            {
                throw new SharpNeatException(
                    "Cannot compare behavior characterizations because behavior length differs.");
            }

            double distance = 0;

            // For each behavior array element, add its squared difference
            for (var position = 0; position < behavior1.Length; position++)
            {
                var delta = behavior1[position] - behavior2[position];
                distance += delta * delta;
            }

            return Math.Sqrt(distance);
        }

        #endregion
    }
}