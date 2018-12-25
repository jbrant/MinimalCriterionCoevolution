// This is a test of setting the file header.

#region

using System;
using System.Collections.Generic;
using Redzen.Structures;
using SharpNeat.Core;

#endregion

namespace SharpNeat.EvolutionAlgorithms.Statistics
{
    /// <summary>
    ///     Encapsulates generic, descriptive statistics about the state of an evolutionary algorithm.
    /// </summary>
    public interface IEvolutionAlgorithmStats : ILoggable
    {
        /// <summary>
        ///     Computes genome implementation-specific details about the population.
        /// </summary>
        /// <param name="population">The population from which to compute more specific, descriptive statistics.</param>
        /// <typeparam name="TGenome">The genome type generic.</typeparam>
        void ComputeAlgorithmSpecificPopulationStats<TGenome>(IList<TGenome> population)
            where TGenome : IGenome<TGenome>;

        #region General Stats

        /// <summary>
        ///     The current generation number.
        /// </summary>
        uint Generation { get; set; }

        /// <summary>
        ///     The total number of genome evaluations for the current NEAT search.
        /// </summary>
        ulong TotalEvaluationCount { get; set; }

        /// <summary>
        ///     Current evaluations per second reading.
        /// </summary>
        int EvaluationsPerSec { get; set; }

        /// <summary>
        ///     The clock time of the last update to _evaluationsPerSec.
        /// </summary>
        DateTime EvalsPerSecLastSampleTime { get; set; }

        /// <summary>
        ///     The total evaluation count at the last update to _evaluationsPerSec.
        /// </summary>
        ulong EvalsCountAtLastUpdate { get; set; }

        #endregion

        #region Fitness Stats

        /// <summary>
        ///     The fitness of the best genome.
        /// </summary>
        double MaxFitness { get; set; }

        /// <summary>
        ///     The mean genome fitness.
        /// </summary>
        double MeanFitness { get; set; }

        /// <summary>
        ///     The mean fitness of current specie champions.
        /// </summary>
        double MeanSpecieChampFitness { get; set; }

        #endregion

        #region Complexity Stats

        /// <summary>
        ///     The complexity of the least complex genome.
        /// </summary>
        double MinComplexity { get; set; }

        /// <summary>
        ///     The complexity of the most complex genome.
        /// </summary>
        double MaxComplexity { get; set; }

        /// <summary>
        ///     The mean genome complexity.
        /// </summary>
        double MeanComplexity { get; set; }

        #endregion

        #region Reproduction Stats

        /// <summary>
        ///     Total number of offspring created in the lifetime of a NEAT search.
        /// </summary>
        ulong TotalOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from asexual reproduction.
        /// </summary>
        ulong AsexualOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from sexual reproduction. This includes
        ///     the number of offspring created from interspecies reproduction.
        /// </summary>
        ulong SexualOffspringCount { get; set; }

        /// <summary>
        ///     Total number of genomes created from interspecies sexual reproduction.
        /// </summary>
        ulong InterspeciesOffspringCount { get; set; }

        #endregion

        #region Specie Stats

        /// <summary>
        ///     The number of genomes in the smallest specie.
        /// </summary>
        int MinSpecieSize { get; set; }

        /// <summary>
        ///     The number of genomes in the largest specie.
        /// </summary>
        int MaxSpecieSize { get; set; }

        #endregion

        #region Moving Averages - Fitness / Complexity

        /// <summary>
        ///     A buffer of the N most recent best fitness values. Allows the calculation of a moving average.
        /// </summary>
        DoubleCircularBufferWithStats BestFitnessMa { get; set; }

        /// <summary>
        ///     A buffer of the N most recent mean specie champ fitness values (the average fitness of all specie champs).
        ///     Allows the calculation of a moving average.
        /// </summary>
        DoubleCircularBufferWithStats MeanSpecieChampFitnessMa { get; set; }

        /// <summary>
        ///     A buffer of the N most recent population mean complexity values.
        ///     Allows the calculation of a moving average.
        /// </summary>
        DoubleCircularBufferWithStats ComplexityMa { get; set; }

        /// <summary>
        ///     The previous moving average value for the 'best fitness' series. Allows testing for fitness stalling by comparing
        ///     with the current MA value.
        /// </summary>
        double PrevBestFitnessMa { get; set; }

        /// <summary>
        ///     The previous moving average value for the 'mean specie champ fitness' series. Allows testing for fitness stalling
        ///     by comparing with the current MA value.
        /// </summary>
        double PrevMeanSpecieChampFitnessMa { get; set; }

        /// <summary>
        ///     The previous moving average value for the complexity series. Allows testing for stalling during the simplification
        ///     phase of complexity regulation.
        /// </summary>
        double PrevComplexityMa { get; set; }

        #endregion
    }
}