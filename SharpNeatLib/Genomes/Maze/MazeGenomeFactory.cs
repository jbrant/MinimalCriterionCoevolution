#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    public class MazeGenomeFactory : IGenomeFactory<MazeGenome>
    {
        private readonly UInt32IdGenerator _genomeIdGenerator;
        private readonly FastRandom _rng = new FastRandom();

        #region Maze Genome Factory Methods

        public MazeGenome CreateGenome(uint id, uint birthGeneration, double wallStartLocation,
            double passageStartLocation)
        {
            return new MazeGenome(this, id, birthGeneration, wallStartLocation, passageStartLocation);
        }

        public MazeGenome CreateGenomeCopy(MazeGenome copyFrom, uint id, uint birthGeneration)
        {
            return new MazeGenome(copyFrom, id, birthGeneration);
        }

        #endregion

        #region Interface Properties

        public UInt32IdGenerator GenomeIdGenerator { get; private set; }

        public int SearchMode
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion

        #region Interface Methods

        public List<MazeGenome> CreateGenomeList(int length, uint birthGeneration)
        {
            List<MazeGenome> genomeList = new List<MazeGenome>(length);

            for (int i = 0; i < length; i++)
            {
                genomeList.Add(CreateGenome(birthGeneration));
            }

            return genomeList;
        }

        public List<MazeGenome> CreateGenomeList(int length, uint birthGeneration, MazeGenome seedGenome)
        {
            List<MazeGenome> genomeList = new List<MazeGenome>(length);

            // Add an exact copy of the seed to the list.
            MazeGenome newGenome = CreateGenomeCopy(seedGenome, _genomeIdGenerator.NextId, birthGeneration);
            genomeList.Add(newGenome);

            // For the remainder we create mutated offspring from the seed.
            for (int i = 1; i < length; i++)
            {
                genomeList.Add(seedGenome.CreateOffspring(birthGeneration));
            }
            return genomeList;
        }

        public List<MazeGenome> CreateGenomeList(int length, uint birthGeneration, List<MazeGenome> seedGenomeList)
        {
            if (seedGenomeList.Count == 0)
            {
                throw new SharpNeatException("CreateGenomeList() requires at least on seed genome in seedGenomeList.");
            }

            // Create a copy of the list so that we can shuffle the items without modifying the original list.
            seedGenomeList = new List<MazeGenome>(seedGenomeList);
            Utilities.Shuffle(seedGenomeList, _rng);

            // Make exact copies of seed genomes and insert them into our new genome list.
            List<MazeGenome> genomeList = new List<MazeGenome>(length);
            int idx = 0;
            int seedCount = seedGenomeList.Count;
            for (int seedIdx = 0; idx < length && seedIdx < seedCount; idx++, seedIdx++)
            {
                // Add an exact copy of the seed to the list.
                MazeGenome newGenome = CreateGenomeCopy(seedGenomeList[seedIdx], _genomeIdGenerator.NextId,
                    birthGeneration);
                genomeList.Add(newGenome);
            }

            // Keep spawning offspring from seed genomes until we have the required number of genomes.
            for (; idx < length;)
            {
                for (int seedIdx = 0; idx < length && seedIdx < seedCount; idx++, seedIdx++)
                {
                    genomeList.Add(seedGenomeList[seedIdx].CreateOffspring(birthGeneration));
                }
            }
            return genomeList;
        }

        public MazeGenome CreateGenome(uint birthGeneration)
        {
            return CreateGenome(_genomeIdGenerator.NextId, birthGeneration, _rng.NextDouble(), _rng.NextDouble());
        }

        public bool CheckGenomeType(MazeGenome genome)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}