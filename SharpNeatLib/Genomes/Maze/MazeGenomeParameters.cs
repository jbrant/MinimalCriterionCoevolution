#region

using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    public class MazeGenomeParameters
    {
        #region Instance Fields

        // Roulette wheel for the two mutation probabilities
        private readonly RouletteWheelLayout _rouletteWheelLayout;

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

            // Create a new roulette wheel layout with the default probabilities
            _rouletteWheelLayout = CreateRouletteWheelLayout();
        }

        public MazeGenomeParameters(MazeGenomeParameters copyFrom)
        {
            MutateWallStartLocationProbability = copyFrom.MutateWallStartLocationProbability;
            MutatePassageStartLocationProbability = copyFrom.MutatePassageStartLocationProbability;
            MutateAddWallProbability = copyFrom.MutateAddWallProbability;

            _rouletteWheelLayout = new RouletteWheelLayout(copyFrom._rouletteWheelLayout);
        }

        #endregion

        #region Constants

        // Default mutation probabilities
        private const double DefaultMutateWallStartLocationProbability = 0.1;
        private const double DefaultMutatePassageStartLocationProbability = 0.1;
        private const double DefaultMutateAddWallProbability = 0.01;

        #endregion

        #region Properties

        // Mutation probabilities
        public double MutateWallStartLocationProbability { get; }

        public double MutatePassageStartLocationProbability { get; }

        public double MutateAddWallProbability { get; }

        #endregion
    }
}