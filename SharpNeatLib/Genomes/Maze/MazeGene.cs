namespace SharpNeat.Genomes.Maze
{
    /// <summary>
    ///     The maze gene class encapsulates evolvable details about a wall, including its location within the maze and the
    ///     position of its passage within the wall.  These are real-valued, relative positions as the maze generation
    ///     algorithm will determine their exact location in the phenotype maze.
    /// </summary>
    public class MazeGene
    {
        /// <summary>
        ///     Constructor which accepts a wall location, passage location, and their preliminary orientation and creates a new
        ///     maze gene.
        /// </summary>
        /// <param name="innovationId">
        ///     The unique "innovation" identifier for this gene (analogous to the innovation IDs on NEAT
        ///     connection genes).
        /// </param>
        /// <param name="wallLocation">The unscaled location of the wall (real value between 0 and 1).</param>
        /// <param name="passageLocation">The unscaled location of the wall passage (real value between 0 and 1).</param>
        /// <param name="orientationSeed">
        ///     The "preliminary" orientation of the wall (i.e. either horizontal or vertical).  In
        ///     practice, this value is only considered when constructing the phenotype maze if the area which the wall is dividing
        ///     is square.
        /// </param>
        public MazeGene(uint innovationId, double wallLocation, double passageLocation, bool orientationSeed)
        {
            InnovationId = innovationId;
            WallLocation = wallLocation;
            PassageLocation = passageLocation;
            OrientationSeed = orientationSeed;
        }

        /// <summary>
        ///     Copy constructor for duplicating maze gene.
        /// </summary>
        /// <param name="copyFrom">The maze gene to deep copy.</param>
        public MazeGene(MazeGene copyFrom)
        {
            InnovationId = copyFrom.InnovationId;
            WallLocation = copyFrom.WallLocation;
            PassageLocation = copyFrom.PassageLocation;
            OrientationSeed = copyFrom.OrientationSeed;
        }

        /// <summary>
        ///     The unique "innovation" identifier for this gene.
        /// </summary>
        public uint InnovationId { get; }

        /// <summary>
        ///     The unscaled wall location.
        /// </summary>
        public double WallLocation { get; set; }

        /// <summary>
        ///     The unscaled passage location.
        /// </summary>
        public double PassageLocation { get; set; }

        /// <summary>
        ///     The preliminary wall orientation (horizontal or vertical).
        /// </summary>
        public bool OrientationSeed { get; }

        /// <summary>
        ///     Creates a copy of the current gene.
        /// </summary>
        /// <returns></returns>
        public MazeGene CreateCopy()
        {
            return new MazeGene(this);
        }
    }
}