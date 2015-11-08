#region

using System;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     An enumeration of search algorithm types used for determining the objective (or non-objective, as the case may be)
    ///     measurement method.
    /// </summary>
    public enum SearchType
    {
        /// <summary>
        ///     Fitness evaluation.
        /// </summary>
        Fitness,

        /// <summary>
        ///     Novelty Search evaluation.
        /// </summary>
        NoveltySearch,

        /// <summary>
        ///     Minimal Criteria Search evaluation.  There are two variants of this - MCS with randomly assigned fitness scores
        ///     (this is essentially random drift with a minimal behavioral constraint imposed) and MCS where in fitness score
        ///     isn't taken into consideration at all (comparative behavior or otherwise), but individuals that satisfy the minimal
        ///     criteria are selected one-by-one (or in batch) from a queue.
        /// </summary>
        MinimalCriteriaSearch,

        /// <summary>
        ///     Minimal Criteria Novelty Search evaluation (this is essentially novelty search with a minimal behavioral constraint
        ///     imposed).
        /// </summary>
        MinimalCriteriaNoveltySearch,

        /// <summary>
        ///     Random Search evaluation.
        /// </summary>
        Random
    }

    /// <summary>
    ///     An enumeration of algorithm types used for specifying the manner in which the population is evaluated.
    /// </summary>
    public enum SelectionType
    {
        /// <summary>
        ///     Generational selection in which the entire population is replaced each "generation" (evaluation) with their
        ///     offspring.
        /// </summary>
        Generational,

        /// <summary>
        ///     Steady state selection in which only a subset of the population (1 or more) are selected for reproduction and
        ///     replacement.
        /// </summary>
        SteadyState,

        /// <summary>
        ///     Queueing selection in which individuals are placed in a FIFO queue and selected for reproduction in like manner.
        ///     Those who produce will either be replaced or placed back in the queue.  Reproduction does not necessarily guarantee
        ///     replacement of the same individuals that reproduced (replacement is often based on age).
        /// </summary>
        Queueing
    }

    /// <summary>
    ///     Provides utility methods for determing the appropriate search and selection algorithm.
    /// </summary>
    public static class AlgorithmTypeUtil
    {
        /// <summary>
        ///     Determines the appropriate search type based on the given string value.
        /// </summary>
        /// <param name="strSearchType">The string-valued search type.</param>
        /// <returns>The search type domain type.</returns>
        public static SearchType ConvertStringToSearchType(string strSearchType)
        {
            // Check if fitness search specified
            if ("Fitness".Equals(strSearchType, StringComparison.InvariantCultureIgnoreCase))
            {
                return SearchType.Fitness;
            }

            // Check if novelty search specified
            if ("NoveltySearch".Equals(strSearchType, StringComparison.InvariantCultureIgnoreCase) ||
                "Novelty Search".Equals(strSearchType, StringComparison.InvariantCultureIgnoreCase))
            {
                return SearchType.NoveltySearch;
            }

            // Check if minimal criteria search specified
            if ("MinimalCriteriaSearch".Equals(strSearchType, StringComparison.InvariantCultureIgnoreCase) ||
                "Minimal Criteria Search".Equals(strSearchType, StringComparison.InvariantCultureIgnoreCase))
            {
                return SearchType.MinimalCriteriaSearch;
            }

            // If nothing matches, return random search
            return SearchType.Random;
        }

        /// <summary>
        ///     Determines the appropriate algorithm selection type based on the given string value.
        /// </summary>
        /// <param name="strSelectionType">The string-valued selection type.</param>
        /// <returns>The selection type domain type.</returns>
        public static SelectionType ConvertStringToSelectionType(string strSelectionType)
        {
            // Check if steady state selection specified
            if ("SteadyState".Equals(strSelectionType, StringComparison.InvariantCultureIgnoreCase) ||
                "Steady State".Equals(strSelectionType, StringComparison.InvariantCultureIgnoreCase))
            {
                return SelectionType.SteadyState;
            }

            // Check if queueing selection specified
            if ("Queueing".Equals(strSelectionType, StringComparison.InvariantCultureIgnoreCase))
            {
                return SelectionType.Queueing;
            }

            // If nothing matches, return generational selection
            return SelectionType.Generational;
        }
    }
}