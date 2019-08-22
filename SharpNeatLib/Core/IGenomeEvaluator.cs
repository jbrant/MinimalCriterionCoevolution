#region

using System.Collections.Generic;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Generic interface for evaluating a list of genomes. By operating on a list we allow concrete
    ///     implementations of this interface to choose between evaluating each genome independently of the others,
    ///     perhaps across several execution threads, or in some collective evaluation scheme such as an artificial
    ///     life/world scenario.
    /// </summary>
    public interface IGenomeEvaluator<TGenome> : ILoggable
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        ///     Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        ulong EvaluationCount { get; }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness or behavior has been achieved and that
        ///     the the evolutionary algorithm search should stop. This property's value can remain false
        ///     to allow the algorithm to run indefinitely.
        /// </summary>
        bool StopConditionSatisfied { get; }

        /// <summary>
        ///     Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        void Reset();

        /// <summary>
        ///     Initializes state variables in the genome evalutor.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Cleans up genome/phenome evaluator state after end of execution or upon execution interruption.
        /// </summary>
        void Cleanup();

        /// <summary>
        ///     Updates the environment or other evaluation criteria against which the evaluated genomes are being compared.  This
        ///     is typically used in a coevoluationary context.
        /// </summary>
        /// <param name="comparisonPhenomes">The phenomes against which the evaluation is being carried out.</param>
        /// <param name="lastGeneration">The generation that was just executed.</param>
        void UpdateEvaluationBaseline(IEnumerable<object> comparisonPhenomes, uint lastGeneration);

        /// <summary>
        ///     Decodes a list of genomes to their corresponding phenotypes.
        /// </summary>
        /// <param name="genomeList">The list of genomes to decode.</param>
        /// <returns>Collection of decoded phenotypes.</returns>
        IEnumerable<object> DecodeGenomes(IList<TGenome> genomeList);

        /// <summary>
        ///     Evaluates the fitness or behavioral novelty of a list of genomes.
        /// </summary>
        /// <param name="genomeList">The list of genomes under evaluation.</param>
        /// <param name="currentGeneration">The current generation for which we're evaluating.</param>
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        void Evaluate(IList<TGenome> genomeList, uint currentGeneration, bool runSimulation = true);

        /// <summary>
        ///     Evalutes the fitness or behavioral novelty of a single genome and potentially against a list of other genomes.
        /// </summary>
        /// <param name="genomesToEvaluate">The list of genomes under evaluation.</param>
        /// <param name="population">The genomes against which to evaluate.</param>
        /// <param name="currentGeneration">The current generation for which we're evaluating.</param>
        /// ///
        /// <param name="runSimulation">
        ///     Determines whether to run the simulation to get behavioral characteristics before
        ///     evaluating fitness or behavioral novelty.
        /// </param>
        void Evaluate(IList<TGenome> genomesToEvaluate, IList<TGenome> population, uint currentGeneration,
            bool runSimulation = true);
    }
}