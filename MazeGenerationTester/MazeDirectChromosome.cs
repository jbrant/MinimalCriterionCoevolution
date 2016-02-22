#region

using System.Collections;
using AForge;
using AForge.Genetic;

#endregion

namespace MazeGenerationTester
{
    internal sealed class MazeDirectChromosome : ChromosomeBase
    {
        private static readonly ThreadSafeRandom _randomNumGenerator = new ThreadSafeRandom();
        private readonly int _initialFilledCells;
        private readonly double _initialFilledProportion;
        private readonly int _mazeSideLength;

        public BitArray GeneArray;

        public MazeDirectChromosome(int mazeSideLength, double initialFilledProportion)
        {
            _mazeSideLength = mazeSideLength;
            _initialFilledProportion = initialFilledProportion;
            _initialFilledCells = (int) (initialFilledProportion*(mazeSideLength*mazeSideLength));

            Generate();
        }

        private MazeDirectChromosome(MazeDirectChromosome source)
        {
            _mazeSideLength = source._mazeSideLength;
            GeneArray = source.GeneArray;
            fitness = source.fitness;
        }

        public override void Generate()
        {
            GeneArray = new BitArray(_mazeSideLength*_mazeSideLength);

            // Fill the specified number of cells at random positions
            for (int cnt = 0; cnt < _initialFilledCells; cnt++)
            {
                GeneArray[_randomNumGenerator.Next(GeneArray.Count)] = true;
            }
        }

        public override IChromosome CreateNew()
        {
            return new MazeDirectChromosome(_mazeSideLength, _initialFilledProportion);
        }

        public override IChromosome Clone()
        {
            return new MazeDirectChromosome(this);
        }

        public override void Mutate()
        {
            // Get a random position in the bit string to mutate
            int position = _randomNumGenerator.Next(GeneArray.Count);

            // Flip the bit
            GeneArray[position] = !GeneArray[position];
        }

        public override void Crossover(IChromosome pair)
        {
            MazeDirectChromosome secondChromosome = pair as MazeDirectChromosome;

            // Determine splice location
            int spliceLocation = _randomNumGenerator.Next(GeneArray.Count);

            // Perform single-point crossover
            for (int position = spliceLocation; position < GeneArray.Count; position++)
            {
                GeneArray[position] = secondChromosome.GeneArray[position];
            }
        }
    }
}