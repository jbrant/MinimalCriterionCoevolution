#region

using System.Collections.Concurrent;
using System.Collections.Generic;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Defines a contract for novelty archives, which encapsulate the state of the archive of "novel" organisms, with
    ///     respect to some domain-specific measure of novelty.
    /// </summary>
    /// <typeparam name="TGenome"></typeparam>
    public interface INoveltyArchive<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        ///     The cross-generational list of novel genomes.
        /// </summary>
        IProducerConsumerCollection<TGenome> Archive { get; }

        /// <summary>
        ///     Updates the archive parameters for the next generation.
        /// </summary>
        void UpdateArchiveParameters();

        /// <summary>
        ///     Tests whether a given genome should be added to the novelty archive based on a domain-dependent feature
        ///     characterization.
        /// </summary>
        /// <param name="genomeUnderEvaluation">The candidate genome to evaluate for archive addition.</param>
        void TestAndAddCandidateToArchive(TGenome genomeUnderEvaluation);
    }
}