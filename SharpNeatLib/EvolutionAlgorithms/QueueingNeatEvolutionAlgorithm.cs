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
using SharpNeat.Utility;

#endregion

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    ///     Implementation of a queue-based NEAT evolution algorithm.  Note that this omits the speciation aspects of NEAT
    ///     (because the algorithm inherently imposes no preference other than age-based removal) and reproduction is asexual
    ///     only.
    /// </summary>
    /// <typeparam name="TGenome">The genome type that the algorithm will operate on.</typeparam>
    public class QueueingNeatEvolutionAlgorithm<TGenome> : AbstractNeatEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        #region Instance Fields

        /// <summary>
        ///     The number of genomes to generate, evaluate, and remove in a single "generation".
        /// </summary>
        private readonly int _batchSize;

        /// <summary>
        ///     Flag that indicates whether bridging (i.e. a temporary nudge in a potentially beneficial direction) is enabled.
        /// </summary>
        private readonly bool _isBridgingEnabled;

        /// <summary>
        ///     Flag that indicates whether the minimal criteria is dynamically updating.
        /// </summary>
        private readonly bool _isDynamicMinimalCriteria;

        /// <summary>
        ///     The number of generations/batches between updates to the minimal criteria.  This value is only used when the
        ///     minimal criteria is changing (dynamically) throughout evolution.
        /// </summary>
        private readonly int _mcUpdateInterval;

        #endregion

        #region Private Methods

        /// <summary>
        ///     Creates the specified number of offspring asexually using the desired offspring count as a gauge for the FIFO
        ///     parent selection (it's a one-to-one mapping).
        /// </summary>
        /// <param name="offspringCount">The number of offspring to produce.</param>
        /// <returns>The list of offspring.</returns>
        private List<TGenome> CreateOffspring(int offspringCount)
        {
            List<TGenome> offspringList = new List<TGenome>(offspringCount);

            // Get the parent genomes
            List<TGenome> parentList = ((List<TGenome>) GenomeList).GetRange(0, offspringCount);

            // Remove the parents from the queue
            ((List<TGenome>) GenomeList).RemoveRange(0, offspringCount);

            // Generate an offspring asexually for each parent genome (this is not done asexually because depending on the batch size, 
            // we may not be able to have genomes from the same species mate)
            offspringList.AddRange(parentList.Select(parentGenome => parentGenome.CreateOffspring(CurrentGeneration)));

            // Move the parents who replaced offspring to the back of the queue (list)
            ((List<TGenome>) GenomeList).AddRange(parentList);

            return offspringList;
        }

        /// <summary>
        ///     Removes the specified number of oldest genomes from the population.
        /// </summary>
        /// <param name="numGenomesToRemove">The number of oldest genomes to remove from the population.</param>
        private void RemoveOldestSpecieGenomes(int numGenomesToRemove)
        {
            List<TGenome> genomesToRemove = new List<TGenome>();
            List<Tuple<Specie<TGenome>, int>> specieRemovalCounts =
                new List<Tuple<Specie<TGenome>, int>>(SpecieList.Count);

            // Sort the species by size
            IEnumerable<Specie<TGenome>> sizeSortedSpecies =
                SpecieList.OrderByDescending(x => x.GenomeList.Count).AsParallel();

            foreach (Specie<TGenome> specie in sizeSortedSpecies)
            {
                // Calculate the number of genomes to remove from the current specie
                int curNumToRemove =
                    (int) Math.Round(((double) specie.GenomeList.Count/GenomeList.Count)*numGenomesToRemove, 0);

                // Tag the species with the number to remove for that species
                specieRemovalCounts.Add(new Tuple<Specie<TGenome>, int>(specie, curNumToRemove));
            }

            // Sum the total number of genomes identified for removal
            int totalMarkedForRemoval = specieRemovalCounts.Sum(s => s.Item2);

            // Get the discrepant amount of genomes remaining to be removed or retained
            int removalDiff = Math.Abs(totalMarkedForRemoval - numGenomesToRemove);

            while (removalDiff > 0)
            {
                // Get the index of the of the tuple with the species that can either absorb an additional loss
                // or is the smallest non-empty species and can take an additional genome
                int specieIdx = (totalMarkedForRemoval < numGenomesToRemove)
                    ? specieRemovalCounts.FindIndex(s => s.Item1.GenomeList.Count >= 1)
                    : specieRemovalCounts.FindLastIndex(s => s.Item1.GenomeList.Count >= 1);

                // Mark another genome for removal or retention from the species at the identified index
                specieRemovalCounts[specieIdx] =
                    new Tuple<Specie<TGenome>, int>(specieRemovalCounts[specieIdx].Item1,
                        (totalMarkedForRemoval < numGenomesToRemove)
                            ? specieRemovalCounts[specieIdx].Item2 + 1
                            : specieRemovalCounts[specieIdx].Item2 - 1);

                removalDiff--;
            }

            foreach (Tuple<Specie<TGenome>, int> specieRemovalCount in specieRemovalCounts)
            {
                // Sort the specie members by age (oldest to youngest)
                IEnumerable<TGenome> ageSortedSpeciePopulation =
                    specieRemovalCount.Item1.GenomeList.OrderBy(g => g.BirthGeneration).AsParallel();

                // Add the pre-calculated number of genomes to the removal list
                genomesToRemove.AddRange(ageSortedSpeciePopulation.Take(specieRemovalCount.Item2));
            }

            // Remove the genomes marked for removal
            foreach (TGenome genome in genomesToRemove)
            {
                ((List<TGenome>) GenomeList).Remove(genome);
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
            // Open the loggers
            EvolutionLogger?.Open();
            PopulationLogger?.Open();

            // Update the run phase on the loggers
            EvolutionLogger?.UpdateRunPhase(RunPhase);
            PopulationLogger?.UpdateRunPhase(RunPhase);

            // Write out the headers for both loggers
            EvolutionLogger?.LogHeader(GetLoggableElements(_logFieldEnabledMap),
                Statistics.GetLoggableElements(_logFieldEnabledMap),
                GenomeEvaluator.GetLoggableElements(_logFieldEnabledMap),
                (GenomeList[0] as NeatGenome)?.GetLoggableElements(_logFieldEnabledMap));
            PopulationLogger?.LogHeader(new List<LoggableElement>
            {
                _logFieldEnabledMap.ContainsKey(PopulationGenomesFieldElements.Generation) &&
                _logFieldEnabledMap[PopulationGenomesFieldElements.Generation]
                    ? new LoggableElement(PopulationGenomesFieldElements.Generation, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(PopulationGenomesFieldElements.RunPhase) &&
                _logFieldEnabledMap[PopulationGenomesFieldElements.RunPhase]
                    ? new LoggableElement(PopulationGenomesFieldElements.RunPhase, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(PopulationGenomesFieldElements.GenomeId) &&
                _logFieldEnabledMap[PopulationGenomesFieldElements.GenomeId]
                    ? new LoggableElement(PopulationGenomesFieldElements.GenomeId, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(PopulationGenomesFieldElements.GenomeXml) &&
                _logFieldEnabledMap[PopulationGenomesFieldElements.GenomeXml]
                    ? new LoggableElement(PopulationGenomesFieldElements.GenomeXml, null)
                    : null
            });

            // Initialize the genome evalutor
            GenomeEvaluator.Initialize();

            // Speciate based on the total population size (note that this doesn't speciate the genomes
            // yet because we're just starting with a seed that likely doesn't satisfy the requirements
            // of establishing the target number of species)
            SpecieList = SpeciationStrategy.InitializeSpeciation(PopulationSize, EaParams.SpecieCount);

            // If the population has not yet undergone intialization evaluations, 
            // run them through a cycle of evaluations now and update the best genome
            // (again, speciation is not taken into account here because there's not yet
            // endough individuals in the population)
            if (GenomeList.Any(genome => genome.EvaluationInfo.EvaluationCount <= 0))
            {
                GenomeEvaluator.Evaluate(GenomeList, CurrentGeneration);
                UpdateBestGenomeWithoutSpeciation(false, false);
            }
        }

        /// <summary>
        ///     Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        public override void PerformOneGeneration()
        {
            bool useAuxFitness = false;

            // If the minimal criteria is dynamic and the MC update interval has been reached, 
            // then determine the new minimal criteria
            if (_isDynamicMinimalCriteria && CurrentGeneration%_mcUpdateInterval == 0)
            {
                // Update the minimal criteria and disposition each genome as viable or not
                // given the new minimal criteria
                GenomeEvaluator.Update(GenomeList as List<TGenome>);

                // Remove individuals from the queue who do not meet the updated minimal criteria
                (GenomeList as List<TGenome>)?.RemoveAll(genome => genome.EvaluationInfo.IsViable == false);
            }

            // Get the initial batch size as the minimum of the batch size or the size of the population.
            // When we're first starting, the population will likely be smaller than the desired batch size.
            int curBatchSize = Math.Min(_batchSize, GenomeList.Count);

            // Produce number of offspring equivalent to the given batch size
            List<TGenome> childGenomes = CreateOffspring(curBatchSize);

            // First evaluate the offspring batch with bridging disabled
            GenomeEvaluator.Evaluate(childGenomes, CurrentGeneration);

            // If no one met the stop condition, evaluate the batch with bridging
            if (_isBridgingEnabled && GenomeEvaluator.StopConditionSatisfied == false)
            {
                // Store off fitnesses calculated without bridging
                List<AuxFitnessInfo> fitnessesWithoutBridging = new List<AuxFitnessInfo>(childGenomes.Count);
                fitnessesWithoutBridging.AddRange(
                    childGenomes.Select(
                        childGenome => new AuxFitnessInfo("No Bridging Fitness", childGenome.EvaluationInfo.Fitness)));

                // Evaluate genomes with bridging enabled
                GenomeEvaluator.Evaluate(childGenomes, CurrentGeneration, enableBridging: true);

                // Load the "no bridging" fitness as auxiliary fitness values
                for (int cnt = 0; cnt < childGenomes.Count; cnt++)
                {
                    childGenomes[cnt].EvaluationInfo.AuxFitnessArr[0] = fitnessesWithoutBridging[cnt];
                }

                // If bridging was enabled, the primary fitness is now the fitness with bridging, which doesn't
                // really give an indication of the inherent capabilities of the genome.  Therefore, the champ
                // genome info needs to be set using the non-bridging fitness (which is now in the aux fitness)
                useAuxFitness = true;

                // TODO: If running bridging again, need to copy aux fitness back into main fitness property
            }

            // Remove child genomes that are not viable
            childGenomes.RemoveAll(genome => genome.EvaluationInfo.IsViable == false);

            // If the population cap has been exceeded, remove oldest genomes to keep population size constant
            if ((GenomeList.Count + childGenomes.Count) > PopulationSize)
            {
                // Calculate number of genomes to remove
                int genomesToRemove = (GenomeList.Count + childGenomes.Count) - PopulationSize;

                // Remove the above-computed number of oldest genomes from the population
                RemoveOldestSpecieGenomes(genomesToRemove);
            }

            // Add new children
            (GenomeList as List<TGenome>)?.AddRange(childGenomes);

            // Update the total offspring count based on the number of *viable* offspring produced
            Statistics._totalOffspringCount = (ulong) childGenomes.Count;

            // Don't speciate until the queue size is greater than the desired number of species
            if (GenomeList.Count > SpecieList.Count)
            {
                // Clear all the species and respeciate
                ClearAllSpecies();
                SpeciationStrategy.SpeciateGenomes(GenomeList, SpecieList);

                // Update the best genome within each species and the population
                // statistics (include specie statistics)
                UpdateBestGenome(false, useAuxFitness);
                UpdateStats(true, useAuxFitness);
            }
            else
            {
                // Update the global best genome (since population is not yet speciated)
                // and the population statistics (without specie statistics)
                UpdateBestGenomeWithoutSpeciation(false, useAuxFitness);
                UpdateStats(false, useAuxFitness);
            }

            Debug.Assert(GenomeList.Count <= PopulationSize);

            // If there is a logger defined, log the generation stats
            EvolutionLogger?.LogRow(GetLoggableElements(_logFieldEnabledMap),
                GenomeEvaluator.GetLoggableElements(_logFieldEnabledMap),
                Statistics.GetLoggableElements(_logFieldEnabledMap),
                (CurrentChampGenome as NeatGenome)?.GetLoggableElements(_logFieldEnabledMap));

            // Also, if we're at the appropriate batch interval, dump the population
            if (PopulationLoggingInterval != null &&
                (CurrentGeneration == 1 || CurrentGeneration%PopulationLoggingInterval == 0))
            {
                foreach (TGenome genome in GenomeList)
                {
                    PopulationLogger?.LogRow(new List<LoggableElement>
                    {
                        _logFieldEnabledMap.ContainsKey(PopulationGenomesFieldElements.Generation) &&
                        _logFieldEnabledMap[PopulationGenomesFieldElements.Generation]
                            ? new LoggableElement(PopulationGenomesFieldElements.Generation, CurrentGeneration)
                            : null,
                        _logFieldEnabledMap.ContainsKey(PopulationGenomesFieldElements.RunPhase) &&
                        _logFieldEnabledMap[PopulationGenomesFieldElements.RunPhase]
                            ? new LoggableElement(PopulationGenomesFieldElements.RunPhase, RunPhase)
                            : null,
                        _logFieldEnabledMap.ContainsKey(PopulationGenomesFieldElements.GenomeId) &&
                        _logFieldEnabledMap[PopulationGenomesFieldElements.GenomeId]
                            ? new LoggableElement(PopulationGenomesFieldElements.GenomeId, genome.Id)
                            : null,
                        _logFieldEnabledMap.ContainsKey(PopulationGenomesFieldElements.GenomeXml) &&
                        _logFieldEnabledMap[PopulationGenomesFieldElements.GenomeXml]
                            ? new LoggableElement(PopulationGenomesFieldElements.GenomeXml,
                                XmlIoUtils.GetGenomeXml(genome))
                            : null
                    });
                }
            }
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
        /// <param name="isBridgingEnabled">Flag that indicates whether bridging is enabled.</param>
        /// <param name="isDynamicMinimalCriteria">
        ///     Flag that indicates whether the minimal criteria is automatically/dynamically
        ///     determined.
        /// </param>
        /// <param name="mcUpdateInterval">
        ///     The number of batches/generations that are permitted to elapse between updates to the
        ///     minimal criteria.
        /// </param>
        public QueueingNeatEvolutionAlgorithm(IDataLogger logger = null, RunPhase runPhase = RunPhase.Primary,
            bool isBridgingEnabled = false, bool isDynamicMinimalCriteria = false, int mcUpdateInterval = 0)
            : this(
                new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric()),
                new NullComplexityRegulationStrategy(), 10, runPhase, isBridgingEnabled, isDynamicMinimalCriteria,
                logger)
        {
            SpeciationStrategy = new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric());
            ComplexityRegulationStrategy = new NullComplexityRegulationStrategy();
            _batchSize = 10;
            _mcUpdateInterval = mcUpdateInterval;
        }

        /// <summary>
        ///     Constructs steady state evolution algorithm with the given NEAT parameters and complexity regulation strategy.
        /// </summary>
        /// <param name="speciationStrategy">The speciation strategy.</param>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="batchSize">The batch size of offspring to produce, evaluate, and remove.</param>
        /// <param name="runPhase">
        ///     The experiment phase indicating whether this is an initialization process or the primary
        ///     algorithm.
        /// </param>
        /// <param name="isBridgingEnabled">Flag that indicates whether bridging is enabled.</param>
        /// <param name="isDynamicMinimalCriteria">
        ///     Flag that indicates whether the minimal criteria is automatically/dynamically
        ///     determined.
        /// </param>
        /// <param name="logger">The evolution data logger (optional).</param>
        /// <param name="logFieldEnabledMap">Dictionary of logging fields that can be dynamically enabled or disabled.</param>
        /// <param name="populationLogger">The population data logger.</param>
        /// <param name="populationLoggingInterval">The interval at which the population is logged.</param>
        /// <param name="mcUpdateInterval">
        ///     The number of batches/generations that are permitted to elapse between updates to the
        ///     minimal criteria.
        /// </param>
        public QueueingNeatEvolutionAlgorithm(
            ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize,
            RunPhase runPhase = RunPhase.Primary,
            bool isBridgingEnabled = false,
            bool isDynamicMinimalCriteria = false,
            IDataLogger logger = null, IDictionary<FieldElement, bool> logFieldEnabledMap = null,
            IDataLogger populationLogger = null,
            int? populationLoggingInterval = null,
            int mcUpdateInterval = 0)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _batchSize = batchSize;
            EvolutionLogger = logger;
            RunPhase = runPhase;
            _isBridgingEnabled = isBridgingEnabled;
            _isDynamicMinimalCriteria = isDynamicMinimalCriteria;
            _logFieldEnabledMap = logFieldEnabledMap;
            PopulationLogger = populationLogger;
            PopulationLoggingInterval = populationLoggingInterval;
            _mcUpdateInterval = mcUpdateInterval;
        }

        /// <summary>
        ///     Constructs steady state evolution algorithm with the given NEAT parameters and complexity regulation strategy.
        /// </summary>
        /// <param name="eaParams">The NEAT algorithm parameters.</param>
        /// <param name="speciationStrategy">The speciation strategy.</param>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="batchSize">The batch size of offspring to produce, evaluate, and remove.</param>
        /// <param name="runPhase">
        ///     The experiment phase indicating whether this is an initialization process or the primary
        ///     algorithm.
        /// </param>
        /// <param name="isBridgingEnabled">Flag that indicates whether bridging is enabled.</param>
        /// <param name="isDynamicMinimalCriteria">
        ///     Flag that indicates whether the minimal criteria is automatically/dynamically
        ///     determined.
        /// </param>
        /// <param name="logger">The evolution data logger (optional).</param>
        /// <param name="logFieldEnabledMap">Dictionary of logging fields that can be dynamically enabled or disabled.</param>
        /// <param name="populationLogger">The population data logger.</param>
        /// <param name="populationLoggingInterval">The interval at which the population is logged.</param>
        /// <param name="mcUpdateInterval">
        ///     The number of batches/generations that are permitted to elapse between updates to the
        ///     minimal criteria.
        /// </param>
        public QueueingNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
            ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize,
            RunPhase runPhase = RunPhase.Primary,
            bool isBridgingEnabled = false,
            bool isDynamicMinimalCriteria = false,
            IDataLogger logger = null, IDictionary<FieldElement, bool> logFieldEnabledMap = null,
            IDataLogger populationLogger = null,
            int? populationLoggingInterval = null,
            int mcUpdateInterval = 0) : base(eaParams)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _batchSize = batchSize;
            EvolutionLogger = logger;
            PopulationLogger = populationLogger;
            PopulationLoggingInterval = populationLoggingInterval;
            RunPhase = runPhase;
            _isBridgingEnabled = isBridgingEnabled;
            _isDynamicMinimalCriteria = isDynamicMinimalCriteria;
            _logFieldEnabledMap = logFieldEnabledMap;
            _mcUpdateInterval = mcUpdateInterval;
        }

        #endregion
    }
}