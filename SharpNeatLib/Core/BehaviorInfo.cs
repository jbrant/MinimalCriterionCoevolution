namespace SharpNeat.Core
{
    /// <summary>
    ///     Wrapper struct for behavior values.
    /// </summary>
    public struct BehaviorInfo : ITrialInfo
    {
        /// <summary>
        ///     Preconstructed BehaviorInfo indicating "no behavior".
        /// </summary>
        public static BehaviorInfo NoBehavior = new BehaviorInfo(new double[0]);

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <param name="behaviors">The behavior array.</param>
        public BehaviorInfo(double[] behaviors)
        {
            Behaviors = behaviors;
            DoesBehaviorSatisfyMinimalCriteria = true;
        }

        /// <summary>
        ///     The numeric behavior representation.
        /// </summary>
        public double[] Behaviors { get; }

        /// <summary>
        ///     Flag indicating whether the behaviors satisfy the minimal criteria.  This flag is set after evaluation.
        /// </summary>
        public bool DoesBehaviorSatisfyMinimalCriteria { get; set; }

        /// <summary>
        ///     Calculates the distance between two behaviors.
        /// </summary>
        /// <param name="behavior1">The first behavior in the distance calculation.</param>
        /// <param name="behavior2">The second behavior in the distance calculation.</param>
        /// <returns>A measure of the behavioral distance.</returns>
        public static double CalculateDistance(BehaviorInfo behavior1, BehaviorInfo behavior2)
        {
            if (behavior1.Behaviors.Length != behavior2.Behaviors.Length)
            {
                throw new SharpNeatException(
                    "Cannot compare behavior characterizations because behavior length differs.");
            }

            // Calculate the difference between the behavior double-precision arrays
            return CalculateDistance(behavior1.Behaviors, behavior2.Behaviors);
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
                distance += delta*delta;
            }

            return distance;
        }
    }
}