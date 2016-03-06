#region

using System;
using System.Collections.Generic;
using AForge;
using AForge.Genetic;
using MazeGenerationTester.RecursiveDivision.Evolution;

#endregion

namespace MazeGenerationTester.RecursiveDivision
{
    internal class MazeChromosome : ChromosomeBase
    {
        private readonly ThreadSafeRandom _randomNumGenerator = new ThreadSafeRandom();
        public List<MazeGene> GeneList;

        public MazeChromosome()
        {
        }

        public MazeChromosome(MazeChromosome source)
        {
            GeneList = source.GeneList;
        }

        public override void Generate()
        {
            GeneList = new List<MazeGene>();
        }

        public override IChromosome CreateNew()
        {
            return new MazeChromosome();
        }

        public override IChromosome Clone()
        {
            return new MazeChromosome(this);
        }

        public override void Mutate()
        {
            // Get a random position in the chromosome to mutate
            int genePosition = _randomNumGenerator.Next(GeneList.Count);

            // Mutate the gene at that location
            GeneList[genePosition].Mutate(_randomNumGenerator);
        }

        public void MutateAddGene()
        {
            GeneList.Add(new MazeGene(_randomNumGenerator.NextDouble(), _randomNumGenerator.NextDouble()));
        }

        public override void Crossover(IChromosome pair)
        {
            throw new NotImplementedException();
        }
    }
}