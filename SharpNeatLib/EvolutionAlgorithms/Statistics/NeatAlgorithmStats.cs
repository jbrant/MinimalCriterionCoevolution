// This is a test of setting the file header.

#region

using System.Collections.Generic;
using SharpNeat.Genomes.Neat;

#endregion

namespace SharpNeat.EvolutionAlgorithms.Statistics
{
    public class NeatAlgorithmStats : AbstractEvolutionaryAlgorithmStats<NeatGenome>
    {
        public NeatAlgorithmStats(EvolutionAlgorithmParameters eaParams) : base(eaParams)
        {
        }

        public override void SetAlgorithmSpecificsPopulationStats(IList<NeatGenome> population)
        {
            // No additional NEAT info to set
        }
    }
}