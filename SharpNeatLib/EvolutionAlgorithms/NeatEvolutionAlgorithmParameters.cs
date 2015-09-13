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

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    ///     Parameters specific to the NEAT evolution algorithm.
    /// </summary>
    public class NeatEvolutionAlgorithmParameters
    {
        #region Private Methods

        /// <summary>
        ///     Normalize the sexual and asexual proportions such that their sum equals 1.
        /// </summary>
        private void NormalizeProportions()
        {
            double total = OffspringAsexualProportion + OffspringSexualProportion;
            OffspringAsexualProportion = OffspringAsexualProportion/total;
            OffspringSexualProportion = OffspringSexualProportion/total;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Creates a set of parameters based on the current set and that are suitable for the simplifying
        ///     phase of the evolution algorithm when running with complexity regulation enabled.
        /// </summary>
        public NeatEvolutionAlgorithmParameters CreateSimplifyingParameters()
        {
            // Make a copy of the current 'complexifying' parameters (as required by complexity regulation)
            // and modify the copy to be suitable for simplifcation. Basically we disable sexual reproduction
            // whle in simplifying mode to prevent proliferation of structure through sexual reproduction.
            NeatEvolutionAlgorithmParameters eaParams = new NeatEvolutionAlgorithmParameters(this)
            {
                OffspringAsexualProportion = 1.0,
                OffspringSexualProportion = 0.0
            };

            return eaParams;
        }

        #endregion

        #region Constants

        private const int DefaultSpecieCount = 10;
        private const double DefaultElitismProportion = 0.2;
        private const double DefaultSelectionProportion = 0.2;

        private const double DefaultOffspringAsexualProportion = 0.5;
        private const double DefaultOffspringSexualProportion = 0.5;
        private const double DefaultInterspeciesMatingProportion = 0.01;

        private const int DefaultDestFitnessMovingAverageHistoryLength = 100;
        private const int DefgaultMeanSpecieChampFitnessMovingAverageHistoryLength = 100;
        private const int DefaultComplexityMovingAverageHistoryLength = 100;

        private const int DefaultMinTimeAlive = 5;

        #endregion

        #region Constructor

        /// <summary>
        ///     Constructs with the default parameters.
        /// </summary>
        public NeatEvolutionAlgorithmParameters()
        {
            SpecieCount = DefaultSpecieCount;
            ElitismProportion = DefaultElitismProportion;
            SelectionProportion = DefaultSelectionProportion;

            OffspringAsexualProportion = DefaultOffspringAsexualProportion;
            OffspringSexualProportion = DefaultOffspringSexualProportion;
            InterspeciesMatingProportion = DefaultInterspeciesMatingProportion;

            BestFitnessMovingAverageHistoryLength = DefaultDestFitnessMovingAverageHistoryLength;
            MeanSpecieChampFitnessMovingAverageHistoryLength = DefgaultMeanSpecieChampFitnessMovingAverageHistoryLength;
            ComplexityMovingAverageHistoryLength = DefaultComplexityMovingAverageHistoryLength;

            MinTimeAlive = DefaultMinTimeAlive;

            NormalizeProportions();
        }

        /// <summary>
        ///     Copy constructor.
        /// </summary>
        public NeatEvolutionAlgorithmParameters(NeatEvolutionAlgorithmParameters copyFrom)
        {
            SpecieCount = copyFrom.SpecieCount;
            ElitismProportion = copyFrom.ElitismProportion;
            SelectionProportion = copyFrom.SelectionProportion;

            OffspringAsexualProportion = copyFrom.OffspringAsexualProportion;
            OffspringSexualProportion = copyFrom.OffspringSexualProportion;
            InterspeciesMatingProportion = copyFrom.InterspeciesMatingProportion;

            BestFitnessMovingAverageHistoryLength = copyFrom.BestFitnessMovingAverageHistoryLength;
            MeanSpecieChampFitnessMovingAverageHistoryLength = copyFrom.MeanSpecieChampFitnessMovingAverageHistoryLength;
            ComplexityMovingAverageHistoryLength = copyFrom.ComplexityMovingAverageHistoryLength;

            MinTimeAlive = copyFrom.MinTimeAlive;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the specie count.
        /// </summary>
        public int SpecieCount { get; set; }

        /// <summary>
        ///     Gets or sets the elitism proportion.
        ///     We sort specie genomes by fitness and keep the top N%, the other genomes are
        ///     removed to make way for the offspring.
        /// </summary>
        public double ElitismProportion { get; set; }

        /// <summary>
        ///     Gets or sets the selection proportion.
        ///     We sort specie genomes by fitness and select parent genomes for producing offspring from
        ///     the top N%. Selection is performed prior to elitism being applied, therefore selecting from more
        ///     genomes than will be made elite is possible.
        /// </summary>
        public double SelectionProportion { get; set; }

        /// <summary>
        ///     Gets or sets the proportion of offspring to be produced from asexual reproduction (mutation).
        /// </summary>
        public double OffspringAsexualProportion { get; set; }

        /// <summary>
        ///     Gets or sets the proportion of offspring to be produced from sexual reproduction.
        /// </summary>
        public double OffspringSexualProportion { get; set; }

        /// <summary>
        ///     Gets or sets the proportion of sexual reproductions that will use genomes from different species.
        /// </summary>
        public double InterspeciesMatingProportion { get; set; }

        /// <summary>
        ///     Gets or sets the history buffer length used for calculating the best fitness moving average.
        /// </summary>
        public int BestFitnessMovingAverageHistoryLength { get; set; }

        /// <summary>
        ///     Gets or sets the history buffer length used for calculating the mean specie champ fitness
        ///     moving average.
        /// </summary>
        public int MeanSpecieChampFitnessMovingAverageHistoryLength { get; set; }

        /// <summary>
        ///     Gets or sets the history buffer length used for calculating the mean genome complexity moving
        ///     average.
        /// </summary>
        public int ComplexityMovingAverageHistoryLength { get; set; }

        /// <summary>
        ///     Gets or sets the minimum time that a genome must exist before being considered for removal.  This is really only
        ///     applicable for non-generational (e.g. steady state) algorithms.
        /// </summary>
        public int MinTimeAlive { get; set; }

        #endregion
    }
}