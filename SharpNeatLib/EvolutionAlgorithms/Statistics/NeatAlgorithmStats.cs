// This is a test of setting the file header.

#region

using System.Collections.Generic;

#endregion

namespace SharpNeat.EvolutionAlgorithms.Statistics
{
    /// <summary>
    ///     Encapsulates descriptive statistics about the extant population of NEAT genomes.
    /// </summary>
    public class NeatAlgorithmStats : AbstractEvolutionaryAlgorithmStats
    {
        /// <summary>
        ///     NeatAlgorithmStats constructor.
        /// </summary>
        /// <param name="eaParams">Evolution algorithm parameters required for initialization.</param>
        public NeatAlgorithmStats(EvolutionAlgorithmParameters eaParams) : base(eaParams)
        {
        }

        /// <summary>
        ///     Computes NEAT genome implementation-specific details about the population.
        /// </summary>
        /// <param name="population">The NEAT population from which to compute more specific, descriptive statistics.</param>
        public override void ComputeAlgorithmSpecificPopulationStats<TGenome>(IList<TGenome> population)
        {
            // No additional NEAT info to set
        }
    }
}