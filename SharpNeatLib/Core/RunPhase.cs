namespace SharpNeat.Core
{
    /// <summary>
    ///     An enumeration of possible execution phases (different evolutionary algorithms) for a given experiment.
    /// </summary>
    public enum RunPhase
    {
        /// <summary>
        ///     Initialization algorithm is executing (usually to seed the primary algorithm).
        /// </summary>
        Initialization,

        /// <summary>
        ///     Primary evolutionary algorithm is executing.
        /// </summary>
        Primary
    }
}