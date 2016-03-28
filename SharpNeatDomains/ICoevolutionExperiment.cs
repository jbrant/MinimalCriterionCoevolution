#region

using System.Collections.Generic;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;

#endregion

namespace SharpNeat.Domains
{
    /// <summary>
    ///     Interface for classes implementing coevolution experiments; specifically, those coevolving mazes with NEAT-based
    ///     controllers.
    /// </summary>
    public interface ICoevolutionExperiment
    {
        /// <summary>
        ///     Gets the name of the experiment.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets human readable explanatory text for the experiment.
        /// </summary>
        string Description { get; }

        /// <summary>
        ///     Gets the default agent population size to use for the experiment.
        /// </summary>
        int AgentDefaultPopulationSize { get; }

        /// <summary>
        ///     Gets the default maze population size to use for the experiment.
        /// </summary>
        int MazeDefaultPopulationSize { get; }

        /// <summary>
        ///     The number of genomes in the initial agent population.
        /// </summary>
        int AgentSeedGenomeCount { get; }

        /// <summary>
        ///     The number of genomes in the initial maze population.
        /// </summary>
        int MazeSeedGenomeCount { get; }

        /// <summary>
        ///     Create an agent genome factory for the experiment.
        /// </summary>
        /// <returns>An initialized agent genome factory.</returns>
        IGenomeFactory<NeatGenome> CreateAgentGenomeFactory();

        /// <summary>
        ///     Create a maze genome factory for the experiment.
        /// </summary>
        /// <returns>An initialized maze genome factory.</returns>
        IGenomeFactory<MazeGenome> CreateMazeGenomeFactory();

        /// <summary>
        ///     Initialize the experiment with some optional XML configutation data.
        /// </summary>
        void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger = null,
            IDataLogger evaluationDataLogger = null);

        /// <summary>
        ///     Creates and returns a coevolution algorithm container, which encapsulates two evolutionary algorithms.  This
        ///     initializes the algorithms with their default population sizes and automatically generates a starting population of
        ///     that size.
        /// </summary>
        /// <returns>Coevolution algorithm container.</returns>
        ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer();

        /// <summary>
        ///     Creates and returns a coevolution algorithm container, which encapsulates two evolutionary algorithms.  This
        ///     initializes the algorithms with the given population sizes and automatically generates a starting population for
        ///     each based on their respective size.
        /// </summary>
        /// <param name="populationSize1">The first population size.</param>
        /// <param name="populationSize2">The second population size.</param>
        /// <returns>Coevolution algorithm container.</returns>
        ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer(int populationSize1,
            int populationSize2);

        /// <summary>
        ///     Creates and returns a coevolution algorithm container, which encapsulates two evolutionary algorithms.  This
        ///     initializes the algorithms with two preconstructed populations and the genome factories used to create them.
        /// </summary>
        /// <param name="genomeFactory1">The first population genome factory.</param>
        /// <param name="genomeFactory2">The second population genome factory.</param>
        /// <param name="genomeList1">The first population genome list.</param>
        /// <param name="genomeList2">The second population genome list.</param>
        /// <returns>Coevolution algorithm container.</returns>
        ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> CreateCoevolutionAlgorithmContainer(
            IGenomeFactory<NeatGenome> genomeFactory1, IGenomeFactory<MazeGenome> genomeFactory2,
            List<NeatGenome> genomeList1, List<MazeGenome> genomeList2);
    }
}