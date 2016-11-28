#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;

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
            Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>();
            int[,] unscaledGrid = new int[genome.MazeBoundaryHeight, genome.MazeBoundaryWidth];

            // Initialize new maze (phenotype)
            MazeStructure maze = new MazeStructure(genome.MazeBoundaryWidth, genome.MazeBoundaryHeight, _scaleMultiplier);

            // Queue up the first "room" (which will encompass the entirety of the maze grid)
            mazeRoomQueue.Enqueue(new MazeStructureRoom(0, 0, genome.MazeBoundaryWidth, genome.MazeBoundaryHeight));

            // Iterate through all of the genes, generating 
            foreach (MazeGene mazeGene in genome.GeneList)
            {
                // Make sure there are rooms left in the queue before attempting to dequeue and bisect
                if (mazeRoomQueue.Count > 0)
                {
                    // Dequeue a room and run division on it
                    Tuple<MazeStructureRoom, MazeStructureRoom> subRooms = mazeRoomQueue.Dequeue()
                        .DivideRoom(unscaledGrid, mazeGene.WallLocation,
                            mazeGene.PassageLocation,
                            mazeGene.OrientationSeed);

                    if (subRooms != null)
                    {
                        // Get the two resulting sub rooms and enqueue both of them
                        mazeRoomQueue.Enqueue(subRooms.Item1);
                        mazeRoomQueue.Enqueue(subRooms.Item2);
                    }
                }
            }

            // Convert to walls and scale to the desired lengths
            maze.ConvertGridArrayToWalls(unscaledGrid);

            return maze;
        }

        #endregion
    }
}