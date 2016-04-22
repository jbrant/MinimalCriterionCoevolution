#region

using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    /// <summary>
    ///     The maze genome parameters class captures mutation parameters used by the maze evolution process, such as the
    ///     mutation types, their respective probabilities, and the magnitude by which they perturb the existing structure.
    /// </summary>
    public class MazeGenomeParameters
    {
        #region Private Methods

        /// <summary>
        ///     Creates a new roulette whell layout based on the different mutation types and their associated probabilities.
        /// </summary>
        /// <returns>The initialized roulette wheel layout.</returns>
        private RouletteWheelLayout CreateRouletteWheelLayout()
        {
            double[] probabilities =
            {
                MutateWallStartLocationProbability,
                MutatePassageStartLocationProbability,
                MutateAddWallProbability
            };
            return new RouletteWheelLayout(probabilities);
        }

        #endregion

        #region Instance Fields

        /// <summary>
        ///     The roulette wheel layout enables probablistic selection of different mutation types.
        /// </summary>
        public RouletteWheelLayout RouletteWheelLayout;

        /// <summary>
        ///     Backing field for wall start location mutation probability.
        /// </summary>
        private double _mutateWallStartLocationProbability;

        /// <summary>
        ///     Backing field for passage start location mutation probability.
        /// </summary>
        private double _mutatePassageStartLocationProbability;

        /// <summary>
        ///     Backing field for add wall mutation probability.
        /// </summary>
        private double _mutateAddWallProbability;

        #endregion

        #region Constructors

        /// <summary>
        ///     Default constructor, setting all maze genome parameters to their default and creating the roulette wheel layout.
        /// </summary>
        public MazeGenomeParameters()
        {
            MutateWallStartLocationProbability = DefaultMutateWallStartLocationProbability;
            MutatePassageStartLocationProbability = DefaultMutatePassageStartLocationProbability;
            MutateAddWallProbability = DefaultMutateAddWallProbability;
            PerturbanceMagnitude = DefaultPerturbanceMagnitude;

            // Create a new roulette wheel layout with the default probabilities
            RouletteWheelLayout = CreateRouletteWheelLayout();
        }

        /// <summary>
        ///     Constructor which takes an existing maze genome parameters configuration and copies all of the parameters from it.
        /// </summary>
        /// <param name="copyFrom">The existing maze genome parameters configuration to copy.</param>
        public MazeGenomeParameters(MazeGenomeParameters copyFrom)
        {
            MutateWallStartLocationProbability = copyFrom.MutateWallStartLocationProbability;
            MutatePassageStartLocationProbability = copyFrom.MutatePassageStartLocationProbability;
            MutateAddWallProbability = copyFrom.MutateAddWallProbability;
            PerturbanceMagnitude = copyFrom.PerturbanceMagnitude;

            RouletteWheelLayout = new RouletteWheelLayout(copyFrom.RouletteWheelLayout);
        }

        #endregion

        #region Constants

        // Default mutation probabilities
        private const double DefaultMutateWallStartLocationProbability = 0.1;
        private const double DefaultMutatePassageStartLocationProbability = 0.1;
        private const double DefaultMutateAddWallProbability = 0.01;

        // Default perturbance magnitude
        private const double DefaultPerturbanceMagnitude = 0.2;

        #endregion

        #region Properties

        /// <summary>
        ///     The probability of mutating the position of a wall in the maze.
        /// </summary>
        // Mutation probabilities
        public double MutateWallStartLocationProbability
        {
            get { return _mutateWallStartLocationProbability; }
            set
            {
                _mutateWallStartLocationProbability = value;
                RouletteWheelLayout = CreateRouletteWheelLayout();
            }
        }

        /// <summary>
        ///     The probability of mutating the position of the passage within a given maze wall.
        /// </summary>
        public double MutatePassageStartLocationProbability
        {
            get { return _mutatePassageStartLocationProbability; }
            set
            {
                _mutatePassageStartLocationProbability = value;
                RouletteWheelLayout = CreateRouletteWheelLayout();
            }
        }

        /// <summary>
        ///     The probability of adding a new wall to the maze.
        /// </summary>
        public double MutateAddWallProbability
        {
            get { return _mutateAddWallProbability; }
            set
            {
                _mutateAddWallProbability = value;
                RouletteWheelLayout = CreateRouletteWheelLayout();
            }
        }

        /// <summary>
        ///     The magnitude of the mutation (only applies to wall and passage position mutations).
        /// </summary>
        public double PerturbanceMagnitude { get; set; }

        #endregion
    }
}