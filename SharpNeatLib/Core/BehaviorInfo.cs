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
        }

        /// <summary>
        ///     The numeric behavior representation.
        /// </summary>
        public double[] Behaviors { get; private set; }
    }
}