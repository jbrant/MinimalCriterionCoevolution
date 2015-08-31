using System.Collections.Generic;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;

namespace SharpNeat.Core
{
    public interface INeatEvolutionAlgorithm<TGenome> : IEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        /// Gets the algorithm statistics object.
        /// </summary>
        NeatAlgorithmStats Statistics { get; }

        /// <summary>
        /// Gets the current complexity regulation mode.
        /// </summary>
        ComplexityRegulationMode ComplexityRegulationMode { get; }

        /// <summary>
        /// Gets a list of all current species. The genomes contained within the species are the same genomes
        /// available through the GenomeList property.
        /// </summary>
        IList<Specie<TGenome>> SpecieList { get; }        
    }
}
