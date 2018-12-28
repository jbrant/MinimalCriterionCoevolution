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
using SharpNeat.NoveltyArchives;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Core
{
    /// <inheritdoc />
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
        #region Private Instance fields

        private readonly AbstractNoveltyArchive<TGenome> _noveltyArchive;
        private readonly PopulationEvaluationMethod _populationEvaluationMethod;
        private readonly BatchEvaluationMethod _batchEvaluationMethod;
        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly int _nearestNeighbors;
        private readonly ParallelOptions _parallelOptions;
        private readonly IPhenomeEvaluator<TPhenome, BehaviorInfo> _phenomeEvaluator;
        private readonly IDataLogger _evaluationLogger;
        private readonly SearchType _searchType;
        private readonly bool _decodeGenomeToXml;

        #endregion

        #region Delegates for population (generational) and batch (steady state) based evaluations

        /// <summary>
        ///     Delegate for population (generational) evaluation.
        /// </summary>
        /// <param name="genomeList">The list of genomes (population) to evaluate.</param>
        /// <param name="currentGeneration">The current generation for which we're evaluating.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        private delegate void PopulationEvaluationMethod(
            IList<TGenome> genomeList, uint currentGeneration, bool runSimulation);

        /// <summary>
        ///     Delegate for batch (steady-state) evaluation.
        /// </summary>
        /// <param name="genomesToEvaluate">The list of genomes (batch) to evaluate.</param>
        /// <param name="population">The population of genomes against which the batch of genomes are being evaluated.</param>
        /// <param name="currentGeneration">The current generation for which we're evaluating.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        private delegate void BatchEvaluationMethod(
            IList<TGenome> genomesToEvaluate, IList<TGenome> population, uint currentGeneration, bool runSimulation);

        #endregion

        #region Constructors

        /// <inheritdoc />
        /// <summary>
        ///     Construct with the provided IGenomeDecoder and IPhenomeEvaluator.
        ///     Phenome caching is enabled by default and the evaluation logger is optional.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="searchType">The search algorithm type.</param>
        /// <param name="parallelOptions">Controls the number of parallel evaluations.</param>
        /// <param name="evaluationLogger">A reference to the evaluation data logger (optional).</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public ParallelGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator,
            SearchType searchType, ParallelOptions parallelOptions, IDataLogger evaluationLogger = null,
            bool decodeGenomeToXml = false)
            : this(
                genomeDecoder, phenomeEvaluator, searchType, parallelOptions, true, 0, null,
                evaluationLogger, decodeGenomeToXml)
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Construct with the provided IGenomeDecoder and IPhenomeEvaluator.
        ///     Phenome caching is enabled by default.
        ///     The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="searchType">The search algorithm type.</param>
        /// <param name="nearestNeighbors">The number of nearest neighbors to use in behavior distance calculations.</param>
        /// <param name="archive">A reference to the elite archive (optional).</param>
        /// <param name="evaluationLogger">A reference to the evaluation data logger (optional).</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public ParallelGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator,
            SearchType searchType,
            int nearestNeighbors, AbstractNoveltyArchive<TGenome> archive = null, IDataLogger evaluationLogger = null,
            bool decodeGenomeToXml = false)
            : this(
                genomeDecoder, phenomeEvaluator, searchType, new ParallelOptions(), true,
                nearestNeighbors, archive,
                evaluationLogger, decodeGenomeToXml)
        {
        }

        /// <summary>
        ///     Construct with the provided IGenomeDecoder, IPhenomeEvaluator, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="searchType">The search algorithm type.</param>
        /// <param name="options">Controls the number of parallel evaluations.</param>
        /// <param name="enablePhenomeCaching">Whether or not to enable phenome caching.</param>
        /// <param name="nearestNeighbors">The number of nearest neighbors to use in behavior distance calculations.</param>
        /// <param name="archive">A reference to the elite archive (optional).</param>
        /// <param name="evaluationLogger">A reference to the evaluation data logger (optional).</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        private ParallelGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator,
            SearchType searchType,
            ParallelOptions options, bool enablePhenomeCaching, int nearestNeighbors,
            AbstractNoveltyArchive<TGenome> archive = null, IDataLogger evaluationLogger = null,
            bool decodeGenomeToXml = false)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _searchType = searchType;
            _parallelOptions = options;
            _nearestNeighbors = nearestNeighbors;
            _noveltyArchive = archive;
            _evaluationLogger = evaluationLogger;
            _decodeGenomeToXml = decodeGenomeToXml;

            // Determine the appropriate evaluation method.
            if (enablePhenomeCaching)
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

        /// <inheritdoc />
        /// <summary>
        ///     Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        public ulong EvaluationCount => _phenomeEvaluator.EvaluationCount;

        /// <inheritdoc />
        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that
        ///     the the evolutionary algorithm/search should stop. This property's value can remain false
        ///     to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied => _phenomeEvaluator.StopConditionSatisfied;

        #endregion

        #region Public Evaluate and Reset methods

        /// <inheritdoc />
        /// <summary>
        ///     Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
            _phenomeEvaluator.Reset();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        /// <summary>
        ///     Cleans up evaluator state after end of execution or upon execution interruption.  In particular, this closes out
        ///     any existing evaluation logger instance.
        /// </summary>
        public void Cleanup()
        {
            _evaluationLogger?.Close();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Update the genome evaluator based on some characteristic of the given population.
        /// </summary>
        /// <param name="population">The current population.</param>
        public void Update(List<TGenome> population)
        {
            // Pass the population into the phenome evaluator to carry out any necessary
            // update operations
            _phenomeEvaluator.Update(population);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Updates the environment or other evaluation criteria against which the evaluated genomes are being compared.  This
        ///     is typically used in a coevoluationary context.
        /// </summary>
        /// <param name="comparisonPhenomes">The phenomes against which the evaluation is being carried out.</param>
        public void UpdateEvaluationBaseline(IEnumerable<object> comparisonPhenomes)
        {
            _phenomeEvaluator.UpdateEvaluatorPhenotypes(comparisonPhenomes);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Decodes a list of genomes to their corresponding phenotypes.
        /// </summary>
        /// <param name="genomeList">The list of genomes to decode.</param>
        /// <returns>The decoded phenomes.</returns>
        public IEnumerable<object> DecodeGenomes(IList<TGenome> genomeList)
        {
            return EvaluationUtils<TGenome, TPhenome>.DecodeGenomes(genomeList, _genomeDecoder);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Evaluates a list of genomes. Here we decode each genome in using the contained IGenomeDecoder
        ///     and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
        /// </summary>
        /// <param name="genomeList">The list of genomes under evaluation.</param>
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        public void Evaluate(IList<TGenome> genomeList, uint currentGeneration, bool runSimulation = true)
        {
            _populationEvaluationMethod(genomeList, currentGeneration, runSimulation);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Evalutes a single genome alone and against a list of other genomes.
        /// </summary>
        /// <param name="genomesToEvaluate">The list of genomes under evaluation.</param>
        /// <param name="population">The genomes against which to evaluate.</param>
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        public void Evaluate(IList<TGenome> genomesToEvaluate, IList<TGenome> population, uint currentGeneration,
            bool runSimulation = true)
        {
            _batchEvaluationMethod(genomesToEvaluate, population, currentGeneration, runSimulation);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Returns ParallelGenomeBehaviorEvaluator loggable elements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable elements for ParallelGenomeBehaviorEvaluator.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return _phenomeEvaluator.GetLoggableElements(logFieldEnableMap);
        }

        #endregion

        #region Private Evaluation Wrapper methods

        /// <summary>
        ///     Main genome evaluation loop with no phenome caching (decode on each loop).
        /// </summary>
        /// <param name="genomeList">The list of genomes under evaluation.</param>
        /// <param name="currentGeneration">The generation during which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        private void EvaluateAllBehaviors_NonCaching(IList<TGenome> genomeList, uint currentGeneration,
            bool runSimulation)
        {
            if (runSimulation)
            {
                Parallel.ForEach(genomeList, _parallelOptions,
                    delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_NonCaching(genome, _genomeDecoder,
                            _phenomeEvaluator, currentGeneration, _evaluationLogger, _decodeGenomeToXml);
                    });
            }

            switch (_searchType)
            {
                // If we're doing novelty search, include nearest neighbor measure and novelty archive (if applicable)
                case SearchType.NoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                            _noveltyArchive, false);
                    });
                    break;
                case SearchType.MinimalCriteriaNoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                            _noveltyArchive, true);
                    });
                    break;
                case SearchType.MinimalCriteriaSearch:
                    // If we're doing minimal criteria search with queueing, the objective distance will be assigned as the fitness
                    // (but it will ultimately have no bearing on the progression of search)
                    Parallel.ForEach(genomeList, _parallelOptions,
                        delegate(TGenome genome) { EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, true); });
                    break;
            }
        }

        /// <summary>
        ///     Evaluates the behavior of a "batch" of genomes against another provided list of genomes (the population) and the
        ///     novelty archive.  This is used in steady-state evaluation of a batch of offspring vs. the existing population.
        ///     Phenotypes for each genome in the batch are decoded upon invocation (no caching).
        /// </summary>
        /// <param name="genomesToEvaluate">The batch of genomes to evaluate.</param>
        /// <param name="population">The population of genomes against which the batch is being evaluated.</param>
        /// <param name="currentGeneration">The generation during which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        private void EvaluateBatchBehaviors_NonCaching(IList<TGenome> genomesToEvaluate, IList<TGenome> population,
            uint currentGeneration, bool runSimulation)
        {
            if (runSimulation)
            {
                // Decode and evaluate the behavior of the genomes under evaluation
                Parallel.ForEach(genomesToEvaluate, _parallelOptions,
                    delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_NonCaching(genome, _genomeDecoder,
                            _phenomeEvaluator, currentGeneration, _evaluationLogger, _decodeGenomeToXml);
                    });
            }

            switch (_searchType)
            {
                // If we're doing novelty search, include nearest neighbor measure and novelty archive (if applicable)
                case SearchType.NoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    Parallel.ForEach(genomesToEvaluate, _parallelOptions, delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                            _noveltyArchive, false);
                    });
                    break;
                case SearchType.MinimalCriteriaNoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    Parallel.ForEach(genomesToEvaluate, _parallelOptions, delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                            _noveltyArchive, true);
                    });
                    break;
                // If we're doing minimal criteria search with queueing, the objective distance will be assigned as the fitness
                // (but it will ultimately have no bearing on the progression of search)
                case SearchType.MinimalCriteriaSearch:
                    Parallel.ForEach(genomesToEvaluate, _parallelOptions,
                        delegate(TGenome genome) { EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, true); });
                    break;
            }
        }

        /// <summary>
        ///     Main genome evaluation loop with phenome caching (decode only if no cached phenome is present
        ///     from a previous decode).
        /// </summary>
        /// <param name="genomeList">The list of genomes under evaluation.</param>
        /// <param name="currentGeneration">The generation during which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        private void EvaluateAllBehaviors_Caching(IList<TGenome> genomeList, uint currentGeneration, bool runSimulation)
        {
            if (runSimulation)
            {
                Parallel.ForEach(genomeList, _parallelOptions,
                    delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_Caching(genome, _genomeDecoder,
                            _phenomeEvaluator, currentGeneration, _evaluationLogger, _decodeGenomeToXml);
                    });
            }

            switch (_searchType)
            {
                // If we're doing novelty search, include nearest neighbor measure and novelty archive (if applicable)
                case SearchType.NoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                            _noveltyArchive, false);
                    });
                    break;
                case SearchType.MinimalCriteriaNoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    Parallel.ForEach(genomeList, _parallelOptions, delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                            _noveltyArchive, true);
                    });
                    break;
                // If we're doing minimal criteria search with queueing, the objective distance will be assigned as the fitness
                // (but it will ultimately have no bearing on the progression of search)
                case SearchType.MinimalCriteriaSearch:
                    Parallel.ForEach(genomeList, _parallelOptions,
                        delegate(TGenome genome) { EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, true); });
                    break;
            }
        }

        /// <summary>
        ///     Evaluates the behavior of a "batch" of genomes against another provided list of genomes (the population) and the
        ///     novelty archive.  This is used in steady-state evaluation of a batch of offspring vs. the existing population.  We
        ///     first try to retrieve the phenome from each batch genome's cache and then decode the genome if it has not yet been
        ///     cached.
        /// </summary>
        /// <param name="genomesToEvaluate">The batch of genomes to evaluate.</param>
        /// <param name="population">The population of genomes against which the batch is being evaluated.</param>
        /// <param name="currentGeneration">The generation during which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        private void EvaluateBatchBehaviors_Caching(IList<TGenome> genomesToEvaluate, IList<TGenome> population,
            uint currentGeneration, bool runSimulation)
        {
            if (runSimulation)
            {
                // Decode and evaluate the behavior of the genomes under evaluation
                Parallel.ForEach(genomesToEvaluate, _parallelOptions,
                    delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_Caching(genome, _genomeDecoder,
                            _phenomeEvaluator, currentGeneration, _evaluationLogger, _decodeGenomeToXml);
                    });
            }

            switch (_searchType)
            {
                // If we're doing novelty search, include nearest neighbor measure and novelty archive (if applicable)
                case SearchType.NoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    Parallel.ForEach(genomesToEvaluate, _parallelOptions, delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                            _noveltyArchive, false);
                    });
                    break;
                case SearchType.MinimalCriteriaNoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    Parallel.ForEach(genomesToEvaluate, _parallelOptions, delegate(TGenome genome)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                            _noveltyArchive, true);
                    });
                    break;
                // If we're doing minimal criteria search with queueing, the objective distance will be assigned as the fitness
                // (but it will ultimately have no bearing on the progression of search)
                case SearchType.MinimalCriteriaSearch:
                    Parallel.ForEach(genomesToEvaluate, _parallelOptions,
                        delegate(TGenome genome) { EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, true); });
                    break;
            }

            #endregion
        }
    }
}