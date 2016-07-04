#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    /// <summary>
    ///     The maze genome contains maze genes (which themselves encode the walls and their passages in the form of
    ///     real-valued numbers) and provides routines for mutating those genes and producing offspring.
    /// </summary>
    public class MazeGenome : IGenome<MazeGenome>
    {
        #region Instance Variables

        private CoordinateVector _position;

        #endregion

        #region Maze Properties

        /// <summary>
        ///     Reference to the maze genome factory (for the purposes of creating offspring).
        /// </summary>
        public MazeGenomeFactory GenomeFactory { get; set; }

        /// <summary>
        ///     The list of maze genes composing the genome (each gene encodes the un-normalized location of a wall in the maze and
        ///     its passage).
        /// </summary>
        public IList<MazeGene> GeneList { get; }

        #endregion

        #region Maze Genome Constructors

        /// <summary>
        ///     Constructor which constructs a new maze genome with the given unique identifier and birth generation.
        /// </summary>
        /// <param name="genomeFactory">Reference to the genome factory.</param>
        /// <param name="id">The unique identifier of the new maze genome.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        public MazeGenome(MazeGenomeFactory genomeFactory, uint id, uint birthGeneration)
        {
            // Set the genome factory
            GenomeFactory = genomeFactory;

            // Set the unique genome ID and the birth generation
            Id = id;
            BirthGeneration = birthGeneration;

            // Create new evaluation info with no fitness history
            EvaluationInfo = new EvaluationInfo(0);

            // Instantiate new gene list
            GeneList = new List<MazeGene>();
        }

        /// <summary>
        ///     Constructor which constructs a new maze genome using the given base genome (this is often used in asexual
        ///     reproduction).  The new genome still has a unique identifier and a separately specified birth generation.
        /// </summary>
        /// <param name="copyFrom">The template genome on which the new maze genome is based.</param>
        /// <param name="id">The unique identifier of the new maze genome.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        public MazeGenome(MazeGenome copyFrom, uint id, uint birthGeneration)
        {
            // Set the unique genome ID and the birth generation
            Id = id;
            BirthGeneration = birthGeneration;

            // Copy the other parameters off of the given genome
            GenomeFactory = copyFrom.GenomeFactory;
            GeneList = new List<MazeGene>(copyFrom.GeneList);
            EvaluationInfo = new EvaluationInfo(copyFrom.EvaluationInfo.FitnessHistoryLength);
        }

        /// <summary>
        ///     Constructor which constructs a new maze genome with the given unique identifier, birth generation, and list of wall
        ///     genes.
        /// </summary>
        /// <param name="genomeFactory">Reference to the genome factory.</param>
        /// <param name="id">The unique identifier of the new maze genome.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        /// <param name="geneList">The list of wall genes.</param>
        public MazeGenome(MazeGenomeFactory genomeFactory, uint id, uint birthGeneration, IList<MazeGene> geneList)
            : this(genomeFactory, id, birthGeneration)
        {
            GeneList = geneList;
        }

        #endregion

        #region Interface Properties

        /// <summary>
        ///     The unique identifier of the maze genome.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        ///     NOT USED (required by interface).
        /// </summary>
        public int SpecieIdx { get; set; }

        /// <summary>
        ///     The birth generation of the maze genome.
        /// </summary>
        public uint BirthGeneration { get; }

        /// <summary>
        ///     Evaluation statistics for the maze genome (i.e. fitness, etc.).
        /// </summary>
        public EvaluationInfo EvaluationInfo { get; }

        /// <summary>
        ///     Computes the complexity of the maze genome.
        /// </summary>
        public double Complexity
        {
            // TODO: Need to figure out how to compute maze complexity
            get { return GeneList.Count; }
        }

        /// <summary>
        ///     Gets a coordinate that represents the genome's position in the search space (also known
        ///     as the genetic encoding space). This allows speciation/clustering algorithms to operate on
        ///     an abstract cordinate data type rather than being coded against specific IGenome types.
        /// </summary>
        public CoordinateVector Position
        {
            get
            {
                if (null == _position)
                {
                    int interiorWallCount = GeneList.Count;

                    // Create array of key/value pairs to hold innovation IDs and their corresponding 
                    // "position" in the genetic encoding space                    
                    KeyValuePair<ulong, double>[] coordElemArray = new KeyValuePair<ulong, double>[interiorWallCount];

                    for (int i = 0; i < interiorWallCount; i++)
                    {
                        double wallLocation = GeneList[i].WallLocation;
                        double passageLocation = GeneList[i].PassageLocation;

                        // Calculate cantor pairing of relative wall and passage positions
                        double compositeGeneCoordinate = ((wallLocation + passageLocation)*
                                                          (wallLocation + passageLocation + 1))/2 + passageLocation;

                        // Add gene coordinate to array
                        coordElemArray[i] = new KeyValuePair<ulong, double>(GeneList[i].InnovationId,
                            compositeGeneCoordinate);
                    }

                    // Construct the genome coordinate vector
                    _position = new CoordinateVector(coordElemArray);
                }

                return _position;
            }
        }

        /// <summary>
        ///     The decoded phenotype produced from this maze genome (i.e. the physical maze itself).  This is used to speed up the
        ///     decoding process.
        /// </summary>
        public object CachedPhenome { get; set; }

        #endregion

        #region Public Interface Methods

        /// <summary>
        ///     Asexually reproduces a new maze genome based on copying this genome and assigning the given birth generation.
        /// </summary>
        /// <param name="birthGeneration">The birth generation of the new maze genome.</param>
        /// <returns>The new maze genome.</returns>
        public MazeGenome CreateOffspring(uint birthGeneration)
        {
            // Make a new genome that is a copy of this one but with a new genome ID
            MazeGenome offspring = GenomeFactory.CreateGenomeCopy(this, GenomeFactory.GenomeIdGenerator.NextId,
                birthGeneration);

            // Mutate the new genome
            offspring.Mutate();

            return offspring;
        }

        /// <summary>
        ///     NOT USED (required by interface) - this would perform sexual reproduction (i.e. crossover), but that's not
        ///     currently supported.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="birthGeneration"></param>
        /// <returns></returns>
        public MazeGenome CreateOffspring(MazeGenome parent, uint birthGeneration)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Maze Genome Methods

        /// <summary>
        ///     Performs a mutation operation, either shift the wall location, shifting the passage location, or adding a new wall
        ///     (gene).
        /// </summary>
        private void Mutate()
        {
            int outcome;

            // If there are not yet any walls to mutate, the mutation will be to add a wall
            // (otherwise, the resulting maze will be exactly the same structure)
            if (GeneList.Count <= 0)
            {
                MutateAddWall();
                return;
            }

            do
            {
                // Get random mutation to perform
                outcome = RouletteWheel.SingleThrow(GenomeFactory.MazeGenomeParameters.RouletteWheelLayout,
                    GenomeFactory.Rng);
            } while (GenomeFactory.MaxComplexity != null && GeneList.Count > GenomeFactory.MaxComplexity && outcome >= 2);


            switch (outcome)
            {
                case 0:
                    MutateWallStartLocations();
                    break;
                case 1:
                    MutatePassageStartLocations();
                    break;
                case 2:
                    MutateAddWall();
                    break;
            }
        }

        /// <summary>
        ///     Mutates the location of the wall based on both the wall start mutation probability and the perturbance magnitude.
        /// </summary>
        private void MutateWallStartLocations()
        {
            bool mutationOccurred = false;

            // Don't try to mutate if the gene list is empty
            if (GeneList.Count <= 0)
                return;

            // Iterate through each gene (wall) and probabilistically shift its location
            foreach (MazeGene mazeGene in GeneList)
            {
                if (GenomeFactory.Rng.NextDouble() <
                    GenomeFactory.MazeGenomeParameters.MutateWallStartLocationProbability)
                {
                    mazeGene.WallLocation = BoundStartLocation(mazeGene.WallLocation +
                                                               (((GenomeFactory.Rng.NextDouble()*2) - 1)*
                                                                GenomeFactory.MazeGenomeParameters
                                                                    .PerturbanceMagnitude));
                    mutationOccurred = true;
                }
            }

            // Ensure that a mutation actually occurs
            if (mutationOccurred == false)
            {
                // Select a random gene to mutate
                MazeGene mazeGene = GeneList[GenomeFactory.Rng.Next(GeneList.Count)];

                // Perform mutation
                mazeGene.WallLocation = BoundStartLocation(mazeGene.WallLocation +
                                                           (((GenomeFactory.Rng.NextDouble()*2) - 1)*
                                                            GenomeFactory.MazeGenomeParameters
                                                                .PerturbanceMagnitude));
            }
        }

        /// <summary>
        ///     Mutates the location of the wall passage based on both the passage start mutation probability and the perturbance
        ///     magnitude.
        /// </summary>
        private void MutatePassageStartLocations()
        {
            bool mutationOccurred = false;

            // Don't try to mutate if the gene list is empty
            if (GeneList.Count <= 0)
                return;

            // Iterate through each gene (wall) and probabilistically shift its passage location
            foreach (MazeGene mazeGene in GeneList)
            {
                if (GenomeFactory.Rng.NextDouble() <
                    GenomeFactory.MazeGenomeParameters.MutatePassageStartLocationProbability)
                {
                    mazeGene.PassageLocation = BoundStartLocation(mazeGene.PassageLocation +
                                                                  (((GenomeFactory.Rng.NextDouble()*2) - 1)*
                                                                   GenomeFactory.MazeGenomeParameters
                                                                       .PerturbanceMagnitude));

                    mutationOccurred = true;
                }
            }

            // Ensure that a mutation actually occurs
            if (mutationOccurred == false)
            {
                // Select a random gene to mutate
                MazeGene mazeGene = GeneList[GenomeFactory.Rng.Next(GeneList.Count)];

                mazeGene.PassageLocation = BoundStartLocation(mazeGene.WallLocation +
                                                              (((GenomeFactory.Rng.NextDouble()*2) - 1)*
                                                               GenomeFactory.MazeGenomeParameters
                                                                   .PerturbanceMagnitude));
            }
        }

        /// <summary>
        ///     Probabalistically adds a new wall.  This is equivalent to adding a new gene to the genome
        /// </summary>
        private void MutateAddWall()
        {
            // Generate new wall and passage start locations
            double newWallStartLocation = GenomeFactory.Rng.NextDoubleNonZero();
            double newPassageStartLocation = GenomeFactory.Rng.NextDoubleNonZero();

            // Add new gene to the genome
            GeneList.Add(new MazeGene(GenomeFactory.InnovationIdGenerator.NextId, newWallStartLocation,
                newPassageStartLocation, GenomeFactory.Rng.NextBool()));
        }

        /// <summary>
        ///     Bounds the starting location for a wall or passage such that its non-negative and doesn't exceed 1.
        /// </summary>
        /// <param name="proposedLocation">The location proposed by the mutation.</param>
        /// <returns>The bounded location.</returns>
        private double BoundStartLocation(double proposedLocation)
        {
            if (proposedLocation < 0)
                return 0;
            if (proposedLocation > 1)
                return 1;
            return proposedLocation;
        }

        #endregion
    }
}