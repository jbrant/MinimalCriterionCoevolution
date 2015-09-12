#region

using System.Collections.Concurrent;
using System.Collections.Generic;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Encapsulates the state of the archive of "novel" organisms, with respect to some domain-specific measure of
    ///     novelty.
    /// </summary>
    /// <typeparam name="TGenome">The genotype to store and evaluate.</typeparam>
    public abstract class AbstractNoveltyArchive<TGenome> : INoveltyArchive<TGenome> 
        where TGenome : class, IGenome<TGenome>
    {
        #region Constructors

        /// <summary>
        ///     Novelty archive constructor.
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
        /// <param name="maxGenerationArchiveAddition">
        ///     The maximum number of organisms that can be added during a given
        ///     generation without precipitating a reduction of the archive addition threshold.
        /// </param>
        /// <param name="maxGenerationsWithoutAddition">
        ///     The maximum number of generations that can elapse without adding an organism to the archive.
        /// </param>
        protected AbstractNoveltyArchive(double initialArchiveAdditionThreshold,
            double thresholdDecreaseMultiplier = 0.95,
            double thresholdIncreaseMultiplier = 1.3, int maxGenerationArchiveAddition = 4,
            int maxGenerationsWithoutAddition = 10)
        {
            ArchiveAdditionThreshold = initialArchiveAdditionThreshold;
            _thresholdDecreaseMultiplier = thresholdDecreaseMultiplier;
            _thresholdIncreaseMultiplier = thresholdIncreaseMultiplier;
            _maxGenerationArchiveAddition = maxGenerationArchiveAddition;
            _maxGenerationsWithoutArchiveAddition = maxGenerationsWithoutAddition;

            NumGenomesAddedThisGeneration = 0;
            _numGenerationsWithoutAdditions = 0;

            Archive = new ConcurrentBag<TGenome>();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The cross-generational list of novel genomes.
        /// </summary>
        public IProducerConsumerCollection<TGenome> Archive { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        ///     Updates the archive parameters, such as the addition threshold and the count of organisms added for the current
        ///     generation, for the next generation.
        /// </summary>
        public void UpdateArchiveParameters()
        {
            // Adjust the archive based on the number of genomes added this generation
            // or the number of generations elapsed without any archive additions
            if (NumGenomesAddedThisGeneration > _maxGenerationArchiveAddition)
            {
                ArchiveAdditionThreshold *= _thresholdIncreaseMultiplier;
            }
            else if (_numGenerationsWithoutAdditions == _maxGenerationsWithoutArchiveAddition)
            {
                ArchiveAdditionThreshold *= _thresholdDecreaseMultiplier;

                // Reset number of generations without addition
                _numGenerationsWithoutAdditions = 0;
            }

            // If there were no genomes added this generation, increment the counter
            if (NumGenomesAddedThisGeneration == 0)
            {
                _numGenerationsWithoutAdditions++;
            }
            // On the other hand, if there were additions this generation, reset the num
            // generations without additions counter
            else if (NumGenomesAddedThisGeneration > 0)
            {
                _numGenerationsWithoutAdditions = 0;
            }

            // Reset the count of organisms added for the next generation
            NumGenomesAddedThisGeneration = 0;
        }

        #endregion

        #region Abstract methods

        /// <summary>
        ///     Tests whether a given genome should be added to the novelty archive based on a domain-dependent feature
        ///     characterization.
        /// </summary>
        /// <param name="genomeUnderEvaluation">The candidate genome to evaluate for archive addition.</param>
        public abstract void TestAndAddCandidateToArchive(TGenome genomeUnderEvaluation);

        #endregion

        #region Private instance fields

        /// <summary>
        ///     The maximum number of organisms that can be added during a given generation without precipitating an increase of
        ///     the archive addition threshold.
        /// </summary>
        private readonly int _maxGenerationArchiveAddition;

        /// <summary>
        ///     The maximum number of generations that can elapse without adding an organism to the archive.
        /// </summary>
        private readonly int _maxGenerationsWithoutArchiveAddition;

        /// <summary>
        ///     The fraction by which to decrease the archive addition threshold.
        /// </summary>
        private readonly double _thresholdDecreaseMultiplier;

        /// <summary>
        ///     The fraction by which to increase the archive addition threshold.
        /// </summary>
        private readonly double _thresholdIncreaseMultiplier;

        /// <summary>
        ///     The maximum number of generations that can elapse without any additions without lowering the addition threshold.
        /// </summary>
        private int _numGenerationsWithoutAdditions;

        #endregion

        #region Protected instance fields

        /// <summary>
        ///     Tracks the number of genomes that have been added to the archive for the current generation.  This is used for
        ///     determining how to modify the archive threshold.
        /// </summary>
        protected int NumGenomesAddedThisGeneration;

        /// <summary>
        ///     The real-valued threshold constituting above which a genome will be added to the archive.
        /// </summary>
        protected double ArchiveAdditionThreshold;

        #endregion
    }
}