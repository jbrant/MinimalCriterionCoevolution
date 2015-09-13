#region

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

#endregion

namespace SharpNeat.Domains.ThreeParity
{
    /// <summary>
    ///     3-Parity XOR experiment.
    /// </summary>
    internal class ThreeParityExperiment : IGuiNeatExperiment
    {
        /// <summary>
        ///     The activation scheme (i.e. acyclic, relaxing).
        /// </summary>
        private NetworkActivationScheme _activationScheme;

        /// <summary>
        ///     The complexity regulation strategy (i.e. complexifying, simplifying).
        /// </summary>
        private string _complexityRegulationStrategy;

        /// <summary>
        ///     The threshold at which to simplify.
        /// </summary>
        private int? _complexityThreshold;

        /// <summary>
        ///     Path/File to which to write generational data log.
        /// </summary>
        private string _generationalLogFile;

        /// <summary>
        ///     Multithreading control.
        /// </summary>
        private ParallelOptions _parallelOptions;

        /// <summary>
        ///     The number of species.
        /// </summary>
        private int _specieCount;

        /// <summary>
        ///     The name of the experiment.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     The description of the experiment.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        ///     The number of ANN input nodes.
        /// </summary>
        public int InputCount => 3;

        /// <summary>
        ///     The number of ANN output nodes.
        /// </summary>
        public int OutputCount => 1;

        /// <summary>
        ///     The number of genomes in the population (this count will be held constant throughout evolution).
        /// </summary>
        public int DefaultPopulationSize { get; private set; }

        /// <summary>
        ///     The parameters specific to NEAT-algorithm control.
        /// </summary>
        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters { get; private set; }

        /// <summary>
        ///     The parameters for controlling NEAT genomes.
        /// </summary>
        public NeatGenomeParameters NeatGenomeParameters { get; private set; }

        /// <summary>
        ///     Initializes the experiment with the provided XML configuration.
        /// </summary>
        /// <param name="name">The name of the experiment.</param>
        /// <param name="xmlConfig">The reference to the XML configuration.</param>
        public void Initialize(string name, XmlElement xmlConfig)
        {
            Name = name;
            DefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");
            _specieCount = XmlUtils.GetValueAsInt(xmlConfig, "SpecieCount");
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            _complexityRegulationStrategy = XmlUtils.TryGetValueAsString(xmlConfig, "ComplexityRegulationStrategy");
            _complexityThreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");
            Description = XmlUtils.TryGetValueAsString(xmlConfig, "Description");
            _parallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);
            _generationalLogFile = XmlUtils.TryGetValueAsString(xmlConfig, "GenerationalLogFile");

            NeatEvolutionAlgorithmParameters = new NeatEvolutionAlgorithmParameters();
            NeatEvolutionAlgorithmParameters.SpecieCount = _specieCount;
            NeatGenomeParameters = new NeatGenomeParameters();
            NeatGenomeParameters.FeedforwardOnly = _activationScheme.AcyclicNetwork;
            NeatGenomeParameters.ActivationFn = PlainSigmoid.__DefaultInstance;
        }

        /// <summary>
        ///     Loads the starting list of genomes (if any) from the XML configuration.
        /// </summary>
        /// <param name="xr">The XML file reader.</param>
        /// <returns>The initialized list of genomes.</returns>
        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            NeatGenomeFactory genomeFactory = (NeatGenomeFactory) CreateGenomeFactory();
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, genomeFactory);
        }

        /// <summary>
        ///     Serializes the population of genomes to an XML file for later reuse.
        /// </summary>
        /// <param name="xw">The XML file writer.</param>
        /// <param name="genomeList">The list of genomes to serialize.</param>
        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            // Writing node IDs is not necessary for NEAT.
            NeatGenomeXmlIO.WriteComplete(xw, genomeList, false);
        }

        /// <summary>
        ///     Constructs the genome decoder using the given activation scheme.
        /// </summary>
        /// <returns>The genome decoder.</returns>
        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            return new NeatGenomeDecoder(_activationScheme);
        }

        /// <summary>
        ///     Constructs the genome factory given the input/output nodes and NEAT parameters.
        /// </summary>
        /// <returns>The genome factory.</returns>
        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return new NeatGenomeFactory(InputCount, OutputCount, NeatGenomeParameters);
        }

        /// <summary>
        ///     Constructs and returns a NEAT evolution algorithm, using the default population size specified in the XML
        ///     configuration.
        /// </summary>
        /// <returns>The NEAT evolution algorithm.</returns>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize);
        }

        /// <summary>
        ///     Constructs and returns a NEAT evolution algorithm, using the given population size.
        /// </summary>
        /// <param name="populationSize">The nubmer of genomes in the population.</param>
        /// <returns>The NEAT evolution algorithm.</returns>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            // Instantiate and return the evolution algorithm
            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
        }

        /// <summary>
        ///     Constructs and returns a NEAT evolution algorithm, using the given genome factory and genome list.
        /// </summary>
        /// <param name="genomeFactory">The genome factory to use for generating offspring during evolution.</param>
        /// <param name="genomeList">The initial list of genomes.</param>
        /// <returns>The NEAT evolution algorithm.</returns>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList)
        {
            FileDataLogger logger = null;

            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy =
                new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, _parallelOptions);

            // Create complexity regulation strategy.
            IComplexityRegulationStrategy complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(_complexityRegulationStrategy, _complexityThreshold);

            // Initialize the logger
            if (_generationalLogFile != null)
            {
                logger =
                    new FileDataLogger(_generationalLogFile);
            }

            // Create the evolution algorithm.
            GenerationalNeatEvolutionAlgorithm<NeatGenome> ea =
                new GenerationalNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                    speciationStrategy, complexityRegulationStrategy, logger);

            // Create black box evaluator.
            ThreeParityEvaluator evaluator = new ThreeParityEvaluator();

            // Create genome decoder. Decodes to a neural network packaged with an activation scheme.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

            // Create a genome list evaluator. This packages up the genome decoder with the genome evaluator.
            IGenomeEvaluator<NeatGenome> innerFitnessEvaluator =
                new ParallelGenomeFitnessEvaluator<NeatGenome, IBlackBox>(genomeDecoder, evaluator, _parallelOptions);

            // Wrap the list evaluator in a 'selective' evaulator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
            // that were in the population in previous generations (elite genomes). This is determiend by examining each genome's evaluation info object.
            IGenomeEvaluator<NeatGenome> selectiveFitnessEvaluator = new SelectiveGenomeFitnessEvaluator<NeatGenome>(
                innerFitnessEvaluator,
                SelectiveGenomeFitnessEvaluator<NeatGenome>.CreatePredicate_OnceOnly());

            // Initialize the evolution algorithm.
            ea.Initialize(selectiveFitnessEvaluator, genomeFactory, genomeList);

            // Finished. Return the evolution algorithm
            return ea;
        }

        /// <summary>
        ///     Creates a form for displaying genomes.
        /// </summary>
        /// <returns></returns>
        public AbstractGenomeView CreateGenomeView()
        {
            return new NeatGenomeView();
        }

        /// <summary>
        ///     Creates a form for displaying a domain-specific visualization (not used for the 3-parity experiment).
        /// </summary>
        /// <returns></returns>
        public AbstractDomainView CreateDomainView()
        {
            return null;
        }
    }
}