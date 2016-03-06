#region

using AForge;

#endregion

namespace MazeGenerationTester.RecursiveDivision.Evolution
{
    public struct MazeGene
    {
        public MazeGene(double wallStartPosition, double passageStartPosition)
        {
            WallStartPosition = wallStartPosition;
            PassageStartPosition = passageStartPosition;
        }

        public double WallStartPosition { get; private set; }
        public double PassageStartPosition { get; private set; }

        public void Mutate(ThreadSafeRandom randomNumberGenerator)
        {
            // Randomly assign new wall and passage positions
            WallStartPosition = randomNumberGenerator.NextDouble();
            PassageStartPosition = randomNumberGenerator.NextDouble();
        }
    }
}