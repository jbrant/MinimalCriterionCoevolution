#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.SpeciationStrategies;

#endregion

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    ///     Implementation of a steady state NEAT evolution algorithm.
    ///     Incorporates:
    ///     - Speciation with fitness sharing.
    ///     - Creating offspring via both sexual and asexual reproduction.
    /// </summary>
    /// <typeparam name="TGenome">The genome type that the algorithm will operate on.</typeparam>
    public class QueueingNeatEvolutionAlgorithm<TGenome> : AbstractNeatEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        #region Overridden Methods

        /// <summary>
        ///     Intercepts the call to initialize, calls the base intializer first to generate an initial population, then ensures
        ///     that all individuals in the initial population satisfy the minimal criteria.
        /// </summary>
        protected override void Initialize()
        {
            /*
            // Remove all genomes from the initial population that do not satisfy the minimal criteria
            ((List<TGenome>) GenomeList).RemoveAll(genome => genome.EvaluationInfo.IsViable == false);

            // If there were genomes removed that did not satsify the minimal criteria, we need to generate
            // new genomes (all of which are required to meet the minimal criteria) to fill out the specified 
            // population size
            if (GenomeList.Count < PopulationSize)
            {
                // Compute the number of additional genomes that need to be generated
                int numGenomesToGenerate = PopulationSize - GenomeList.Count;

                // Continue the loop until all necessary genomes (that all meet the minimal criteria) have been 
                // generated and added to the population
                do
                {
                    // Produce the necessary number of genomes to fill in the remaining population gap
                    List<TGenome> newGenomes = GenomeFactory.CreateGenomeList(numGenomesToGenerate, 0);

                    // Evaluate the genomes to determine which meet the minimal criteria
                    GenomeEvaluator.Evaluate(newGenomes, CurrentGeneration);

                    // Remove the newly created genomes that do not meet the minimal criteria
                    newGenomes.RemoveAll(genome => genome.EvaluationInfo.IsViable == false);

                    // Add the new genomes that do satisfy the minimal criteria to the population
                    ((List<TGenome>) GenomeList).AddRange(newGenomes);

                    // Update the count of additional genomes that need to be created
                    numGenomesToGenerate = PopulationSize - GenomeList.Count;
                } while (numGenomesToGenerate > 0);
            }*/
        }

        /// <summary>
        ///     Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        protected override void PerformOneGeneration()
        {
            // Get the initial batch size as the minimum of the batch size or the size of the population.
            // When we're first starting, the population will likely be smaller than the desired batch size.
            int curBatchSize = Math.Min(_batchSize, GenomeList.Count);
            List<TGenome> childGenomes = new List<TGenome>(_batchSize);

            // Produce number of offspring equivalent to the given batch size
            childGenomes = CreateOffspring(curBatchSize);

            // Evaluate the offspring batch
            GenomeEvaluator.Evaluate(childGenomes, CurrentGeneration);

            // Remove child genomes that are not viable
            childGenomes.RemoveAll(genome => genome.EvaluationInfo.IsViable == false);

            // TODO: Remove oldest (only if we've reached the target population size AND viable genomes have been added)
            if ((GenomeList.Count + childGenomes.Count) > PopulationSize)
            {
                // Calculate number of genomes to remove
                int genomesToRemove = (GenomeList.Count + childGenomes.Count) - PopulationSize;

                // Remove the calculated number of oldest genomes
                (GenomeList as List<TGenome>)?.RemoveRange(0, genomesToRemove);
            }

            // Add new children
            (GenomeList as List<TGenome>)?.AddRange(childGenomes);

            // Update stats and store reference to best genome.
            UpdateBestGenomeWithoutSpeciation(false);
            UpdateStats(false);

            Debug.Assert(GenomeList.Count <= PopulationSize);

            // If there is a logger defined, log the generation stats
            EvolutionLogger?.LogRow(GetLoggableElements(), Statistics.GetLoggableElements(),
                (CurrentChampGenome as NeatGenome)?.GetLoggableElements());
        }

        #endregion

        #region Instance Fields

        /// <summary>
        ///     The number of genomes to generate, evaluate, and remove in a single "generation".
        /// </summary>
        private readonly int _batchSize;

        /// <summary>
        ///     The number of generations after to which to re-evaluate the entire population.
        /// </summary>
        private readonly int _populationEvaluationFrequency;

        /// <summary>
        ///     The queue of genomes that's used to bootstrap the global genome list.
        /// </summary>
        private readonly Queue<TGenome> _genomeQueue;

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs steady state evolution algorithm with the default clustering strategy (k-means clustering) using
        ///     manhattan distance and null complexity regulation strategy.
        /// </summary>
        /// <param name="logger">The data logger (optional).</param>
        public QueueingNeatEvolutionAlgorithm(IDataLogger logger = null)
            : this(
                new NullComplexityRegulationStrategy(), 10, logger)
        {
            SpeciationStrategy = new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric());
            ComplexityRegulationStrategy = new NullComplexityRegulationStrategy();
            _batchSize = 10;
            _populationEvaluationFrequency = 100;
        }

        /// <summary>
        ///     Constructs steady state evolution algorithm with the given NEAT parameters and complexity regulation strategy.
        /// </summary>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="batchSize">The batch size of offspring to produce, evaluate, and remove.</param>
        /// <param name="logger">The data logger (optional).</param>
        public QueueingNeatEvolutionAlgorithm(
            IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize,
            IDataLogger logger = null)
        {
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _batchSize = batchSize;
            EvolutionLogger = logger;
        }

        /// <summary>
        ///     Constructs steady state evolution algorithm with the given NEAT parameters and complexity regulation strategy.
        /// </summary>
        /// <param name="eaParams">The NEAT algorithm parameters.</param>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="batchSize">The batch size of offspring to produce, evaluate, and remove.</param>
        /// <param name="logger">The data logger (optional).</param>
        public QueueingNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize,
            IDataLogger logger = null) : base(eaParams)
        {
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _batchSize = batchSize;
            EvolutionLogger = logger;
        }

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

            // Generate an offspring asexually for each parent genome (this is not done asexually because depending on the batch size, 
            // we may not be able to have genomes from the same species mate)
            offspringList.AddRange(parentList.Select(parentGenome => parentGenome.CreateOffspring(CurrentGeneration)));

            return offspringList;
        }

        /// <summary>
        ///     Determines whether or not a species is viable for sexual reproduction (i.e. crossover) based on the number of
        ///     genomes in the species as well as the fitness of its constituent genomes.
        /// </summary>
        /// <param name="candidateSpecie">The species being considered for sexual reproduction.</param>
        /// <returns>Whether or not the given species is viable for sexual reproduction (i.e. crossover).</returns>
        private bool IsSpecieViableForSexualReproduction(Specie<TGenome> candidateSpecie)
        {
            bool speciesReproductivelyViable = true;

            // If there is only one genome in the species, the species is automatically not viable
            // because there is no one with whom to mate
            if (candidateSpecie.GenomeList.Count <= 1)
            {
                speciesReproductivelyViable = false;
            }
            else
            {
                int nonZeroFitnessCnt = 0;

                // Iterate through the genomes in the species, making sure that 2 or more have non-zero fitness (this 
                // is because the roullette will selection will cause an endless loop if not)
                foreach (TGenome genome in GenomeList.Where(genome => genome.EvaluationInfo.Fitness > 0))
                {
                    nonZeroFitnessCnt++;

                    // If we've already found more than one genome that has non-zero fitness, we're good
                    if (nonZeroFitnessCnt > 1)
                    {
                        break;
                    }
                }

                // If there was one or fewer genomes found with non-zero fitness, this species is not viable for crossover
                if (nonZeroFitnessCnt <= 1)
                {
                    speciesReproductivelyViable = false;
                }
            }

            return speciesReproductivelyViable;
        }

        /// <summary>
        ///     Selects genomes to remove based on their adjusted fitness.
        /// </summary>
        /// <param name="numGenomesToRemove">The number of genomes to remove from the existing population.</param>
        /// <returns>The list of genomes selected for removal.</returns>
        private List<TGenome> SelectGenomesForRemoval(int numGenomesToRemove)
        {
            List<TGenome> genomesToRemove = new List<TGenome>(numGenomesToRemove);
            Dictionary<TGenome, double> removalCandidatesMap = new Dictionary<TGenome, double>();

            // Iterate through each genome from each species and calculate its adjusted fitness relative to others in that species
            foreach (var specie in SpecieList)
            {
                for (int genomeIdx = 0; genomeIdx < specie.GenomeList.Count; genomeIdx++)
                {
                    // Add adjusted fitness and the genome reference to the map (dictionary)
                    removalCandidatesMap.Add(specie.GenomeList[genomeIdx],
                        specie.CalcGenomeAdjustedFitness(genomeIdx));
                }
            }

            // Build a stack in ascending order of fitness (that is, lower fitness genomes will be popped first)
            var removalCandidatesStack =
                new Stack<KeyValuePair<TGenome, double>>(removalCandidatesMap.OrderByDescending(i => i.Value));

            // Iterate up to the number to remove, incrementally building the genomes to remove list
            for (int curRemoveIdx = 0; curRemoveIdx < numGenomesToRemove; curRemoveIdx++)
            {
                // Add genome to remove                
                genomesToRemove.Add(removalCandidatesStack.Pop().Key);
            }

            return genomesToRemove;
        }

        #endregion
    }
}