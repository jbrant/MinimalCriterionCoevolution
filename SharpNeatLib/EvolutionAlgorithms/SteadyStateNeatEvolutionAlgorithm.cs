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
    public class SteadyStateNeatEvolutionAlgorithm<TGenome> : AbstractNeatEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        #region Overridden Methods

        /// <summary>
        ///     Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        protected override void PerformOneGeneration()
        {
            // Re-evaluate the fitness of the population after the specified number of evaluations have elapsed
            if (CurrentGeneration%_populationEvaluationFrequency == 0)
            {
                GenomeEvaluator.Evaluate(GenomeList);
            }

            // Calculate statistics for each specie (mean fitness and target size)
            SpecieStats[] specieStatsArr = CalcSpecieStats();

            // Produce number of offspring equivalent to the given batch size
            List<TGenome> childGenomes = CreateOffspring(specieStatsArr, _batchSize);

            // Evaluate the offspring batch
            GenomeEvaluator.Evaluate(childGenomes, GenomeList);

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

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs steady state evolution algorithm with the default clustering strategy (k-means clustering) using
        ///     manhattan distance and null complexity regulation strategy.
        /// </summary>
        /// <param name="logger">The data logger (optional).</param>
        public SteadyStateNeatEvolutionAlgorithm(IDataLogger logger = null)
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
        public SteadyStateNeatEvolutionAlgorithm(
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
        public SteadyStateNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
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
        ///     Updates the specie stats array, calculating and persisting the mean fitness of each species.
        /// </summary>
        /// <returns>The updated specie stats array.</returns>
        private SpecieStats[] CalcSpecieStats()
        {
            // Build stats array and get the mean fitness of each specie.
            int specieCount = SpecieList.Count;
            SpecieStats[] specieStatsArr = new SpecieStats[specieCount];
            for (int i = 0; i < specieCount; i++)
            {
                SpecieStats inst = new SpecieStats();
                specieStatsArr[i] = inst;
                inst.MeanFitness = SpecieList[i].CalcMeanFitness();
            }

            return specieStatsArr;
        }

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
        private List<TGenome> CreateOffspring(SpecieStats[] specieStatsArr, int offspringCount)
        {
            List<TGenome> offspringList = new List<TGenome>(offspringCount);
            int specieCount = SpecieList.Count;

            // Probabilities for species roulette wheel selector
            double[] specieProbabilities = new double[specieCount];

            // Roulette wheel layout for genomes within species
            RouletteWheelLayout[] genomeRwlArr = new RouletteWheelLayout[specieCount];

            // Build array of probabilities based on specie mean fitness
            for (int curSpecie = 0; curSpecie < specieCount; curSpecie++)
            {
                // Set probability for current species as specie mean fitness
                specieProbabilities[curSpecie] = specieStatsArr[curSpecie].MeanFitness;

                int genomeCount = SpecieList[curSpecie].GenomeList.Count;

                // Decare array for specie genome probabilities
                double[] genomeProbabilities = new double[genomeCount];

                // Build probability array for genome selection within species
                // based on genome fitness
                for (int curGenome = 0; curGenome < genomeCount; curGenome++)
                {
                    genomeProbabilities[curGenome] = SpecieList[curSpecie].GenomeList[curGenome].EvaluationInfo.Fitness;
                }

                // Create the genome roulette wheel layout for the current species
                genomeRwlArr[curSpecie] = new RouletteWheelLayout(genomeProbabilities);
            }

            // Create the specie roulette wheel layout
            RouletteWheelLayout specieRwl = new RouletteWheelLayout(specieProbabilities);

            for (int curOffspring = 0; curOffspring < offspringCount; curOffspring++)
            {
                // Select specie from which to generate the next offspring
                int specieIdx = RouletteWheel.SingleThrow(specieRwl, RandomNumGenerator);

                // If random number is equal to or less than specified asexual offspring proportion or
                // if there is only one genome in the species, then use asexual reproduction
                if (RandomNumGenerator.NextDouble() <= EaParams.OffspringAsexualProportion ||
                    SpecieList[specieIdx].GenomeList.Count <= 1)
                {
                    // Throw ball to select genome from species (essentially intra-specie fitness proportionate selection)
                    int genomeIdx = RouletteWheel.SingleThrow(genomeRwlArr[specieIdx], RandomNumGenerator);

                    // Create offspring asexually (from the above-selected parent)
                    TGenome offspring = SpecieList[specieIdx].GenomeList[genomeIdx].CreateOffspring(CurrentGeneration);

                    // Add that offspring to the genome list
                    offspringList.Add(offspring);
                }
                // Otherwise, mate two parents
                else
                {
                    TGenome parent1, parent2;

                    // If random number is equal to or less than specified interspecies mating proportion, then
                    // mate between two parent genomes from two different species
                    if (RandomNumGenerator.NextDouble() <= EaParams.InterspeciesMatingProportion)
                    {
                        // Throw ball again to get a second species
                        int specie2Idx = RouletteWheel.SingleThrow(specieRwl, RandomNumGenerator);

                        // Throw ball twice to select the two parent genomes (one from each species)
                        int parent1GenomeIdx = RouletteWheel.SingleThrow(genomeRwlArr[specieIdx], RandomNumGenerator);
                        int parent2GenomeIdx = RouletteWheel.SingleThrow(genomeRwlArr[specie2Idx], RandomNumGenerator);

                        // Get the two parents out of the two species genome list
                        parent1 = SpecieList[specieIdx].GenomeList[parent1GenomeIdx];
                        parent2 = SpecieList[specie2Idx].GenomeList[parent2GenomeIdx];
                    }
                    // Otherwise, mate two parents from within the currently selected species
                    else
                    {
                        // Throw ball twice to select the two parent genomes
                        int parent1GenomeIdx = RouletteWheel.SingleThrow(genomeRwlArr[specieIdx], RandomNumGenerator);
                        int parent2GenomeIdx = RouletteWheel.SingleThrow(genomeRwlArr[specieIdx], RandomNumGenerator);

                        // If the same parent happened to be selected twice, throw ball until they differ
                        while (parent1GenomeIdx == parent2GenomeIdx)
                        {
                            parent2GenomeIdx = RouletteWheel.SingleThrow(genomeRwlArr[specieIdx], RandomNumGenerator);
                        }

                        // Get the two parents out of the species genome list
                        parent1 = SpecieList[specieIdx].GenomeList[parent1GenomeIdx];
                        parent2 = SpecieList[specieIdx].GenomeList[parent2GenomeIdx];
                    }

                    // Perform recombination
                    TGenome offspring = parent1.CreateOffspring(parent2, CurrentGeneration);
                    offspringList.Add(offspring);
                }
            }

            return offspringList;
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
                    // Only add as a removal candidate if genome is old enough (this is an attempt to refrain
                    // from penalizing new innovations)
                    if ((CurrentGeneration - specie.GenomeList[genomeIdx].BirthGeneration) > EaParams.MinTimeAlive ||
                        CurrentGeneration <= EaParams.MinTimeAlive)
                    {
                        // Add adjusted fitness and the genome reference to the map (dictionary)
                        removalCandidatesMap.Add(specie.GenomeList[genomeIdx],
                            specie.CalcGenomeAdjustedFitness(genomeIdx));
                    }
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