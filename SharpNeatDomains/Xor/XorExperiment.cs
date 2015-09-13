/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */

#region

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using log4net;
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

namespace SharpNeat.Domains.Xor
{
    /// <summary>
    ///     INeatExperiment for the XOR logic gate problem domain.
    /// </summary>
    public class XorExperiment : IGuiNeatExperiment
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private NetworkActivationScheme _activationScheme;
        private string _complexityRegulationStr;
        private int? _complexityThreshold;
        private string _generationalLogFile;
        private ParallelOptions _parallelOptions;
        private int _specieCount;

        #region Constructor

        #endregion

        #region INeatExperiment

        /// <summary>
        ///     Gets the name of the experiment.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Gets human readable explanatory text for the experiment.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        ///     Gets the number of inputs required by the network/black-box that the underlying problem domain is based on.
        /// </summary>
        public int InputCount
        {
            get { return 2; }
        }

        /// <summary>
        ///     Gets the number of outputs required by the network/black-box that the underlying problem domain is based on.
        /// </summary>
        public int OutputCount
        {
            get { return 1; }
        }

        /// <summary>
        ///     Gets the default population size to use for the experiment.
        /// </summary>
        public int DefaultPopulationSize { get; private set; }

        /// <summary>
        ///     Gets the NeatEvolutionAlgorithmParameters to be used for the experiment. Parameters on this object can be
        ///     modified. Calls to CreateEvolutionAlgorithm() make a copy of and use this object in whatever state it is in
        ///     at the time of the call.
        /// </summary>
        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters { get; private set; }

        /// <summary>
        ///     Gets the NeatGenomeParameters to be used for the experiment. Parameters on this object can be modified. Calls
        ///     to CreateEvolutionAlgorithm() make a copy of and use this object in whatever state it is in at the time of the
        ///     call.
        /// </summary>
        public NeatGenomeParameters NeatGenomeParameters { get; private set; }

        /// <summary>
        ///     Initialize the experiment with some optional XML configutation data.
        /// </summary>
        public void Initialize(string name, XmlElement xmlConfig)
        {
            Name = name;
            DefaultPopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");
            _specieCount = XmlUtils.GetValueAsInt(xmlConfig, "SpecieCount");
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            _complexityRegulationStr = XmlUtils.TryGetValueAsString(xmlConfig, "ComplexityRegulationStrategy");
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
        ///     Load a population of genomes from an XmlReader and returns the genomes in a new list.
        ///     The genome factory for the genomes can be obtained from any one of the genomes.
        /// </summary>
        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            NeatGenomeFactory genomeFactory = (NeatGenomeFactory) CreateGenomeFactory();
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, genomeFactory);
        }

        /// <summary>
        ///     Save a population of genomes to an XmlWriter.
        /// </summary>
        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            // Writing node IDs is not necessary for NEAT.
            NeatGenomeXmlIO.WriteComplete(xw, genomeList, false);
        }

        /// <summary>
        ///     Create a genome decoder for the experiment.
        /// </summary>
        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            return new NeatGenomeDecoder(_activationScheme);
        }

        /// <summary>
        ///     Create a genome factory for the experiment.
        ///     Create a genome factory with our neat genome parameters object and the appropriate number of input and output
        ///     neuron genes.
        /// </summary>
        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return new NeatGenomeFactory(InputCount, OutputCount, NeatGenomeParameters);
        }

        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various
        ///     sub-parts
        ///     of the algorithm are also constructed and connected up.
        ///     Uses the experiments default population size defined in the experiment's config XML.
        /// </summary>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize);
        }

        /// <summary>
        ///     Create and return a GenerationalNeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various
        ///     sub-parts
        ///     of the algorithm are also constructed and connected up.
        ///     This overload accepts a population size parameter that specifies how many genomes to create in an initial randomly
        ///     generated population.
        /// </summary>
        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            // Create evolution algorithm.
            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
        }

        /// <summary>
        ///     Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various sub-parts
        ///     of the algorithm are also constructed and connected up.
        ///     This overload accepts a pre-built genome population and their associated/parent genome factory.
        /// </summary>
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
                ExperimentUtils.CreateComplexityRegulationStrategy(_complexityRegulationStr, _complexityThreshold);

            // Initialize the logger
            if (_generationalLogFile != null)
            {
                logger =
                    new FileDataLogger(_generationalLogFile);
            }

            // Create the evolution algorithm.
            GenerationalNeatEvolutionAlgorithm<NeatGenome> ea =
                new GenerationalNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters, speciationStrategy,
                    complexityRegulationStrategy, logger);

            // Create IBlackBox evaluator.
            XorBlackBoxEvaluator evaluator = new XorBlackBoxEvaluator();

            // Create genome decoder.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

            // Create a genome list evaluator. This packages up the genome decoder with the genome evaluator.
            IGenomeEvaluator<NeatGenome> innerFitnessEvaluator =
                new ParallelGenomeFitnessEvaluator<NeatGenome, IBlackBox>(genomeDecoder, evaluator, _parallelOptions);

            // Wrap the list evaluator in a 'selective' evaulator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
            // that were in the population in previous generations (elite genomes). This is determined by examining each genome's evaluation info object.
            IGenomeEvaluator<NeatGenome> selectiveFitnessEvaluator = new SelectiveGenomeFitnessEvaluator<NeatGenome>(
                innerFitnessEvaluator,
                SelectiveGenomeFitnessEvaluator<NeatGenome>.CreatePredicate_OnceOnly());
            // Initialize the evolution algorithm.
            ea.Initialize(selectiveFitnessEvaluator, genomeFactory, genomeList);

            // Finished. Return the evolution algorithm
            return ea;
        }

        /// <summary>
        ///     Create a System.Windows.Forms derived object for displaying genomes.
        /// </summary>
        public AbstractGenomeView CreateGenomeView()
        {
            return new NeatGenomeView();
        }

        /// <summary>
        ///     Create a System.Windows.Forms derived object for displaying output for a domain (e.g. show best genome's
        ///     output/performance/behaviour in the domain).
        /// </summary>
        public AbstractDomainView CreateDomainView()
        {
            return null;
        }

        #endregion
    }
}