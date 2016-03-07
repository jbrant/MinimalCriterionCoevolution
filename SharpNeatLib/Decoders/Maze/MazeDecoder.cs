#region

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeat.Decoders.Maze
{
    internal class MazeDecoder : IGenomeDecoder<MazeGenome, MazeGrid>
    {
        #region Interface Methods

        public MazeGrid Decode(MazeGenome genome)
        {            
            Queue<MazeRoom> mazeRoomQueue = new Queue<MazeRoom>();
            int[,] unscaledGrid = new int[_mazeBoundaryHeight, _mazeBoundaryWidth];

            // Initialize new maze (phenotype)
            MazeGrid maze = new MazeGrid(_mazeBoundaryWidth, _mazeBoundaryHeight, _scaleMultiplier);

            // Queue up the first "room" (which will encompass the entirety of the maze grid)
            mazeRoomQueue.Enqueue(new MazeRoom(0, 0, _mazeBoundaryWidth, _mazeBoundaryHeight));

            // Iterate through all of the genes, generating 
            foreach (MazeGene mazeGene in genome.GeneList)
            {
                // Dequeue a room and run division on it
                Tuple<MazeRoom, MazeRoom> subRooms = mazeRoomQueue.Dequeue()
                    .DivideRoom(unscaledGrid, mazeGene.WallLocation,
                        mazeGene.PassageLocation,
                        mazeGene.OrientationSeed);

                if (subRooms != null)
                {
                    // Get the two resulting sub rooms and enqueue both of them
                    mazeRoomQueue.Enqueue(subRooms.Item1);
                    mazeRoomQueue.Enqueue(subRooms.Item2);
                }
                else
                {
                    // Otherwise, break out of the loop because we can't subdivide anymore
                    break;
                }
            }

            return null;
        }

        #endregion

        #region Instance variables

        private readonly int _mazeBoundaryHeight;
        private readonly int _mazeBoundaryWidth;
        private readonly int _scaleMultiplier;

        #endregion

        #region Constructors

        public MazeDecoder(int mazeBoundaryHeight, int mazeBoundaryWidth) : this(mazeBoundaryHeight, mazeBoundaryWidth, 1)
        {
        }

        public MazeDecoder(int mazeBoundaryHeight, int mazeBoundaryWidth, int scaleMultiplier)
        {
            _mazeBoundaryHeight = mazeBoundaryHeight;
            _mazeBoundaryWidth = mazeBoundaryWidth;
            _scaleMultiplier = scaleMultiplier;
        }

        #endregion

        #region Private Methods

        #endregion
    }
}