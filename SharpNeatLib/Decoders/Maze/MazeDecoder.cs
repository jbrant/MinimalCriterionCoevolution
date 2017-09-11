#region

using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Decoders.Maze
{
    /// <summary>
    ///     The maze decoder translates a given maze genome into its phenotypic representation - a collection of 2D liens which
    ///     constitute the maze walls and are scaled to the desired size/resolution.
    /// </summary>
    public class MazeDecoder : IGenomeDecoder<MazeGenome, MazeStructure>
    {
        #region Instance variables

        // The amount by which to scale the size/length of the walls in the phenotype maze
        private readonly int _scaleMultiplier;

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructor which accepts only the scaling multiplier to scale the maze phenotype walls as desired.
        /// </summary>
        /// <param name="scaleMultiplier">The scaling factor for the phenotypic maze.</param>
        public MazeDecoder(int scaleMultiplier)
        {
            _scaleMultiplier = scaleMultiplier;
        }

        #endregion

        #region Interface Methods

        /// <summary>
        ///     Decodes a given maze genome into its phenotypic maze grid, which specifies the lines (walls) in two dimensional
        ///     space.
        /// </summary>
        /// <param name="genome">The maze genome to decode.</param>
        /// <returns>The maze grid phenotype, which can be directly plotted or fed to an agent for navigation.</returns>
        public MazeStructure Decode(MazeGenome genome)
        {
            // Initialize new maze (phenotype)
            MazeStructure maze = new MazeStructure(genome.MazeBoundaryWidth, genome.MazeBoundaryHeight, _scaleMultiplier);

            // TODO: Generate path

            // Convert to walls and scale to the desired lengths
            maze.ConvertGridArrayToWalls(MazeUtils.ConvertMazeGenomeToUnscaledStructure(genome));

            return maze;
        }

        #endregion
    }
}