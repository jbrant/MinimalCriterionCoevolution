namespace SharpNeat.Core
{
    /// <summary>
    ///     An enumeration of evaluation types used for determining the objective (or non-objective, as the case may be)
    ///     measurement method.
    /// </summary>
    public enum EvaluationType
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
        ///     Minimal Criteria Search evaluation  with randomly assigned fitness scores (this is essentially random drift with a
        ///     minimal behavioral constraint imposed).
        /// </summary>
        MinimalCriteriaSearchRandom,

        /// <summary>
        ///     Minimal Criteria Search evaluation wherein fitness score isn't taken into consideration at all (comparative
        ///     behavior or otherwise), but individuals that satisfy the minimal criteria are selected one-by-one (or in batch)
        ///     from a queue.
        /// </summary>
        MinimalCriteriaSearchQueueing,

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
}