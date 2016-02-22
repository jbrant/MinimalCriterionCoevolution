#region

using System;
using AForge.Genetic;

#endregion

namespace MazeGenerationTester
{
    public class ExampleFitnessFunction : IFitnessFunction
    {
        private readonly Random _rand = new Random();

        public double Evaluate(IChromosome chromosome)
        {
            return _rand.NextDouble();
        }
    }
}