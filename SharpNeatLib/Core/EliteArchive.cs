using System.Collections.Generic;

namespace SharpNeat.Core
{
    public abstract class EliteArchive<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        ///     The real-valued threshold constituting above which a genome will be added to the archive.
        /// </summary>
        protected double ArchiveAdditionThreshold;

        /// <summary>
        ///     The fraction by which to decrease the archive addition threshold.
        /// </summary>
        private double _thresholdDecreaseMultiplier;

        /// <summary>
        ///     The fraction by which to increase the archive addition threshold.
        /// </summary>
        private double _thresholdIncreaseMultiplier;

        /// <summary>
        ///     Elite archive constructor.
        /// </summary>
        /// <param name="initialArchiveAdditionThreshold">The archive addition threshold value at which to start evolution.</param>
        /// <param name="thresholdDecreaseMultiplier">
        ///     The fraction by which to decrease the archive addition threshold (default is
        ///     0.95).
        /// </param>
        /// <param name="thresholdIncreaseMultiplier">
        ///     The fraction by which to increase the archive addition threshold (default is
        ///     0.3).
        /// </param>
        public EliteArchive(double initialArchiveAdditionThreshold, double thresholdDecreaseMultiplier = 0.95,
            double thresholdIncreaseMultiplier = 0.3)
        {
            ArchiveAdditionThreshold = initialArchiveAdditionThreshold;
            _thresholdDecreaseMultiplier = thresholdDecreaseMultiplier;
            _thresholdIncreaseMultiplier = thresholdIncreaseMultiplier;

            Archive = new List<TGenome>();
        }

        /// <summary>
        ///     The cross-generational list of elite genomes.
        /// </summary>
        public IList<TGenome> Archive { get; private set; }

        public abstract void TestAndAddCandidateToArchive(TGenome genomeUnderEvaluation);
    }
}