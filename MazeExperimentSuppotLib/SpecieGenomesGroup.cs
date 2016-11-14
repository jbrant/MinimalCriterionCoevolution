#region

using System.Collections.Generic;

#endregion

namespace MazeExperimentSuppotLib
{
    /// <summary>
    ///     Encapsulates the unique identifier for a single species and the unique identifiers of its constituent genomes.
    /// </summary>
    public struct SpecieGenomesGroup
    {
        #region Constructor

        /// <summary>
        ///     Specie genomes group constructor.
        /// </summary>
        /// <param name="specieId">The unique specie identifier.</param>
        /// <param name="genomeIds">The unique identifiers of all genomes within the specie.</param>
        public SpecieGenomesGroup(int specieId, IList<int> genomeIds)
        {
            SpecieId = specieId;
            GenomeIds = genomeIds;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The unique specie identifier.
        /// </summary>
        public int SpecieId { get; }

        /// <summary>
        ///     The unique identifiers of all genomes within the specie.
        /// </summary>
        public IList<int> GenomeIds { get; }

        #endregion
    }
}