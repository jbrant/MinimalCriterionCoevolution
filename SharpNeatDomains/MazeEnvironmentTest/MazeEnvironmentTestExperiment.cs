using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ExperimentEntities;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.HyperNeat;
using SharpNeat.Decoders.Neat;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using RunPhase = SharpNeat.Core.RunPhase;

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
        private int _substrateResolution;
        private int _batchSize;

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

            _substrateResolution = XmlUtils.GetValueAsInt(xmlConfig, "Resolution");
            NeatEvolutionAlgorithmParameters = new NeatEvolutionAlgorithmParameters();
            NeatGenomeParameters = new NeatGenomeParameters();
        }

        public void Initialize(ExperimentDictionary databaseContext)
        {
            throw new NotImplementedException();
        }

        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            NeatGenomeFactory genomeFactory = (NeatGenomeFactory)CreateGenomeFactory();
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, genomeFactory);
        }

        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            NeatGenomeXmlIO.WriteComplete(xw, genomeList, true);
        }

        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            return new NeatGenomeDecoder(_activationScheme);
        }

        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return new CppnGenomeFactory(InputCount, OutputCount, DefaultActivationFunctionLibrary.CreateLibraryCppn(), NeatGenomeParameters);
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize);
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList)
        {
            IComplexityRegulationStrategy complexityRegulationStrategy = ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategy, Complexitythreshold);

            AbstractNeatEvolutionAlgorithm<NeatGenome> ea =
                new QueueingNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters, null,
                    complexityRegulationStrategy, _batchSize);

            // TODO: Create the network evaluator

            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

            return null;
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList,
            ulong startingEvaluations)
        {
            throw new NotImplementedException();
        }

        public AbstractGenomeView CreateGenomeView()
        {
            return new CppnGenomeView(DefaultActivationFunctionLibrary.CreateLibraryCppn());
        }

        public AbstractDomainView CreateDomainView()
        {
            return null;
        }
    }
}
