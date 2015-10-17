#region

using SharpNeat.Core;
using SharpNeat.NoveltyArchives;

#endregion

namespace SharpNeat.EliteArchives
{
    /// <summary>
    ///     Encapsulates the state of the archive of behaviorally novel organisms.
    /// </summary>
    /// <typeparam name="TGenome">The genotype to store and evaluate.</typeparam>
    public class BehavioralNoveltyArchive<TGenome> : AbstractNoveltyArchive<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
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
        public BehavioralNoveltyArchive(double initialArchiveAdditionThreshold,
            double thresholdDecreaseMultiplier = 0.95,
            double thresholdIncreaseMultiplier = 1.3, int maxGenerationArchiveAddition = 5,
            int maxGenerationsWithoutAddition = 10)
            : base(
                initialArchiveAdditionThreshold, thresholdDecreaseMultiplier, thresholdIncreaseMultiplier,
                maxGenerationArchiveAddition, maxGenerationsWithoutAddition)
        {
        }

        /// <summary>
        ///     Tests whether a given genome is greater than the current archive addition threshold, and if so, adds it to the
        ///     novelty archive.
        /// </summary>
        /// <param name="genomeUnderEvaluation">The candidate genome to evaluate for archive addition.</param>
        public override void TestAndAddCandidateToArchive(TGenome genomeUnderEvaluation)
        {
            // If the genome's behavioral novelty was sparse enough such that it was above the archive addition
            // threshold compared to every existing archive member, then add it to the archive
            if (genomeUnderEvaluation.EvaluationInfo.Fitness > ArchiveAdditionThreshold)
            {
                Archive.TryAdd(genomeUnderEvaluation);

                // Increment the number of genomes added to archive for the current generation
                NumGenomesAddedThisGeneration++;
            }
        }
    }
}