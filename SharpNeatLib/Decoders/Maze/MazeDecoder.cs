#region

using System;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeat.Decoders.Maze
{
    internal class MazeDecoder : IGenomeDecoder<MazeGenome, MazeGrid>
    {
        public MazeGrid Decode(MazeGenome genome)
        {
            throw new NotImplementedException();
        }
    }
}