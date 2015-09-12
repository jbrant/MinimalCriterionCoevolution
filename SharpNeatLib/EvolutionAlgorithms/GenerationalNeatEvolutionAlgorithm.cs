/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    ///     Implementation of the generational NEAT evolution algorithm.
    ///     Incorporates:
    ///     - Speciation with fitness sharing.
    ///     - Creating offspring via both sexual and asexual reproduction.
    /// </summary>
    /// <typeparam name="TGenome">The genome type that the algorithm will operate on.</typeparam>
    public class GenerationalNeatEvolutionAlgorithm<TGenome> : AbstractNeatEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        #region Evolution Algorithm Main Method [PerformOneGeneration]

        /// <summary>
        ///     Progress forward by one generation. Perform one generation/iteration of the evolution algorithm.
        /// </summary>
        protected override void PerformOneGeneration()
        {
            // Calculate statistics for each specie (mean fitness, target size, number of offspring to produce etc.)
            int offspringCount;
            SpecieStats[] specieStatsArr = CalcSpecieStats(out offspringCount);

            // Create offspring.
            IList<TGenome> offspringList = CreateOffspring(specieStatsArr, offspringCount);

            // Trim species back to their elite genomes.
            bool emptySpeciesFlag = TrimSpeciesBackToElite(specieStatsArr);

            // Rebuild _genomeList. It will now contain just the elite genomes.
            RebuildGenomeList();

            // Append offspring genomes to the elite genomes in _genomeList. We do this before calling the
            // _genomeListEvaluator.EvaluateFitness because some evaluation schemes re-evaluate the elite genomes 
            // (otherwise we could just evaluate offspringList).
            ((List<TGenome>) GenomeList).AddRange(offspringList);

            // EvaluateFitness genomes.            
            GenomeEvaluator.Evaluate(GenomeList);

            // Integrate offspring into species.
            if (emptySpeciesFlag)
            {
                // We have one or more terminated species. Therefore we need to fully re-speciate all genomes to divide them
                // evenly between the required number of species.

                // Clear all genomes from species (we still have the elite genomes in _genomeList).
                ClearAllSpecies();

                // Speciate genomeList.
                SpeciationStrategy.SpeciateGenomes(GenomeList, SpecieList);
            }
            else
            {
                // Integrate offspring into the existing species. 
                SpeciationStrategy.SpeciateOffspring(offspringList, SpecieList);
            }
            Debug.Assert(!SpeciationUtils<TGenome>.TestEmptySpecies(SpecieList),
                "Speciation resulted in one or more empty species.");

            // Sort the genomes in each specie. Fittest first (secondary sort - youngest first).
            SortSpecieGenomes();

            // Update stats and store reference to best genome.
            UpdateBestGenome();
            UpdateStats();

            // Update the novelty archive parameters (if exists) and reset for next generation
            AbstractNoveltyArchive?.UpdateArchiveParameters();

            // Determine the complexity regulation mode and switch over to the appropriate set of evolution
            // algorithm parameters. Also notify the genome factory to allow it to modify how it creates genomes
            // (e.g. reduce or disable additive mutations).
            ComplexityRegulationMode = ComplexityRegulationStrategy.DetermineMode(Statistics);
            GenomeFactory.SearchMode = (int) ComplexityRegulationMode;
            switch (ComplexityRegulationMode)
            {
                case ComplexityRegulationMode.Complexifying:
                    EaParams = EaParamsComplexifying;
                    break;
                case ComplexityRegulationMode.Simplifying:
                    EaParams = EaParamsSimplifying;
                    break;
            }

            // TODO: More checks.
            Debug.Assert(GenomeList.Count == PopulationSize);
            
            // If there is a logger defined, log the generation stats
            EvolutionLogger?.LogRow(GetLoggableElements(), Statistics.GetLoggableElements(),
                (CurrentChampGenome as NeatGenome)?.GetLoggableElements());
        }

        #endregion

        #region Private Methods [Low Level Helper Methods]

        /// <summary>
        ///     Trims the genomeList in each specie back to the number of elite genomes specified in
        ///     specieStatsArr. Returns true if there are empty species following trimming.
        /// </summary>
        private bool TrimSpeciesBackToElite(SpecieStats[] specieStatsArr)
        {
            bool emptySpeciesFlag = false;
            int count = SpecieList.Count;
            for (int i = 0; i < count; i++)
            {
                Specie<TGenome> specie = SpecieList[i];
                SpecieStats stats = specieStatsArr[i];

                int removeCount = specie.GenomeList.Count - stats.EliteSizeInt;
                specie.GenomeList.RemoveRange(stats.EliteSizeInt, removeCount);

                if (0 == stats.EliteSizeInt)
                {
                    emptySpeciesFlag = true;
                }
            }
            return emptySpeciesFlag;
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs with the default NeatEvolutionAlgorithmParameters, speciation strategy
        ///     (KMeansClusteringStrategy with ManhattanDistanceMetric) and complexity regulation strategy
        ///     (NullComplexityRegulationStrategy).
        /// </summary>
        /// <param name="logger">The data logger (optional).</param>
        public GenerationalNeatEvolutionAlgorithm(IDataLogger logger = null)
            : this(
                new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric()),
                new NullComplexityRegulationStrategy(), logger)
        {
        }

        /// <summary>
        ///     Constructs with a custom NeatEvolutionAlgorithmParameters and an optional IDataLogger, using the default speciation
        ///     strategy (KMeansClusteringStrategy with ManhattanDistanceMetric) and default complexity regulation strategy
        ///     (NullComplexityRegulationStrategy).
        /// </summary>
        /// <param name="eaParams">The NEAT parameters to use for controlling evolution.</param>
        /// <param name="logger">The data logger (optional).</param>
        public GenerationalNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams, IDataLogger logger = null)
            : this(
                eaParams, new KMeansClusteringStrategy<TGenome>(new ManhattanDistanceMetric()),
                new NullComplexityRegulationStrategy(), logger)
        {
        }

        /// <summary>
        ///     Constructs with a custom speciation strategy and complexity regulation strategy, using the default
        ///     NeatEvolutionAlgorithmParameters.
        /// </summary>
        /// <param name="speciationStrategy">The strategy to use for controlling NEAT speciation.</param>
        /// <param name="complexityRegulationStrategy">The strategy to use for complexing and simplifying NEAT networks.</param>
        /// <param name="logger">The data logger (optional).</param>
        public GenerationalNeatEvolutionAlgorithm(ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            IDataLogger logger = null)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            EvolutionLogger = logger;
        }

        /// <summary>
        ///     Constructs with a custom NeatEvolutionAlgorithmParameters, speciation strategy, and complexity regulation strategy.
        /// </summary>
        /// <param name="eaParams">The NEAT parameters to use for controlling evolution.</param>
        /// <param name="speciationStrategy">The strategy to use for controlling NEAT speciation.</param>
        /// <param name="complexityRegulationStrategy">The strategy to use for complexing and simplifying NEAT networks.</param>
        /// <param name="logger">The data logger (optional).</param>
        public GenerationalNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
            ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy,
            IDataLogger logger = null) : base(eaParams)
        {
            SpeciationStrategy = speciationStrategy;
            ComplexityRegulationStrategy = complexityRegulationStrategy;
            EvolutionLogger = logger;
        }

        #endregion

        #region Private Methods [High Level Algorithm Methods. CalcSpecieStats/CreateOffspring]

        /// <summary>
        ///     Calculate statistics for each specie. This method is at the heart of the evolutionary algorithm,
        ///     the key things that are achieved in this method are - for each specie we calculate:
        ///     1) The target size based on fitness of the specie's member genomes.
        ///     2) The elite size based on the current size. Potentially this could be higher than the target
        ///     size, so a target size is taken to be a hard limit.
        ///     3) Following (1) and (2) we can calculate the total number offspring that need to be generated
        ///     for the current generation.
        /// </summary>
        private SpecieStats[] CalcSpecieStats(out int offspringCount)
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
            {
                // Handle specific case where all genomes/species have a zero fitness. 
                // Assign all species an equal targetSize.
                double targetSizeReal = PopulationSize/(double) specieCount;

                for (int i = 0; i < specieCount; i++)
                {
                    SpecieStats inst = specieStatsArr[i];
                    inst.TargetSizeReal = targetSizeReal;

                    // Stochastic rounding will result in equal allocation if targetSizeReal is a whole
                    // number, otherwise it will help to distribute allocations evenly.
                    inst.TargetSizeInt = (int) Utilities.ProbabilisticRound(targetSizeReal, RandomNumGenerator);

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
                    inst.TargetSizeReal = (inst.MeanFitness/totalMeanFitness)*PopulationSize;

                    // Discretize targetSize (stochastic rounding).
                    inst.TargetSizeInt = (int) Utilities.ProbabilisticRound(inst.TargetSizeReal, RandomNumGenerator);

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
                        probabilities[i] = Math.Max(0.0, inst.TargetSizeReal - inst.TargetSizeInt);
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
                    probabilities[i] = Math.Max(0.0, inst.TargetSizeInt - inst.TargetSizeReal);
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
                {
                    // Scan forward from this specie to find a suitable one.
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
                            throw new SharpNeatException(
                                "CalcSpecieStats(). Error adjusting target population size down. Is the population size less than or equal to the number of species?");
                        }
                    }
                }
            }

            // Now determine the eliteSize for each specie. This is the number of genomes that will remain in a 
            // specie from the current generation and is a proportion of the specie's current size.
            // Also here we calculate the total number of offspring that will need to be generated.
            offspringCount = 0;
            for (int i = 0; i < specieCount; i++)
            {
                // Special case - zero target size.
                if (0 == specieStatsArr[i].TargetSizeInt)
                {
                    specieStatsArr[i].EliteSizeInt = 0;
                    continue;
                }

                // Discretize the real size with a probabilistic handling of the fractional part.
                double eliteSizeReal = SpecieList[i].GenomeList.Count*EaParams.ElitismProportion;
                int eliteSizeInt = (int) Utilities.ProbabilisticRound(eliteSizeReal, RandomNumGenerator);

                // Ensure eliteSizeInt is no larger than the current target size (remember it was calculated 
                // against the current size of the specie not its new target size).
                SpecieStats inst = specieStatsArr[i];
                inst.EliteSizeInt = Math.Min(eliteSizeInt, inst.TargetSizeInt);

                // Ensure the champ specie preserves the champ genome. We do this even if the targetsize is just 1
                // - which means the champ genome will remain and no offspring will be produced from it, apart from 
                // the (usually small) chance of a cross-species mating.
                if (i == BestSpecieIndex && inst.EliteSizeInt == 0)
                {
                    Debug.Assert(inst.TargetSizeInt != 0, "Zero target size assigned to champ specie.");
                    inst.EliteSizeInt = 1;
                }

                // Now we can determine how many offspring to produce for the specie.
                inst.OffspringCount = inst.TargetSizeInt - inst.EliteSizeInt;
                offspringCount += inst.OffspringCount;

                // While we're here we determine the split between asexual and sexual reproduction. Again using 
                // some probabilistic logic to compensate for any rounding bias.
                double offspringAsexualCountReal = inst.OffspringCount*EaParams.OffspringAsexualProportion;
                inst.OffspringAsexualCount =
                    (int) Utilities.ProbabilisticRound(offspringAsexualCountReal, RandomNumGenerator);
                inst.OffspringSexualCount = inst.OffspringCount - inst.OffspringAsexualCount;

                // Also while we're here we calculate the selectionSize. The number of the specie's fittest genomes
                // that are selected from to create offspring. This should always be at least 1.
                double selectionSizeReal = SpecieList[i].GenomeList.Count*EaParams.SelectionProportion;
                inst.SelectionSize = Math.Max(1,
                    (int) Utilities.ProbabilisticRound(selectionSizeReal, RandomNumGenerator));
            }

            return specieStatsArr;
        }

        /// <summary>
        ///     Create the required number of offspring genomes, using specieStatsArr as the basis for selecting how
        ///     many offspring are produced from each species.
        /// </summary>
        private List<TGenome> CreateOffspring(SpecieStats[] specieStatsArr, int offspringCount)
        {
            // Build a RouletteWheelLayout for selecting species for cross-species reproduction.
            // While we're in the loop we also pre-build a RouletteWheelLayout for each specie;
            // Doing this before the main loop means we have RouletteWheelLayouts available for
            // all species when performing cross-specie matings.
            int specieCount = specieStatsArr.Length;
            double[] specieFitnessArr = new double[specieCount];
            RouletteWheelLayout[] rwlArr = new RouletteWheelLayout[specieCount];

            // Count of species with non-zero selection size.
            // If this is exactly 1 then we skip inter-species mating. One is a special case because for 0 the 
            // species all get an even chance of selection, and for >1 we can just select normally.
            int nonZeroSpecieCount = 0;
            for (int i = 0; i < specieCount; i++)
            {
                // Array of probabilities for specie selection. Note that some of these probabilites can be zero, but at least one of them won't be.
                SpecieStats inst = specieStatsArr[i];
                specieFitnessArr[i] = inst.SelectionSize;
                if (0 != inst.SelectionSize)
                {
                    nonZeroSpecieCount++;
                }

                // For each specie we build a RouletteWheelLayout for genome selection within 
                // that specie. Fitter genomes have higher probability of selection.
                List<TGenome> genomeList = SpecieList[i].GenomeList;
                double[] probabilities = new double[inst.SelectionSize];
                for (int j = 0; j < inst.SelectionSize; j++)
                {
                    probabilities[j] = genomeList[j].EvaluationInfo.Fitness;
                }
                rwlArr[i] = new RouletteWheelLayout(probabilities);
            }

            // Complete construction of RouletteWheelLayout for specie selection.
            RouletteWheelLayout rwlSpecies = new RouletteWheelLayout(specieFitnessArr);

            // Produce offspring from each specie in turn and store them in offspringList.
            List<TGenome> offspringList = new List<TGenome>(offspringCount);
            for (int specieIdx = 0; specieIdx < specieCount; specieIdx++)
            {
                SpecieStats inst = specieStatsArr[specieIdx];
                List<TGenome> genomeList = SpecieList[specieIdx].GenomeList;

                // Get RouletteWheelLayout for genome selection.
                RouletteWheelLayout rwl = rwlArr[specieIdx];

                // --- Produce the required number of offspring from asexual reproduction.
                for (int i = 0; i < inst.OffspringAsexualCount; i++)
                {
                    int genomeIdx = RouletteWheel.SingleThrow(rwl, RandomNumGenerator);
                    TGenome offspring = genomeList[genomeIdx].CreateOffspring(CurrentGeneration);
                    offspringList.Add(offspring);
                }
                Statistics._asexualOffspringCount += (ulong) inst.OffspringAsexualCount;

                // --- Produce the required number of offspring from sexual reproduction.
                // Cross-specie mating.
                // If nonZeroSpecieCount is exactly 1 then we skip inter-species mating. One is a special case because
                // for 0 the  species all get an even chance of selection, and for >1 we can just select species normally.
                int crossSpecieMatings = nonZeroSpecieCount == 1
                    ? 0
                    : (int) Utilities.ProbabilisticRound(EaParams.InterspeciesMatingProportion
                                                         *inst.OffspringSexualCount, RandomNumGenerator);
                Statistics._sexualOffspringCount += (ulong) (inst.OffspringSexualCount - crossSpecieMatings);
                Statistics._interspeciesOffspringCount += (ulong) crossSpecieMatings;

                // An index that keeps track of how many offspring have been produced in total.
                int matingsCount = 0;
                for (; matingsCount < crossSpecieMatings; matingsCount++)
                {
                    TGenome offspring = CreateOffspring_CrossSpecieMating(rwl, rwlArr, rwlSpecies, specieIdx, genomeList);
                    offspringList.Add(offspring);
                }

                // For the remainder we use normal intra-specie mating.
                // Test for special case - we only have one genome to select from in the current specie. 
                if (1 == inst.SelectionSize)
                {
                    // Fall-back to asexual reproduction.
                    for (; matingsCount < inst.OffspringSexualCount; matingsCount++)
                    {
                        int genomeIdx = RouletteWheel.SingleThrow(rwl, RandomNumGenerator);
                        TGenome offspring = genomeList[genomeIdx].CreateOffspring(CurrentGeneration);
                        offspringList.Add(offspring);
                    }
                }
                else
                {
                    // Remainder of matings are normal within-specie.
                    for (; matingsCount < inst.OffspringSexualCount; matingsCount++)
                    {
                        // Select parents. SelectRouletteWheelItem() guarantees parent2Idx!=parent1Idx
                        int parent1Idx = RouletteWheel.SingleThrow(rwl, RandomNumGenerator);
                        TGenome parent1 = genomeList[parent1Idx];

                        // Remove selected parent from set of possible outcomes.
                        RouletteWheelLayout rwlTmp = rwl.RemoveOutcome(parent1Idx);
                        if (0.0 != rwlTmp.ProbabilitiesTotal)
                        {
                            // Get the two parents to mate.
                            int parent2Idx = RouletteWheel.SingleThrow(rwlTmp, RandomNumGenerator);
                            TGenome parent2 = genomeList[parent2Idx];
                            TGenome offspring = parent1.CreateOffspring(parent2, CurrentGeneration);
                            offspringList.Add(offspring);
                        }
                        else
                        {
                            // No other parent has a non-zero selection probability (they all have zero fitness).
                            // Fall back to asexual reproduction of the single genome with a non-zero fitness.
                            TGenome offspring = parent1.CreateOffspring(CurrentGeneration);
                            offspringList.Add(offspring);
                        }
                    }
                }
            }

            Statistics._totalOffspringCount += (ulong) offspringCount;
            return offspringList;
        }

        /// <summary>
        ///     Cross specie mating.
        /// </summary>
        /// <param name="rwl">RouletteWheelLayout for selectign genomes in teh current specie.</param>
        /// <param name="rwlArr">Array of RouletteWheelLayout objects for genome selection. One for each specie.</param>
        /// <param name="rwlSpecies">RouletteWheelLayout for selecting species. Based on relative fitness of species.</param>
        /// <param name="currentSpecieIdx">Current specie's index in _specieList</param>
        /// <param name="genomeList">Current specie's genome list.</param>
        private TGenome CreateOffspring_CrossSpecieMating(RouletteWheelLayout rwl,
            RouletteWheelLayout[] rwlArr,
            RouletteWheelLayout rwlSpecies,
            int currentSpecieIdx,
            IList<TGenome> genomeList)
        {
            // Select parent from current specie.
            int parent1Idx = RouletteWheel.SingleThrow(rwl, RandomNumGenerator);

            // Select specie other than current one for 2nd parent genome.
            RouletteWheelLayout rwlSpeciesTmp = rwlSpecies.RemoveOutcome(currentSpecieIdx);
            int specie2Idx = RouletteWheel.SingleThrow(rwlSpeciesTmp, RandomNumGenerator);

            // Select a parent genome from the second specie.
            int parent2Idx = RouletteWheel.SingleThrow(rwlArr[specie2Idx], RandomNumGenerator);

            // Get the two parents to mate.
            TGenome parent1 = genomeList[parent1Idx];
            TGenome parent2 = SpecieList[specie2Idx].GenomeList[parent2Idx];
            return parent1.CreateOffspring(parent2, CurrentGeneration);
        }

        #endregion
    }
}