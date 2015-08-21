using SharpNeat.Core;

namespace SharpNeat.EliteArchives
{
    public class NoveltyArchive<TGenome> : EliteArchive<TGenome>
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
        /// <param name="maxGenerationalArchiveAddition">
        ///     The maximum number of organisms that can be added during a given
        ///     generation without precipitating a reduction of the archive addition threshold.
        /// </param>
        /// <param name="minGenerationalArchiveAddition">
        ///     The minimum number of organisms that can be added during a given
        ///     generation without precipitating a reduction of the archive addition threshold.
        /// </param>
        public NoveltyArchive(double initialArchiveAdditionThreshold, double thresholdDecreaseMultiplier = 0.95,
            double thresholdIncreaseMultiplier = 0.3, int maxGenerationalArchiveAddition = 5,
            int minGenerationalArchiveAddition = 1)
            : base(
                initialArchiveAdditionThreshold, thresholdDecreaseMultiplier, thresholdIncreaseMultiplier,
                maxGenerationalArchiveAddition, minGenerationalArchiveAddition)
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
            Archive.Add(genomeUnderEvaluation);

            // Increment the number of genomes added to archive for the current generation
            _numGenomesAddedThisGeneration++;
        }
    }
}