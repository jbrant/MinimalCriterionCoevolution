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
    public interface IGenomeEvaluator<TGenome>
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
        ///     Evaluates the fitness or behavior of a list of genomes.
        /// </summary>
        /// <param name="genomeList">The list of genomes under evaluation.</param>
        void Evaluate(IList<TGenome> genomeList);

        /// <summary>
        ///     Evalutes the fitness or behavior of a single genome and potentially against a list of other genomes.
        /// </summary>
        /// <param name="genomesToEvaluate">The list of genomes under evaluation.</param>
        /// <param name="population">The genomes against which to evaluate.</param>
        void Evaluate(IList<TGenome> genomesToEvaluate, IList<TGenome> population);
    }
}