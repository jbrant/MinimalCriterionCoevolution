using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.SpeciationStrategies;

namespace SharpNeat.EvolutionAlgorithms
{
    public class SteadyStateNeatEvolutionAlgorithm<TGenome> : AbstractSteadyStateAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        private NeatEvolutionAlgorithmParameters _eaParams;
        private readonly NeatEvolutionAlgorithmParameters _eaParamsComplexifying;
        private readonly NeatEvolutionAlgorithmParameters _eaParamsSimplifying;

        private readonly ISpeciationStrategy<TGenome> _speciationStrategy;

        private readonly IComplexityRegulationStrategy _complexityRegulationStrategy;

        /// <summary>Index of the specie that contains _currentBestGenome.</summary>
        private int _bestSpecieIdx;

        public SteadyStateNeatEvolutionAlgorithm()
        {
            _eaParams = new NeatEvolutionAlgorithmParameters();
            _eaParamsComplexifying = _eaParams;
            _eaParamsSimplifying = _eaParams.CreateSimplifyingParameters();

            _speciationStrategy = new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric());
        }

        public SteadyStateNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
            ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy)
        {
            _eaParams = eaParams;
            _eaParamsComplexifying = _eaParams;
            _eaParamsSimplifying = _eaParams.CreateSimplifyingParameters();

            _speciationStrategy = speciationStrategy;

            ComplexityRegulationMode = ComplexityRegulationMode.Complexifying;
            _complexityRegulationStrategy = complexityRegulationStrategy;
        }        

        /// <summary>
        /// Gets a list of all current genomes. The current population of genomes. These genomes
        /// are also divided into the species available through the SpeciesList property.
        /// </summary>
        public IList<TGenome> GenomeList { get; private set; }

        /// <summary>
        /// Gets a list of all current species. The genomes contained within the species are the same genomes
        /// available through the GenomeList property.
        /// </summary>
        public IList<Specie<TGenome>> SpecieList { get; private set; }

        /// <summary>
        /// Gets the algorithm statistics object.
        /// </summary>
        public NeatAlgorithmStats Statistics { get; private set; }

        /// <summary>
        /// Gets the current complexity regulation mode.
        /// </summary>
        public ComplexityRegulationMode ComplexityRegulationMode { get; private set; }

        /// <summary>
        /// Initializes the evolution algorithm with the provided IGenomeListEvaluator, IGenomeFactory
        /// and an initial population of genomes.
        /// </summary>
        /// <param name="genomeListEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">The factory that was used to create the genomeList and which is therefore referenced by the genomes.</param>
        /// <param name="genomeList">An initial genome population.</param>
        /// <param name="eliteArchive">The cross-generational archive of high-performing genomes (optional).</param>
        public override void Initialize(IGenomeListEvaluator<TGenome> genomeListEvaluator,
                                        IGenomeFactory<TGenome> genomeFactory,
                                        List<TGenome> genomeList,
                                        EliteArchive<TGenome> eliteArchive = null)
        {
            base.Initialize(genomeListEvaluator, genomeFactory, genomeList, eliteArchive);
            Initialize();
        }

        /// <summary>
        /// Initializes the evolution algorithm with the provided IGenomeListEvaluator
        /// and an IGenomeFactory that can be used to create an initial population of genomes.
        /// </summary>
        /// <param name="genomeListEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">The factory that was used to create the genomeList and which is therefore referenced by the genomes.</param>
        /// <param name="populationSize">The number of genomes to create for the initial population.</param>
        /// <param name="eliteArchive">The cross-generational archive of high-performing genomes (optional).</param>
        public override void Initialize(IGenomeListEvaluator<TGenome> genomeListEvaluator,
                                        IGenomeFactory<TGenome> genomeFactory,
                                        int populationSize,
                                        EliteArchive<TGenome> eliteArchive = null)
        {
            base.Initialize(genomeListEvaluator, genomeFactory, populationSize, eliteArchive);
            Initialize();
        }

        /// <summary>
        /// Code common to both public Initialize methods.
        /// </summary>
        private void Initialize()
        {
            // Evaluate the entire population at first.
            GenomeListEvaluator.Evaluate(GenomeList);

            // Speciate the genomes.
            SpecieList = _speciationStrategy.InitializeSpeciation(GenomeList, _eaParams.SpecieCount);
            Debug.Assert(!TestForEmptySpecies(SpecieList), "Speciation resulted in one or more empty species.");

            // Sort the genomes in each specie fittest first, secondary sort youngest first.
            SortSpecieGenomes();

            // Store ref to best genome.
            UpdateBestGenome();
        }

        /// <summary>
        /// Returns true if there is one or more empty species.
        /// </summary>
        private bool TestForEmptySpecies(IList<Specie<TGenome>> specieList)
        {
            foreach (Specie<TGenome> specie in specieList)
            {
                if (specie.GenomeList.Count == 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        protected override void PerformOneEvaluation()
        {
            // TODO: Calculate specie stats (but without offspring count)

            // TODO: Randomly sample population and select fittest

            // TODO: Randomly sample population and remove non-fittest

            // TODO: Generate child asexually (reference NeatEvolutionAlgorithm lines 550 - 588)

            // TODO: Run child trial

            // TODO: Re-speciate the whole population

            // TODO: Sort species and update best genome/stats

            // TODO: Update archive
        }

        /// <summary>
        /// Sorts the genomes within each species fittest first, secondary sorts on age.
        /// </summary>
        private void SortSpecieGenomes()
        {
            int minSize = SpecieList[0].GenomeList.Count;
            int maxSize = minSize;
            int specieCount = SpecieList.Count;

            for (int i = 0; i < specieCount; i++)
            {
                SpecieList[i].GenomeList.Sort(GenomeFitnessComparer<TGenome>.Singleton);
                minSize = Math.Min(minSize, SpecieList[i].GenomeList.Count);
                maxSize = Math.Max(maxSize, SpecieList[i].GenomeList.Count);
            }

            // Update stats.
            Statistics._minSpecieSize = minSize;
            Statistics._maxSpecieSize = maxSize;
        }

        /// <summary>
        /// Updates _currentBestGenome and _bestSpecieIdx, these are the fittest genome and index of the specie
        /// containing the fittest genome respectively.
        /// 
        /// This method assumes that all specie genomes are sorted fittest first and can therefore save much work
        /// by not having to scan all genomes.
        /// Note. We may have several genomes with equal best fitness, we just select one of them in that case.
        /// </summary>
        protected void UpdateBestGenome()
        {
            // If all genomes have the same fitness (including zero) then we simply return the first genome.
            TGenome bestGenome = null;
            double bestFitness = -1.0;
            int bestSpecieIdx = -1;

            int count = SpecieList.Count;
            for (int i = 0; i < count; i++)
            {
                // Get the specie's first genome. Genomes are sorted, therefore this is also the fittest 
                // genome in the specie.
                TGenome genome = SpecieList[i].GenomeList[0];
                if (genome.EvaluationInfo.Fitness > bestFitness)
                {
                    bestGenome = genome;
                    bestFitness = genome.EvaluationInfo.Fitness;
                    bestSpecieIdx = i;
                }
            }

            CurrentChampGenome = bestGenome;
            _bestSpecieIdx = bestSpecieIdx;
        }
    }
}
