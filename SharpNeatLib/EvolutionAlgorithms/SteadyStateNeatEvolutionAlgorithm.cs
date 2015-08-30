using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.SpeciationStrategies;
using SharpNeat.Utility;

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

        private readonly FastRandom _rng = new FastRandom();

        private readonly NeatAlgorithmStats _stats;

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

            // Calculate statistics for each specie (mean fitness and target size)
            SpecieStats[] specieStatsArr = CalcSpecieStats();

            // TODO: Randomly sample population and select fittest (?)

            // TODO: Generate child asexually (reference NeatGenerationalEvolutionAlgorithm lines 550 - 588)

            // Produce a single offspring
            TGenome childGenome = CreateSingleOffspring();

            // TODO: Randomly sample population and remove non-fittest

            // Select a genome for removal
            TGenome genomeToRemove = SelectGenomeForRemoval(3);

            // Add new child and remove the one marked for deletion
            GenomeList.Add(childGenome);
            GenomeList.Remove(genomeToRemove);

            // TODO: Run child trial

            GenomeListEvaluator.Evaluate(GenomeList);

            // TODO: Re-speciate the whole population

            ClearAllSpecies();
            _speciationStrategy.SpeciateGenomes(GenomeList, SpecieList);

            // TODO: Sort species and update best genome/stats

            // Sort the genomes in each specie. Fittest first (secondary sort - youngest first).
            SortSpecieGenomes();

            // Update stats and store reference to best genome.
            UpdateBestGenome();
            UpdateStats();

            // TODO: Update archive

            // Update the elite archive parameters and reset for next evaluation
            EliteArchive?.UpdateArchiveParameters();

            Debug.Assert(GenomeList.Count == PopulationSize);
        }

        /// <summary>
        /// Clear the genome list within each specie.
        /// </summary>
        private void ClearAllSpecies()
        {
            foreach (Specie<TGenome> specie in SpecieList)
            {
                specie.GenomeList.Clear();
            }
        }

        private SpecieStats[] CalcSpecieStats()
        {
            double totalMeanFitness = 0.0;

            // Build stats array and get the mean fitness of each specie.
            int specieCount = SpecieList.Count;
            SpecieStats[] specieStatsArr = new SpecieStats[specieCount];
            for (int i = 0; i < specieCount; i++)
            {
                SpecieStats inst = new SpecieStats();
                specieStatsArr[i] = inst;
                inst._meanFitness = SpecieList[i].CalcMeanFitness();
                totalMeanFitness += inst._meanFitness;
            }

            // Calculate the new target size of each specie using fitness sharing. 
            // Keep a total of all allocated target sizes, typically this will vary slightly from the
            // overall target population size due to rounding of each real/fractional target size.
            int totalTargetSizeInt = 0;

            if (0.0 == totalMeanFitness)
            {   // Handle specific case where all genomes/species have a zero fitness. 
                // Assign all species an equal targetSize.
                double targetSizeReal = (double)PopulationSize / (double)specieCount;

                for (int i = 0; i < specieCount; i++)
                {
                    SpecieStats inst = specieStatsArr[i];
                    inst._targetSizeReal = targetSizeReal;

                    // Stochastic rounding will result in equal allocation if targetSizeReal is a whole
                    // number, otherwise it will help to distribute allocations evenly.
                    inst._targetSizeInt = (int)Utilities.ProbabilisticRound(targetSizeReal, _rng);

                    // Total up discretized target sizes.
                    totalTargetSizeInt += inst._targetSizeInt;
                }
            }
            else
            {
                // The size of each specie is based on its fitness relative to the other species.
                for (int i = 0; i < specieCount; i++)
                {
                    SpecieStats inst = specieStatsArr[i];
                    inst._targetSizeReal = (inst._meanFitness / totalMeanFitness) * (double)PopulationSize;

                    // Discretize targetSize (stochastic rounding).
                    inst._targetSizeInt = (int)Utilities.ProbabilisticRound(inst._targetSizeReal, _rng);

                    // Total up discretized target sizes.
                    totalTargetSizeInt += inst._targetSizeInt;
                }
            }

            // Discretized target sizes may total up to a value that is not equal to the required overall population
            // size. Here we check this and if there is a difference then we adjust the specie's targetSizeInt values
            // to compensate for the difference.
            //
            // E.g. If we are short of the required populationSize then we add the required additional allocation to
            // selected species based on the difference between each specie's targetSizeReal and targetSizeInt values.
            // What we're effectively doing here is assigning the additional required target allocation to species based
            // on their real target size in relation to their actual (integer) target size.
            // Those species that have an actual allocation below there real allocation (the difference will often 
            // be a fractional amount) will be assigned extra allocation probabilistically, where the probability is
            // based on the differences between real and actual target values.
            //
            // Where the actual target allocation is higher than the required target (due to rounding up), we use the same
            // method but we adjust specie target sizes down rather than up.
            int targetSizeDeltaInt = totalTargetSizeInt - PopulationSize;

            if (targetSizeDeltaInt < 0)
            {
                // Check for special case. If we are short by just 1 then increment targetSizeInt for the specie containing
                // the best genome. We always ensure that this specie has a minimum target size of 1 with a final test (below),
                // by incrementing here we avoid the probabilistic allocation below followed by a further correction if
                // the champ specie ended up with a zero target size.
                if (-1 == targetSizeDeltaInt)
                {
                    specieStatsArr[_bestSpecieIdx]._targetSizeInt++;
                }
                else
                {
                    // We are short of the required populationSize. Add the required additional allocations.
                    // Determine each specie's relative probability of receiving additional allocation.
                    double[] probabilities = new double[specieCount];
                    for (int i = 0; i < specieCount; i++)
                    {
                        SpecieStats inst = specieStatsArr[i];
                        probabilities[i] = Math.Max(0.0, inst._targetSizeReal - (double)inst._targetSizeInt);
                    }

                    // Use a built in class for choosing an item based on a list of relative probabilities.
                    RouletteWheelLayout rwl = new RouletteWheelLayout(probabilities);

                    // Probabilistically assign the required number of additional allocations.
                    // ENHANCEMENT: We can improve the allocation fairness by updating the RouletteWheelLayout 
                    // after each allocation (to reflect that allocation).
                    // targetSizeDeltaInt is negative, so flip the sign for code clarity.
                    targetSizeDeltaInt *= -1;
                    for (int i = 0; i < targetSizeDeltaInt; i++)
                    {
                        int specieIdx = RouletteWheel.SingleThrow(rwl, _rng);
                        specieStatsArr[specieIdx]._targetSizeInt++;
                    }
                }
            }
            else if (targetSizeDeltaInt > 0)
            {
                // We have overshot the required populationSize. Adjust target sizes down to compensate.
                // Determine each specie's relative probability of target size downward adjustment.
                double[] probabilities = new double[specieCount];
                for (int i = 0; i < specieCount; i++)
                {
                    SpecieStats inst = specieStatsArr[i];
                    probabilities[i] = Math.Max(0.0, (double)inst._targetSizeInt - inst._targetSizeReal);
                }

                // Use a built in class for choosing an item based on a list of relative probabilities.
                RouletteWheelLayout rwl = new RouletteWheelLayout(probabilities);

                // Probabilistically decrement specie target sizes.
                // ENHANCEMENT: We can improve the selection fairness by updating the RouletteWheelLayout 
                // after each decrement (to reflect that decrement).
                for (int i = 0; i < targetSizeDeltaInt;)
                {
                    int specieIdx = RouletteWheel.SingleThrow(rwl, _rng);

                    // Skip empty species. This can happen because the same species can be selected more than once.
                    if (0 != specieStatsArr[specieIdx]._targetSizeInt)
                    {
                        specieStatsArr[specieIdx]._targetSizeInt--;
                        i++;
                    }
                }
            }

            // We now have Sum(_targetSizeInt) == _populationSize. 
            Debug.Assert(SumTargetSizeInt(specieStatsArr) == PopulationSize);

            // TODO: Better way of ensuring champ species has non-zero target size?
            // However we need to check that the specie with the best genome has a non-zero targetSizeInt in order
            // to ensure that the best genome is preserved. A zero size may have been allocated in some pathological cases.
            if (0 == specieStatsArr[_bestSpecieIdx]._targetSizeInt)
            {
                specieStatsArr[_bestSpecieIdx]._targetSizeInt++;

                // Adjust down the target size of one of the other species to compensate.
                // Pick a specie at random (but not the champ specie). Note that this may result in a specie with a zero 
                // target size, this is OK at this stage. We handle allocations of zero in PerformOneGeneration().
                int idx = RouletteWheel.SingleThrowEven(specieCount - 1, _rng);
                idx = idx == _bestSpecieIdx ? idx + 1 : idx;

                if (specieStatsArr[idx]._targetSizeInt > 0)
                {
                    specieStatsArr[idx]._targetSizeInt--;
                }
                else
                {   // Scan forward from this specie to find a suitable one.
                    bool done = false;
                    idx++;
                    for (; idx < specieCount; idx++)
                    {
                        if (idx != _bestSpecieIdx && specieStatsArr[idx]._targetSizeInt > 0)
                        {
                            specieStatsArr[idx]._targetSizeInt--;
                            done = true;
                            break;
                        }
                    }

                    // Scan forward from start of species list.
                    if (!done)
                    {
                        for (int i = 0; i < specieCount; i++)
                        {
                            if (i != _bestSpecieIdx && specieStatsArr[i]._targetSizeInt > 0)
                            {
                                specieStatsArr[i]._targetSizeInt--;
                                done = true;
                                break;
                            }
                        }
                        if (!done)
                        {
                            throw new SharpNeatException("CalcSpecieStats(). Error adjusting target population size down. Is the population size less than or equal to the number of species?");
                        }
                    }
                }
            }            

            return specieStatsArr;
        }

        private TGenome CreateSingleOffspring()
        {
            // Choose a random species from which to pick an individual for reproduction
            int specieIdx = _rng.Next(0, SpecieList.Count - 1);
            List<TGenome> specieGenomes = SpecieList[specieIdx].GenomeList;

            // Set up array of selection probability based on current fitness
            double[] probabilities = new double[specieGenomes.Count];
            for (int curGenome = 0; curGenome < specieGenomes.Count; curGenome++)
            {
                probabilities[curGenome] = specieGenomes[curGenome].EvaluationInfo.Fitness;
            }

            // Create a roulette wheel layout based on the probability array
            RouletteWheelLayout rwl = new RouletteWheelLayout(probabilities);

            // Select a genome from the species to reproduce
            int genomeIndex = RouletteWheel.SingleThrow(rwl, _rng);
            TGenome genome = specieGenomes[genomeIndex].CreateOffspring(CurrentGeneration);

            return genome;
        }

        private TGenome SelectGenomeForRemoval(int sampleSize)
        {
            TGenome genomeToRemove = null;
            const double curLowFitness = Double.MaxValue;

            for (int cnt = 0; cnt < sampleSize; cnt++)
            {
                TGenome candidateGenome = GenomeList[_rng.Next(0, GenomeList.Count - 1)];

                if (candidateGenome.EvaluationInfo.Fitness < curLowFitness)
                {
                    genomeToRemove = candidateGenome;
                }
            }

            return genomeToRemove;            
        }

        private static int SumTargetSizeInt(SpecieStats[] specieStatsArr)
        {
            int total = 0;
            foreach (SpecieStats inst in specieStatsArr)
            {
                total += inst._targetSizeInt;
            }
            return total;
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

        /// <summary>
        /// Updates the NeatAlgorithmStats object.
        /// </summary>
        private void UpdateStats()
        {
            _stats._generation = CurrentGeneration;
            _stats._totalEvaluationCount = GenomeListEvaluator.EvaluationCount;

            // Evaluation per second.
            DateTime now = DateTime.Now;
            TimeSpan duration = now - _stats._evalsPerSecLastSampleTime;

            // To smooth out the evals per sec statistic we only update if at least 1 second has elapsed 
            // since it was last updated.
            if (duration.Ticks > 9999)
            {
                long evalsSinceLastUpdate = (long)(GenomeListEvaluator.EvaluationCount - _stats._evalsCountAtLastUpdate);
                _stats._evaluationsPerSec = (int)((evalsSinceLastUpdate * 1e7) / duration.Ticks);

                // Reset working variables.
                _stats._evalsCountAtLastUpdate = GenomeListEvaluator.EvaluationCount;
                _stats._evalsPerSecLastSampleTime = now;
            }

            // Fitness and complexity stats.
            double totalFitness = GenomeList[0].EvaluationInfo.Fitness;
            double totalComplexity = GenomeList[0].Complexity;
            double maxComplexity = totalComplexity;

            int count = GenomeList.Count;
            for (int i = 1; i < count; i++)
            {
                totalFitness += GenomeList[i].EvaluationInfo.Fitness;
                totalComplexity += GenomeList[i].Complexity;
                maxComplexity = Math.Max(maxComplexity, GenomeList[i].Complexity);
            }

            _stats._maxFitness = CurrentChampGenome.EvaluationInfo.Fitness;
            _stats._meanFitness = totalFitness / count;

            _stats._maxComplexity = maxComplexity;
            _stats._meanComplexity = totalComplexity / count;

            // Specie champs mean fitness.
            double totalSpecieChampFitness = SpecieList[0].GenomeList[0].EvaluationInfo.Fitness;
            int specieCount = SpecieList.Count;
            for (int i = 1; i < specieCount; i++)
            {
                totalSpecieChampFitness += SpecieList[i].GenomeList[0].EvaluationInfo.Fitness;
            }
            _stats._meanSpecieChampFitness = totalSpecieChampFitness / specieCount;

            // Moving averages.
            _stats._prevBestFitnessMA = _stats._bestFitnessMA.Mean;
            _stats._bestFitnessMA.Enqueue(_stats._maxFitness);

            _stats._prevMeanSpecieChampFitnessMA = _stats._meanSpecieChampFitnessMA.Mean;
            _stats._meanSpecieChampFitnessMA.Enqueue(_stats._meanSpecieChampFitness);

            _stats._prevComplexityMA = _stats._complexityMA.Mean;
            _stats._complexityMA.Enqueue(_stats._meanComplexity);
        }

        class SpecieStats
        {
            // Real/continuous stats.
            public double _meanFitness;
            public double _targetSizeReal;

            // Integer stats.
            public int _targetSizeInt;
            public int _offspringAsexualCount;
        }
    }
}
