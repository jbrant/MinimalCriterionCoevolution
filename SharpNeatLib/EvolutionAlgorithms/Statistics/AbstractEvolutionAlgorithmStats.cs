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
    public abstract class AbstractEvolutionaryAlgorithmStats<TGenome> : IEvolutionAlgorithmStats
        where TGenome : IGenome<TGenome>
    {
        protected AbstractEvolutionaryAlgorithmStats(EvolutionAlgorithmParameters eaParams)
        {
            _bestFitnessMA = new DoubleCircularBufferWithStats(eaParams.BestFitnessMovingAverageHistoryLength);
            _meanSpecieChampFitnessMA =
                new DoubleCircularBufferWithStats(eaParams.MeanSpecieChampFitnessMovingAverageHistoryLength);
            _complexityMA = new DoubleCircularBufferWithStats(eaParams.ComplexityMovingAverageHistoryLength);
        }

        public virtual List<LoggableElement> GetLoggableElements(
            IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return new List<LoggableElement>
            {
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.Generation) == true &&
                 logFieldEnableMap[EvolutionFieldElements.Generation])
                    ? new LoggableElement(EvolutionFieldElements.Generation, _generation)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.TotalEvaluations) == true &&
                 logFieldEnableMap[EvolutionFieldElements.TotalEvaluations])
                    ? new LoggableElement(EvolutionFieldElements.TotalEvaluations, _totalEvaluationCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.EvaluationsPerSecond) == true &&
                 logFieldEnableMap[EvolutionFieldElements.EvaluationsPerSecond])
                    ? new LoggableElement(EvolutionFieldElements.EvaluationsPerSecond, _evaluationsPerSec)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxFitness) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MaxFitness])
                    ? new LoggableElement(EvolutionFieldElements.MaxFitness, _maxFitness)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanFitness) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MeanFitness])
                    ? new LoggableElement(EvolutionFieldElements.MeanFitness, _meanFitness)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanSpecieChampFitness) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MeanSpecieChampFitness])
                    ? new LoggableElement(EvolutionFieldElements.MeanSpecieChampFitness, _meanSpecieChampFitness)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinComplexity) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MinComplexity])
                    ? new LoggableElement(EvolutionFieldElements.MinComplexity, _minComplexity)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxComplexity) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MaxComplexity])
                    ? new LoggableElement(EvolutionFieldElements.MaxComplexity, _maxComplexity)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanComplexity) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MeanComplexity])
                    ? new LoggableElement(EvolutionFieldElements.MeanComplexity, _meanComplexity)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.TotalOffspringCount) == true &&
                 logFieldEnableMap[EvolutionFieldElements.TotalOffspringCount])
                    ? new LoggableElement(EvolutionFieldElements.TotalOffspringCount, _totalOffspringCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.AsexualOffspringCount) == true &&
                 logFieldEnableMap[EvolutionFieldElements.AsexualOffspringCount])
                    ? new LoggableElement(EvolutionFieldElements.AsexualOffspringCount, _asexualOffspringCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.SexualOffspringCount) == true &&
                 logFieldEnableMap[EvolutionFieldElements.SexualOffspringCount])
                    ? new LoggableElement(EvolutionFieldElements.SexualOffspringCount, _sexualOffspringCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.InterspeciesOffspringCount) == true &&
                 logFieldEnableMap[EvolutionFieldElements.InterspeciesOffspringCount])
                    ? new LoggableElement(EvolutionFieldElements.InterspeciesOffspringCount,
                        _interspeciesOffspringCount)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinSpecieSize) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MinSpecieSize])
                    ? new LoggableElement(EvolutionFieldElements.MinSpecieSize, _minSpecieSize)
                    : null,
                (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxSpecieSize) == true &&
                 logFieldEnableMap[EvolutionFieldElements.MaxSpecieSize])
                    ? new LoggableElement(EvolutionFieldElements.MaxSpecieSize, _maxSpecieSize)
                    : null
            };
        }

        #region Abstract methods

        public abstract void SetAlgorithmSpecificsPopulationStats(IList<TGenome> population);

        #endregion

        #region General Stats

        /// <summary>
        ///     The current generation number.
        /// </summary>
        public uint _generation { get; set; }

        /// <summary>
        ///     The total number of genome evaluations for the current NEAT search.
        /// </summary>
        public ulong _totalEvaluationCount { get; set; }

        /// <summary>
        ///     Current evaluations per second reading.
        /// </summary>
        public int _evaluationsPerSec { get; set; }

        /// <summary>
        ///     The clock time of the last update to _evaluationsPerSec.
        /// </summary>
        public DateTime _evalsPerSecLastSampleTime { get; set; }

        /// <summary>
        ///     The total evaluation count at the last update to _evaluationsPerSec.
        /// </summary>
        public ulong _evalsCountAtLastUpdate { get; set; }

        #endregion

        #region Fitness Stats

        /// <summary>
        ///     The fitness of the best genome.
        /// </summary>
        public double _maxFitness { get; set; }

        /// <summary>
        ///     The mean genome fitness.
        /// </summary>
        public double _meanFitness { get; set; }

        /// <summary>
        ///     The mean fitness of current specie champions.
        /// </summary>
        public double _meanSpecieChampFitness { get; set; }

        #endregion

        #region Complexity Stats

        /// <summary>
        ///     The complexity of the least complex genome.
        /// </summary>
        public double _minComplexity { get; set; }

        /// <summary>
        ///     The complexity of the most complex genome.
        /// </summary>
        public double _maxComplexity { get; set; }

        /// <summary>
        ///     The mean genome complexity.
        /// </summary>
        public double _meanComplexity { get; set; }

        #endregion

        #region Reproduction Stats

        /// <summary>
        ///     Total number of offspring created in the lifetime of a NEAT search.
        /// </summary>
        public ulong _totalOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from asexual reproduction.
        /// </summary>
        public ulong _asexualOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from sexual reproduction. This includes
        ///     the number of offspring created from interspecies reproduction.
        /// </summary>
        public ulong _sexualOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from interspecies sexual reproduction.
        /// </summary>
        public ulong _interspeciesOffspringCount { get; set; }

        #endregion

        #region Specie Stats

        /// <summary>
        ///     The number of genomes in the smallest specie.
        /// </summary>
        public int _minSpecieSize { get; set; }

        /// <summary>
        ///     The number of genomes in the largest specie.
        /// </summary>
        public int _maxSpecieSize { get; set; }

        #endregion

        #region Moving Averages - Fitness / Complexity

        /// <summary>
        ///     A buffer of the N most recent best fitness values. Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats _bestFitnessMA { get; set; }

        /// <summary>
        ///     A buffer of the N most recent mean specie champ fitness values (the average fitness of all specie champs).
        ///     Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats _meanSpecieChampFitnessMA { get; set; }

        /// <summary>
        ///     A buffer of the N most recent population mean complexity values.
        ///     Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats _complexityMA { get; set; }

        /// <summary>
        ///     The previous moving average value for the 'best fitness' series. Allows testing for fitness stalling by comparing
        ///     with the current MA value.
        /// </summary>
        public double _prevBestFitnessMA { get; set; }

        /// <summary>
        ///     The previous moving average value for the 'mean specie champ fitness' series. Allows testing for fitness stalling
        ///     by comparing with the current MA value.
        /// </summary>
        public double _prevMeanSpecieChampFitnessMA { get; set; }

        /// <summary>
        ///     The previous moving average value for the complexity series. Allows testing for stalling during the simplification
        ///     phase of complexity regulation.
        /// </summary>
        public double _prevComplexityMA { get; set; }

        #endregion
    }
}