#region

using System.Collections.Generic;
using SharpNeat.NoveltyArchives;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     A concrete implementation of IGenomeFitnessEvaluator that evaulates genome's phenotypic behaviors independently of
    ///     each other and in series on a single thread.
    ///     - Genome decoding is performed by a provided IGenomeDecoder.
    ///     - Phenome evaluation is performed by a provided IPhenomeEvaluator.
    ///     - This class evaluates on a single thread only, and therefore is a good choice when debugging code.
    /// </summary>
    /// <typeparam name="TGenome">The genome type that is decoded.</typeparam>
    /// <typeparam name="TPhenome">The phenome type that is decoded to and then evaluated.</typeparam>
    public class SerialGenomeBehaviorEvaluator<TGenome, TPhenome> : IGenomeEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        #region Private Instance fields

        private readonly BatchEvaluationMethod _batchEvaluationMethod;
        private readonly PopulationEvaluationMethod _populationEvaluationMethod;
        private readonly AbstractNoveltyArchive<TGenome> _noveltyArchive;
        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly int _nearestNeighbors;
        private readonly IPhenomeEvaluator<TPhenome, BehaviorInfo> _phenomeEvaluator;
        private readonly IDataLogger _evaluationLogger;
        private readonly SelectionType _selectionType;
        private readonly SearchType _searchType;
        private readonly bool _decodeGenomeToXml;

        #endregion

        #region Delegates for population (generational) and batch (steady state) based evaluations

        /// <summary>
        ///     Delegate for population (generational) evaluation.
        /// </summary>
        /// <param name="genomeList">The list of genomes (population) to evaluate.</param>
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
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
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        private delegate void BatchEvaluationMethod(
            IList<TGenome> genomesToEvaluate, IList<TGenome> population, uint currentGeneration, bool runSimulation);

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and the
        ///     evaluation method.  Also sets the number of nearest neighbors to utilize in behavior distance calculations and
        ///     accepts an optional elite archive.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="selectionType">The selection algorithm type.</param>
        /// <param name="searchType">The search algorithm type.</param>
        /// <param name="evaluationLogger">A reference to the evaluation data logger (optional).</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public SerialGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator, SelectionType selectionType,
            SearchType searchType,
            IDataLogger evaluationLogger = null, bool decodeGenomeToXml = false) : this(
                genomeDecoder, phenomeEvaluator, selectionType, searchType, true, 0, null, evaluationLogger,
                decodeGenomeToXml)
        {
        }

        /// <summary>
        ///     Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and the
        ///     evaluation method.  Also sets the number of nearest neighbors to utilize in behavior distance calculations and
        ///     accepts an optional elite archive.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="selectionType">The selection algorithm type.</param>
        /// <param name="searchType">The search algorithm type.</param>
        /// <param name="nearestNeighbors">The number of nearest neighbors to use in behavior distance calculations.</param>
        /// <param name="archive">A reference to the elite archive (optional).</param>
        /// <param name="evaluationLogger">A reference to the evaluation data logger (optional).</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public SerialGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator, SelectionType selectionType,
            SearchType searchType,
            int nearestNeighbors,
            AbstractNoveltyArchive<TGenome> archive = null, IDataLogger evaluationLogger = null,
            bool decodeGenomeToXml = false)
            : this(
                genomeDecoder, phenomeEvaluator, selectionType, searchType, true, nearestNeighbors, archive,
                evaluationLogger, decodeGenomeToXml)
        {
        }

        /// <summary>
        ///     Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and setting the
        ///     caching method.  Also sets the number of nearest neighbors to utilize in behavior distance calculations and accepts
        ///     an optional elite archive.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="selectionType">The selection algorithm type.</param>
        /// <param name="searchType">The search algorithm type.</param>
        /// <param name="enablePhenomeCaching">Whether or not to enable phenome caching.</param>
        /// <param name="nearestNeighbors">The number of nearest neighbors to use in behavior distance calculations.</param>
        /// <param name="archive">A reference to the elite archive (optional).</param>
        /// <param name="evaluationLogger">A reference to the evaluation data logger (optional).</param>
        /// <param name="decodeGenomeToXml">
        ///     Whether a genome should be decoded to its XML string representation (generally used to
        ///     support logging).
        /// </param>
        public SerialGenomeBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator, SelectionType selectionType,
            SearchType searchType,
            bool enablePhenomeCaching, int nearestNeighbors, AbstractNoveltyArchive<TGenome> archive = null,
            IDataLogger evaluationLogger = null, bool decodeGenomeToXml = false)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _selectionType = selectionType;
            _searchType = searchType;
            _nearestNeighbors = nearestNeighbors;
            _noveltyArchive = archive;
            _evaluationLogger = evaluationLogger;
            _decodeGenomeToXml = decodeGenomeToXml;

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
        ///     Evaluates a the behavior-based fitness of a list of genomes. Here we decode each genome in series using the
        ///     contained IGenomeDecoder and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
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

        /// <summary>
        ///     Evaluates a the behavior-based fitness of a single genome versus the given list of genomes. Here we decode each
        ///     genome in series using the contained IGenomeDecoder and evaluate the resulting TPhenome using the contained
        ///     IPhenomeEvaluator.
        /// </summary>
        /// <param name="genomesToEvaluate">The genomes under evaluation.</param>
        /// <param name="population">The genomes against which to evaluate.</param>
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty).
        /// </param>
        public void Evaluate(IList<TGenome> genomesToEvaluate, IList<TGenome> population, uint currentGeneration,
            bool runSimulation = true)
        {
            _batchEvaluationMethod(genomesToEvaluate, population, currentGeneration, runSimulation);
        }

        /// <summary>
        ///     Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
            _phenomeEvaluator.Reset();
        }

        #endregion

        #region Private Evaluation Wrapper methods

        /// <summary>
        ///     Evalutes the behavior of all genomes in a given list (i.e. the population) against each other and the novelty
        ///     archive.  Phenotypes for each genome are decoded upon invocation (no caching).
        /// </summary>
        /// <param name="genomeList">The list of genomes (population) under evaluation.</param>
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty).
        /// </param>
        private void EvaluateAllBehaviors_NonCaching(IList<TGenome> genomeList, uint currentGeneration,
            bool runSimulation)
        {
            if (runSimulation)
            {
                // Decode and evaluate the behavior of each genome in turn.
                foreach (var genome in genomeList)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_NonCaching(genome, _genomeDecoder,
                        _phenomeEvaluator, currentGeneration, _evaluationLogger, _decodeGenomeToXml);
                }
            }

            switch (_searchType)
            {
                // If we're doing novelty search, include nearest neighbor measure and novelty archive (if applicable)
                case SearchType.NoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    foreach (var genome in genomeList)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                            _noveltyArchive, false);
                    }
                    break;
                case SearchType.MinimalCriteriaNoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    foreach (var genome in genomeList)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                            _noveltyArchive, true);
                    }
                    break;
                // If we're doing minimal criteria search with queueing, the objective distance will be assigned as the fitness
                // (but it will ultimately have no bearing on the progression of search)
                case SearchType.MinimalCriteriaSearch:
                    foreach (var genome in genomeList)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, true);
                    }
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
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty).
        /// </param>
        private void EvaluateBatchBehaviors_NonCaching(IList<TGenome> genomesToEvaluate, IList<TGenome> population,
            uint currentGeneration, bool runSimulation)
        {
            if (runSimulation)
            {
                // Decode and evaluate the behavior of the genomes under evaluation
                foreach (var genome in genomesToEvaluate)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_NonCaching(genome, _genomeDecoder,
                        _phenomeEvaluator, currentGeneration, _evaluationLogger, _decodeGenomeToXml);
                }
            }

            switch (_searchType)
            {
                // If we're doing novelty search, include nearest neighbor measure and novelty archive (if applicable)
                case SearchType.NoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    foreach (var genome in genomesToEvaluate)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                            _noveltyArchive, false);
                    }
                    break;
                case SearchType.MinimalCriteriaNoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    foreach (var genome in genomesToEvaluate)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                            _noveltyArchive, true);
                    }
                    break;
                // If we're doing minimal criteria search with queueing, the objective distance will be assigned as the fitness
                // (but it will ultimately have no bearing on the progression of search)
                case SearchType.MinimalCriteriaSearch:
                    foreach (var genome in genomesToEvaluate)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, true);
                    }
                    break;
            }
        }

        /// <summary>
        ///     Evalutes the behavior of all genomes in a given list (i.e. the population) against each other and the novelty
        ///     archive.  We first try to retrieve the phenome from each genome's cache and then decode the genome if it has not
        ///     yet been cached.
        /// </summary>
        /// <param name="genomeList">The list of genomes (population) under evaluation.</param>
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty).
        /// </param>
        private void EvaluateAllBehaviors_Caching(IList<TGenome> genomeList, uint currentGeneration, bool runSimulation)
        {
            if (runSimulation)
            {
                // Decode and evaluate the behavior of each genome in turn.
                foreach (var genome in genomeList)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_Caching(genome, _genomeDecoder,
                        _phenomeEvaluator, currentGeneration, _evaluationLogger, _decodeGenomeToXml);
                }
            }

            switch (_searchType)
            {
                // If we're doing novelty search, include nearest neighbor measure and novelty archive (if applicable)
                case SearchType.NoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    foreach (var genome in genomeList)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                            _noveltyArchive, false);
                    }
                    break;
                case SearchType.MinimalCriteriaNoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    foreach (var genome in genomeList)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, genomeList, _nearestNeighbors,
                            _noveltyArchive, true);
                    }
                    break;
                // If we're doing minimal criteria search with queueing, the objective distance will be assigned as the fitness
                // (but it will ultimately have no bearing on the progression of search)
                case SearchType.MinimalCriteriaSearch:
                    foreach (var genome in genomeList)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, true);
                    }
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
        /// <param name="currentGeneration">The current generation for which the genomes are being evaluated.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty).
        /// </param>
        private void EvaluateBatchBehaviors_Caching(IList<TGenome> genomesToEvaluate, IList<TGenome> population,
            uint currentGeneration, bool runSimulation)
        {
            if (runSimulation)
            {
                // Decode and evaluate the behavior of the genomes under evaluation
                foreach (var genome in genomesToEvaluate)
                {
                    EvaluationUtils<TGenome, TPhenome>.EvaluateBehavior_Caching(genome, _genomeDecoder,
                        _phenomeEvaluator, currentGeneration, _evaluationLogger, _decodeGenomeToXml);
                }
            }

            switch (_searchType)
            {
                // If we're doing novelty search, include nearest neighbor measure and novelty archive (if applicable)
                case SearchType.NoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    foreach (var genome in genomesToEvaluate)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                            _noveltyArchive, false);
                    }
                    break;
                case SearchType.MinimalCriteriaNoveltySearch:
                    // After the behavior of each genome in the offspring batch has been evaluated,
                    // iterate through each genome and compare its behavioral novelty (distance) to its 
                    // k -nearest neighbors from the population in behavior space (and the archive if applicable)
                    foreach (var genome in genomesToEvaluate)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, population, _nearestNeighbors,
                            _noveltyArchive, true);
                    }
                    break;
                // If we're doing minimal criteria search with queueing, the objective distance will be assigned as the fitness
                // (but it will ultimately have no bearing on the progression of search)
                case SearchType.MinimalCriteriaSearch:
                    foreach (var genome in genomesToEvaluate)
                    {
                        EvaluationUtils<TGenome, TPhenome>.EvaluateFitness(genome, true);
                    }
                    break;
            }
        }

        #endregion
    }
}