using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;

namespace SharpNeat.Utility
{
    public static class MazeUtils
    {
        public static int DetermineMaxPartitions(int mazeHeight, int mazeWidth, int numSamples)
        {
            int[] maxMazeResolutions = new int[numSamples];

            for (int curSample = 0; curSample < numSamples; curSample++)
            {
                Queue<MazeStructureRoom> mazeRoomQueue = new Queue<MazeStructureRoom>();
                int[,] mazeSegments = new int[mazeHeight, mazeWidth];

                // Queue up the first "room" (which will encompass the entirety of the maze grid)
                mazeRoomQueue.Enqueue(new MazeStructureRoom(0, 0, mazeWidth, mazeHeight));


            }
        }
    }
}
