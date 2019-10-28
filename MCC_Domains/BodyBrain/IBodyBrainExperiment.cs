using System.Collections.Generic;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;

namespace MCC_Domains.BodyBrain
{
    /// <summary>
    ///     Interface for classes implementing MCC body/brain experiments; specifically, those coevolving bodies with brains
    ///     (controllers).
    /// </summary>
    public interface IBodyBrainExperiment
    {
        /// <summary>
        ///     Gets the name of the experiment.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The default (max) brain population size.
        /// </summary>
        int BrainDefaultPopulationSize { get; }

        /// <summary>
        ///     The default (max) body population size.
        /// </summary>
        int BodyDefaultPopulationSize { get; }

        /// <summary>
        ///     The number of CPPN genomes in the brain seed population.
        /// </summary>
        int BrainSeedGenomeCount { get; }

        /// <summary>
        ///     The number of CPPN genomes in the body seed population.
        /// </summary>
        int BodySeedGenomeCount { get; }

        /// <summary>
        ///     Creates a new CPPN genome factory for brains.
        /// </summary>
        /// <returns>The constructed brain genome factory.</returns>
        IGenomeFactory<NeatGenome> CreateBrainGenomeFactory();

        /// <summary>
        ///     Creates a new CPPN genome factory for bodies.
        /// </summary>
        /// <returns>The constructed body genome factory.</returns>
        IGenomeFactory<NeatGenome> CreateBodyGenomeFactory();

        /// <summary>
        ///     Save a population of brain genomes to an XmlWriter.
        /// </summary>
        /// <param name="xw">Reference to the XML writer.</param>
        /// <param name="brainGenomeList">The list of brain genomes to write.</param>
        void SaveBrainPopulation(XmlWriter xw, IList<NeatGenome> brainGenomeList);

        /// <summary>
        ///     Save a population of body genomes to an XmlWriter.
        /// </summary>
        /// <param name="xw">Reference to the XML writer.</param>
        /// <param name="bodyGenomeList">The list of body genomes to write.</param>
        void SaveBodyPopulation(XmlWriter xw, IList<NeatGenome> bodyGenomeList);

        /// <summary>
        ///     Creates the evolution algorithm container using the given factories and genome lists.
        /// </summary>
        /// <param name="brainGenomeFactory">The brain genome factory.</param>
        /// <param name="bodyGenomeFactory">The body genome factory.</param>
        /// <param name="brainGenomes">The brain genome list.</param>
        /// <param name="bodyGenomes">The body genome list.</param>
        /// <returns>The instantiated MCC algorithm container.</returns>
        IMCCAlgorithmContainer<NeatGenome, NeatGenome> CreateMCCAlgorithmContainer(
            IGenomeFactory<NeatGenome> brainGenomeFactory, IGenomeFactory<NeatGenome> bodyGenomeFactory,
            List<NeatGenome> brainGenomes, List<NeatGenome> bodyGenomes);
    }
}