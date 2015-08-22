using System.Collections.Concurrent;

namespace SharpNeat.Core
{
    /// <summary>
    ///     Encapsulates the state of the archive of "elite" organisms, with respect to some domain-specific measure of
    ///     elitism.
    /// </summary>
    /// <typeparam name="TGenome">The genotype to store and evaluate.</typeparam>
    public abstract class EliteArchive<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        ///     The maximum number of organisms that can be added during a given generation without precipitating a reduction of
        ///     the archive addition threshold.
        /// </summary>
        private readonly int _maxGenerationalArchiveAddition;

        /// <summary>
        ///     The minimum number of organisms that can be added during a given generation without precipitating a reduction of
        ///     the archive addition threshold.
        /// </summary>
        private readonly int _minGenerationalArchiveAddition;

        /// <summary>
        ///     The fraction by which to decrease the archive addition threshold.
        /// </summary>
        private readonly double _thresholdDecreaseMultiplier;

        /// <summary>
        ///     The fraction by which to increase the archive addition threshold.
        /// </summary>
        private readonly double _thresholdIncreaseMultiplier;

        /// <summary>
        ///     Tracks the number of genomes that have been added to the archive for the current generation.  This is used for
        ///     determining how to modify the archive threshold.
        /// </summary>
        protected int _numGenomesAddedThisGeneration;

        /// <summary>
        ///     The real-valued threshold constituting above which a genome will be added to the archive.
        /// </summary>
        protected double ArchiveAdditionThreshold;

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
        /// <param name="maxGenerationalArchiveAddition">
        ///     The maximum number of organisms that can be added during a given
        ///     generation without precipitating a reduction of the archive addition threshold.
        /// </param>
        /// <param name="minGenerationalArchiveAddition">
        ///     The minimum number of organisms that can be added during a given
        ///     generation without precipitating a reduction of the archive addition threshold.
        /// </param>
        public EliteArchive(double initialArchiveAdditionThreshold, double thresholdDecreaseMultiplier = 0.95,
            double thresholdIncreaseMultiplier = 0.3, int maxGenerationalArchiveAddition = 5,
            int minGenerationalArchiveAddition = 1)
        {
            ArchiveAdditionThreshold = initialArchiveAdditionThreshold;
            _thresholdDecreaseMultiplier = thresholdDecreaseMultiplier;
            _thresholdIncreaseMultiplier = thresholdIncreaseMultiplier;
            _maxGenerationalArchiveAddition = maxGenerationalArchiveAddition;
            _minGenerationalArchiveAddition = minGenerationalArchiveAddition;

            _numGenomesAddedThisGeneration = 0;

            //_archive = new List<TGenome>();
            Archive = new ConcurrentBag<TGenome>();
        }

        /// <summary>
        ///     The cross-generational list of elite genomes.
        /// </summary>
        public ConcurrentBag<TGenome> Archive { get; private set; }

        /// <summary>
        ///     Updates the archive parameters, such as the addition threshold and the count of organisms added for the current
        ///     generation, for the next generation.
        /// </summary>
        public void UpdateArchiveParameters()
        {
            // Adjust the archive threshold based on the number of organisms
            // added to the archive during the current generation
            if (_numGenomesAddedThisGeneration < _minGenerationalArchiveAddition)
            {
                ArchiveAdditionThreshold *= _thresholdDecreaseMultiplier;
            }
            else if (_numGenomesAddedThisGeneration > _maxGenerationalArchiveAddition)
            {
                ArchiveAdditionThreshold *= _thresholdIncreaseMultiplier;
            }

            // Reset the count of organisms added for the next generation
            _numGenomesAddedThisGeneration = 0;
        }

        /// <summary>
        ///     Tests whether a given genome should be added to the elite archive based on a domain-dependent feature
        ///     characterization.
        /// </summary>
        /// <param name="genomeUnderEvaluation">The candidate genome to evaluate for archive addition.</param>
        public abstract void TestAndAddCandidateToArchive(TGenome genomeUnderEvaluation);
    }
}