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

        /// <summary>
        ///     Flag that indicates whether the species sizes should be capped (using the maximum specie size set in the EA
        ///     parameters).
        /// </summary>
        private readonly bool _isFixedSpecieSize;

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

        private void RemoveOldestFromOverfullSpecies(List<TGenome> offspring,
            IDictionary<Specie<TGenome>, int> closestSpecieAssignmentCount)
        {
            // Find and process each specie with excessive population
            foreach (var specieAssignmentCount in closestSpecieAssignmentCount)
            {
                // Handle species that have exceeded their cap
                if (specieAssignmentCount.Key.GenomeList.Count + specieAssignmentCount.Value > EaParams.MaxSpecieSize)
                {
                    // Compute the magnitude of cap exceedance
                    int sizeDiff = specieAssignmentCount.Key.GenomeList.Count + specieAssignmentCount.Value -
                                   EaParams.MaxSpecieSize;


                    // Handle the case where there's more offspring assigned into species than are currently in the species,
                    // and remove those genomes from the offspring list first
                    if (sizeDiff > specieAssignmentCount.Key.GenomeList.Count)
                    {
                        // TODO: Need to get the exact assignments of the affected offspring
                    }
                }
            }
        }

        /// <summary>
        ///     Removes the oldest from the species to which offspring have been assigned.  This is only invoked if the
        ///     addition of said offspring push the population size above the queue capacity.
        /// </summary>
        /// <param name="offspring">The offspring that have satisfied the criterion and been added to the queue.</param>
        private void RemoveOldestFromAssignedSpecies(List<TGenome> offspring)
        {
            // Get the number of offspring assigned to each affected specie
            IDictionary<Specie<TGenome>, int> specieAssignmentCountMap =
                SpeciationStrategy.FindClosestSpecieAssignments(offspring, SpecieList);

            // Roulette wheel is used below in the event that offspring coming into a species
            // exceed the current specie size (it probabalistically selects the largest species as an alternate)
            RouletteWheelLayout rwl = null;

            // Initialize removal map with the count of per-specie removals
            IDictionary<int, int> specieRemovalCountMap =
                specieAssignmentCountMap.ToDictionary(specieAssignmentCountPair => specieAssignmentCountPair.Key.Idx,
                    specieAssignmentCountPair => specieAssignmentCountPair.Value);

            // Validate that each species has the capacity to support removal of the specified
            // number of genomes (based on the number of offspring coming into it)
            foreach (int specieIdx in specieRemovalCountMap.Keys.ToList())
            {
                // Handle the case where there's more offspring coming into the species than there are
                // current genomes in that species
                if (SpecieList[specieIdx].GenomeList.Count < specieRemovalCountMap[specieIdx])
                {
                    // Setup the roulette wheel if this is the first time selecting alternate species
                    if (rwl == null)
                    {
                        // Only those species who are not already maxed out in terms of genome removals are candidates for additional removal
                        List<int> candidateSpecieIdxs = (from specie in SpecieList
                            where
                                specieRemovalCountMap.ContainsKey(specie.Idx) == false ||
                                specie.GenomeList.Count > specieRemovalCountMap[specie.Idx]
                            select specie.Idx).ToList();

                        double[] probabilities = new double[SpecieList.Count];

                        // Compute the sum of the mean specie ages in the original species
                        // (this will be used as the denominator in determing the specie roulette wheel probability)
                        double specieMeanAgeSum =
                            candidateSpecieIdxs.Sum(specie => SpecieList[specie].CalcMeanAge(CurrentGeneration));

                        // Compute the probability of specie selection based on mean age of genomes 
                        // within species divided by the sum of the specie mean ages
                        foreach (var specie in candidateSpecieIdxs)
                        {
                            probabilities[specie] = SpecieList[specie].CalcMeanAge(CurrentGeneration)/specieMeanAgeSum;
                        }

                        // Instiate the roulette wheel based on the age-based probabilities
                        rwl = new RouletteWheelLayout(probabilities);
                    }

                    // Calculate the genome shortage to support removal operation for the current species
                    int genomeShortage = specieRemovalCountMap[specieIdx] - SpecieList[specieIdx].GenomeList.Count;

                    // Attempt to find specie that has the requisite capacity to support each 
                    // of the remaining genome removals
                    for (int genomeCnt = 0; genomeCnt < genomeShortage; genomeCnt++)
                    {
                        // This holds the index of the specie that will incur the extra removal
                        int candidateSpecieIdx;

                        do
                        {
                            // Get the candidate species for additional genome removal
                            candidateSpecieIdx = RouletteWheel.SingleThrow(rwl, RandomNumGenerator);
                        } while (specieRemovalCountMap.ContainsKey(candidateSpecieIdx) &&
                                 specieRemovalCountMap[candidateSpecieIdx] >=
                                 SpecieList[candidateSpecieIdx].GenomeList.Count);
                        // Retry until we find a specie that can incur additional genome loss 
                        // (i.e. that is either not in the removal map currently OR is in the removal map 
                        // but does not have the full amount of its constituent genomes earmarked for removal)

                        // Add to species removal map or increment existing removal count for species
                        // for the selected species
                        if (specieRemovalCountMap.ContainsKey(candidateSpecieIdx))
                        {
                            specieRemovalCountMap[candidateSpecieIdx]++;
                        }
                        else
                        {
                            specieRemovalCountMap.Add(candidateSpecieIdx, 1);
                        }

                        // If the species has now hit capacity in terms of removals, remove it as a candidate for future additional removals
                        if (SpecieList[candidateSpecieIdx].GenomeList.Count <= specieRemovalCountMap[candidateSpecieIdx])
                        {
                            rwl = rwl.RemoveOutcome(candidateSpecieIdx);
                        }
                    }

                    // Decrement the amount to remove from the current species via the computed genome shortage
                    specieRemovalCountMap[specieIdx] -= genomeShortage;
                }
            }

            // Iterate through all affected species, sort by oldest members, 
            // and remove the determined amount
            foreach (int specieIdx in specieRemovalCountMap.Keys)
            {
                // Sort the population by age (oldest to youngest)
                IEnumerable<TGenome> ageSortedPopulation =
                    (SpecieList[specieIdx].GenomeList).OrderBy(g => g.BirthGeneration).AsParallel();

                // Select the specified number of oldest genomes
                IEnumerable<TGenome> oldestGenomes = ageSortedPopulation.Take(specieRemovalCountMap[specieIdx]);

                // Remove the oldest genomes from the specie/population
                foreach (TGenome oldestGenome in oldestGenomes)
                {
                    ((List<TGenome>) GenomeList).Remove(oldestGenome);
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
                    : null,
                _logFieldEnabledMap[PopulationGenomesFieldElements.SpecieId]
                    ? new LoggableElement(PopulationGenomesFieldElements.SpecieId, null)
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

            // Add new children
            (GenomeList as List<TGenome>)?.AddRange(childGenomes);

            // If we're using fixed specie sizes and the cap on one of the species has been exceeded, we need
            // to remove the oldest from the affected species to bring that species back to the cap
            if (_isFixedSpecieSize)
            {
                // Find the offspring specie assignments
                IDictionary<Specie<TGenome>, int> closestSpecieAssignments =
                    SpeciationStrategy.FindClosestSpecieAssignments(childGenomes, SpecieList);

                // Adjust size of affected specie
                if (SpeciationUtils<TGenome>.CheckSpecieSizeLimitExceeded(closestSpecieAssignments, SpecieList,
                    EaParams.MaxSpecieSize))
                {
                    RemoveOldestFromOverfullSpecies(childGenomes, closestSpecieAssignments);
                }
            }
            // If the population cap has been exceeded, remove oldest genomes to keep population size constant
            else if (GenomeList.Count > PopulationSize)
            {
                // Remove the above-computed number of oldest genomes from the population
                RemoveOldestFromAssignedSpecies(childGenomes);
            }

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
                            : null,
                        _logFieldEnabledMap[PopulationGenomesFieldElements.SpecieId]
                            ? new LoggableElement(PopulationGenomesFieldElements.SpecieId,
                                genome.SpecieIdx)
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
        /// <param name="isFixedSpecieSize">Flag that indicates whether the species sizes should be capped.</param>
        /// <param name="mcUpdateInterval">
        ///     The number of batches/generations that are permitted to elapse between updates to the
        ///     minimal criteria.
        /// </param>
        public QueueingNeatEvolutionAlgorithm(IDataLogger logger = null, RunPhase runPhase = RunPhase.Primary,
            bool isBridgingEnabled = false, bool isDynamicMinimalCriteria = false, bool isFixedSpecieSize = false,
            int mcUpdateInterval = 0)
            : this(
                new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric()),
                new NullComplexityRegulationStrategy(), 10, runPhase, isBridgingEnabled, isDynamicMinimalCriteria,
                logger)
        {
            SpeciationStrategy = new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric());
            ComplexityRegulationStrategy = new NullComplexityRegulationStrategy();
            _batchSize = 10;
            _mcUpdateInterval = mcUpdateInterval;
            _isFixedSpecieSize = isFixedSpecieSize;
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
        /// <param name="isFixedSpecieSize">Flag that indicates whether the species sizes should be capped.</param>
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
            bool isFixedSpecieSize = false,
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
            _isFixedSpecieSize = isFixedSpecieSize;
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
        /// <param name="isFixedSpecieSize">Flag that indicates whether the species sizes should be capped.</param>
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
            bool isFixedSpecieSize = false,
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
            _isFixedSpecieSize = isFixedSpecieSize;
        }

        #endregion
    }
}