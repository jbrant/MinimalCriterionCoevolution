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
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
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

        private int _substrateResolution;

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
            int numPixels = _substrateResolution*_substrateResolution;

            int xEnd = _substrateResolution/2;
            int yEnd = xEnd;
            int xStart = (_substrateResolution / 2) * -1;
            int yStart = xStart;

            SubstrateNodeSet inputLayer = new SubstrateNodeSet(numPixels);

            uint inputId = 1, outputId = (uint)numPixels + 1;

            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++, inputId++)
                {
                    inputLayer.NodeList.Add(new SubstrateNode(inputId, new double[] {x, y}));
                }
            }

            return null;
        }

        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return new CppnGenomeFactory(InputCount, OutputCount, DefaultActivationFunctionLibrary.CreateLibraryCppn(), NeatGenomeParameters);
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
