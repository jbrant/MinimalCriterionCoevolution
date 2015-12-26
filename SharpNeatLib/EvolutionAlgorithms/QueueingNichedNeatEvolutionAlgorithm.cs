#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.SpeciationStrategies;

#endregion

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    ///     Implementation of a queue-based NEAT evolution algorithm with a separate queue per niche (whether genetic or
    ///     behavioral).  Note that this omits the speciation aspects of NEAT (because the algorithm inherently imposes no
    ///     preference other than age-based removal) and reproduction is asexual only.
    /// </summary>
    /// <typeparam name="TGenome">The genome type that the algorithm will operate on.</typeparam>
    public class QueueingNichedNeatEvolutionAlgorithm<TGenome> : AbstractNeatEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        #region Instance Fields

        /// <summary>
        ///     The proportion (percentage) of genomes to produce offspring in a single "generation".  In other words,
        ///     a certain percentage of the niche population will be selected for reproduction; the exact number will vary based on
        ///     the niche size.
        /// </summary>
        private readonly double _reproductionProportion;

        /// <summary>
        ///     The number of genomes that can "reside" within a single niche.
        /// </summary>
        private readonly uint _nicheCapacity;

        /// <summary>
        ///     The genome populations in each niche.
        /// </summary>
        private readonly Dictionary<uint, List<TGenome>> _nichePopulations;

        #endregion

        #region Private Methods

        /// <summary>
        ///     Creates the specified number of offspring asexually using the desired offspring count as a gauge for the FIFO
        ///     parent selection (it's a one-to-one mapping).
        /// </summary>
        /// <param name="offspringCount">The number of offspring to produce.</param>
        /// <returns>The list of offspring.</returns>
        private List<TGenome> CreateOffspring(List<TGenome> nichePopulation)
        {
            // Calculate the number of offspring to be generated from the given niche population
            int numOffspring = nichePopulation.Count* (int)_reproductionProportion;

            List<TGenome> offspringList = new List<TGenome>(numOffspring);

            // Get the parent genomes
            List<TGenome> parentList = nichePopulation.GetRange(0, numOffspring);

            // Remove the parents from the queue
            nichePopulation.RemoveRange(0, numOffspring);

            // Generate an offspring asexually for each parent genome (this is not done asexually because depending on the batch size, 
            // we may not be able to have genomes from the same species mate)
            offspringList.AddRange(parentList.Select(parentGenome => parentGenome.CreateOffspring(CurrentGeneration)));

            // Move the parents who replaced offspring to the back of the queue (list)
            nichePopulation.AddRange(parentList);

            return offspringList;
        }

        /// <summary>
        ///     Removes the specified number of oldest genomes from the population.
        /// </summary>
        /// <param name="numGenomesToRemove">The number of oldest genomes to remove from the population.</param>
        private void RemoveOldestGenomes(int numGenomesToRemove)
        {
            // Sort the population by age (oldest to youngest)
            IEnumerable<TGenome> ageSortedPopulation =
                ((List<TGenome>) GenomeList).OrderBy(g => g.BirthGeneration).AsParallel();

            // Select the specified number of oldest genomes
            IEnumerable<TGenome> oldestGenomes = ageSortedPopulation.Take(numGenomesToRemove);

            // Remove the oldest genomes from the population
            foreach (TGenome oldestGenome in oldestGenomes)
            {
                ((List<TGenome>) GenomeList).Remove(oldestGenome);
            }
        }

        #endregion

        #region Overridden Methods

        /// <summary>
        ///     Intercepts the call to initialize, calls the base intializer first to generate an initial population, then ensures
        ///     that all individuals in the initial population satisfy the minimal criteria.
        /// </summary>
        protected override void Initialize()
        {
            // Open the logger
            EvolutionLogger?.Open();

            // Update the run phase on the logger
            EvolutionLogger?.UpdateRunPhase(RunPhase);

            // Write out the header
            EvolutionLogger?.LogHeader(GetLoggableElements(), Statistics.GetLoggableElements(),
                (CurrentChampGenome as NeatGenome)?.GetLoggableElements());

            // Initialize the genome evalutor
            GenomeEvaluator.Initialize();

            // If the population has not yet undergone intialization evaluations, 
            // run them through a cycle of evaluations now and update the best genome
            if (GenomeList.Any(genome => genome.EvaluationInfo.EvaluationCount <= 0))
            {
                GenomeEvaluator.Evaluate(GenomeList, CurrentGeneration);
                UpdateBestGenomeWithoutSpeciation(false, false);
            }

            // Populate niche map with population of genomes that are specific to that niche
            foreach (TGenome genome in GenomeList)
            {
                // If the dictionary does not currently contain a sub-population for the niche to which 
                // the current genome is assigned, create one and add that genome as the founding member
                if (_nichePopulations.ContainsKey(genome.EvaluationInfo.NicheId) == false)
                {
                    _nichePopulations.Add(genome.EvaluationInfo.NicheId, new List<TGenome> {genome});
                }
                // Otherwise, if the assigned niche is below maximum capacity, get the existing sub-population 
                // for the niche and add the genome to that population
                else if (_nichePopulations[genome.EvaluationInfo.NicheId].Count < _nicheCapacity)
                {
                    _nichePopulations[genome.EvaluationInfo.NicheId].Add(genome);
                }

                // If the niche is full, the genome has nowhere to be placed and is therefore not assigned 
                // and will be removed from the population (in reality, this will probably never happen)
                else
                {
                    GenomeList.Remove(genome);
                }
            }
        }

        /// <summary>
        ///     Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        protected override void PerformOneGeneration()
        {
            // Produce offspring from each niche and evaluate
            foreach (KeyValuePair<uint, List<TGenome>> nichePopulation in _nichePopulations)
            {
                List<TGenome> childGenomes = CreateOffspring(nichePopulation.Value);
            }

            

            // If there is a logger defined, log the generation stats
            EvolutionLogger?.LogRow(GetLoggableElements(_logFieldEnabledMap),
                Statistics.GetLoggableElements(_logFieldEnabledMap),
                (CurrentChampGenome as NeatGenome)?.GetLoggableElements(_logFieldEnabledMap));
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs steady state evolution algorithm with the default clustering strategy (k-means clustering) using
        ///     manhattan distance and null complexity regulation strategy.
        /// </summary>
        /// <param name="logger">The data logger (optional).</param>
        /// <param name="runPhase">
        ///     The experiment phase indicating whether this is an initialization process or the primary
        ///     algorithm.
        /// </param>
        public QueueingNichedNeatEvolutionAlgorithm(IDataLogger logger = null, RunPhase runPhase = RunPhase.Primary)
            : this(
                new NullComplexityRegulationStrategy(), 0.2, 100, runPhase, logger)
        {
            SpeciationStrategy = new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric());
            ComplexityRegulationStrategy = new NullComplexityRegulationStrategy();
        }

        /// <summary>
        ///     Constructs steady state evolution algorithm with the given NEAT parameters and complexity regulation strategy.
        /// </summary>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="reproductionProportion">The percentage of individuals from each niche to reproduce offspring.</param>
        /// <param name="nicheCapacity">The number of genomes that can "reside" within a single niche.</param>
        /// <param name="runPhase">
        ///     The experiment phase indicating whether this is an initialization process or the primary
        ///     algorithm.
        /// </param>
        /// <param name="logger">The data logger (optional).</param>
        /// <param name="logFieldEnabledMap">Dictionary of logging fields that can be dynamically enabled or disabled.</param>
        public QueueingNichedNeatEvolutionAlgorithm(
            IComplexityRegulationStrategy complexityRegulationStrategy,
            double reproductionProportion,
            uint nicheCapacity,
            RunPhase runPhase = RunPhase.Primary,
            IDataLogger logger = null, IDictionary<FieldElement, bool> logFieldEnabledMap = null)
        {
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _reproductionProportion = reproductionProportion;
            _nicheCapacity = nicheCapacity;
            EvolutionLogger = logger;
            RunPhase = runPhase;
            _logFieldEnabledMap = logFieldEnabledMap;
            _nichePopulations = new Dictionary<uint, List<TGenome>>();
        }

        /// <summary>
        ///     Constructs steady state evolution algorithm with the given NEAT parameters and complexity regulation strategy.
        /// </summary>
        /// <param name="eaParams">The NEAT algorithm parameters.</param>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="reproductionProportion">The percentage of individuals from each niche to reproduce offspring.</param>
        /// <param name="nicheCapacity">The number of genomes that can "reside" within a single niche.</param>
        /// <param name="runPhase">
        ///     The experiment phase indicating whether this is an initialization process or the primary
        ///     algorithm.
        /// </param>
        /// <param name="logger">The data logger (optional).</param>
        /// <param name="logFieldEnabledMap">Dictionary of logging fields that can be dynamically enabled or disabled.</param>
        public QueueingNichedNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            double reproductionProportion,
            uint nicheCapacity,
            RunPhase runPhase = RunPhase.Primary,
            IDataLogger logger = null, IDictionary<FieldElement, bool> logFieldEnabledMap = null) : base(eaParams)
        {
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _reproductionProportion = reproductionProportion;
            _nicheCapacity = nicheCapacity;
            EvolutionLogger = logger;
            RunPhase = runPhase;
            _logFieldEnabledMap = logFieldEnabledMap;
            _nichePopulations = new Dictionary<uint, List<TGenome>>();
        }

        #endregion
    }
}