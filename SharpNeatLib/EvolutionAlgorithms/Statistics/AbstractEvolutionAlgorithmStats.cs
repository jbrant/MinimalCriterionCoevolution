// This is a test of setting the file header.

#region

using System;
using System.Collections.Generic;
using Redzen.Structures;
using SharpNeat.Core;
using SharpNeat.Loggers;

#endregion

namespace SharpNeat.EvolutionAlgorithms.Statistics
{
    /// <summary>
    ///     Encapsulates generic, descriptive statistics about the state of an evolutionary algorithm.
    /// </summary>
    public abstract class AbstractEvolutionaryAlgorithmStats : IEvolutionAlgorithmStats
    {
        /// <summary>
        ///     AbstractEvolutionaryAlgorithmStats constructor.
        /// </summary>
        /// <param name="eaParams">Evolution algorithm parameters required for initialization.</param>
        protected AbstractEvolutionaryAlgorithmStats(EvolutionAlgorithmParameters eaParams)
        {
            BestFitnessMa = new DoubleCircularBufferWithStats(eaParams.BestFitnessMovingAverageHistoryLength);
            MeanSpecieChampFitnessMa =
                new DoubleCircularBufferWithStats(eaParams.MeanSpecieChampFitnessMovingAverageHistoryLength);
            ComplexityMa = new DoubleCircularBufferWithStats(eaParams.ComplexityMovingAverageHistoryLength);
        }

        /// <summary>
        ///     Returns the fields within AbstractEvolutionAlgorithmStats that are enabled for logging.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable fields within AbstractEvolutionAlgorithmStats.</returns>
        public virtual List<LoggableElement> GetLoggableElements(
            IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return new List<LoggableElement>
            {
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.Generation) == true &&
                 logFieldEnableMap[EvolutionFieldElements.Generation])
                    ? new LoggableElement(EvolutionFieldElements.Generation, Generation)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.TotalEvaluations) == true &&
                 logFieldEnableMap[EvolutionFieldElements.TotalEvaluations])
                    ? new LoggableElement(EvolutionFieldElements.TotalEvaluations, TotalEvaluationCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.EvaluationsPerSecond) == true &&
                 logFieldEnableMap[EvolutionFieldElements.EvaluationsPerSecond])
                    ? new LoggableElement(EvolutionFieldElements.EvaluationsPerSecond, EvaluationsPerSec)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxFitness) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MaxFitness])
                    ? new LoggableElement(EvolutionFieldElements.MaxFitness, MaxFitness)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanFitness) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MeanFitness])
                    ? new LoggableElement(EvolutionFieldElements.MeanFitness, MeanFitness)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanSpecieChampFitness) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MeanSpecieChampFitness])
                    ? new LoggableElement(EvolutionFieldElements.MeanSpecieChampFitness, MeanSpecieChampFitness)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinComplexity) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MinComplexity])
                    ? new LoggableElement(EvolutionFieldElements.MinComplexity, MinComplexity)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxComplexity) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MaxComplexity])
                    ? new LoggableElement(EvolutionFieldElements.MaxComplexity, MaxComplexity)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanComplexity) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MeanComplexity])
                    ? new LoggableElement(EvolutionFieldElements.MeanComplexity, MeanComplexity)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.TotalOffspringCount) == true &&
                 logFieldEnableMap[EvolutionFieldElements.TotalOffspringCount])
                    ? new LoggableElement(EvolutionFieldElements.TotalOffspringCount, TotalOffspringCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.AsexualOffspringCount) == true &&
                 logFieldEnableMap[EvolutionFieldElements.AsexualOffspringCount])
                    ? new LoggableElement(EvolutionFieldElements.AsexualOffspringCount, AsexualOffspringCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.SexualOffspringCount) == true &&
                 logFieldEnableMap[EvolutionFieldElements.SexualOffspringCount])
                    ? new LoggableElement(EvolutionFieldElements.SexualOffspringCount, SexualOffspringCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.InterspeciesOffspringCount) == true &&
                 logFieldEnableMap[EvolutionFieldElements.InterspeciesOffspringCount])
                    ? new LoggableElement(EvolutionFieldElements.InterspeciesOffspringCount,
                        InterspeciesOffspringCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinSpecieSize) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MinSpecieSize])
                    ? new LoggableElement(EvolutionFieldElements.MinSpecieSize, MinSpecieSize)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxSpecieSize) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MaxSpecieSize])
                    ? new LoggableElement(EvolutionFieldElements.MaxSpecieSize, MaxSpecieSize)
                    : null
            };
        }

        #region Abstract methods

        /// <summary>
        ///     Computes genome implementation-specific details about the population.
        /// </summary>
        /// <param name="population">The population from which to compute more specific, descriptive statistics.</param>
        /// <typeparam name="TGenome">The genome type generic.</typeparam>
        public abstract void ComputeAlgorithmSpecificPopulationStats<TGenome>(IList<TGenome> population)
            where TGenome : IGenome<TGenome>;

        #endregion

        #region General Stats

        /// <summary>
        ///     The current generation number.
        /// </summary>
        public uint Generation { get; set; }

        /// <summary>
        ///     The total number of genome evaluations for the current NEAT search.
        /// </summary>
        public ulong TotalEvaluationCount { get; set; }

        /// <summary>
        ///     Current evaluations per second reading.
        /// </summary>
        public int EvaluationsPerSec { get; set; }

        /// <summary>
        ///     The clock time of the last update to _evaluationsPerSec.
        /// </summary>
        public DateTime EvalsPerSecLastSampleTime { get; set; }

        /// <summary>
        ///     The total evaluation count at the last update to _evaluationsPerSec.
        /// </summary>
        public ulong EvalsCountAtLastUpdate { get; set; }

        #endregion

        #region Fitness Stats

        /// <summary>
        ///     The fitness of the best genome.
        /// </summary>
        public double MaxFitness { get; set; }

        /// <summary>
        ///     The mean genome fitness.
        /// </summary>
        public double MeanFitness { get; set; }

        /// <summary>
        ///     The mean fitness of current specie champions.
        /// </summary>
        public double MeanSpecieChampFitness { get; set; }

        #endregion

        #region Complexity Stats

        /// <summary>
        ///     The complexity of the least complex genome.
        /// </summary>
        public double MinComplexity { get; set; }

        /// <summary>
        ///     The complexity of the most complex genome.
        /// </summary>
        public double MaxComplexity { get; set; }

        /// <summary>
        ///     The mean genome complexity.
        /// </summary>
        public double MeanComplexity { get; set; }

        #endregion

        #region Reproduction Stats

        /// <summary>
        ///     Total number of offspring created in the lifetime of a NEAT search.
        /// </summary>
        public ulong TotalOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from asexual reproduction.
        /// </summary>
        public ulong AsexualOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from sexual reproduction. This includes
        ///     the number of offspring created from interspecies reproduction.
        /// </summary>
        public ulong SexualOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from interspecies sexual reproduction.
        /// </summary>
        public ulong InterspeciesOffspringCount { get; set; }

        #endregion

        #region Specie Stats

        /// <summary>
        ///     The number of genomes in the smallest specie.
        /// </summary>
        public int MinSpecieSize { get; set; }

        /// <summary>
        ///     The number of genomes in the largest specie.
        /// </summary>
        public int MaxSpecieSize { get; set; }

        #endregion

        #region Moving Averages - Fitness / Complexity

        /// <summary>
        ///     A buffer of the N most recent best fitness values. Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats BestFitnessMa { get; set; }

        /// <summary>
        ///     A buffer of the N most recent mean specie champ fitness values (the average fitness of all specie champs).
        ///     Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats MeanSpecieChampFitnessMa { get; set; }

        /// <summary>
        ///     A buffer of the N most recent population mean complexity values.
        ///     Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats ComplexityMa { get; set; }

        /// <summary>
        ///     The previous moving average value for the 'best fitness' series. Allows testing for fitness stalling by comparing
        ///     with the current MA value.
        /// </summary>
        public double PrevBestFitnessMa { get; set; }

        /// <summary>
        ///     The previous moving average value for the 'mean specie champ fitness' series. Allows testing for fitness stalling
        ///     by comparing with the current MA value.
        /// </summary>
        public double PrevMeanSpecieChampFitnessMa { get; set; }

        /// <summary>
        ///     The previous moving average value for the complexity series. Allows testing for stalling during the simplification
        ///     phase of complexity regulation.
        /// </summary>
        public double PrevComplexityMa { get; set; }

        #endregion
    }
}