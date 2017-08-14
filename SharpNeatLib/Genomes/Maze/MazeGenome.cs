#region

using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        ///     Height of the evolved maze genome (before being scaled to phenotype).
        /// </summary>
        public int MazeBoundaryHeight { get; private set; }

        /// <summary>
        ///     Width of evolved maze genome (before being scaled to phenotype).
        /// </summary>
        public int MazeBoundaryWidth { get; private set; }

        /// <summary>
        ///     The maximum complexity of the maze (at the evolved resolution).  Note that this is set when the genome is birthed,
        ///     but can also change as a result of a mutation; however, it's stored on the genome instead of being calculated via
        ///     the "get" call because of the computational cost involved in calculating it.
        /// </summary>
        public int MaxComplexity { get; private set; }

        #endregion

        #region Maze Genome Constructors

        /// <summary>
        ///     Constructor which constructs a new maze genome with the given unique identifier and birth generation.
        /// </summary>
        /// <param name="id">The unique identifier of the new maze genome.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        protected MazeGenome(uint id, uint birthGeneration)
        {
            // Set the unique genome ID and the birth generation
            Id = id;
            BirthGeneration = birthGeneration;

            // Create new evaluation info with no fitness history
            EvaluationInfo = new EvaluationInfo(0);

            // Instantiate new gene list
            GeneList = new List<MazeGene>();
        }

        /// <summary>
        ///     Constructor which constructs a new maze genome with the given genome factory, unique identifier and birth
        ///     generation.
        /// </summary>
        /// <param name="genomeFactory">Reference to the genome factory.</param>
        /// <param name="id">The unique identifier of the new maze genome.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        public MazeGenome(MazeGenomeFactory genomeFactory, uint id, uint birthGeneration) : this(id, birthGeneration)
        {
            // Ensure that genome factory is non-null
            if (genomeFactory == null)
            {
                throw new SharpNeatException(
                    string.Format(
                        "Null genome factory passed in during construction of maze genome with id [{0}] and birth generation [{1}].  If the maze height/width are not explicitly specified, the genome factory is required for instantiating genome with boundary length defaults.",
                        id, birthGeneration));
            }

            // Set the initial maze height and width
            MazeBoundaryHeight = genomeFactory.BaseMazeHeight;
            MazeBoundaryWidth = genomeFactory.BaseMazeWidth;

            // Set the genome factory
            GenomeFactory = genomeFactory;
        }

        /// <summary>
        ///     Constructor which constructs a new maze genome with the given unique identifier, birth generation, and initial maze
        ///     height/width.
        /// </summary>
        /// <param name="genomeFactory">Reference to the genome factory.</param>
        /// <param name="id">The unique identifier of the new maze genome.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        /// <param name="height">The base/initial height of the maze genome.</param>
        /// <param name="width">The base/initial width of the maze genome.</param>
        public MazeGenome(MazeGenomeFactory genomeFactory, uint id, uint birthGeneration, int height, int width)
            : this(id, birthGeneration)
        {
            // Set the initial maze height and width
            MazeBoundaryHeight = height;
            MazeBoundaryWidth = width;

            // Set the genome factory
            GenomeFactory = genomeFactory;

            // Compute max complexity based on existing genome complexity and maze dimensions
            MaxComplexity = MazeUtils.DetermineMaxPartitions(this);
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
            MazeBoundaryHeight = copyFrom.MazeBoundaryHeight;
            MazeBoundaryWidth = copyFrom.MazeBoundaryWidth;
            GeneList = new List<MazeGene>(DeepCopyMazeGeneList(copyFrom.GeneList));
            EvaluationInfo = new EvaluationInfo(copyFrom.EvaluationInfo.FitnessHistoryLength);

            // Compute max complexity based on existing genome complexity and maze dimensions
            MaxComplexity = MazeUtils.DetermineMaxPartitions(this);
        }

        /// <summary>
        ///     Constructor which constructs a new maze genome with the given unique identifier, birth generation, and list of wall
        ///     genes.
        /// </summary>
        /// <param name="genomeFactory">Reference to the genome factory.</param>
        /// <param name="id">The unique identifier of the new maze genome.</param>
        /// <param name="birthGeneration">The birth generation.</param>
        /// <param name="height">The base/initial height of the maze genome.</param>
        /// <param name="width">The base/initial width of the maze genome.</param>
        /// <param name="geneList">The list of wall genes.</param>
        public MazeGenome(MazeGenomeFactory genomeFactory, uint id, uint birthGeneration, int height, int width,
            IList<MazeGene> geneList)
            : this(genomeFactory, id, birthGeneration, height, width)
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
        public double Complexity => GeneList.Count;

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
            } while (GeneList.Count > MaxComplexity && outcome >= 2);

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
                case 3:
                    MutateDeleteWall();
                    break;
                case 4:
                    MutateExpandMaze();
                    break;
            }

            // TODO: Check if maze meets deceptiveness/complexity requirements (repeat mutation if not):
            // TODO: 1. At least three 90 degree or 270 degree turns
            // TODO: 2. Path (main path and deceptive path) should fill the maze - i.e. maze coverage

            // TODO: This should probably be a utility method called from each of
        }

        /// <summary>
        ///     Mutates the location of the wall based on both the wall start mutation probability and the perturbance magnitude.
        /// </summary>
        private void MutateWallStartLocations()
        {
            bool mutationOccurred = false;
            int mazeTreeDepth = (int) Math.Log(GeneList.Count, 2) + 1;

            // Don't try to mutate if the gene list is empty
            if (GeneList.Count <= 0)
                return;

            // Iterate through each gene (wall) and probabilistically shift its location (scaling perturbance magnitude by wall effect size)
            for (int geneIdx = 0; geneIdx < GeneList.Count; geneIdx++)
            {
                if (GenomeFactory.Rng.NextDouble() <
                    GenomeFactory.MazeGenomeParameters.MutateWallStartLocationProbability)
                {
                    GeneList[geneIdx].WallLocation = BoundStartLocation(GeneList[geneIdx].WallLocation +
                                                                        (((GenomeFactory.Rng.NextDouble()*2) - 1)*
                                                                         GenomeFactory.MazeGenomeParameters
                                                                             .PerturbanceMagnitude*
                                                                         ((double)
                                                                             ((int) (Math.Log(geneIdx + 1, 2)) + 1)/
                                                                          mazeTreeDepth)));
                    mutationOccurred = true;
                }
            }

            // Ensure that a mutation actually occurs
            if (mutationOccurred == false)
            {
                // Select a random gene to mutate
                int mazeGeneIdx = GenomeFactory.Rng.Next(GeneList.Count);

                // Perform mutation
                GeneList[mazeGeneIdx].WallLocation = BoundStartLocation(GeneList[mazeGeneIdx].WallLocation +
                                                                        (((GenomeFactory.Rng.NextDouble()*2) - 1)*
                                                                         GenomeFactory.MazeGenomeParameters
                                                                             .PerturbanceMagnitude*
                                                                         ((double)
                                                                             ((int) (Math.Log(mazeGeneIdx + 1, 2)) + 1)/
                                                                          mazeTreeDepth)));
            }

            // If the mutation caused a reduction in max complexity, remove non-coding genes
            RemoveNonCodingGenes();
        }

        /// <summary>
        ///     Mutates the location of the wall passage based on both the passage start mutation probability and the perturbance
        ///     magnitude.
        /// </summary>
        private void MutatePassageStartLocations()
        {
            bool mutationOccurred = false;
            int mazeTreeDepth = (int) Math.Log(GeneList.Count, 2) + 1;

            // Don't try to mutate if the gene list is empty
            if (GeneList.Count <= 0)
                return;

            // Iterate through each gene (wall) and probabilistically shift its passage location (scaling perturbance magnitude by wall effect size)
            for (int geneIdx = 0; geneIdx < GeneList.Count; geneIdx++)
            {
                if (GenomeFactory.Rng.NextDouble() <
                    GenomeFactory.MazeGenomeParameters.MutatePassageStartLocationProbability)
                {
                    GeneList[geneIdx].PassageLocation = BoundStartLocation(GeneList[geneIdx].PassageLocation +
                                                                           (((GenomeFactory.Rng.NextDouble()*2) - 1)*
                                                                            GenomeFactory.MazeGenomeParameters
                                                                                .PerturbanceMagnitude*
                                                                            ((double)
                                                                                ((int) (Math.Log(geneIdx + 1, 2)) + 1)/
                                                                             mazeTreeDepth)));

                    mutationOccurred = true;
                }
            }

            // Ensure that a mutation actually occurs
            if (mutationOccurred == false)
            {
                // Select a random gene to mutate
                int mazeGeneIdx = GenomeFactory.Rng.Next(GeneList.Count);

                GeneList[mazeGeneIdx].PassageLocation = BoundStartLocation(GeneList[mazeGeneIdx].WallLocation +
                                                                           (((GenomeFactory.Rng.NextDouble()*2) - 1)*
                                                                            GenomeFactory.MazeGenomeParameters
                                                                                .PerturbanceMagnitude*
                                                                            ((double)
                                                                                ((int) (Math.Log(mazeGeneIdx + 1, 2)) +
                                                                                 1)/
                                                                             mazeTreeDepth)));
            }

            // If the mutation caused a reduction in max complexity, remove non-coding genes
            RemoveNonCodingGenes();
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
        ///     Probabalistically deletes a random wall.  This is equivalent to deleting a gene from the genome.
        /// </summary>
        private void MutateDeleteWall()
        {
            // Don't attempt to delete a wall if only one exists
            if (GeneList.Count < 2)
            {
                return;
            }

            // Select a random wall to be deleted
            // TODO: Probably need to scale deletion mutation here based on effect size
            int wallIdx = GenomeFactory.Rng.Next(GeneList.Count);

            // Delete the wall
            GeneList.RemoveAt(wallIdx);

            // If the mutation caused a reduction in max complexity, remove non-coding genes
            RemoveNonCodingGenes();
        }

        /// <summary>
        ///     Probabalistically expands the maze area by one unit.
        /// </summary>
        private void MutateExpandMaze()
        {
            // TODO: This could also support incrementing in only one dimension
            // Increment maze height and width by 1
            MazeBoundaryHeight += 1;
            MazeBoundaryWidth += 1;
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

        /// <summary>
        ///     Performs a deep copy on the maze genes.
        /// </summary>
        /// <param name="copyFrom">The source maze gene list to duplicate.</param>
        /// <returns>A newly constructed maze gene list.</returns>
        private IList<MazeGene> DeepCopyMazeGeneList(IList<MazeGene> copyFrom)
        {
            List<MazeGene> copiedGeneList = new List<MazeGene>(copyFrom.Count);

            // Duplicate all maze genes
            copiedGeneList.AddRange(copyFrom.Select(mazeGene => mazeGene.CreateCopy()));

            return copiedGeneList;
        }

        /// <summary>
        ///     Recomputes the maximum complexity supported by the maze genome following a mutation and removes non-coding genes in
        ///     the event that the mutation reduced the maximum complexity supported by the maze.
        /// </summary>
        private void RemoveNonCodingGenes()
        {
            // Recompute max complexity in the event that mutation changed wall/passage placement in a way that reduces the complexity cap
            MaxComplexity = MazeUtils.DetermineMaxPartitions(this);

            // If the max complexity is now lower, remove the non-coding genes
            if (MaxComplexity < GeneList.Count)
            {
                ((List<MazeGene>) GeneList).RemoveRange(GeneList.Count - (MaxComplexity - GeneList.Count),
                    MaxComplexity - GeneList.Count);
            }
        }

        #endregion
    }
}