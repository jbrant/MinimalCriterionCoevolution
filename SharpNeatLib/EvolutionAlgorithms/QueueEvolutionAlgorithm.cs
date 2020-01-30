#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.EvolutionAlgorithms.Statistics;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.EvolutionAlgorithms
{
    /// <inheritdoc />
    /// <summary>
    ///     Implementation of a queue-based NEAT evolution algorithm, with each species having its own, logical queue
    ///     ("logical" because all genomes are ultimately part of the same global population).
    /// </summary>
    public class QueueEvolutionAlgorithm<TGenome> : AbstractComplexifyingEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        #region Instance Fields

        /// <summary>
        ///     The number of genomes to generate, evaluate, and remove in a single "generation".
        /// </summary>
        private readonly int _batchSize;

        #endregion

        #region Constructors

        /// <inheritdoc />
        /// <summary>
        ///     QueueEvolutionAlgorithm constructor.
        /// </summary>
        /// <param name="eaParams">The NEAT algorithm parameters.c</param>
        /// <param name="stats">The evolution algorithm statistics container.</param>
        /// <param name="speciationStrategy">The speciation strategy.</param>
        /// <param name="respeciateInterval">The batch interval at which the population should be respeciated.</param>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="batchSize">The batch size of offspring to produce, evaluate, and remove.</param>
        /// <param name="runPhase">
        ///     The experiment phase indicating whether this is an initialization process or the primary
        ///     algorithm.
        /// </param>
        /// <param name="evolutionLogger">The evolution data logger (optional).</param>
        /// <param name="logFieldEnabledMap">Dictionary of logging fields that can be dynamically enabled or disabled.</param>
        /// <param name="populationLogger">The population data logger (optional).</param>
        /// <param name="simulationTrialLogger">The simulation trial data logger (optional).</param>
        /// <param name="genomeLogger">The genome data logger (optional).</param>
        public QueueEvolutionAlgorithm(EvolutionAlgorithmParameters eaParams, IEvolutionAlgorithmStats stats,
            ISpeciationStrategy<TGenome> speciationStrategy, int respeciateInterval,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize, RunPhase runPhase = RunPhase.Primary, IDataLogger evolutionLogger = null,
            IDictionary<FieldElement, bool> logFieldEnabledMap = null, IDataLogger populationLogger = null,
            IDataLogger genomeLogger = null, IDataLogger simulationTrialLogger = null) : base(eaParams, stats)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _batchSize = batchSize;
            EvolutionLogger = evolutionLogger;
            PopulationLogger = populationLogger;
            GenomeLogger = genomeLogger;
            SimulationTrialLogger = simulationTrialLogger;
            RunPhase = runPhase;
            _logFieldEnabledMap = logFieldEnabledMap;
        }

        /// <inheritdoc />
        /// <summary>
        ///     QueueEvolutionAlgorithm constructor omitting speciation strategy.
        /// </summary>
        /// <param name="eaParams">The NEAT algorithm parameters.c</param>
        /// <param name="stats">The evolution algorithm statistics container.</param>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="batchSize">The batch size of offspring to produce, evaluate, and remove.</param>
        /// <param name="runPhase">
        ///     The experiment phase indicating whether this is an initialization process or the primary
        ///     algorithm.
        /// </param>
        /// <param name="evolutionLogger">The evolution data logger (optional).</param>
        /// <param name="logFieldEnabledMap">Dictionary of logging fields that can be dynamically enabled or disabled.</param>
        /// <param name="populationLogger">The population data logger (optional).</param>
        /// <param name="genomeLogger">The genome data logger (optional).</param>
        public QueueEvolutionAlgorithm(EvolutionAlgorithmParameters eaParams, IEvolutionAlgorithmStats stats,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize, RunPhase runPhase = RunPhase.Primary, IDataLogger evolutionLogger = null,
            IDictionary<FieldElement, bool> logFieldEnabledMap = null, IDataLogger populationLogger = null,
            IDataLogger genomeLogger = null, IDataLogger simulationTrialLogger = null) : this(eaParams, stats, null, 0,
            complexityRegulationStrategy, batchSize, runPhase, evolutionLogger, logFieldEnabledMap, populationLogger,
            genomeLogger, simulationTrialLogger)
        {
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Creates the specified number of offspring by selecting the equivalent number of parents from the given species and
        ///     reproducing.
        /// </summary>
        /// <param name="offspringCount">The number of offspring to produce.</param>
        /// <param name="species">The species from which the parents are selected.</param>
        /// <returns>The list of offspring.</returns>
        private IEnumerable<TGenome> CreateOffspring(int offspringCount, Specie<TGenome> species)
        {
            var offspringList = new List<TGenome>(offspringCount);

            // Get the parent genomes
            var parentList = species.GenomeList.GetRange(0, offspringCount);

            // Remove the parents from the queue
            species.GenomeList.RemoveRange(0, offspringCount);

            // Generate an offspring asexually for each parent genome (this is not done asexually 
            // because depending on the batch size, we may not be able to have genomes from the 
            // same species mate)
            offspringList.AddRange(parentList.Select(parentGenome => parentGenome.CreateOffspring(CurrentGeneration)));

            // Move the parents who replaced offspring to the back of the queue (list)
            species.GenomeList.AddRange(parentList);

            return offspringList;
        }

        /// <summary>
        ///     Creates the specified number of offspring by selecting the equivalent number of parents from the population queue
        ///     and reproducing.
        /// </summary>
        /// <param name="offspringCount">The number of offspring to produce.</param>
        /// <returns>The list of offspring.</returns>
        private List<TGenome> CreateOffspring(int offspringCount)
        {
            var offspringList = new List<TGenome>(offspringCount);

            // Get the parent genomes
            var parentList = ((List<TGenome>) GenomeList).GetRange(0, offspringCount);

            // Remove the parents from the queue
            ((List<TGenome>) GenomeList).RemoveRange(0, offspringCount);

            // Generate the specified number of offspring
            offspringList.AddRange(parentList.Select(parentGenome => parentGenome.CreateOffspring(CurrentGeneration)));

            // Move the parents who replaced offspring to the back of the queue (list)
            ((List<TGenome>) GenomeList).AddRange(parentList);

            return offspringList;
        }

        /// <summary>
        ///     Removes the oldest genomes from species that have exceeded their size cap.
        /// </summary>
        private void RemoveOldestFromOverfullSpecies()
        {
            // Iterate over each species and remove the oldest genomes from any that have exceeded their cap
            foreach (var specie in SpecieList.ToList())
            {
                if (specie.GenomeList.Count <= EaParams.MaxSpecieSize) continue;

                // Sort the specie population by age (oldest to youngest)
                IEnumerable<TGenome> ageSortedPopulation =
                    specie.GenomeList.OrderBy(g => g.BirthGeneration).AsParallel();

                // Select the specified number of oldest genomes
                var oldestGenomes = ageSortedPopulation.Take(specie.GenomeList.Count - EaParams.MaxSpecieSize);

                // Remove the oldest genomes from the specie/population
                foreach (var oldestGenome in oldestGenomes)
                {
                    // Remove from the population
                    ((List<TGenome>) GenomeList).Remove(oldestGenome);

                    // Remove the genome reference from the specie
                    SpecieList[specie.Idx].GenomeList.Remove(oldestGenome);
                }
            }
        }

        /// <summary>
        ///     Removes the specified number of oldest genomes from the population queue.
        /// </summary>
        /// <param name="numGenomesToRemove">The number of oldest genomes to remove from the population.</param>
        private void RemoveGlobalOldestGenomes(int numGenomesToRemove)
        {
            // Sort the population by age (oldest to youngest)
            IEnumerable<TGenome> ageSortedPopulation =
                ((List<TGenome>) GenomeList).OrderBy(g => g.BirthGeneration).AsParallel();

            // Select the specified number of oldest genomes
            var oldestGenomes = ageSortedPopulation.Take(numGenomesToRemove);

            // Remove the oldest genomes from the population
            foreach (var oldestGenome in oldestGenomes)
            {
                ((List<TGenome>) GenomeList).Remove(oldestGenome);
            }
        }

        #endregion

        #region Overridden methods

        /// <inheritdoc />
        /// <summary>
        ///     Overrides the base initialization method, with the primary difference being that the initial population is expected
        ///     to have already undergone evaluation (via a bootstrapping process).
        /// </summary>
        protected override void Initialize()
        {
            // Open the loggers
            EvolutionLogger?.Open();
            PopulationLogger?.Open();
            GenomeLogger?.Open();
            SimulationTrialLogger?.Open();

            // Update the run phase on the loggers
            EvolutionLogger?.UpdateRunPhase(RunPhase);
            PopulationLogger?.UpdateRunPhase(RunPhase);
            GenomeLogger?.UpdateRunPhase(RunPhase);
            SimulationTrialLogger?.UpdateRunPhase(RunPhase);

            // Write out the headers for all loggers
            EvolutionLogger?.LogHeader(GetLoggableElements(_logFieldEnabledMap),
                Statistics.GetLoggableElements(_logFieldEnabledMap),
                (GenomeList[0] as NeatGenome)?.GetLoggableElements(_logFieldEnabledMap));
            PopulationLogger?.LogHeader(new List<LoggableElement>
            {
                _logFieldEnabledMap.ContainsKey(PopulationFieldElements.RunPhase) &&
                _logFieldEnabledMap[PopulationFieldElements.RunPhase]
                    ? new LoggableElement(PopulationFieldElements.RunPhase, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(PopulationFieldElements.Generation) &&
                _logFieldEnabledMap[PopulationFieldElements.Generation]
                    ? new LoggableElement(PopulationFieldElements.Generation, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(PopulationFieldElements.GenomeId) &&
                _logFieldEnabledMap[PopulationFieldElements.GenomeId]
                    ? new LoggableElement(PopulationFieldElements.GenomeId, null)
                    : null,
                _logFieldEnabledMap[PopulationFieldElements.SpecieId]
                    ? new LoggableElement(PopulationFieldElements.SpecieId, null)
                    : null
            });
            SimulationTrialLogger?.LogHeader(new List<LoggableElement>
            {
                _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.Generation) &&
                _logFieldEnabledMap[SimulationTrialFieldElements.Generation]
                    ? new LoggableElement(SimulationTrialFieldElements.Generation, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.GenomeId) &&
                _logFieldEnabledMap[SimulationTrialFieldElements.GenomeId]
                    ? new LoggableElement(SimulationTrialFieldElements.GenomeId, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.IsSuccessful) &&
                _logFieldEnabledMap[SimulationTrialFieldElements.IsSuccessful]
                    ? new LoggableElement(SimulationTrialFieldElements.IsSuccessful, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.NumTimesteps) &&
                _logFieldEnabledMap[SimulationTrialFieldElements.NumTimesteps]
                    ? new LoggableElement(SimulationTrialFieldElements.NumTimesteps, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.Distance) &&
                _logFieldEnabledMap[SimulationTrialFieldElements.Distance]
                    ? new LoggableElement(SimulationTrialFieldElements.Distance, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.PairedGenomeId) &&
                _logFieldEnabledMap[SimulationTrialFieldElements.PairedGenomeId]
                    ? new LoggableElement(SimulationTrialFieldElements.PairedGenomeId, null)
                    : null
            });
            GenomeLogger?.LogHeader(new List<LoggableElement>
            {
                _logFieldEnabledMap.ContainsKey(GenomeFieldElements.RunPhase) &&
                _logFieldEnabledMap[GenomeFieldElements.RunPhase]
                    ? new LoggableElement(GenomeFieldElements.RunPhase, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(GenomeFieldElements.GenomeId) &&
                _logFieldEnabledMap[GenomeFieldElements.GenomeId]
                    ? new LoggableElement(GenomeFieldElements.GenomeId, null)
                    : null,
                _logFieldEnabledMap.ContainsKey(GenomeFieldElements.GenomeXml) &&
                _logFieldEnabledMap[GenomeFieldElements.GenomeXml]
                    ? new LoggableElement(GenomeFieldElements.GenomeXml, null)
                    : null
            });

            // Initialize the genome evaluator
            GenomeEvaluator.Initialize();

            // Ensure that all genomes have been evaluated
            foreach (var genome in GenomeList)
            {
                Debug.Assert(genome.EvaluationInfo.IsEvaluated,
                    "All seed genomes must have been evaluated by bootstrap process.");
            }

            // Speciate the initial population.  Note that speciation during initialization 
            // will be the first and only iteration of speciation executed.  Each species is henceforth 
            // considered to have its own queue (even though the global population itself is physically 
            // stored in the same data structure).
            if (EaParams.SpecieCount > 0)
                SpecieList = SpeciationStrategy.InitializeSpeciation(GenomeList, EaParams.SpecieCount);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        public override void PerformOneGeneration()
        {
            List<TGenome> childGenomes;

            if (EaParams.SpecieCount > 0)
            {
                childGenomes = new List<TGenome>(_batchSize);

                // Iterate through each species and asexually reproduce offspring
                foreach (var specie in SpecieList)
                {
                    // Get the batch size for this iteration, which will be lower-bounded by
                    // the number of genomes in the current species
                    var curSpecieBatchSize = Math.Min(_batchSize, specie.GenomeList.Count);

                    // Produce number of offspring equivalent to the batch size
                    childGenomes.AddRange(CreateOffspring(curSpecieBatchSize, specie));
                }
            }
            else
            {
                var curBatchSize = Math.Min(_batchSize, GenomeList.Count);

                // Produce number of offspring equivalent to the given batch size
                childGenomes = CreateOffspring(curBatchSize);
            }

            // Evaluate child genomes
            GenomeEvaluator.Evaluate(childGenomes, CurrentGeneration);

            // Remove offspring that are not viable
            childGenomes.RemoveAll(genome => genome.EvaluationInfo.IsViable == false);

            // Add offspring to the global population
            (GenomeList as List<TGenome>)?.AddRange(childGenomes);

            if (EaParams.SpecieCount > 0)
            {
                // Recompute centroids and assign child genomes to species with closest genetic 
                // similarity (but don't respeciate)
                SpeciationStrategy.SpeciateOffspring(childGenomes, SpecieList, false);

                // Respeciate after elapsed number of batches
                if (CurrentGeneration % 20 == 0)
                {
                    ClearAllSpecies();
                    SpeciationStrategy.SpeciateGenomes(GenomeList, SpecieList);
                }

                // Perform per-species removals based on whether the cap for that species has been exceeded
                RemoveOldestFromOverfullSpecies();

                // Update the best genome within each species and the population statistics
                UpdateBestGenome(false);
                UpdateStats(true);
            }
            else
            {
                // Calculate number of genomes to remove
                var genomesToRemove = GenomeList.Count - PopulationSize;

                // Remove the above-computed number of oldest genomes from the population
                RemoveGlobalOldestGenomes(genomesToRemove);

                // Update the global best genome and the population statistics
                UpdateBestGenomeWithoutSpeciation(false, false);
                UpdateStats(false);
            }

            // Update the total offspring count based on the number of *viable* offspring produced
            Statistics.TotalOffspringCount = (ulong) childGenomes.Count;

            // Ensure that the population remains within the given size constraints
            Debug.Assert(GenomeList.Count <= PopulationSize);

            // If there is a logger defined, log the generation stats
            EvolutionLogger?.LogRow(GetLoggableElements(_logFieldEnabledMap),
                Statistics.GetLoggableElements(_logFieldEnabledMap),
                (CurrentChampGenome as NeatGenome)?.GetLoggableElements(_logFieldEnabledMap));

            // Dump the extant population to file
            foreach (var genome in GenomeList)
            {
                PopulationLogger?.LogRow(new List<LoggableElement>
                {
                    _logFieldEnabledMap.ContainsKey(PopulationFieldElements.RunPhase) &&
                    _logFieldEnabledMap[PopulationFieldElements.RunPhase]
                        ? new LoggableElement(PopulationFieldElements.RunPhase, RunPhase)
                        : null,
                    _logFieldEnabledMap.ContainsKey(PopulationFieldElements.Generation) &&
                    _logFieldEnabledMap[PopulationFieldElements.Generation]
                        ? new LoggableElement(PopulationFieldElements.Generation, CurrentGeneration)
                        : null,
                    _logFieldEnabledMap.ContainsKey(PopulationFieldElements.GenomeId) &&
                    _logFieldEnabledMap[PopulationFieldElements.GenomeId]
                        ? new LoggableElement(PopulationFieldElements.GenomeId, genome.Id)
                        : null,
                    _logFieldEnabledMap[PopulationFieldElements.SpecieId]
                        ? new LoggableElement(PopulationFieldElements.SpecieId,
                            genome.SpecieIdx)
                        : null
                });
            }

            // Log simulation trial results for child genomes
            foreach (var genome in childGenomes)
            {
                foreach (var trial in genome.EvaluationInfo.TrialData)
                {
                    SimulationTrialLogger?.LogRow(new List<LoggableElement>
                    {
                        _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.Generation) &&
                        _logFieldEnabledMap[SimulationTrialFieldElements.Generation]
                            ? new LoggableElement(SimulationTrialFieldElements.Generation, CurrentGeneration)
                            : null,
                        _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.GenomeId) &&
                        _logFieldEnabledMap[SimulationTrialFieldElements.GenomeId]
                            ? new LoggableElement(SimulationTrialFieldElements.GenomeId, genome.Id)
                            : null,
                        _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.IsSuccessful) &&
                        _logFieldEnabledMap[SimulationTrialFieldElements.IsSuccessful]
                            ? new LoggableElement(SimulationTrialFieldElements.IsSuccessful, trial.IsSuccessful)
                            : null,
                        _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.NumTimesteps) &&
                        _logFieldEnabledMap[SimulationTrialFieldElements.NumTimesteps]
                            ? new LoggableElement(SimulationTrialFieldElements.NumTimesteps, trial.NumTimesteps)
                            : null,
                        _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.Distance) &&
                        _logFieldEnabledMap[SimulationTrialFieldElements.Distance]
                            ? new LoggableElement(SimulationTrialFieldElements.Distance,
                                trial.ObjectiveDistance)
                            : null,
                        _logFieldEnabledMap.ContainsKey(SimulationTrialFieldElements.PairedGenomeId) &&
                        _logFieldEnabledMap[SimulationTrialFieldElements.PairedGenomeId]
                            ? new LoggableElement(SimulationTrialFieldElements.PairedGenomeId, trial.PairedGenomeId)
                            : null
                    });
                }
            }

            // Dump genome definitions for entire population if this is the first batch
            if (CurrentGeneration == 1)
            {
                foreach (var genome in GenomeList)
                {
                    GenomeLogger?.LogRow(new List<LoggableElement>
                    {
                        _logFieldEnabledMap.ContainsKey(GenomeFieldElements.RunPhase) &&
                        _logFieldEnabledMap[GenomeFieldElements.RunPhase]
                            ? new LoggableElement(GenomeFieldElements.RunPhase, RunPhase)
                            : null,
                        _logFieldEnabledMap.ContainsKey(GenomeFieldElements.GenomeId) &&
                        _logFieldEnabledMap[GenomeFieldElements.GenomeId]
                            ? new LoggableElement(GenomeFieldElements.GenomeId, genome.Id)
                            : null,
                        _logFieldEnabledMap.ContainsKey(GenomeFieldElements.GenomeXml) &&
                        _logFieldEnabledMap[GenomeFieldElements.GenomeXml]
                            ? new LoggableElement(GenomeFieldElements.GenomeXml, XmlIoUtils.GetGenomeXml(genome))
                            : null
                    });
                }
            }
            // Otherwise, just dump new child genome definitions to file
            else
            {
                foreach (var childGenome in childGenomes)
                {
                    GenomeLogger?.LogRow(new List<LoggableElement>
                    {
                        _logFieldEnabledMap.ContainsKey(GenomeFieldElements.RunPhase) &&
                        _logFieldEnabledMap[GenomeFieldElements.RunPhase]
                            ? new LoggableElement(GenomeFieldElements.RunPhase, RunPhase)
                            : null,
                        _logFieldEnabledMap.ContainsKey(GenomeFieldElements.GenomeId) &&
                        _logFieldEnabledMap[GenomeFieldElements.GenomeId]
                            ? new LoggableElement(GenomeFieldElements.GenomeId, childGenome.Id)
                            : null,
                        _logFieldEnabledMap.ContainsKey(GenomeFieldElements.GenomeXml) &&
                        _logFieldEnabledMap[GenomeFieldElements.GenomeXml]
                            ? new LoggableElement(GenomeFieldElements.GenomeXml, XmlIoUtils.GetGenomeXml(childGenome))
                            : null
                    });
                }
            }
        }

        #endregion
    }
}