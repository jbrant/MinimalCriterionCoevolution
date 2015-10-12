#region

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.SpeciationStrategies;
using SharpNeat.Utility;

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
        /// Intercepts the call to initialize, calls the base intializer first to generate an initial population, then ensures that all individuals in the initial population satisfy the minimal criteria.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            
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
                    GenomeEvaluator.Evaluate(newGenomes);

                    // Remove the newly created genomes that do not meet the minimal criteria
                    newGenomes.RemoveAll(genome => genome.EvaluationInfo.IsViable == false);

                    // Add the new genomes that do satisfy the minimal criteria to the population
                    ((List<TGenome>)GenomeList).AddRange(newGenomes);

                    // Update the count of additional genomes that need to be created
                    numGenomesToGenerate = PopulationSize - GenomeList.Count;

                } while (numGenomesToGenerate > 0);                
            }
        }

        /// <summary>
        ///     Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        protected override void PerformOneGeneration()
        {
            // TODO: Select number proportional to batch size
            // Produce number of offspring equivalent to the given batch size
            List<TGenome> childGenomes = CreateOffspring(_batchSize);

            // TODO: Evaluate select only for minimal criteria
            // Evaluate the offspring batch
            GenomeEvaluator.Evaluate(childGenomes, GenomeList);

            // TODO: Remove only the amount that satisfied minimal criteria
            // Determine genomes to remove based on their adjusted fitness
            List<TGenome> genomesToRemove = SelectGenomesForRemoval(_batchSize);

            // Remove the worst individuals from the previous iteration
            (GenomeList as List<TGenome>)?.RemoveAll(x => genomesToRemove.Contains(x));

            // Add new children
            (GenomeList as List<TGenome>)?.AddRange(childGenomes);

            // Clear all the species and respeciate
            ClearAllSpecies();
            SpeciationStrategy.SpeciateGenomes(GenomeList, SpecieList);

            // Sort the genomes in each specie. Fittest first (secondary sort - youngest first).
            SortSpecieGenomes();

            // Update stats and store reference to best genome.
            UpdateBestGenome();
            UpdateStats();

            // Update the elite archive parameters and reset for next evaluation
            AbstractNoveltyArchive?.UpdateArchiveParameters();

            Debug.Assert(GenomeList.Count == PopulationSize);

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
        /// The queue of genomes that's used to bootstrap the global genome list.
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
                new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric()),
                new NullComplexityRegulationStrategy(), 10, 100, logger)
        {
            SpeciationStrategy = new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric());
            ComplexityRegulationStrategy = new NullComplexityRegulationStrategy();
            _batchSize = 10;
            _populationEvaluationFrequency = 100;
        }

        /// <summary>
        ///     Constructs steady state evolution algorithm with the given NEAT parameters, speciation strategy, and complexity
        ///     regulation strategy.
        /// </summary>
        /// <param name="eaParams">The NEAT algorithm parameters.</param>
        /// <param name="speciationStrategy">The speciation strategy.</param>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="batchSize">The batch size of offspring to produce, evaluate, and remove.</param>
        /// <param name="populationEvaluationFrequency">The frequency at which to evaluate the fitness of the entire population.</param>
        /// <param name="logger">The data logger (optional).</param>
        public QueueingNeatEvolutionAlgorithm(
            ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize,
            int populationEvaluationFrequency,
            IDataLogger logger = null)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _batchSize = batchSize;
            _populationEvaluationFrequency = populationEvaluationFrequency;
            EvolutionLogger = logger;
        }

        /// <summary>
        ///     Constructs steady state evolution algorithm with the given NEAT parameters, speciation strategy, and complexity
        ///     regulation strategy.
        /// </summary>
        /// <param name="eaParams">The NEAT algorithm parameters.</param>
        /// <param name="speciationStrategy">The speciation strategy.</param>
        /// <param name="complexityRegulationStrategy">The complexity regulation strategy.</param>
        /// <param name="batchSize">The batch size of offspring to produce, evaluate, and remove.</param>
        /// <param name="populationEvaluationFrequency">The frequency at which to evaluate the fitness of the entire population.</param>
        /// <param name="logger">The data logger (optional).</param>
        public QueueingNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
            ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            int batchSize,
            int populationEvaluationFrequency,
            IDataLogger logger = null) : base(eaParams)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            _batchSize = batchSize;
            _populationEvaluationFrequency = populationEvaluationFrequency;
            EvolutionLogger = logger;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Creates the specified number of offspring using roulette wheel species and genomes selection (which is based on the
        ///     fitness stats of the species in the given stats array).
        /// </summary>
        /// <param name="specieStatsArr">
        ///     The specie stats array, which is used to support specie reproduction selection based on
        ///     specie size and mean fitness.
        /// </param>
        /// <param name="offspringCount">The number of offspring to produce.</param>
        /// <returns>The list of offspring.</returns>
        private List<TGenome> CreateOffspring(int offspringCount)
        {
            List<TGenome> offspringList = new List<TGenome>(offspringCount);
            
            // TODO: Remove parent genomes and produce offspring

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