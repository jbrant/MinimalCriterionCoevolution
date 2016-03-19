using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;

namespace SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment
{
    public class CoevolutionMazeNavigationMCSExperiment : ICoevolutionExperiment
    {
        #region Public Properties

        public string Name { get; private set; }

        public string Description { get; private set; }

        public int DefaultPopulationSize1 { get; private set; }

        public int DefaultPopulationSize2 { get; private set; }

        #endregion

        #region Instance Variables

        private NeatEvolutionAlgorithmParameters _neatEvolutionAlgorithmParameters;

        private NeatGenomeParameters _neatGenomeParameters;

        private MazeGenomeParameters _mazeGenomeParameters;

        /// <summary>
        ///     The maximum number of evaluations allowed (optional).
        /// </summary>
        private ulong? MaxEvaluations;

        /// <summary>
        ///     The maximum number of generations allowed (optional).
        /// </summary>
        private int? MaxGenerations;

        #endregion

        #region Public Methods

        public void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger = null,
            IDataLogger evaluationDataLogger = null)
        {
            // Set boiler plate properties
            Name = name;
            DefaultPopulationSize1 = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationSize1") ?? default(int);
            DefaultPopulationSize2 = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationSize2") ?? default(int);
            Description = XmlUtils.GetValueAsString(xmlConfig, "Description");

            // Set the evolution/genome parameters
            _neatEvolutionAlgorithmParameters = ExperimentUtils.ReadNeatEvolutionAlgorithmParameters(xmlConfig);
            _neatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);
            
        }

        #endregion
    }
}
