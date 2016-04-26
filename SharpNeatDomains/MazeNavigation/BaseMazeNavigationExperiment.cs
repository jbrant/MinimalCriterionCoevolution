#region

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using ExperimentEntities;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation
{
    /// <summary>
    ///     The base class for all maze navigation experiments.
    /// </summary>
    public abstract class BaseMazeNavigationExperiment : IGuiNeatExperiment
    {
        #region Private members

        /// <summary>
        ///     The activation scheme (i.e. cyclic or acyclic).
        /// </summary>
        private NetworkActivationScheme _activationScheme;

        #endregion

        /// <summary>
        ///     Initialize the experiment with configuration file parameters.
        /// </summary>
        /// <param name="name">The name of the experiment</param>
        /// <param name="xmlConfig">The parent XML configuration element</param>
        /// <param name="evolutionDataLogger">The optional evolution data logger.</param>
        /// <param name="evaluationDataLogger">The optional evaluation data logger.</param>
        public virtual void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger,
            IDataLogger evaluationDataLogger)
        {
            // Set all properties
            Name = name;
            DefaultPopulationSize = XmlUtils.TryGetValueAsInt(xmlConfig, "PopulationSize") ?? default(int);
            Description = XmlUtils.GetValueAsString(xmlConfig, "Description");

            // Set all internal class variables
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            ComplexityRegulationStrategy = XmlUtils.TryGetValueAsString(xmlConfig, "ComplexityRegulationStrategy");
            Complexitythreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");
            ParallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);
            SerializeGenomeToXml = XmlUtils.TryGetValueAsBool(xmlConfig, "DecodeGenomesToXml") ?? false;
            MaxGenerations = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxGenerations");
            MaxEvaluations = XmlUtils.TryGetValueAsULong(xmlConfig, "MaxEvaluations");
            MaxRestarts = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxRestarts");

            // Set evolution/genome parameters
            NeatEvolutionAlgorithmParameters = ExperimentUtils.ReadNeatEvolutionAlgorithmParameters(xmlConfig);
            NeatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(xmlConfig);
            NeatGenomeParameters.FeedforwardOnly = _activationScheme.AcyclicNetwork;

            // Set experiment-specific parameters
            MaxTimesteps = XmlUtils.GetValueAsInt(xmlConfig, "MaxTimesteps");
            MinSuccessDistance = XmlUtils.GetValueAsInt(xmlConfig, "MinSuccessDistance");
            MaxDistanceToTarget = XmlUtils.GetValueAsInt(xmlConfig, "MaxDistanceToTarget");
            MazeVariant =
                MazeVariantUtil.convertStringToMazeVariant(XmlUtils.TryGetValueAsString(xmlConfig, "MazeVariant"));
        }

        /// <summary>
        ///     Initialize the experiment with database configuration parameters.
        /// </summary>
        /// <param name="experimentDictionary">The handle to the experiment dictionary row pulled from the database.</param>
        public virtual void Initialize(ExperimentDictionary experimentDictionary)
        {
            // Set all properties
            Name = experimentDictionary.ExperimentName;
            DefaultPopulationSize = experimentDictionary.Primary_PopulationSize;
            Description = experimentDictionary.ExperimentName;

            // Set all internal class variables
            _activationScheme = NetworkActivationScheme.CreateAcyclicScheme();
            ComplexityRegulationStrategy = experimentDictionary.Primary_ComplexityRegulationStrategy;
            Complexitythreshold = experimentDictionary.Primary_ComplexityThreshold;
            ParallelOptions = new ParallelOptions();
            SerializeGenomeToXml = experimentDictionary.SerializeGenomeToXml;
            MaxEvaluations = (ulong) experimentDictionary.MaxEvaluations;
            MaxRestarts = experimentDictionary.MaxRestarts;

            // Set evolution/genome parameters
            NeatEvolutionAlgorithmParameters = ExperimentUtils.ReadNeatEvolutionAlgorithmParameters(
                experimentDictionary, true);
            NeatGenomeParameters = ExperimentUtils.ReadNeatGenomeParameters(experimentDictionary, true);

            // Set experiment-specific parameters
            MaxTimesteps = experimentDictionary.MaxTimesteps;
            MinSuccessDistance = experimentDictionary.MinSuccessDistance;
            MaxDistanceToTarget = experimentDictionary.MaxDistanceToTarget ?? default(int);
            MazeVariant = MazeVariantUtil.convertStringToMazeVariant(experimentDictionary.ExperimentDomainName);
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
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various
        ///     sub-parts of the algorithm are also constructed and connected up.  Uses the default population size.
        /// </summary>
        /// <returns>NEAT evolutionary algorithm</returns>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize);
        }

        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search based on
        ///     the given population size. Various sub-parts of the algorithm are also constructed and connected up.
        /// </summary>
        /// <param name="populationSize">The genome population size</param>
        /// <returns>NEAT evoluationary algorithm</returns>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            var genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            var genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            // Create evolution algorithm.
            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
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

        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search based on
        ///     the given genome factory and genome list.  Various sub-parts of the algorithm are also constructed and connected
        ///     up.
        /// </summary>
        /// <param name="genomeFactory">The genome factory from which to generate new genomes</param>
        /// <param name="genomeList">The current genome population</param>
        /// <returns>Constructed evolutionary algorithm</returns>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList)
        {
            return CreateEvolutionAlgorithm(genomeFactory, genomeList, 0);
        }

        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search based on
        ///     the given genome factory and genome list.  Various sub-parts of the algorithm are also constructed and connected
        ///     up.
        /// </summary>
        /// <param name="genomeFactory">The genome factory from which to generate new genomes</param>
        /// <param name="genomeList">The current genome population</param>
        /// <param name="startingEvaluations">The number of evaluations that have been executed prior to the current run.</param>
        /// <returns>Constructed evolutionary algorithm</returns>
        public abstract INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList, ulong startingEvaluations);

        #region Public properties

        /// <summary>
        ///     The maximum number of experiment restarts allowed (usually from a new seed).
        /// </summary>
        public int? MaxRestarts { get; private set; }

        /// <summary>
        ///     Name of the experiment.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Description of the experiment.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        ///     The neural network input count (excluding the bias).
        /// </summary>
        public int InputCount => 10;

        /// <summary>
        ///     The neural network output count.
        /// </summary>
        public int OutputCount => 2;

        /// <summary>
        ///     The default population size for the experiment.
        /// </summary>
        public int DefaultPopulationSize { get; protected set; }

        /// <summary>
        ///     The number of genomes in the initial population.
        /// </summary>
        public int SeedGenomeCount { get; protected set; }

        /// <summary>
        ///     The NEAT parameters to use for the experiment.
        /// </summary>
        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters { get; private set; }

        /// <summary>
        ///     The NEAT genome parameters to use for the experiment.
        /// </summary>
        public NeatGenomeParameters NeatGenomeParameters { get; private set; }

        #endregion

        #region Protected members

        /// <summary>
        ///     The strategy for decomplexifying once the network reaches a certain size (i.e. relative to other networks or an
        ///     absolute number of genes).
        /// </summary>
        protected string ComplexityRegulationStrategy;

        /// <summary>
        ///     The threshold at which the network should be de-complexified (in terms of the number of genes).
        /// </summary>
        protected int? Complexitythreshold;

        /// <summary>
        ///     The maximum possible distance to the target location.
        /// </summary>
        protected int MaxDistanceToTarget;

        /// <summary>
        ///     The maximum number of evaluations allowed (optional).
        /// </summary>
        protected ulong? MaxEvaluations;

        /// <summary>
        ///     The maximum number of generations allowed (optional).
        /// </summary>
        protected int? MaxGenerations;

        /// <summary>
        ///     The maximum number of timesteps allowed for a single simulation.
        /// </summary>
        protected int MaxTimesteps;

        /// <summary>
        ///     The maze to use as the simulation environment.
        /// </summary>
        protected MazeVariant MazeVariant;

        /// <summary>
        ///     The minimum distance to the target required in order to have "solved" the maze.
        /// </summary>
        protected int MinSuccessDistance;

        /// <summary>
        ///     Switches between synchronous and asynchronous execution (with user-defined number of threads).
        /// </summary>
        protected ParallelOptions ParallelOptions;

        /// <summary>
        ///     Dictates whether genome XML should be serialized and logged.
        /// </summary>
        protected bool SerializeGenomeToXml;

        #endregion
    }
}