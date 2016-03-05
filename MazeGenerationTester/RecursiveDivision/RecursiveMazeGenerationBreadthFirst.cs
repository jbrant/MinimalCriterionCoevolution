using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazeGenerationTester.RecursiveDivision
{
    public class RecursiveMazeGenerationBreadthFirst
    {
        private readonly Queue<MazeRoom> _mazeRoomQueue = new Queue<MazeRoom>();
        private readonly Random _randomGenerator = new Random(1234);

        public RecursiveMazeGenerationBreadthFirst(int width, int height, Random randomNumGenerator)
        {
            Grid = new int[height, width];
            _randomGenerator = randomNumGenerator;
        }

        public int[,] Grid { get; }

        public void RunBreadthFirstGeneration(int maxIterations)
        {
            int curDivisionIterations = 0;

            // Create the first "room" (which will encompass the entirety of the maze space) and enqueue it
            _mazeRoomQueue.Enqueue(new MazeRoom(0, 0, Grid.GetLength(1), Grid.GetLength(0), _randomGenerator));

            // Begin the division loop
            do
            {
                // Dequeue a room and run division on it
                Tuple<MazeRoom, MazeRoom> subRooms = _mazeRoomQueue.Dequeue().DivideRoom(Grid);

                if (subRooms != null)
                {
                    // Get the two resulting sub rooms and enqueue both of them
                    _mazeRoomQueue.Enqueue(subRooms.Item1);
                    _mazeRoomQueue.Enqueue(subRooms.Item2);
                }

                // Increment the number of division iterations
                curDivisionIterations++;
            } while (_mazeRoomQueue.Count > 0 && curDivisionIterations < maxIterations);
        }
    }
}
