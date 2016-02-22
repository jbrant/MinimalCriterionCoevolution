#region

using System;
using AForge;
using AForge.Genetic;

#endregion

namespace MazeGenerationTester
{
    internal sealed class MazePositiveIndirectChromosome : ChromosomeBase
    {
        private const int _maxGeneValue = 9999;
        private static readonly ThreadSafeRandom _randomNumGenerator = new ThreadSafeRandom();
        private readonly int _cellsFilledLimit;
        private readonly int _geneLength;
        private readonly int _mazeSideLength;
        public Tuple<int, int>[] GeneArray;

        public MazePositiveIndirectChromosome(int mazeSideLength, int geneLength, int cellsFilledLimit)
        {
            _mazeSideLength = mazeSideLength;
            _geneLength = geneLength;
            _cellsFilledLimit = cellsFilledLimit;

            Generate();
        }

        private MazePositiveIndirectChromosome(MazePositiveIndirectChromosome source)
        {
            _mazeSideLength = source._mazeSideLength;
            GeneArray = source.GeneArray;
            fitness = source.fitness;
        }

        public override void Generate()
        {
            GeneArray = new Tuple<int, int>[_geneLength];
        }

        public override IChromosome CreateNew()
        {
            return new MazePositiveIndirectChromosome(_mazeSideLength, _geneLength, _cellsFilledLimit);
        }

        public override IChromosome Clone()
        {
            return new MazePositiveIndirectChromosome(this);
        }

        public override void Mutate()
        {
            // Get a random position in the chromosome to mutate
            int position = _randomNumGenerator.Next(GeneArray.Length);

            // Generate a new tuple at the selected position
            GeneArray[position] = new Tuple<int, int>(_randomNumGenerator.Next(_maxGeneValue),
                _randomNumGenerator.Next(_maxGeneValue));
        }

        public override void Crossover(IChromosome pair)
        {
            MazePositiveIndirectChromosome secondChromosome = (MazePositiveIndirectChromosome) pair;

            // Determine splice location
            int spliceLocation = _randomNumGenerator.Next(GeneArray.Length);

            // Perform single-point crossover
            for (int position = spliceLocation; position < GeneArray.Length; position++)
            {
                GeneArray[position] = secondChromosome.GeneArray[position];
            }
        }
    }
}