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
using System.Globalization;
using System.Reflection;
using log4net;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.SpeciationStrategies;
using SharpNeat.Utility;

#endregion

// Disable missing comment warnings for non-private variables.
#pragma warning disable 1591

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    ///     Abstract class providing some common/baseline data and methods for implementions of INeatEvolutionAlgorithm.
    /// </summary>
    /// <typeparam name="TGenome">The genome type that the algorithm will operate on.</typeparam>
    public abstract class AbstractNeatEvolutionAlgorithm<TGenome> : AbstractEvolutionAlgorithm<TGenome>,
        INeatEvolutionAlgorithm<TGenome>, ILoggable
        where TGenome : class, IGenome<TGenome>
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Logging Methods

        /// <summary>
        ///     Returns AbstractNeatEvolutionAlgorithm LoggableElements.
        /// </summary>
        /// <returns>The LoggableElements for AbstractNeatEvolutionAlgorithm.</returns>
        public List<LoggableElement> GetLoggableElements()
        {
            return new List<LoggableElement>
            {
                new LoggableElement("AbstractNeatEvolutionAlgorithm - Specie Count",
                    Convert.ToString(SpecieList.Count, CultureInfo.InvariantCulture))
            };
        }

        #endregion

        #region Base Constructor

        /// <summary>
        ///     Abstract paramaterless constructor.
        /// </summary>
        protected AbstractNeatEvolutionAlgorithm()
        {
            EaParams = new NeatEvolutionAlgorithmParameters();
            EaParamsComplexifying = EaParams;
            EaParamsSimplifying = EaParams.CreateSimplifyingParameters();
            Statistics = new NeatAlgorithmStats(EaParams);
            ComplexityRegulationMode = ComplexityRegulationMode.Complexifying;
        }

        /// <summary>
        ///     Abstract constructor accepting custom NEAT parameters.
        /// </summary>
        protected AbstractNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams)
        {
            EaParams = eaParams;
            EaParamsComplexifying = EaParams;
            EaParamsSimplifying = EaParams.CreateSimplifyingParameters();
            Statistics = new NeatAlgorithmStats(EaParams);
            ComplexityRegulationMode = ComplexityRegulationMode.Complexifying;
        }

        #endregion

        #region Helper methods

        /// <summary>
        ///     Updates _currentBestGenome and _bestSpecieIdx, these are the fittest genome and index of the specie
        ///     containing the fittest genome respectively.
        ///     This method assumes that all specie genomes are sorted fittest first and can therefore save much work
        ///     by not having to scan all genomes.
        ///     Note. We may have several genomes with equal best fitness, we just select one of them in that case.
        /// </summary>
        protected void UpdateBestGenome()
        {
            // If all genomes have the same fitness (including zero) then we simply return the first genome.
            TGenome bestGenome = null;
            var bestFitness = -1.0;
            var bestSpecieIdx = -1;

            var count = SpecieList.Count;
            for (var i = 0; i < count; i++)
            {
                // Get the specie's first genome. Genomes are sorted, therefore this is also the fittest 
                // genome in the specie.
                var genome = SpecieList[i].GenomeList[0];
                if (genome.EvaluationInfo.Fitness > bestFitness)
                {
                    bestGenome = genome;
                    bestFitness = genome.EvaluationInfo.Fitness;
                    bestSpecieIdx = i;
                }
            }

            CurrentChampGenome = bestGenome;
            BestSpecieIndex = bestSpecieIdx;
        }

        /// <summary>
        ///     Updates the NeatAlgorithmStats object.
        /// </summary>
        protected void UpdateStats()
        {
            Statistics._generation = CurrentGeneration;
            Statistics._totalEvaluationCount = GenomeEvaluator.EvaluationCount;

            // Evaluation per second.
            var now = DateTime.Now;
            var duration = now - Statistics._evalsPerSecLastSampleTime;

            // To smooth out the evals per sec statistic we only update if at least 1 second has elapsed 
            // since it was last updated.
            if (duration.Ticks > 9999)
            {
                var evalsSinceLastUpdate =
                    (long) (GenomeEvaluator.EvaluationCount - Statistics._evalsCountAtLastUpdate);
                Statistics._evaluationsPerSec = (int) ((evalsSinceLastUpdate*1e7)/duration.Ticks);

                // Reset working variables.
                Statistics._evalsCountAtLastUpdate = GenomeEvaluator.EvaluationCount;
                Statistics._evalsPerSecLastSampleTime = now;
            }

            // Fitness and complexity stats.
            var totalFitness = GenomeList[0].EvaluationInfo.Fitness;
            var totalComplexity = GenomeList[0].Complexity;
            var maxComplexity = totalComplexity;

            var count = GenomeList.Count;
            for (var i = 1; i < count; i++)
            {
                totalFitness += GenomeList[i].EvaluationInfo.Fitness;
                totalComplexity += GenomeList[i].Complexity;
                maxComplexity = Math.Max(maxComplexity, GenomeList[i].Complexity);
            }

            Statistics._maxFitness = CurrentChampGenome.EvaluationInfo.Fitness;
            Statistics._meanFitness = totalFitness/count;

            Statistics._maxComplexity = maxComplexity;
            Statistics._meanComplexity = totalComplexity/count;

            // Specie champs mean fitness.
            var totalSpecieChampFitness = SpecieList[0].GenomeList[0].EvaluationInfo.Fitness;
            var specieCount = SpecieList.Count;
            for (var i = 1; i < specieCount; i++)
            {
                totalSpecieChampFitness += SpecieList[i].GenomeList[0].EvaluationInfo.Fitness;
            }
            Statistics._meanSpecieChampFitness = totalSpecieChampFitness/specieCount;

            // Moving averages.
            Statistics._prevBestFitnessMA = Statistics._bestFitnessMA.Mean;
            Statistics._bestFitnessMA.Enqueue(Statistics._maxFitness);

            Statistics._prevMeanSpecieChampFitnessMA = Statistics._meanSpecieChampFitnessMA.Mean;
            Statistics._meanSpecieChampFitnessMA.Enqueue(Statistics._meanSpecieChampFitness);

            Statistics._prevComplexityMA = Statistics._complexityMA.Mean;
            Statistics._complexityMA.Enqueue(Statistics._meanComplexity);
        }

        /// <summary>
        ///     Sorts the genomes within each species fittest first, secondary sorts on age.
        /// </summary>
        protected void SortSpecieGenomes()
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
        ///     Clear the genome list within each specie.
        /// </summary>
        protected void ClearAllSpecies()
        {
            foreach (Specie<TGenome> specie in SpecieList)
            {
                specie.GenomeList.Clear();
            }
        }

        /// <summary>
        ///     Rebuild _genomeList from genomes held within the species.
        /// </summary>
        protected void RebuildGenomeList()
        {
            GenomeList.Clear();
            foreach (Specie<TGenome> specie in SpecieList)
            {
                ((List<TGenome>) GenomeList).AddRange(specie.GenomeList);
            }
        }

        #endregion

        #region INeatEvolutionAlgorithm<TGenome> Members

        /// <summary>
        ///     Gets the algorithm statistics object.
        /// </summary>
        public NeatAlgorithmStats Statistics { get; protected set; }

        /// <summary>
        ///     Gets the current complexity regulation mode.
        /// </summary>
        public ComplexityRegulationMode ComplexityRegulationMode { get; protected set; }

        /// <summary>
        ///     Gets a list of all current species. The genomes contained within the species are the same genomes
        ///     available through the GenomeList property.
        /// </summary>
        public IList<Specie<TGenome>> SpecieList { get; protected set; }

        #endregion

        #region Instance fields

        /// <summary>
        ///     Parameters for NEAT evolutionary algorithm control (mutation rate, crossover rate, etc.).
        /// </summary>
        protected NeatEvolutionAlgorithmParameters EaParams;

        /// <summary>
        ///     EA Parameters for complexification.
        /// </summary>
        protected NeatEvolutionAlgorithmParameters EaParamsComplexifying;

        /// <summary>
        ///     EA Parameters for simplification.
        /// </summary>
        protected NeatEvolutionAlgorithmParameters EaParamsSimplifying;

        /// <summary>
        ///     The speciation strategy.
        /// </summary>
        protected ISpeciationStrategy<TGenome> SpeciationStrategy;

        /// <summary>
        ///     The complexity regulation strategy (for simplifying networks).
        /// </summary>
        protected IComplexityRegulationStrategy ComplexityRegulationStrategy;

        /// <summary>
        ///     Index of the specie that contains _currentBestGenome.
        /// </summary>
        protected int BestSpecieIndex;

        /// <summary>
        ///     Random number generator.
        /// </summary>
        protected readonly FastRandom RandomNumGenerator = new FastRandom();

        #endregion

        #region Initialization Methods

        /// <summary>
        ///     Initializes the evolution algorithm with the provided IGenomeFitnessEvaluator, IGenomeFactory
        ///     and an initial population of genomes.
        /// </summary>
        /// <param name="genomeFitnessEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">
        ///     The factory that was used to create the genomeList and which is therefore referenced by the
        ///     genomes.
        /// </param>
        /// <param name="genomeList">An initial genome population.</param>
        /// <param name="abstractNoveltyArchive">The cross-generational archive of high-performing/novel genomes (optional).</param>
        public override void Initialize(IGenomeEvaluator<TGenome> genomeFitnessEvaluator,
            IGenomeFactory<TGenome> genomeFactory,
            List<TGenome> genomeList,
            AbstractNoveltyArchive<TGenome> abstractNoveltyArchive = null)
        {
            base.Initialize(genomeFitnessEvaluator, genomeFactory, genomeList, abstractNoveltyArchive);
            Initialize();
        }

        /// <summary>
        ///     Initializes the evolution algorithm with the provided IGenomeFitnessEvaluator
        ///     and an IGenomeFactory that can be used to create an initial population of genomes.
        /// </summary>
        /// <param name="genomeFitnessEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">
        ///     The factory that was used to create the genomeList and which is therefore referenced by the
        ///     genomes.
        /// </param>
        /// <param name="populationSize">The number of genomes to create for the initial population.</param>
        /// <param name="abstractNoveltyArchive">The cross-generational archive of high-performing/novel genomes (optional).</param>
        public override void Initialize(IGenomeEvaluator<TGenome> genomeFitnessEvaluator,
            IGenomeFactory<TGenome> genomeFactory,
            int populationSize,
            AbstractNoveltyArchive<TGenome> abstractNoveltyArchive = null)
        {
            base.Initialize(genomeFitnessEvaluator, genomeFactory, populationSize, abstractNoveltyArchive);
            Initialize();
        }

        /// <summary>
        ///     Code common to both public Initialize methods.
        /// </summary>
        protected virtual void Initialize()
        {
            // Evaluate the genomes.
            GenomeEvaluator.Evaluate(GenomeList);

            // Speciate the genomes.
            SpecieList = SpeciationStrategy.InitializeSpeciation(GenomeList, EaParams.SpecieCount);
            Debug.Assert(!SpeciationUtils<TGenome>.TestEmptySpecies(SpecieList),
                "Speciation resulted in one or more empty species.");

            // Sort the genomes in each specie fittest first, secondary sort youngest first.
            SortSpecieGenomes();

            // Store ref to best genome.
            UpdateBestGenome();

            // Open the logger
            EvolutionLogger?.Open();

            // Write out the header
            EvolutionLogger?.LogHeader(GetLoggableElements(), Statistics.GetLoggableElements(),
                (CurrentChampGenome as NeatGenome)?.GetLoggableElements());
        }

        #endregion
    }
}