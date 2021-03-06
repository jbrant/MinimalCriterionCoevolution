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

using System.Collections.Generic;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Represents a strategy for dividing genomes into distinct species.
    ///     Speciation in NEAT is the process of dividing genomes in the population into distinct sub-populations (species)
    ///     based on genome similarity, that is, we want similar genomes to be in the same specie so that they form a
    ///     gene pool that is more likely to produce fit offspring. This type of speciation is very much like the concept
    ///     of clustering as used in the fields of computer science and data mining. This interface allows us to abstract
    ///     the implementation of the speciation/clustering algorithm away from the main NEAT algorithm.
    ///     Each cluster/specie is assigned an ID that is in turn assigned to the genomes in the cluster. In addition each
    ///     instance of the Specie class contains a list of all genomes within that specie.
    /// </summary>
    public interface ISpeciationStrategy<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        ///     Speciates the genomes in genomeList into the number of species specified by specieCount
        ///     and returns a newly constructed list of Specie objects containing the speciated genomes.
        /// </summary>
        IList<Specie<TGenome>> InitializeSpeciation(IList<TGenome> genomeList, int specieCount);

        /// <summary>
        ///     Speciates given the total population size into the number of species specified by specieCount
        ///     and returns a newly constructed list of Specie objects with the associated carrying capacities.
        ///     Note that this method does not actually speciate any genomes yet.
        /// </summary>
        IList<Specie<TGenome>> InitializeSpeciation(int genomeCount, int specieCount);

        /// <summary>
        ///     Speciates the genomes in genomeList into the provided species. It is assumed that
        ///     the genomeList represents all of the required genomes and that the species are currently empty.
        ///     This method can be used for initialization or completely respeciating an existing genome population.
        /// </summary>
        void SpeciateGenomes(IList<TGenome> genomeList, IList<Specie<TGenome>> specieList);

        /// <summary>
        ///     Speciates the offspring genomes in genomeList into the provided species. In contrast to
        ///     SpeciateGenomes() genomeList is taken to be a list of new genomes (e.g. offspring) that should be
        ///     added to existing species. That is, the specieList contain genomes that are not in genomeList
        ///     that we wish to keep; typically these would be elite genomes that are the parents of the
        ///     offspring.
        /// </summary>
        void SpeciateOffspring(IList<TGenome> genomeList, IList<Specie<TGenome>> specieList, bool respeciate);

        /// <summary>
        ///     Determines the closest species to each offspring genome and returns a dictionary indicating
        ///     the affected species along with a count of genomes assigned to that species.  Importantly, this
        ///     does not physically speciate the genomes themselves.
        /// </summary>
        /// <param name="offspringList">The list of genomes for which to determine closest species.</param>
        /// <param name="specieList">The list of species against which to compare genome distance.</param>
        /// <returns>The number of genomes assigned to each species.</returns>
        IDictionary<Specie<TGenome>, int> FindClosestSpecieAssignments(IList<TGenome> offspringList,
            IList<Specie<TGenome>> specieList);
    }
}