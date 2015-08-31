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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpNeat.Utility;

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
        private readonly EliteArchive<TGenome> _eliteArchive;
        private readonly bool _enablePhenomeCaching;
        private readonly EvaluationMethod _evalMethod;
        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly int _nearestNeighbors;
        private readonly ParallelOptions _parallelOptions;
        private readonly IPhenomeEvaluator<TPhenome, BehaviorInfo> _phenomeEvaluator;

        private delegate void EvaluationMethod(IList<TGenome> genomeList);

        private Object _archiveEvaluationLock = new object();

        #region Constructors

        /// <summary>
        ///     Construct with the provided IGenomeDecoder and IPhenomeEvaluator.
        ///     Phenome caching is enabled by default.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public ParallelGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator, int nearestNeighbors,
            EliteArchive<TGenome> archive = null)
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
            ParallelOptions options, int nearestNeighbors, EliteArchive<TGenome> archive = null)
            : this(genomeDecoder, phenomeEvaluator, options, true, nearestNeighbors, archive)
        {
        }

        /// <summary>
        ///     Construct with the provided IGenomeDecoder, IPhenomeEvaluator, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        private ParallelGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator,
            ParallelOptions options, bool enablePhenomeCaching, int nearestNeighbors,
            EliteArchive<TGenome> archive = null)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _parallelOptions = options;
            _enablePhenomeCaching = enablePhenomeCaching;
            _nearestNeighbors = nearestNeighbors;
            _eliteArchive = archive;

            // Determine the appropriate evaluation method.
            if (_enablePhenomeCaching)
            {
                _evalMethod = Evaluate_Caching;
            }
            else
            {
                _evalMethod = Evaluate_NonCaching;
            }
        }

        #endregion

        #region IGenomeFitnessEvaluator<TGenome> Members

        /// <summary>
        ///     Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        public ulong EvaluationCount
        {
            get { return _phenomeEvaluator.EvaluationCount; }
        }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that
        ///     the the evolutionary algorithm/search should stop. This property's value can remain false
        ///     to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied
        {
            get { return _phenomeEvaluator.StopConditionSatisfied; }
        }

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
            _evalMethod(genomeList);
        }

        /// <summary>
        /// Evalutes a single genome alone and against a list of other genomes.
        /// </summary>
        /// <param name="genome">The genome under evaluation.</param>
        /// <param name="genomeList">The genomes against which to evaluate.</param>
        public void Evaluate(TGenome genome, IList<TGenome> genomeList)
        {
            // TODO: Need to implement this
            throw new NotImplementedException();
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        ///     Main genome evaluation loop with no phenome caching (decode on each loop).
        /// </summary>
        private void Evaluate_NonCaching(IList<TGenome> genomeList)
        {
            Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
            {
                var phenome = _genomeDecoder.Decode(genome);
                if (null == phenome)
                {
                    // Non-viable genome.
                    genome.EvaluationInfo.SetFitness(0.0);
                    genome.EvaluationInfo.AuxFitnessArr = null;
                    genome.EvaluationInfo.BehaviorCharacterization = new double[0];
                }
                else
                {
                    // EvaluateFitness the behavior and update the genome's behavior characterization
                    var behaviorInfo = _phenomeEvaluator.Evaluate(phenome);
                    genome.EvaluationInfo.BehaviorCharacterization = behaviorInfo.Behaviors;
                }
            });

            // After the behavior of each genome in the current population has been evaluated,
            // iterate again through each genome and compare its behavioral novelty (distance)
            // to its k-nearest neighbors in behavior space (and the archive if applicable)
            Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
            {
                // Compare the current genome's behavior to its k-nearest neighbors in behavior space
                var fitness =
                    BehaviorUtils<TGenome>.CalculateBehavioralDistance(genome.EvaluationInfo.BehaviorCharacterization,
                        genomeList, _nearestNeighbors, _eliteArchive);

                // Update the fitness as the behavioral novelty
                var fitnessInfo = new FitnessInfo(fitness, fitness);
                genome.EvaluationInfo.SetFitness(fitnessInfo._fitness);
                genome.EvaluationInfo.AuxFitnessArr = fitnessInfo._auxFitnessArr;

                // Add the genome to the archive if it qualifies
                lock (_archiveEvaluationLock)
                {
                    _eliteArchive?.TestAndAddCandidateToArchive(genome);
                }
            });
        }

        /// <summary>
        ///     Main genome evaluation loop with phenome caching (decode only if no cached phenome is present
        ///     from a previous decode).
        /// </summary>
        private void Evaluate_Caching(IList<TGenome> genomeList)
        {
            Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
            {
                var phenome = (TPhenome) genome.CachedPhenome;
                if (null == phenome)
                {
                    // Decode the phenome and store a ref against the genome.
                    phenome = _genomeDecoder.Decode(genome);
                    genome.CachedPhenome = phenome;
                }

                if (null == phenome)
                {
                    // Non-viable genome.
                    genome.EvaluationInfo.SetFitness(0.0);
                    genome.EvaluationInfo.AuxFitnessArr = null;
                    genome.EvaluationInfo.BehaviorCharacterization = new double[0];
                }
                else
                {
                    // EvaluateFitness the behavior and update the genome's behavior characterization
                    var behaviorInfo = _phenomeEvaluator.Evaluate(phenome);
                    genome.EvaluationInfo.BehaviorCharacterization = behaviorInfo.Behaviors;
                }
            });

            // After the behavior of each genome in the current population has been evaluated,
            // iterate again through each genome and compare its behavioral novelty (distance)
            // to its k-nearest neighbors in behavior space (and the archive if applicable)
            Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
            {
                // Compare the current genome's behavior to its k-nearest neighbors in behavior space
                var fitness =
                    BehaviorUtils<TGenome>.CalculateBehavioralDistance(genome.EvaluationInfo.BehaviorCharacterization,
                        genomeList, _nearestNeighbors, _eliteArchive);

                // Update the fitness as the behavioral novelty
                var fitnessInfo = new FitnessInfo(fitness, fitness);
                genome.EvaluationInfo.SetFitness(fitnessInfo._fitness);
                genome.EvaluationInfo.AuxFitnessArr = fitnessInfo._auxFitnessArr;

                // Add the genome to the archive if it qualifies
                lock (_archiveEvaluationLock)
                {
                    _eliteArchive?.TestAndAddCandidateToArchive(genome);
                }              
            });
        }

        #endregion
    }
}