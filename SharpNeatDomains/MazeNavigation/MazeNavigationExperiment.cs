using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

namespace SharpNeat.Domains.MazeNavigation
{
    internal class MazeNavigationExperiment : IGuiNeatExperiment
    {
        private NetworkActivationScheme _activationScheme;
        private string _complexityRegulationStrategy;
        private int? _complexitythreshold;
        private ParallelOptions _parallOptions;

        /// <summary>
        ///     Name of the experiment.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Description of the experiment.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        ///     The neureal network input count.
        /// </summary>
        public int InputCount { get; private set; }

        /// <summary>
        ///     The neural network output count.
        /// </summary>
        public int OutputCount { get; private set; }

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
        public void Initialize(string name, XmlElement xmlConfig)
        {
            // Set all properties
            Name = name;
            DefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");
            Description = XmlUtils.GetValueAsString(xmlConfig, "Description");

            // Set all internal class variables
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            _complexityRegulationStrategy = XmlUtils.TryGetValueAsString(xmlConfig, "ComplexityRegulationStrategy");
            _complexitythreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");
            _parallOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);

            // Set evolution/genome parameters
            NeatEvolutionAlgorithmParameters = new NeatEvolutionAlgorithmParameters();
            NeatEvolutionAlgorithmParameters.SpecieCount = XmlUtils.GetValueAsInt(xmlConfig, "SpecieCount");
            NeatGenomeParameters = new NeatGenomeParameters();
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
        ///     Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various sub-parts of
        ///     the algorithm are also constructed and connected up.  Uses the default population size.
        /// </summary>
        /// <returns>NEAT evolutionary algorithm</returns>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize);
        }

        /// <summary>
        ///     Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search based on the given
        ///     population size. Various sub-parts of the algorithm are also constructed and connected up.
        /// </summary>
        /// <param name="populationSize">The genome population size</param>
        /// <returns>NEAT evoluationary algorithm</returns>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            var genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            var genomeList = genomeFactory.CreateGenomeList(DefaultPopulationSize, 0);

            // Create evolution algorithm.
            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
        }

        /// <summary>
        ///     Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search based on the given
        ///     genome factory and genome list.  Various sub-parts of the algorithm are also constructed and connected up.
        /// </summary>
        /// <param name="genomeFactory">The genome factory from which to generate new genomes</param>
        /// <param name="genomeList">The current genome population</param>
        /// <returns>Constructed evolutionary algorithm</returns>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList)
        {
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy =
                new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, _parallOptions);

            // Create complexity regulation strategy.
            var complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(_complexityRegulationStrategy, _complexitythreshold);

            // Create the evolution algorithm.
            var ea = new NeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters, speciationStrategy,
                complexityRegulationStrategy);

            // Create IBlackBox evaluator.
            var mazeNavigationEvaluator = new MazeNavigationEvaluator();

            // Create genome decoder.
            var genomeDecoder = CreateGenomeDecoder();

            // Create a genome list evaluator. This packages up the genome decoder with the genome evaluator.
            IGenomeListEvaluator<NeatGenome> listEvaluator =
                new ParallelGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                    _parallOptions);

            // Initialize the evolution algorithm.
            ea.Initialize(listEvaluator, genomeFactory, genomeList);

            // Finished. Return the evolution algorithm
            return ea;
        }

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
            return null;
        }
    }
}