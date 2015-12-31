#region

using System;
using System.Collections.Generic;
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
        private readonly int _nicheCapacity;

        /// <summary>
        ///     The genome populations in each niche.
        /// </summary>
        private readonly Dictionary<int, List<TGenome>> _nichePopulations;

        #endregion

        #region Private Methods

        /// <summary>
        ///     Creates the specified number of offspring asexually using the desired offspring count as a gauge for the FIFO
        ///     parent selection (it's a one-to-one mapping).
        /// </summary>
        /// <param name="nichePopulation">The current population of the niche under consideration.</param>
        /// <returns>The list of offspring.</returns>
        private List<TGenome> CreateOffspring(List<TGenome> nichePopulation)
        {
            // Calculate the number of offspring to be generated from the given niche population
            int numOffspring = Math.Max(1, (int) (nichePopulation.Count*_reproductionProportion));

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
        ///     Inserts a genome into the appropriate niche sub-population.
        /// </summary>
        /// <param name="genome">The genome to be inserted.</param>
        /// <param name="isInitialization">
        ///     Indicates whether this is being invoked as part of the algorithm initialization process
        ///     or as part of the primary loop.
        /// </param>
        private void AddGenomeToNiche(TGenome genome, bool isInitialization)
        {
            // If the dictionary does not currently contain a sub-population for the niche to which 
            // the current genome is assigned, create one and add that genome as the founding member
            if (_nichePopulations.ContainsKey(genome.EvaluationInfo.NicheId) == false)
            {
                _nichePopulations.Add(genome.EvaluationInfo.NicheId, new List<TGenome> {genome});
            }

            // Otherwise, if this is part of the algorithm primary loop (i.e. we're just mapping genomes into niches 
            // regardless of size to be resized later) or if this is part of the initialization process AND the assigned 
            // niche is below maximum capacity, get the existing sub-population for the niche and add the genome to that population
            else if (isInitialization == false || _nichePopulations[genome.EvaluationInfo.NicheId].Count < _nicheCapacity)
            {
                _nichePopulations[genome.EvaluationInfo.NicheId].Add(genome);
            }

            // If the niche is full, the genome has nowhere to be placed and is therefore not assigned 
            // and will be removed from the population (this will only fire during initialization)
            else
            {
                GenomeList.Remove(genome);
            }
        }

        /// <summary>
        ///     Removes the oldest genomes from each niche sub-population if that niche has exceeded its capacity.
        /// </summary>
        private void RemoveOldestGenomes()
        {
            foreach (KeyValuePair<int, List<TGenome>> nichePopulation in _nichePopulations)
            {
                // Sort the population by age (oldest to youngest)
                IEnumerable<TGenome> ageSortedNichePopulation =
                    nichePopulation.Value.OrderBy(g => g.BirthGeneration).AsParallel();

                // Select the requisitive number of oldest genomes proportional to the amount 
                // by which the niche capacity is exceeded (if the niche capacity has not been exceeded,
                // there will be zero genomes selected for removal)
                IEnumerable<TGenome> oldestNicheGenomes =
                    ageSortedNichePopulation.Take(Math.Max(0, nichePopulation.Value.Count - _nicheCapacity));

                // Remove the oldest genomes from the niche population and from the global population
                foreach (TGenome oldestNicheGenome in oldestNicheGenomes)
                {
                    nichePopulation.Value.Remove(oldestNicheGenome);
                    ((List<TGenome>) GenomeList).Remove(oldestNicheGenome);
                }
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
            // (this is iterating over a copy of the genome list because the contents could be modified within the loop)
            foreach (TGenome genome in GenomeList.ToList())
            {
                AddGenomeToNiche(genome, true);
            }
        }

        /// <summary>
        ///     Perform one iteration of the evolution algorithm.
        /// </summary>
        protected override void PerformOneGeneration()
        {
            // Initialize new list of child genomes
            List<TGenome> childGenomes = new List<TGenome>();

            // Produce offspring from each niche and add them to the list of child genomes
            foreach (KeyValuePair<int, List<TGenome>> nichePopulation in _nichePopulations)
            {
                childGenomes.AddRange(CreateOffspring(nichePopulation.Value));
            }

            // Evaluate all of the children
            GenomeEvaluator.Evaluate(childGenomes, CurrentGeneration);

            // Remove child genomes that are not viable
            childGenomes.RemoveAll(genome => genome.EvaluationInfo.IsViable == false);

            // Add genomes to the global population
            (GenomeList as List<TGenome>)?.AddRange(childGenomes);

            // Insert children into the appropriate niches
            foreach (TGenome childGenome in childGenomes)
            {
                AddGenomeToNiche(childGenome, false);
            }

            // Remove oldest from niches if they exceed niche capacity
            RemoveOldestGenomes();

            // Update the total offspring count based on the number of *viable* offspring produced
            Statistics._totalOffspringCount = (ulong) childGenomes.Count;

            // Update stats and store reference to best genome.
            UpdateBestGenomeWithoutSpeciation(false, false);
            UpdateStats(false, false);

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
            int nicheCapacity,
            RunPhase runPhase = RunPhase.Primary,
            IDataLogger logger = null, IDictionary<FieldElement, bool> logFieldEnabledMap = null)
        {
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _reproductionProportion = reproductionProportion;
            _nicheCapacity = nicheCapacity;
            EvolutionLogger = logger;
            RunPhase = runPhase;
            _logFieldEnabledMap = logFieldEnabledMap;
            _nichePopulations = new Dictionary<int, List<TGenome>>();
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
            int nicheCapacity,
            RunPhase runPhase = RunPhase.Primary,
            IDataLogger logger = null, IDictionary<FieldElement, bool> logFieldEnabledMap = null) : base(eaParams)
        {
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _reproductionProportion = reproductionProportion;
            _nicheCapacity = nicheCapacity;
            EvolutionLogger = logger;
            RunPhase = runPhase;
            _logFieldEnabledMap = logFieldEnabledMap;
            _nichePopulations = new Dictionary<int, List<TGenome>>();
        }

        #endregion
    }
}