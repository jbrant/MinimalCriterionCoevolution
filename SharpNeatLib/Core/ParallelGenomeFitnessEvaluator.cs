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
using SharpNeat.Loggers;
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
        #region Evaluation delegates

        /// <summary>
        ///     The delegate for population evaluation.
        /// </summary>
        /// <param name="genomeList"></param>
        private delegate void EvaluationMethod(IList<TGenome> genomeList, uint currentGeneration);

        #endregion

        #region Private Instance fields

        private readonly bool _enablePhenomeCaching;
        private readonly EvaluationMethod _evalMethod;
        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly ParallelOptions _parallelOptions;
        private readonly IPhenomeEvaluator<TPhenome, FitnessInfo> _phenomeEvaluator;
        private readonly IDataLogger _evaluationLogger;
        private readonly bool _decodeGenomeToXml;

        #endregion

        #region Constructors

        /// <summary>
        ///     Construct with the provided IGenomeDecoder and IPhenomeEvaluator.
        ///     Phenome caching is enabled by default.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public ParallelGenomeFitnessEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, FitnessInfo> phenomeEvaluator, IDataLogger evaluationLogger = null,
            bool decodeGenomeToXml = false)
            : this(genomeDecoder, phenomeEvaluator, new ParallelOptions(), true, evaluationLogger, decodeGenomeToXml)
        {
        }

        /// <summary>
        ///     Construct with the provided IGenomeDecoder, IPhenomeEvaluator and ParalleOptions.
        ///     Phenome caching is enabled by default.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public ParallelGenomeFitnessEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, FitnessInfo> phenomeEvaluator,
            ParallelOptions options, IDataLogger evaluationLogger = null, bool decodeGenomeToXml = false)
            : this(genomeDecoder, phenomeEvaluator, options, true, evaluationLogger, decodeGenomeToXml)
        {
        }

        /// <summary>
        ///     Construct with the provided IGenomeDecoder, IPhenomeEvaluator, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        public ParallelGenomeFitnessEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, FitnessInfo> phenomeEvaluator,
            ParallelOptions options,
            bool enablePhenomeCaching,
            IDataLogger evaluationLogger = null, bool decodeGenomeToXml = false)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _parallelOptions = options;
            _enablePhenomeCaching = enablePhenomeCaching;
            _evaluationLogger = evaluationLogger;
            _decodeGenomeToXml = decodeGenomeToXml;

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
        ///     Initializes state variables in the genome evalutor (primarily the logger).
        /// </summary>
        public void Initialize()
        {
            // Open the logger
            _evaluationLogger?.Open();

            // Defer to phenome initialization
            _phenomeEvaluator.Initialize(_evaluationLogger);
        }

        /// <summary>
        ///     Cleans up evaluator state after end of execution or upon execution interruption.  In particular, this closes out
        ///     any existing evaluation logger instance.
        /// </summary>
        public void Cleanup()
        {
            _evaluationLogger?.Close();
        }

        /// <summary>
        ///     Update the genome evaluator based on some characteristic of the given population.
        /// </summary>
        /// <param name="population">The current population.</param>
        public void Update(List<TGenome> population)
        {
            _phenomeEvaluator.Update(population);
        }

        /// <summary>
        ///     Updates the environment or other evaluation criteria against which the evaluated genomes are being compared.  This
        ///     is typically used in a coevoluationary context.
        /// </summary>
        /// <param name="comparisonPhenomes">The phenomes against which the evaluation is being carried out.</param>
        public void UpdateEvaluationBaseline(IEnumerable<object> comparisonPhenomes)
        {
            _phenomeEvaluator.UpdateEvaluatorPhenotypes(comparisonPhenomes);
        }

        /// <summary>
        ///     Decodes a list of genomes to their corresponding phenotypes.
        /// </summary>
        /// <param name="genomeList">The list of genomes to decode.</param>
        /// <returns>The decoded phenomes.</returns>
        public IEnumerable<object> DecodeGenomes(IList<TGenome> genomeList)
        {
            return EvaluationUtils<TGenome, TPhenome>.DecodeGenomes(genomeList, _genomeDecoder);
        }

        /// <summary>
        ///     Evaluates a list of genomes. Here we decode each genome in using the contained IGenomeDecoder
        ///     and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
        /// </summary>
        /// <param name="genomeList">The list of genomes under evaluation.</param>
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty (this isn't applicable for pure fitness evaluation).
        /// </param>
        /// <param name="enableBridging">Determines whether bridging is enabled on genome evaluations.</param>
        public void Evaluate(IList<TGenome> genomeList, uint currentGeneration, bool runSimulation = true,
            bool enableBridging = false)
        {
            _evalMethod(genomeList, currentGeneration);
        }

        /// <summary>
        ///     Evalutes a single genome alone and against a list of other genomes.
        /// </summary>
        /// <param name="genomesToEvaluate">The genomes under evaluation.</param>
        /// <param name="population">The genomes against which to evaluate.</param>
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty (this isn't applicable for pure fitness evaluation).
        /// </param>
        /// <param name="enableBridging">Determines whether bridging is enabled on genome evaluations.</param>
        public void Evaluate(IList<TGenome> genomesToEvaluate, IList<TGenome> population, uint currentGeneration,
            bool runSimulation = true, bool enableBridging = false)
        {
            _evalMethod(genomesToEvaluate, currentGeneration);
        }

        /// <summary>
        ///     Returns ParallelGenomeFitnessEvaluator loggable elements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable elements for ParallelGenomeFitnessEvaluator.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return _phenomeEvaluator.GetLoggableElements(logFieldEnableMap);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Main genome evaluation loop with no phenome caching (decode on each loop).
        /// </summary>
        private void Evaluate_NonCaching(IList<TGenome> genomeList, uint currentGeneration)
        {
            Parallel.ForEach(genomeList, _parallelOptions,
                delegate(TGenome genome)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateFitness_NonCaching(genome, _genomeDecoder,
                        _phenomeEvaluator, currentGeneration, false, _evaluationLogger, _decodeGenomeToXml);
                });
        }

        /// <summary>
        ///     Main genome evaluation loop with phenome caching (decode only if no cached phenome is present
        ///     from a previous decode).
        /// </summary>
        private void Evaluate_Caching(IList<TGenome> genomeList, uint currentGeneration)
        {
            Parallel.ForEach(genomeList, _parallelOptions,
                delegate(TGenome genome)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateFitness_Caching(genome, _genomeDecoder, _phenomeEvaluator,
                        currentGeneration, false, _evaluationLogger, _decodeGenomeToXml);
                });
        }

        #endregion
    }
}