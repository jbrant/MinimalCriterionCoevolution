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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpNeat.Core;

namespace SharpNeat.SpeciationStrategies
{
    /// <summary>
    ///     Static helper methods for speciation.
    /// </summary>
    public static class SpeciationUtils<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        ///     Returns true if all of the species are empty.
        /// </summary>
        public static bool TestEmptySpecies(IList<Specie<TGenome>> specieList)
        {
            foreach (var specie in specieList)
            {
                if (specie.GenomeList.Count != 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Returns true if all species contain at least 1 genome.
        /// </summary>
        public static bool TestPopulatedSpecies(IList<Specie<TGenome>> specieList)
        {
            foreach (var specie in specieList)
            {
                if (specie.GenomeList.Count == 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Perform an integrity check on the provided species.
        ///     Returns true if everything is OK.
        /// </summary>
        public static bool PerformIntegrityCheck(IList<Specie<TGenome>> specieList)
        {
            // Check that all species contain at least one genome.
            // Also check that the specieIdx of each genome corresponds to the specie it is within.
            foreach (var specie in specieList)
            {
                if (specie.GenomeList.Count == 0)
                {
                    Debug.WriteLine(
                        "Empty species. SpecieIdx = [{0}]. Speciation must allocate at least one genome to each specie.",
                        specie.Idx);
                    return false;
                }

                foreach (var genome in specie.GenomeList)
                {
                    if (genome.SpecieIdx != specie.Idx)
                    {
                        Debug.WriteLine("Genome with incorrect specieIdx [{0}]. Parent SpecieIdx = [{1}]",
                            genome.SpecieIdx, specie.Idx);
                        return false;
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        ///     Prints debugging information regarding the count of genomes per species and that species target size.
        /// </summary>
        /// <param name="specieStatsArr">The species statistics array.</param>
        /// <param name="specieList">The list of species.</param>
        public static void DumpSpecieCounts(SpecieStats[] specieStatsArr, IList<Specie<TGenome>> specieList)
        {
            var count = specieStatsArr.Length;
            for (var i = 0; i < count; i++)
            {
                Debug.Write("[" + specieList[i].GenomeList.Count + "," + specieStatsArr[i].TargetSizeInt + "] ");
            }
            Debug.WriteLine(String.Empty);
        }

        /// <summary>
        ///     Returns the total target size for all species in the population.
        /// </summary>
        /// <param name="specieStatsArr">The statistics for all species in the population.</param>
        /// <returns>The total target size for all species in the population.</returns>
        public static int SumTargetSizeInt(SpecieStats[] specieStatsArr)
        {
            var total = 0;
            foreach (var inst in specieStatsArr)
            {
                total += inst.TargetSizeInt;
            }
            return total;
        }
    }
}