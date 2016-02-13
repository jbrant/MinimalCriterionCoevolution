using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ExperimentEntities;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeEnvironmentTest
{
    class MazeEnvironmentTestExperiment : IGuiNeatExperiment
    {
        public string Name { get; private set; }

        public string Description { get; private set; }

        public int InputCount => 2;

        public int OutputCount => 1;

        public int DefaultPopulationSize { get; protected set; }

        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters { get; private set; }

        public NeatGenomeParameters NeatGenomeParameters { get; private set; }

        protected string ComplexityRegulationStrategy;

        protected int? Complexitythreshold;

        private NetworkActivationScheme _activationScheme;

        private ParallelOptions _parallelOptions;

        public void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger = null,
            IDataLogger evaluationDataLogger = null)
        {
            Name = name;
            DefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            ComplexityRegulationStrategy = XmlUtils.TryGetValueAsString(xmlConfig, "ComplexityRegulationStrategy");
            Complexitythreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");
            Description = XmlUtils.TryGetValueAsString(xmlConfig, "Description");
            _parallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);


        }

        public void Initialize(ExperimentDictionary databaseContext)
        {
            throw new NotImplementedException();
        }

        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            throw new NotImplementedException();
        }

        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            throw new NotImplementedException();
        }

        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            throw new NotImplementedException();
        }

        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            throw new NotImplementedException();
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            throw new NotImplementedException();
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            throw new NotImplementedException();
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList)
        {
            throw new NotImplementedException();
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList,
            ulong startingEvaluations)
        {
            throw new NotImplementedException();
        }

        public AbstractGenomeView CreateGenomeView()
        {
            throw new NotImplementedException();
        }

        public AbstractDomainView CreateDomainView()
        {
            throw new NotImplementedException();
        }
    }
}
