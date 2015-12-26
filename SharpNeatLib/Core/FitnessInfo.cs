/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2012 Colin Green (sharpneat@gmail.com)
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

namespace SharpNeat.Core
{
    /// <summary>
    ///     Wrapper struct for fitness values.
    /// </summary>
    public class FitnessInfo : ITrialInfo
    {
        #region Public members

        /// <summary>
        ///     Precosntructed FitnessInfo for commen case of representing zero fitness.
        /// </summary>
        public static FitnessInfo Zero = new FitnessInfo(0.0, 0.0);

        #endregion

        #region Constructors

        /// <summary>
        ///     Construct with the provided fitness value and auxiliary fitness info.
        /// </summary>
        /// <param name="fitness">The primary fitness of the organism evaluated.</param>
        /// <param name="alternativeFitness">Alternative fitness measure for the organism evaluated.</param>
        public FitnessInfo(double fitness, double alternativeFitness)
        {
            Fitness = fitness;
            AuxFitnessArr = new[] {new AuxFitnessInfo("Alternative Fitness", alternativeFitness)};
        }

        /// <summary>
        ///     Construct with the provided fitness value and auxiliary fitness info array.
        /// </summary>
        /// <param name="fitness">The primary fitness of the organism evaluated.</param>
        /// <param name="auxFitnessArr">Array of alternative fitness measures for the organism evaluated.</param>
        public FitnessInfo(double fitness, AuxFitnessInfo[] auxFitnessArr)
        {
            Fitness = fitness;
            AuxFitnessArr = auxFitnessArr;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Auxiliary fitness info, i.e. for evaluation metrics other than the
        ///     primary fitness metric but that nonetheless we are interested in observing.
        /// </summary>
        public AuxFitnessInfo[] AuxFitnessArr { get; private set; }

        /// <summary>
        ///     Fitness score.
        /// </summary>
        public double Fitness { get; private set; }

        /// <summary>
        ///     The genotypic, phenotypic, or behavioral niche into which the organism under evaluation maps based on the
        ///     evaluation.
        /// </summary>
        public uint NicheId { get; set; }

        #endregion
    }
}