#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    /// <inheritdoc />
    /// <summary>
    ///     The maze genome factory handles the construction of new genomes during algorithm initialization and reproduction.
    /// </summary>
    public class MazeGenomeFactory : IGenomeFactory<MazeGenome>
    {
        #region Constructors

        /// <inheritdoc />
        /// <summary>
        ///     Maze Genome Factory default constructor.
        /// </summary>
        public MazeGenomeFactory()
            : this(new MazeGenomeParameters(), 10, 10, 5, 5)
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Maze Genome Factory constructor which takes only maze boundary lengths.
        /// </summary>
        /// <param name="mazeHeight">The height of the maze.</param>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <param name="mazeQuadrantHeight">Maximum height of quadrants (i.e. rooms or submazes) within a maze.</param>
        /// <param name="mazeQuadrantWidth">Maximum width of quadrants (i.e. rooms or submazes) within a maze.</param>
        public MazeGenomeFactory(int mazeHeight, int mazeWidth, int mazeQuadrantHeight, int mazeQuadrantWidth)
            : this(new MazeGenomeParameters(), mazeHeight, mazeWidth, mazeQuadrantHeight, mazeQuadrantWidth)
        {
        }

        /// <summary>
        ///     Maze Genome Factory constructor which takes custom maze genome parameters, initial maze height and width and
        ///     maximum height and width of maze quadrants (subdivisions within the maze).
        /// </summary>
        /// <param name="mazeGenomeParameters">The maze genome parameters.</param>
        /// <param name="mazeHeight">The height of the maze.</param>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <param name="quadrantHeight">Maximum height of quadrants (i.e. rooms or submazes) within a maze.</param>
        /// <param name="quadrantWidth">Maximum width of quadrants (i.e. rooms or submazes) within a maze.</param>
        public MazeGenomeFactory(MazeGenomeParameters mazeGenomeParameters, int mazeHeight, int mazeWidth,
            int quadrantHeight, int quadrantWidth)
        {
            MazeGenomeParameters = mazeGenomeParameters;
            GenomeIdGenerator = new UInt32IdGenerator();
            InnovationIdGenerator = new UInt32IdGenerator();

            // Set the initial maze height and width
            BaseMazeHeight = mazeHeight;
            BaseMazeWidth = mazeWidth;

            // Set quadrant height and width
            MazeQuadrantHeight = quadrantHeight;
            MazeQuadrantWidth = quadrantWidth;
        }

        #endregion

        #region Maze Genome Factory Methods

        /// <summary>
        ///     Creates a new maze genome with the given genome ID and birth generation.
        /// </summary>
        /// <param name="id">The unqiue genome ID.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        /// <returns>The newly constructed maze genome.</returns>
        private MazeGenome CreateGenome(uint id, uint birthGeneration)
        {
            return new MazeGenome(this, id, birthGeneration);
        }

        /// <summary>
        ///     Creates a new maze genome, copying all properties from an existing maze genome except for the unique genome ID and
        ///     the birth generation.
        /// </summary>
        /// <param name="copyFrom">The genome to copy.</param>
        /// <param name="id">The unique genome ID.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        /// <returns>The newly constructed (mostly identical) genome.</returns>
        public static MazeGenome CreateGenomeCopy(MazeGenome copyFrom, uint id, uint birthGeneration)
        {
            return new MazeGenome(copyFrom, id, birthGeneration);
        }

        #endregion

        #region Interface Properties

        /// <inheritdoc />
        /// <summary>
        ///     Unique ID generator for maze genomes.
        /// </summary>
        public UInt32IdGenerator GenomeIdGenerator { get; }

        /// <summary>
        ///     Unique innovation ID generator for maze genes.
        /// </summary>
        public UInt32IdGenerator InnovationIdGenerator { get; }

        /// <inheritdoc />
        /// <summary>
        ///     NOT USED (but required by interface).
        /// </summary>
        public int SearchMode
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        #endregion

        #region Maze Genome Factory Properties

        /// <summary>
        ///     Random number generator.
        /// </summary>
        public readonly FastRandom Rng = new FastRandom();

        /// <summary>
        ///     Parameters which control maze genome evolution.
        /// </summary>
        public MazeGenomeParameters MazeGenomeParameters { get; }

        /// <summary>
        ///     The height of the initial (base) maze genome (at the evolved resolution).
        /// </summary>
        public int BaseMazeHeight { get; }

        /// <summary>
        ///     The width of the initial (base) maze genome (at the evolved resolution).
        /// </summary>
        public int BaseMazeWidth { get; }

        /// <summary>
        ///     Maximum height of quadrants (i.e. rooms or submazes) within a maze.
        /// </summary>
        public int MazeQuadrantHeight { get; }

        /// <summary>
        ///     Maximum width of quadrants (i.e. rooms or submazes) within a maze.
        /// </summary>
        public int MazeQuadrantWidth { get; }

        #endregion

        #region Interface Methods

        /// <inheritdoc />
        /// <summary>
        ///     Creates a list of new maze genomes, the cardinality of which is specified by the length parameter.  All of the new
        ///     genomes will be assigned the given birth generation.
        /// </summary>
        /// <param name="length">The number of genomes to create.</param>
        /// <param name="birthGeneration">The birth generation for all of the genomes.</param>
        /// <returns>The newly created genomes.</returns>
        public List<MazeGenome> CreateGenomeList(int length, uint birthGeneration)
        {
            var genomeList = new List<MazeGenome>(length);

            for (var i = 0; i < length; i++)
            {
                InnovationIdGenerator.Reset();
                genomeList.Add(CreateGenome(birthGeneration));
            }

            return genomeList;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates a list of new maze genomes, the cardinality of which is specified by the length parameter.  All of the new
        ///     genomes will be assigned the given birth generation.  A seed genome will also be used as a template for the new
        ///     genomes.
        /// </summary>
        /// <param name="length">The number of genomes to create.</param>
        /// <param name="birthGeneration">The birth generation for all of the genomes.</param>
        /// <param name="seedGenome">The seed genome to use as a template.</param>
        /// <returns>The newly created genomes.</returns>
        public List<MazeGenome> CreateGenomeList(int length, uint birthGeneration, MazeGenome seedGenome)
        {
            var genomeList = new List<MazeGenome>(length);

            // Add an exact copy of the seed to the list.
            var newGenome = CreateGenomeCopy(seedGenome, GenomeIdGenerator.NextId, birthGeneration);
            genomeList.Add(newGenome);

            // For the remainder we create mutated offspring from the seed.
            for (var i = 1; i < length; i++)
            {
                genomeList.Add(seedGenome.CreateOffspring(birthGeneration));
            }

            return genomeList;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates a list of new maze genomes, the cardinality of which is specified by the length parameter.  All of the new
        ///     genomes will be assigned the given birth generation.  The list of seed genomes will be used as templates for all of
        ///     the newly created genomes.
        /// </summary>
        /// <param name="length">The number of genomes to create.</param>
        /// <param name="birthGeneration">The birth generation for all of the genomes.</param>
        /// <param name="seedGenomeList">The seed genomes to use as templates.</param>
        /// <returns>The newly created genomes.</returns>
        public List<MazeGenome> CreateGenomeList(int length, uint birthGeneration, List<MazeGenome> seedGenomeList)
        {
            if (seedGenomeList.Count == 0)
            {
                throw new SharpNeatException("CreateGenomeList() requires at least on seed genome in seedGenomeList.");
            }

            // Create a copy of the list so that we can shuffle the items without modifying the original list.
            seedGenomeList = new List<MazeGenome>(seedGenomeList);
            Utilities.Shuffle(seedGenomeList, Rng);

            // Make exact copies of seed genomes and insert them into our new genome list.
            var genomeList = new List<MazeGenome>(length);
            var idx = 0;
            var seedCount = seedGenomeList.Count;
            for (var seedIdx = 0; idx < length && seedIdx < seedCount; idx++, seedIdx++)
            {
                // Add an exact copy of the seed to the list.
                var newGenome = CreateGenomeCopy(seedGenomeList[seedIdx], GenomeIdGenerator.NextId, birthGeneration);
                genomeList.Add(newGenome);
            }

            // Keep spawning offspring from seed genomes until we have the required number of genomes.
            for (; idx < length;)
            {
                for (var seedIdx = 0; idx < length && seedIdx < seedCount; idx++, seedIdx++)
                {
                    genomeList.Add(seedGenomeList[seedIdx].CreateOffspring(birthGeneration));
                }
            }

            return genomeList;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Creates a new genome with the given birth generation.
        /// </summary>
        /// <param name="birthGeneration">The genome birth generation.</param>
        /// <returns>The newly created genome.</returns>
        public MazeGenome CreateGenome(uint birthGeneration)
        {
            return CreateGenome(GenomeIdGenerator.NextId, birthGeneration);
        }

        /// <inheritdoc />
        /// <summary>
        ///     NOT IMPLEMENTED (but required by the interface).
        /// </summary>
        /// <param name="genome"></param>
        /// <returns></returns>
        public bool CheckGenomeType(MazeGenome genome)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}