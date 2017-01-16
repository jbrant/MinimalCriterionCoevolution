namespace MazeExperimentSuppotLib
{
    /// <summary>
    ///     Encapsulates a single measure of population entropy.
    /// </summary>
    public struct PopulationEntropyUnit
    {
        /// <summary>
        ///     Population entropy unit constructor.
        /// </summary>
        /// <param name="populationEntropy">The entropy (diversity) of the population based on cluster assignment proportions.</param>
        public PopulationEntropyUnit(double populationEntropy)
        {
            PopulationEntropy = populationEntropy;
        }

        /// <summary>
        ///     The entropy (diversity) of the population based on cluster assignment proportions.
        /// </summary>
        public double PopulationEntropy { get; set; }
    }
}