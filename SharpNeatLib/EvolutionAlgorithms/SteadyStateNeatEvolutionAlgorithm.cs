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
    public class SteadyStateNeatEvolutionAlgorithm<TGenome> : AbstractNeatEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {       
        public SteadyStateNeatEvolutionAlgorithm()
        {
            SpeciationStrategy = new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric());
            ComplexityRegulationStrategy = new NullComplexityRegulationStrategy();
        }

        public SteadyStateNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
            ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
        }        
        
        /// <summary>
        /// Progress forward by one evaluation. Perform one iteration of the evolution algorithm.
        /// </summary>
        protected override void PerformOneGeneration()
        {
            // TODO: Need to make this a user-definable parameter
            // Re-evaluate the novelty of the population every 25 "generations"
            if (CurrentGeneration%100 == 0)
            {
                GenomeEvaluator.Evaluate(GenomeList);
            }
                     
            // Calculate statistics for each specie (mean fitness and target size)
            SpecieStats[] specieStatsArr = CalcSpecieStats();
            
            // Produce number of offspring equivalent to the given batch size
            List<TGenome> childGenomes = CreateOffspring(specieStatsArr, 10);

            // TODO: Randomly sample population and remove least fit
            
            // Evaluate the offspring batch
            GenomeEvaluator.Evaluate(childGenomes, GenomeList);

            // Determine genomes to remove based on their adjusted fitness
            List<TGenome> genomesToRemove = SelectGenomesForRemoval(10);

            // Remove the worst individuals from the previous iteration
            (GenomeList as List<TGenome>)?.RemoveAll(x => genomesToRemove.Contains(x));

            // Add new children
            (GenomeList as List<TGenome>)?.AddRange(childGenomes);

            // TODO: Re-speciate the whole population

            ClearAllSpecies();
            SpeciationStrategy.SpeciateGenomes(GenomeList, SpecieList);
            
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
                if (RandomNumGenerator.NextDouble() <= EaParams.OffspringAsexualProportion || SpecieList[specieIdx].GenomeList.Count <= 1)
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

        private SpecieStats[] CalcSpecieStats_Old()
        {
            double totalMeanFitness = 0.0;

            // Build stats array and get the mean fitness of each specie.
            int specieCount = SpecieList.Count;
            SpecieStats[] specieStatsArr = new SpecieStats[specieCount];
            for (int i = 0; i < specieCount; i++)
            {
                SpecieStats inst = new SpecieStats();
                specieStatsArr[i] = inst;
                inst.MeanFitness = SpecieList[i].CalcMeanFitness();
                totalMeanFitness += inst.MeanFitness;
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
                    inst.TargetSizeReal = targetSizeReal;

                    // Stochastic rounding will result in equal allocation if targetSizeReal is a whole
                    // number, otherwise it will help to distribute allocations evenly.
                    inst.TargetSizeInt = (int)Utilities.ProbabilisticRound(targetSizeReal, RandomNumGenerator);

                    // Total up discretized target sizes.
                    totalTargetSizeInt += inst.TargetSizeInt;
                }
            }
            else
            {
                // The size of each specie is based on its fitness relative to the other species.
                for (int i = 0; i < specieCount; i++)
                {
                    SpecieStats inst = specieStatsArr[i];
                    inst.TargetSizeReal = (inst.MeanFitness / totalMeanFitness) * (double)PopulationSize;

                    // Discretize targetSize (stochastic rounding).
                    inst.TargetSizeInt = (int)Utilities.ProbabilisticRound(inst.TargetSizeReal, RandomNumGenerator);

                    // Total up discretized target sizes.
                    totalTargetSizeInt += inst.TargetSizeInt;
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
                    specieStatsArr[BestSpecieIndex].TargetSizeInt++;
                }
                else
                {
                    // We are short of the required populationSize. Add the required additional allocations.
                    // Determine each specie's relative probability of receiving additional allocation.
                    double[] probabilities = new double[specieCount];
                    for (int i = 0; i < specieCount; i++)
                    {
                        SpecieStats inst = specieStatsArr[i];
                        probabilities[i] = Math.Max(0.0, inst.TargetSizeReal - (double)inst.TargetSizeInt);
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
                        int specieIdx = RouletteWheel.SingleThrow(rwl, RandomNumGenerator);
                        specieStatsArr[specieIdx].TargetSizeInt++;
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
                    probabilities[i] = Math.Max(0.0, (double)inst.TargetSizeInt - inst.TargetSizeReal);
                }

                // Use a built in class for choosing an item based on a list of relative probabilities.
                RouletteWheelLayout rwl = new RouletteWheelLayout(probabilities);

                // Probabilistically decrement specie target sizes.
                // ENHANCEMENT: We can improve the selection fairness by updating the RouletteWheelLayout 
                // after each decrement (to reflect that decrement).
                for (int i = 0; i < targetSizeDeltaInt;)
                {
                    int specieIdx = RouletteWheel.SingleThrow(rwl, RandomNumGenerator);

                    // Skip empty species. This can happen because the same species can be selected more than once.
                    if (0 != specieStatsArr[specieIdx].TargetSizeInt)
                    {
                        specieStatsArr[specieIdx].TargetSizeInt--;
                        i++;
                    }
                }
            }

            // We now have Sum(_targetSizeInt) == _populationSize. 
            Debug.Assert(SpeciationUtils<TGenome>.SumTargetSizeInt(specieStatsArr) == PopulationSize);

            // TODO: Better way of ensuring champ species has non-zero target size?
            // However we need to check that the specie with the best genome has a non-zero targetSizeInt in order
            // to ensure that the best genome is preserved. A zero size may have been allocated in some pathological cases.
            if (0 == specieStatsArr[BestSpecieIndex].TargetSizeInt)
            {
                specieStatsArr[BestSpecieIndex].TargetSizeInt++;

                // Adjust down the target size of one of the other species to compensate.
                // Pick a specie at random (but not the champ specie). Note that this may result in a specie with a zero 
                // target size, this is OK at this stage. We handle allocations of zero in PerformOneGeneration().
                int idx = RouletteWheel.SingleThrowEven(specieCount - 1, RandomNumGenerator);
                idx = idx == BestSpecieIndex ? idx + 1 : idx;

                if (specieStatsArr[idx].TargetSizeInt > 0)
                {
                    specieStatsArr[idx].TargetSizeInt--;
                }
                else
                {   // Scan forward from this specie to find a suitable one.
                    bool done = false;
                    idx++;
                    for (; idx < specieCount; idx++)
                    {
                        if (idx != BestSpecieIndex && specieStatsArr[idx].TargetSizeInt > 0)
                        {
                            specieStatsArr[idx].TargetSizeInt--;
                            done = true;
                            break;
                        }
                    }

                    // Scan forward from start of species list.
                    if (!done)
                    {
                        for (int i = 0; i < specieCount; i++)
                        {
                            if (i != BestSpecieIndex && specieStatsArr[i].TargetSizeInt > 0)
                            {
                                specieStatsArr[i].TargetSizeInt--;
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
            int specieIdx = RandomNumGenerator.Next(0, SpecieList.Count - 1);
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
            int genomeIndex = RouletteWheel.SingleThrow(rwl, RandomNumGenerator);
            TGenome genome = specieGenomes[genomeIndex].CreateOffspring(CurrentGeneration);

            return genome;
        }

        private List<TGenome> SelectGenomesForRemoval(int numGenomesToRemove)
        {
            List<TGenome> genomesToRemove = new List<TGenome>(numGenomesToRemove);

            //KeyValuePair<double, TGenome> adjustedFitnessMap = new KeyValuePair<double, TGenome>();
            //Dictionary<double, TGenome> adjustedFitnessMap = new Dictionary<double, TGenome>();
            Dictionary<TGenome, double> adjustedFitnessMap = new Dictionary<TGenome, double>();

            // TODO: Implement selection based on calculating the adjusted fitness for each genome compared to genomes in its species

            foreach (var specie in SpecieList)
            {
                for (int genomeIdx = 0; genomeIdx < specie.GenomeList.Count; genomeIdx++)
                {
                    // Add adjusted fitness and the genome reference to the map (dictionary)
                    //adjustedFitnessMap.Add(specie.CalcGenomeAdjustedFitness(genomeIdx), specie.GenomeList[genomeIdx]);
                    adjustedFitnessMap.Add(specie.GenomeList[genomeIdx], specie.CalcGenomeAdjustedFitness(genomeIdx));
                }
            }

            // Sort in ascending order (lowest adjusted fitness first)
            //            List<KeyValuePair<double, TGenome>> sortedAdjFitnessList = adjustedFitnessMap.OrderBy(i => i.Key);

            var stack = new Stack<KeyValuePair<TGenome, double>>(adjustedFitnessMap.OrderByDescending(i => i.Value));

            for (int curRemoveIdx = 0; curRemoveIdx < numGenomesToRemove; curRemoveIdx++)
            {
                // Add genome to remove
                genomesToRemove.Add(stack.Pop().Key);
            }

            
            // TODO: Need to remove this code once alternate method is implemented
            //            TGenome genomeToRemove = null;
            //            const double curLowFitness = Double.MaxValue;
            //
            //            for (int cnt = 0; cnt < sampleSize; cnt++)
            //            {
            //                TGenome candidateGenome = GenomeList[RandomNumGenerator.Next(0, GenomeList.Count - 1)];
            //
            //                if (candidateGenome.EvaluationInfo.Fitness < curLowFitness)
            //                {
            //                    genomeToRemove = candidateGenome;
            //                }
            //            }

            return genomesToRemove;            
        }
    }
}
