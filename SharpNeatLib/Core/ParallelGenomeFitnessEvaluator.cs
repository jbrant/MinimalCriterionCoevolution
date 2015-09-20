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

using System.Collections.Generic;
using System.Threading.Tasks;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     A concrete implementation of IGenomeFitnessEvaluator that evaulates genomes independently of each
    ///     other and in parallel (on multiple execution threads).
    ///     Genome decoding is performed by a provided IGenomeDecoder.
    ///     Phenome evaluation is performed by a provided IPhenomeEvaluator.
    /// </summary>
    public class ParallelGenomeFitnessEvaluator<TGenome, TPhenome> : IGenomeEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        private readonly bool _enablePhenomeCaching;
        private readonly EvaluationMethod _evalMethod;
        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly ParallelOptions _parallelOptions;
        private readonly IPhenomeEvaluator<TPhenome, FitnessInfo> _phenomeEvaluator;

        private delegate void EvaluationMethod(IList<TGenome> genomeList);

        #region Constructors

        /// <summary>
        ///     Construct with the provided IGenomeDecoder and IPhenomeEvaluator.
        ///     Phenome caching is enabled by default.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public ParallelGenomeFitnessEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, FitnessInfo> phenomeEvaluator)
            : this(genomeDecoder, phenomeEvaluator, new ParallelOptions(), true)
        {
        }

        /// <summary>
        ///     Construct with the provided IGenomeDecoder, IPhenomeEvaluator and ParalleOptions.
        ///     Phenome caching is enabled by default.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public ParallelGenomeFitnessEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, FitnessInfo> phenomeEvaluator,
            ParallelOptions options)
            : this(genomeDecoder, phenomeEvaluator, options, true)
        {
        }

        /// <summary>
        ///     Construct with the provided IGenomeDecoder, IPhenomeEvaluator, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        public ParallelGenomeFitnessEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, FitnessInfo> phenomeEvaluator,
            ParallelOptions options,
            bool enablePhenomeCaching)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _parallelOptions = options;
            _enablePhenomeCaching = enablePhenomeCaching;

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
        ///     Evalutes a single genome alone and against a list of other genomes.
        /// </summary>
        /// <param name="genomesToEvaluate">The genomes under evaluation.</param>
        /// <param name="population">The genomes against which to evaluate.</param>
        public void Evaluate(IList<TGenome> genomesToEvaluate, IList<TGenome> population)
        {
            _evalMethod(genomesToEvaluate);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Main genome evaluation loop with no phenome caching (decode on each loop).
        /// </summary>
        private void Evaluate_NonCaching(IList<TGenome> genomeList)
        {
            Parallel.ForEach(genomeList, _parallelOptions,
                delegate(TGenome genome)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateFitness_NonCaching(genome, _genomeDecoder,
                        _phenomeEvaluator);
                });
        }

        /// <summary>
        ///     Main genome evaluation loop with phenome caching (decode only if no cached phenome is present
        ///     from a previous decode).
        /// </summary>
        private void Evaluate_Caching(IList<TGenome> genomeList)
        {
            Parallel.ForEach(genomeList, _parallelOptions,
                delegate(TGenome genome)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateFitness_Caching(genome, _genomeDecoder, _phenomeEvaluator);
                });
        }

        #endregion
    }
}