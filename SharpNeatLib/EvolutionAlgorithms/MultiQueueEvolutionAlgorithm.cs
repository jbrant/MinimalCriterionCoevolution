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
    /// <summary>
    ///     Implementation of a queue-based NEAT evolution algorithm, with each species having its own, logical queue
    ///     ("logical" because all genomes are ultimately part of the same global population).
    /// </summary>
    public class MultiQueueEvolutionAlgorithm<TGenome> : AbstractComplexifyingEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        #region Instance Fields

        /// <summary>
        ///     The number of genomes to generate, evaluate, and remove in a single "generation".
        /// </summary>
        private readonly int _batchSize;

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs multi-queue evolution algorithm with the given EA parameters.
        /// </summary>
        /// <param name="eaParams">The NEAT algorithm parameters.c</param>
        /// <param name="stats">The evolution algorithm statistics container.</param>
        /// <param name="speciationStrategy">The speciation strategy.</param>
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
        /// <param name="populationLoggingInterval">The interval at which the population is logged.</param>
        public MultiQueueEvolutionAlgorithm(EvolutionAlgorithmParameters eaParams, IEvolutionAlgorithmStats stats,
            ISpeciationStrategy<TGenome> speciationStrategy, IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize, RunPhase runPhase = RunPhase.Primary, IDataLogger evolutionLogger = null,
            IDictionary<FieldElement, bool> logFieldEnabledMap = null, IDataLogger populationLogger = null,
            IDataLogger genomeLogger = null, int? populationLoggingInterval = null) : base(eaParams, stats)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _batchSize = batchSize;
            EvolutionLogger = evolutionLogger;
            PopulationLogger = populationLogger;
            GenomeLogger = genomeLogger;
            PopulationLoggingInterval = populationLoggingInterval;
            RunPhase = runPhase;
            _logFieldEnabledMap = logFieldEnabledMap;
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Creates the specified number of offspring asexually using the desired offspring count as a gauge for the FIFO
        ///     parent selection (it's a one-to-one mapping).
        /// </summary>
        /// <param name="offspringCount">The number of offspring to produce.</param>
        /// <returns>The list of offspring.</returns>
        private List<TGenome> CreateOffspring(int offspringCount, Specie<TGenome> specie)
        {
            List<TGenome> offspringList = new List<TGenome>(offspringCount);

            // Get the parent genomes
            List<TGenome> parentList = specie.GenomeList.GetRange(0, offspringCount);

            // Remove the parents from the queue
            specie.GenomeList.RemoveRange(0, offspringCount);

            // Generate an offspring asexually for each parent genome (this is not done asexually 
            // because depending on the batch size, we may not be able to have genomes from the 
            // same species mate)
            offspringList.AddRange(parentList.Select(parentGenome => parentGenome.CreateOffspring(CurrentGeneration)));

            // Move the parents who replaced offspring to the back of the queue (list)
            specie.GenomeList.AddRange(parentList);

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
                if (specie.GenomeList.Count > EaParams.MaxSpecieSize)
                {
                    // Sort the specie population by age (oldest to youngest)
                    IEnumerable<TGenome> ageSortedPopulation =
                        specie.GenomeList.OrderBy(g => g.BirthGeneration).AsParallel();

                    // Select the specified number of oldest genomes
                    IEnumerable<TGenome> oldestGenomes =
                        ageSortedPopulation.Take(specie.GenomeList.Count - EaParams.MaxSpecieSize);

                    // Remove the oldest genomes from the specie/population
                    foreach (TGenome oldestGenome in oldestGenomes)
                    {
                        // Remove from the population
                        ((List<TGenome>) GenomeList).Remove(oldestGenome);

                        // Remove the genome reference from the specie
                        SpecieList[specie.Idx].GenomeList.Remove(oldestGenome);
                    }
                }
            }
        }

        #endregion

        #region Overridden methods

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

            // Update the run phase on the loggers
            EvolutionLogger?.UpdateRunPhase(RunPhase);
            PopulationLogger?.UpdateRunPhase(RunPhase);
            GenomeLogger?.UpdateRunPhase(RunPhase);

            // Write out the headers for all loggers
            EvolutionLogger?.LogHeader(GetLoggableElements(_logFieldEnabledMap),
                Statistics.GetLoggableElements(_logFieldEnabledMap),
                GenomeEvaluator.GetLoggableElements(_logFieldEnabledMap),
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

            // Initialize the genome evalutor
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
            SpecieList = SpeciationStrategy.InitializeSpeciation(GenomeList, EaParams.SpecieCount);
        }

        /// <summary>
        ///     Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        public override void PerformOneGeneration()
        {
            List<TGenome> childGenomes = new List<TGenome>(_batchSize*SpecieList.Count);

            // Iterate through each species and asexually reproduce offspring
            foreach (var specie in SpecieList)
            {
                // Get the batch size for this iteration, which will be lower-bounded by
                // the number of genomes in the current species
                int curSpecieBatchSize = Math.Min(_batchSize, specie.GenomeList.Count);

                // Produce number of offspring equivalent to the batch size
                childGenomes.AddRange(CreateOffspring(curSpecieBatchSize, specie));
            }

            // First evaluate the offspring batch with bridging disabled
            GenomeEvaluator.Evaluate(childGenomes, CurrentGeneration);

            // Remove offspring that are not viable
            childGenomes.RemoveAll(genome => genome.EvaluationInfo.IsViable == false);

            // Add offspring to the global population
            (GenomeList as List<TGenome>)?.AddRange(childGenomes);

            // Recompute centroids and assign child genomes to species with closest genetic 
            // similarity (but don't respeciate)
            SpeciationStrategy.SpeciateOffspring(childGenomes, SpecieList, false);

            // Perform per-species removals based on whether the cap for that species has been exceeded
            RemoveOldestFromOverfullSpecies();

            // Update the best genome within each species and the population statistics
            UpdateBestGenome(false);
            UpdateStats(true);

            // Update the total offspring count based on the number of *viable* offspring produced
            Statistics.TotalOffspringCount = (ulong) childGenomes.Count;

            // Ensure that the 
            Debug.Assert(GenomeList.Count <= PopulationSize);

            // If there is a logger defined, log the generation stats
            EvolutionLogger?.LogRow(GetLoggableElements(_logFieldEnabledMap),
                GenomeEvaluator.GetLoggableElements(_logFieldEnabledMap),
                Statistics.GetLoggableElements(_logFieldEnabledMap),
                (CurrentChampGenome as NeatGenome)?.GetLoggableElements(_logFieldEnabledMap));

            // Dump the extant population to file
            foreach (TGenome genome in GenomeList)
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

            // Dump genome definitions for entire population if this is the first batch
            if (CurrentGeneration == 1)
            {
                foreach (TGenome genome in GenomeList)
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
                foreach (TGenome childGenome in childGenomes)
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