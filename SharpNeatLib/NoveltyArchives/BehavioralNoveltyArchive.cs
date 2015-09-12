using SharpNeat.Core;

namespace SharpNeat.EliteArchives
{
    /// <summary>
    ///     Encapsulates the state of the archive of behaviorally novel organisms.
    /// </summary>
    /// <typeparam name="TGenome">The genotype to store and evaluate.</typeparam>
    public class BehavioralNoveltyArchive<TGenome> : Core.AbstractNoveltyArchive<TGenome>
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
        public BehavioralNoveltyArchive(double initialArchiveAdditionThreshold, double thresholdDecreaseMultiplier = 0.95,
            double thresholdIncreaseMultiplier = 1.3, int maxGenerationArchiveAddition = 5,
            int maxGenerationsWithoutAddition = 10)
            : base(
                initialArchiveAdditionThreshold, thresholdDecreaseMultiplier, thresholdIncreaseMultiplier,
                maxGenerationArchiveAddition, maxGenerationsWithoutAddition)
        {
        }

        /// <summary>
        ///     Tests a given genome against other individuals in the archive based on their comparative behavioral distance.  If
        ///     the behavioral distance of the candidate genome is less than the distance specifieid by the archive addition
        ///     threshold as compared to all genomes currently in the archive, then the candidate genome is added to the archive.
        /// </summary>
        /// <param name="genomeUnderEvaluation">The candidate genome to evaluate for archive addition.</param>
        public override void TestAndAddCandidateToArchive(TGenome genomeUnderEvaluation)
        {
            // Iterate through each genome in the archive and compare its behavioral novelty
            foreach (var curArchiveGenome in Archive)
            {
                // If the distance between the two behavioral novelties is less than the archive threshold,
                // simply return as this won't be getting added to the novelty archive
                if (
                    BehaviorInfo.CalculateDistance(curArchiveGenome.EvaluationInfo.BehaviorCharacterization,
                        genomeUnderEvaluation.EvaluationInfo.BehaviorCharacterization) < ArchiveAdditionThreshold)
                {
                    return;
                }
            }

            // If the genome's behavioral novelty was sparse enough such that it was above the archive addition
            // threshold compared to every existing archive member, then add it to the archive
            Archive.TryAdd(genomeUnderEvaluation);

            // Increment the number of genomes added to archive for the current generation
            NumGenomesAddedThisGeneration++;
        }
    }
}