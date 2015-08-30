namespace SharpNeat.SpeciationStrategies
{
    /// <summary>
    ///     Specie statistics, including genome selection sizes, reproduction counts, and average fitnesses.
    /// </summary>
    public class SpecieStats
    {
        #region Selection data

        /// <summary>
        ///     The number of genomes to select from the species.
        /// </summary>
        public int SelectionSize { get; set; }

        #endregion

        #region Integer statistics

        /// <summary>
        ///     The garget number of genomes in the species (rounded).
        /// </summary>
        public int TargetSizeInt { get; set; }

        /// <summary>
        ///     The number of elites in the species.
        /// </summary>
        public int EliteSizeInt { get; set; }

        /// <summary>
        ///     The total number of offspring that should be produced from the species.
        /// </summary>
        public int OffspringCount { get; set; }

        /// <summary>
        ///     The total number of offspring that should be asexually reproduced from the species.
        /// </summary>
        public int OffspringAsexualCount { get; set; }

        /// <summary>
        ///     The total number of offspring that should be sexually reproduced from the species.
        /// </summary>
        public int OffspringSexualCount { get; set; }

        #endregion

        #region Real/continuous statistics

        /// <summary>
        ///     The mean fitness of genomes in the species.
        /// </summary>
        public double MeanFitness { get; set; }

        /// <summary>
        ///     The target number of genomes in the species.
        /// </summary>
        public double TargetSizeReal { get; set; }

        #endregion
    }
}