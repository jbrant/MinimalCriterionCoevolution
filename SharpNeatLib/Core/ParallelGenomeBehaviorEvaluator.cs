/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2012 Colin Green (sharpneat@gmail.com)
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
using System.Threading.Tasks;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     A concrete implementation of IGenomeFitnessEvaluator that evaulates genome's phenotypic behaviors independently of
    ///     each other and in parallel (on multiple execution threads).
    ///     Genome decoding is performed by a provided IGenomeDecoder.
    ///     Phenome evaluation is performed by a provided IPhenomeEvaluator.
    /// </summary>
    public class ParallelGenomeBehaviorEvaluator<TGenome, TPhenome> : IGenomeEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        #region Lock objects

        private readonly Object _archiveEvaluationLock = new object();

        #endregion

        #region Private Instance fields

        private readonly AbstractNoveltyArchive<TGenome> _noveltyArchive;
        private readonly bool _enablePhenomeCaching;
        private readonly PopulationEvaluationMethod _populationEvaluationMethod;
        private readonly BatchEvaluationMethod _batchEvaluationMethod;
        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly int _nearestNeighbors;
        private readonly ParallelOptions _parallelOptions;
        private readonly IPhenomeEvaluator<TPhenome, BehaviorInfo> _phenomeEvaluator;

        #endregion

        #region Delegates for population (generational) and batch (steady state) based evaluations

        /// <summary>
        ///     Delegate for population (generational) evaluation.
        /// </summary>
        /// <param name="genomeList">The list of genomes (population) to evaluate.</param>
        private delegate void PopulationEvaluationMethod(IList<TGenome> genomeList);

        /// <summary>
        ///     Delegate for batch (steady-state) evaluation.
        /// </summary>
        /// <param name="genomesToEvaluate">The list of genomes (batch) to evaluate.</param>
        /// <param name="population">The population of genomes against which the batch of genomes are being evaluated.</param>
        private delegate void BatchEvaluationMethod(IList<TGenome> genomesToEvaluate, IList<TGenome> population);

        #endregion

        #region Constructors

        /// <summary>
        ///     Construct with the provided IGenomeDecoder and IPhenomeEvaluator.
        ///     Phenome caching is enabled by default.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public ParallelGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator, int nearestNeighbors,
            AbstractNoveltyArchive<TGenome> archive = null)
            : this(genomeDecoder, phenomeEvaluator, new ParallelOptions(), true, nearestNeighbors, archive)
        {
        }

        /// <summary>
        ///     Construct with the provided IGenomeDecoder, IPhenomeEvaluator and ParalleOptions.
        ///     Phenome caching is enabled by default.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public ParallelGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator,
            ParallelOptions options, int nearestNeighbors, AbstractNoveltyArchive<TGenome> archive = null)
            : this(genomeDecoder, phenomeEvaluator, options, true, nearestNeighbors, archive)
        {
        }

        /// <summary>
        ///     Construct with the provided IGenomeDecoder, IPhenomeEvaluator, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        private ParallelGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator,
            ParallelOptions options, bool enablePhenomeCaching, int nearestNeighbors,
            AbstractNoveltyArchive<TGenome> archive = null)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _parallelOptions = options;
            _enablePhenomeCaching = enablePhenomeCaching;
            _nearestNeighbors = nearestNeighbors;
            _noveltyArchive = archive;

            // Determine the appropriate evaluation method.
            if (_enablePhenomeCaching)
            {
                _populationEvaluationMethod = EvaluateAllBehaviors_Caching;
                _batchEvaluationMethod = EvaluateBatchBehaviors_Caching;
            }
            else
            {
                _populationEvaluationMethod = EvaluateAllBehaviors_NonCaching;
                _batchEvaluationMethod = EvaluateBatchBehaviors_NonCaching;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        public ulong EvaluationCount => _phenomeEvaluator.EvaluationCount;

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that
        ///     the the evolutionary algorithm/search should stop. This property's value can remain false
        ///     to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied => _phenomeEvaluator.StopConditionSatisfied;

        #endregion

        #region Public Evaluate and Reset methods

        /// <summary>
        ///     Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
            _phenomeEvaluator.Reset();
        }

        /// <summary>
        ///     Evaluates a list of genomes. Here we decode each genome in using the contained IGenomeDecoder
        ///     and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
        /// </summary>
        /// <param name="genomeList">The list of genomes under evaluation.</param>
        public void Evaluate(IList<TGenome> genomeList)
        {
            _populationEvaluationMethod(genomeList);
        }

        /// <summary>
        ///     Evalutes a single genome alone and against a list of other genomes.
        /// </summary>
        /// <param name="genomesToEvaluate">The list of genomes under evaluation.</param>
        /// <param name="population">The genomes against which to evaluate.</param>
        public void Evaluate(IList<TGenome> genomesToEvaluate, IList<TGenome> population)
        {
            _batchEvaluationMethod(genomesToEvaluate, population);
        }

        #endregion

        #region Private Evaluation Wrapper methods

        /// <summary>
        ///     Main genome evaluation loop with no phenome caching (decode on each loop).
        /// </summary>
        private void EvaluateAllBehaviors_NonCaching(IList<TGenome> genomeList)
        {
            Parallel.ForEach(genomeList, _parallelOptions,
                delegate(TGenome genome)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_NonCaching(genome, _genomeDecoder,
                        _phenomeEvaluator);
                });

            // After the behavior of each genome in the current population has been evaluated,
            // iterate again through each genome and compare its behavioral novelty (distance)
            // to its k-nearest neighbors in behavior space (and the archive if applicable)
            Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
            {
                EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                    _noveltyArchive);

                // Add the genome to the archive if it qualifies
                lock (_archiveEvaluationLock)
                {
                    _noveltyArchive?.TestAndAddCandidateToArchive(genome);
                }
            });
        }

        /// <summary>
        ///     Evaluates the behavior of a "batch" of genomes against another provided list of genomes (the population) and the
        ///     novelty archive.  This is used in steady-state evaluation of a batch of offspring vs. the existing population.
        ///     Phenotypes for each genome in the batch are decoded upon invocation (no caching).
        /// </summary>
        /// <param name="genomesToEvaluate">The batch of genomes to evaluate.</param>
        /// <param name="population">The population of genomes against which the batch is being evaluated.</param>
        private void EvaluateBatchBehaviors_NonCaching(IList<TGenome> genomesToEvaluate, IList<TGenome> population)
        {
            // Decode and evaluate the behavior of the genomes under evaluation
            Parallel.ForEach(genomesToEvaluate, _parallelOptions,
                delegate(TGenome genome)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_NonCaching(genome, _genomeDecoder,
                        _phenomeEvaluator);
                });

            // After the behavior of each genome in the offspring batch has been evaluated,
            // iterate through each genome and compare its behavioral novelty (distance) to its 
            // k -nearest neighbors from the population in behavior space (and the archive if applicable)
            Parallel.ForEach(genomesToEvaluate, _parallelOptions, delegate(TGenome genome)
            {
                EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                    _noveltyArchive);

                // Add the genome to the archive if it qualifies
                lock (_archiveEvaluationLock)
                {
                    _noveltyArchive?.TestAndAddCandidateToArchive(genome);
                }
            });
        }

        /// <summary>
        ///     Main genome evaluation loop with phenome caching (decode only if no cached phenome is present
        ///     from a previous decode).
        /// </summary>
        private void EvaluateAllBehaviors_Caching(IList<TGenome> genomeList)
        {
            Parallel.ForEach(genomeList, _parallelOptions,
                delegate(TGenome genome)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_Caching(genome, _genomeDecoder,
                        _phenomeEvaluator);
                });

            // After the behavior of each genome in the current population has been evaluated,
            // iterate again through each genome and compare its behavioral novelty (distance)
            // to its k-nearest neighbors in behavior space (and the archive if applicable)
            Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
            {
                EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                    _noveltyArchive);

                // Add the genome to the archive if it qualifies
                lock (_archiveEvaluationLock)
                {
                    _noveltyArchive?.TestAndAddCandidateToArchive(genome);
                }
            });
        }

        /// <summary>
        ///     Evaluates the behavior of a "batch" of genomes against another provided list of genomes (the population) and the
        ///     novelty archive.  This is used in steady-state evaluation of a batch of offspring vs. the existing population.  We
        ///     first try to retrieve the phenome from each batch genome's cache and then decode the genome if it has not yet been
        ///     cached.
        /// </summary>
        /// <param name="genomesToEvaluate">The batch of genomes to evaluate.</param>
        /// <param name="population">The population of genomes against which the batch is being evaluated.</param>
        private void EvaluateBatchBehaviors_Caching(IList<TGenome> genomesToEvaluate, IList<TGenome> population)
        {
            // Decode and evaluate the behavior of the genomes under evaluation
            Parallel.ForEach(genomesToEvaluate, _parallelOptions,
                delegate(TGenome genome)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_Caching(genome, _genomeDecoder,
                        _phenomeEvaluator);
                });

            // After the behavior of each genome in the offspring batch has been evaluated,
            // iterate through each genome and compare its behavioral novelty (distance) to its 
            // k -nearest neighbors from the population in behavior space (and the archive if applicable)
            Parallel.ForEach(genomesToEvaluate, _parallelOptions, delegate(TGenome genome)
            {
                EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                    _noveltyArchive);

                // Add the genome to the archive if it qualifies
                lock (_archiveEvaluationLock)
                {
                    _noveltyArchive?.TestAndAddCandidateToArchive(genome);
                }
            });
        }

        #endregion
    }
}