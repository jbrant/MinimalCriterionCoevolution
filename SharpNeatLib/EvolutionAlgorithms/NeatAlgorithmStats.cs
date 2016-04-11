/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
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

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    ///     Neat evolution algorithm statistics.
    /// </summary>
    public class NeatAlgorithmStats : ILoggable
    {
        #region Constructor

        /// <summary>
        ///     Construct a NEAT statistics object based on a specified set of NEAT parameters.
        /// </summary>
        public NeatAlgorithmStats(NeatEvolutionAlgorithmParameters eaParams)
        {
            _bestFitnessMA = new DoubleCircularBufferWithStats(eaParams.BestFitnessMovingAverageHistoryLength);
            _meanSpecieChampFitnessMA =
                new DoubleCircularBufferWithStats(eaParams.MeanSpecieChampFitnessMovingAverageHistoryLength);
            _complexityMA = new DoubleCircularBufferWithStats(eaParams.ComplexityMovingAverageHistoryLength);
        }

        #endregion

        #region Logging Methods

        /// <summary>
        ///     Returns NeatAlgorithmStats LoggableElements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The LoggableElements for NeatAlgorithmStats.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
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
                    ? new LoggableElement(EvolutionFieldElements.InterspeciesOffspringCount, _interspeciesOffspringCount)
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

            #endregion
        }

        #region General Stats

        /// <summary>
        ///     The current generation number.
        /// </summary>
        public uint _generation;

        /// <summary>
        ///     The total number of genome evaluations for the current NEAT search.
        /// </summary>
        public ulong _totalEvaluationCount;

        /// <summary>
        ///     Current evaluations per second reading.
        /// </summary>
        public int _evaluationsPerSec;

        /// <summary>
        ///     The clock time of the last update to _evaluationsPerSec.
        /// </summary>
        public DateTime _evalsPerSecLastSampleTime = new DateTime();

        /// <summary>
        ///     The total evaluation count at the last update to _evaluationsPerSec.
        /// </summary>
        public ulong _evalsCountAtLastUpdate;

        #endregion

        #region Fitness Stats

        /// <summary>
        ///     The fitness of the best genome.
        /// </summary>
        public double _maxFitness;

        /// <summary>
        ///     The mean genome fitness.
        /// </summary>
        public double _meanFitness;

        /// <summary>
        ///     The mean fitness of current specie champions.
        /// </summary>
        public double _meanSpecieChampFitness;

        #endregion

        #region Complexity Stats

        /// <summary>
        ///     The complexity of the least complex genome.
        /// </summary>
        public double _minComplexity;

        /// <summary>
        ///     The complexity of the most complex genome.
        /// </summary>
        public double _maxComplexity;

        /// <summary>
        ///     The mean genome complexity.
        /// </summary>
        public double _meanComplexity;

        #endregion

        #region Reproduction Stats

        /// <summary>
        ///     Total number of offspring created in the lifetime of a NEAT search.
        /// </summary>
        public ulong _totalOffspringCount;

        /// <summary>
        ///     Total number of genomes created from asexual reproduction.
        /// </summary>
        public ulong _asexualOffspringCount;

        /// <summary>
        ///     Total number of genomes created from sexual reproduction. This includes
        ///     the number of offspring created from interspecies reproduction.
        /// </summary>
        public ulong _sexualOffspringCount;

        /// <summary>
        ///     Total number of genomes created from interspecies sexual reproduction.
        /// </summary>
        public ulong _interspeciesOffspringCount;

        #endregion

        #region Specie Stats

        /// <summary>
        ///     The number of genomes in the smallest specie.
        /// </summary>
        public int _minSpecieSize;

        /// <summary>
        ///     The number of genomes in the largest specie.
        /// </summary>
        public int _maxSpecieSize;

        #endregion

        #region Moving Averages - Fitness / Complexity

        /// <summary>
        ///     A buffer of the N most recent best fitness values. Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats _bestFitnessMA = new DoubleCircularBufferWithStats(100);

        /// <summary>
        ///     A buffer of the N most recent mean specie champ fitness values (the average fitness of all specie champs).
        ///     Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats _meanSpecieChampFitnessMA = new DoubleCircularBufferWithStats(100);

        /// <summary>
        ///     A buffer of the N most recent population mean complexity values.
        ///     Allows the calculation of a moving average.
        /// </summary>
        public DoubleCircularBufferWithStats _complexityMA = new DoubleCircularBufferWithStats(100);

        /// <summary>
        ///     The previous moving average value for the 'best fitness' series. Allows testing for fitness stalling by comparing
        ///     with the current MA value.
        /// </summary>
        public double _prevBestFitnessMA;

        /// <summary>
        ///     The previous moving average value for the 'mean specie champ fitness' series. Allows testing for fitness stalling
        ///     by comparing with the current MA value.
        /// </summary>
        public double _prevMeanSpecieChampFitnessMA;

        /// <summary>
        ///     The previous moving average value for the complexity series. Allows testing for stalling during the simplification
        ///     phase of complexity regulation.
        /// </summary>
        public double _prevComplexityMA;

        #endregion
    }
}