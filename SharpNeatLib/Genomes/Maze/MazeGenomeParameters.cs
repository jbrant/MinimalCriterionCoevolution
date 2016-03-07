#region

using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    public class MazeGenomeParameters
    {
        #region Instance Fields

        // Roulette wheel for the two mutation probabilities
        public readonly RouletteWheelLayout RouletteWheelLayout;

        #endregion

        #region Private Methods

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

        #region Constructors

        public MazeGenomeParameters()
        {
            MutateWallStartLocationProbability = DefaultMutateWallStartLocationProbability;
            MutatePassageStartLocationProbability = DefaultMutatePassageStartLocationProbability;
            MutateAddWallProbability = DefaultMutateAddWallProbability;
            PerturbanceMagnitude = DefaultPerturbanceMagnitude;

            // Create a new roulette wheel layout with the default probabilities
            RouletteWheelLayout = CreateRouletteWheelLayout();
        }

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

        // Mutation probabilities
        public double MutateWallStartLocationProbability { get; }

        public double MutatePassageStartLocationProbability { get; }

        public double MutateAddWallProbability { get; }

        // Perturbance magnitude
        public double PerturbanceMagnitude { get; }

        #endregion
    }
}