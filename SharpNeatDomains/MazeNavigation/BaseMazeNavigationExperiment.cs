using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    internal abstract class BaseMazeNavigationExperiment : IGuiNeatExperiment
    {
        private NetworkActivationScheme _activationScheme;
        protected string ComplexityRegulationStrategy;
        protected int? Complexitythreshold;
        protected int? MaxDistanceToTarget;
        protected int? MaxTimesteps;
        protected MazeVariant MazeVariant;
        protected int? MinSuccessDistance;
        protected ParallelOptions ParallelOptions;
        
        /// <summary>
        ///     Name of the experiment.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Description of the experiment.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        ///     The neureal network input count (excluding the bias).
        /// </summary>
        public int InputCount => 10;

        /// <summary>
        ///     The neural network output count.
        /// </summary>
        public int OutputCount => 2;

        /// <summary>
        ///     The default population size for the experiment.
        /// </summary>
        public int DefaultPopulationSize { get; private set; }

        /// <summary>
        ///     The NEAT parameters to use for the experiment.
        /// </summary>
        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters { get; private set; }

        /// <summary>
        ///     The NEAT genome parameters to use for the experiment.
        /// </summary>
        public NeatGenomeParameters NeatGenomeParameters { get; private set; }

        /// <summary>
        ///     Initialize the experiment with configuration file parameters.
        /// </summary>
        /// <param name="name">The name of the experiment</param>
        /// <param name="xmlConfig">The parent XML configuration element</param>
        public virtual void Initialize(string name, XmlElement xmlConfig)
        {
            // Set all properties
            Name = name;
            DefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");
            Description = XmlUtils.GetValueAsString(xmlConfig, "Description");

            // Set all internal class variables
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            ComplexityRegulationStrategy = XmlUtils.TryGetValueAsString(xmlConfig, "ComplexityRegulationStrategy");
            Complexitythreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");
            ParallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);

            // Set evolution/genome parameters
            NeatEvolutionAlgorithmParameters = new NeatEvolutionAlgorithmParameters
            {
                SpecieCount = XmlUtils.GetValueAsInt(xmlConfig, "SpecieCount"),
                InterspeciesMatingProportion = XmlUtils.GetValueAsDouble(xmlConfig,
                    "InterspeciesMatingProbability"),
                MinTimeAlive = XmlUtils.GetValueAsInt(xmlConfig, "MinTimeAlive")
            };
            NeatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);

            // Set experiment-specific parameters
            MaxTimesteps = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxTimesteps");
            MinSuccessDistance = XmlUtils.TryGetValueAsInt(xmlConfig, "MinSuccessDistance");
            MaxDistanceToTarget = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxDistanceToTarget");
            MazeVariant =
                MazeVariantUtl.convertStringToMazeVariant(XmlUtils.TryGetValueAsString(xmlConfig, "MazeVariant"));         
        }

        /// <summary>
        ///     Loads a new population from the XML reader and returns the NEAT genomes in a list.
        /// </summary>
        /// <param name="xr">The XML reader</param>
        /// <returns>List of NEAT genomes</returns>
        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            var genomeFactory = CreateGenomeFactory() as NeatGenomeFactory;
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, genomeFactory);
        }

        /// <summary>
        ///     Saves the population to an XML file.
        /// </summary>
        /// <param name="xw">The XML writer object</param>
        /// <param name="genomeList">The list of NEAT genomes to save</param>
        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            NeatGenomeXmlIO.WriteComplete(xw, genomeList, false);
        }

        /// <summary>
        ///     Creates a genome decoder with the experiment's activation scheme.
        /// </summary>
        /// <returns></returns>
        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            return new NeatGenomeDecoder(_activationScheme);
        }

        /// <summary>
        ///     Creates a genome factory with the experiment-specific inputs, outputs, and genome parameters.
        /// </summary>
        /// <returns>NEAT genome factory</returns>
        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return new NeatGenomeFactory(InputCount, OutputCount, NeatGenomeParameters);
        }

        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various sub-parts of
        ///     the algorithm are also constructed and connected up.  Uses the default population size.
        /// </summary>
        /// <returns>NEAT evolutionary algorithm</returns>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize);
        }

        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search based on the given
        ///     population size. Various sub-parts of the algorithm are also constructed and connected up.
        /// </summary>
        /// <param name="populationSize">The genome population size</param>
        /// <returns>NEAT evoluationary algorithm</returns>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            var genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            var genomeList = genomeFactory.CreateGenomeList(DefaultPopulationSize, 0);

            // Create evolution algorithm.
            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
        }

        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search based on the given
        ///     genome factory and genome list.  Various sub-parts of the algorithm are also constructed and connected up.
        /// </summary>
        /// <param name="genomeFactory">The genome factory from which to generate new genomes</param>
        /// <param name="genomeList">The current genome population</param>
        /// <returns>Constructed evolutionary algorithm</returns>
        public abstract INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList);

        /// <summary>
        ///     Create a System.Windows.Forms derived object for displaying genomes.
        /// </summary>
        /// <returns></returns>
        public AbstractGenomeView CreateGenomeView()
        {
            return new NeatGenomeView();
        }

        /// <summary>
        ///     Create a System.Windows.Forms derived object for displaying output for a domain (e.g. show best genome's
        ///     output/performance/behaviour in the domain).
        /// </summary>
        /// <returns></returns>
        public AbstractDomainView CreateDomainView()
        {
            return new MazeNavigationView(new NeatGenomeDecoder(_activationScheme),
                new MazeNavigationWorld<ITrialInfo>(MazeVariant, MinSuccessDistance, MaxDistanceToTarget, MaxTimesteps));
        }
    }
}