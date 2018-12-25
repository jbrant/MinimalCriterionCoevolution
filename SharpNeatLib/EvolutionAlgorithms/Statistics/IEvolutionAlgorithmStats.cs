// This is a test of setting the file header.

using System;
using Redzen.Structures;
using SharpNeat.Core;

namespace SharpNeat.EvolutionAlgorithms.Statistics
{
    public interface IEvolutionAlgorithmStats : ILoggable
    {
        #region General Stats

        /// <summary>
        ///     The current generation number.
        /// </summary>
        uint _generation { get; set; }

        /// <summary>
        ///     The total number of genome evaluations for the current NEAT search.
        /// </summary>
        ulong _totalEvaluationCount { get; set; }

        /// <summary>
        ///     Current evaluations per second reading.
        /// </summary>
        int _evaluationsPerSec { get; set; }

        /// <summary>
        ///     The clock time of the last update to _evaluationsPerSec.
        /// </summary>
        DateTime _evalsPerSecLastSampleTime { get; set; }

        /// <summary>
        ///     The total evaluation count at the last update to _evaluationsPerSec.
        /// </summary>
        ulong _evalsCountAtLastUpdate { get; set; }

        #endregion
        
        #region Fitness Stats

        /// <summary>
        ///     The fitness of the best genome.
        /// </summary>
        double _maxFitness { get; set; }

        /// <summary>
        ///     The mean genome fitness.
        /// </summary>
        double _meanFitness { get; set; }

        /// <summary>
        ///     The mean fitness of current specie champions.
        /// </summary>
        double _meanSpecieChampFitness { get; set; }

        #endregion

        #region Complexity Stats

        /// <summary>
        ///     The complexity of the least complex genome.
        /// </summary>
        double _minComplexity { get; set; }

        /// <summary>
        ///     The complexity of the most complex genome.
        /// </summary>
        double _maxComplexity { get; set; }

        /// <summary>
        ///     The mean genome complexity.
        /// </summary>
        double _meanComplexity { get; set; }

        #endregion

        #region Reproduction Stats

        /// <summary>
        ///     Total number of offspring created in the lifetime of a NEAT search.
        /// </summary>
        ulong _totalOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from asexual reproduction.
        /// </summary>
        ulong _asexualOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from sexual reproduction. This includes
        ///     the number of offspring created from interspecies reproduction.
        /// </summary>
        ulong _sexualOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from interspecies sexual reproduction.
        /// </summary>
        ulong _interspeciesOffspringCount { get; set; }

        #endregion

        #region Specie Stats

        /// <summary>
        ///     The number of genomes in the smallest specie.
        /// </summary>
        int _minSpecieSize { get; set; }

        /// <summary>
        ///     The number of genomes in the largest specie.
        /// </summary>
        int _maxSpecieSize { get; set; }

        #endregion

        #region Moving Averages - Fitness / Complexity

        /// <summary>
        ///     A buffer of the N most recent best fitness values. Allows the calculation of a moving average.
        /// </summary>
        DoubleCircularBufferWithStats _bestFitnessMA { get; set; }

        /// <summary>
        ///     A buffer of the N most recent mean specie champ fitness values (the average fitness of all specie champs).
        ///     Allows the calculation of a moving average.
        /// </summary>
        DoubleCircularBufferWithStats _meanSpecieChampFitnessMA { get; set; }

        /// <summary>
        ///     A buffer of the N most recent population mean complexity values.
        ///     Allows the calculation of a moving average.
        /// </summary>
        DoubleCircularBufferWithStats _complexityMA { get; set; }

        /// <summary>
        ///     The previous moving average value for the 'best fitness' series. Allows testing for fitness stalling by comparing
        ///     with the current MA value.
        /// </summary>
        double _prevBestFitnessMA { get; set; }

        /// <summary>
        ///     The previous moving average value for the 'mean specie champ fitness' series. Allows testing for fitness stalling
        ///     by comparing with the current MA value.
        /// </summary>
        double _prevMeanSpecieChampFitnessMA { get; set; }

        /// <summary>
        ///     The previous moving average value for the complexity series. Allows testing for stalling during the simplification
        ///     phase of complexity regulation.
        /// </summary>
        double _prevComplexityMA { get; set; }

        #endregion
    }
}