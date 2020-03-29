namespace SharpNeat.Core
{
    /// <summary>
    ///     Captures the salient attributes of a single trial within a genome evaluation.
    /// </summary>
    public struct TrialInfo
    {
        /// <summary>
        ///     Flag indicating whether the trial was successful (e.g. maze domain was solved).
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        ///     Distance between ending position and the target location or goal (used as a proxy for fitness in fitness-based
        ///     evaluations).
        /// </summary>
        public double ObjectiveDistance { get; }

        /// <summary>
        ///     The number of timesteps for which the simulation executed.
        /// </summary>
        public double NumTimesteps { get; }

        /// <summary>
        ///     The ID of the genome against which the current trial was conducted.
        /// </summary>
        public uint PairedGenomeId { get; }

        /// <summary>
        ///     Array storing the trial behavior characterization.
        /// </summary>
        public double[] Behaviors { get; }

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <param name="isSuccessful">Flag indicating whether the trial was successful (e.g. maze domain was solved).</param>
        /// <param name="objectiveDistance">
        ///     Distance between ending position and the target location or goal (used as a proxy for
        ///     fitness in fitness-based evaluations).
        /// </param>
        /// <param name="numTimesteps">The number of timesteps for which the simulation executed.</param>
        /// <param name="pairedGenomeId">The ID of the genome against which the current trial was conducted.</param>
        /// <param name="behaviors">Array storing the trial behavior characterization.</param>
        public TrialInfo(bool isSuccessful, double objectiveDistance, double numTimesteps, uint pairedGenomeId,
            double[] behaviors)
        {
            IsSuccessful = isSuccessful;
            ObjectiveDistance = objectiveDistance;
            NumTimesteps = numTimesteps;
            PairedGenomeId = pairedGenomeId;
            Behaviors = behaviors;
        }
    }
}