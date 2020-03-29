using Redzen.Numerics.Distributions;

namespace SharpNeat.Genomes.Substrate
{
    /// <summary>
    ///     Encapsulates NEAT genome mutation parameters (assuming that the affected network codes for a CPPN) along with
    ///     mutation parameters specific to the modification of a substrate queried by that CPPN.
    /// </summary>
    public class NeatSubstrateGenomeParameters
    {
        #region Constructors

        /// <summary>
        ///     NeatSubstrateGenomeParameters constructor that accepts an underlying set of NEAT genome parameters along with
        ///     optional substrate-specific mutation probabilities.
        /// </summary>
        /// <param name="modifySubstrateResolutionProbability">
        ///     Probability of applying a substrate modification (rather than a
        ///     modification to the nodes or connections in the underlying NEAT network).
        /// </param>
        /// <param name="increaseSubstrateResolutionProbability">
        ///     Probability of increasing the substrate resolution (assuming a
        ///     substrate modification is selected).
        /// </param>
        /// <param name="decreaseSubstrateResolutionProbability">
        ///     Probability of decreasing the substrate resolution (assuming a
        ///     substrate modification is selected).
        /// </param>
        public NeatSubstrateGenomeParameters(
            double modifySubstrateResolutionProbability = DefaultModifySubstrateResolutionProbability,
            double increaseSubstrateResolutionProbability = DefaultIncreaseSubstrateResolutionProbability,
            double decreaseSubstrateResolutionProbability = DefaultDecreaseSubstrateResolutionProbability)
        {
            ModifySubstrateResolutionProbability = modifySubstrateResolutionProbability;
            _increaseSubstrateResolutionProbability = increaseSubstrateResolutionProbability;
            _decreaseSubstrateResolutionProbability = decreaseSubstrateResolutionProbability;
            RouletteWheelLayout = CreateRouletteWheelLayout();
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Creates a new array of substrate mutation probabilities that are sampled from when applying a substrate mutation.
        /// </summary>
        /// <returns>A RouletteWheelLayout that represents the probabilities of each type of substrate mutation.</returns>
        private DiscreteDistribution CreateRouletteWheelLayout()
        {
            var probabilities = new[]
            {
                _increaseSubstrateResolutionProbability,
                _decreaseSubstrateResolutionProbability
            };
            return new DiscreteDistribution(probabilities);
        }

        #endregion

        #region Constants

        /// <summary>
        ///     Default probability of applying a substrate modification (rather than a modification to the nodes or connections in
        ///     the underlying NEAT network).
        /// </summary>
        private const double DefaultModifySubstrateResolutionProbability = 0.01;

        /// <summary>
        ///     Default probability of increasing the substrate resolution (assuming a substrate modification is selected).
        /// </summary>
        private const double DefaultIncreaseSubstrateResolutionProbability = 0.8;

        /// <summary>
        ///     Default probability of decreasing the substrate resolution (assuming a substrate modification is selected).
        /// </summary>
        private const double DefaultDecreaseSubstrateResolutionProbability = 0.2;

        #endregion

        #region Public properties

        /// <summary>
        ///     Probability of applying a substrate modification (rather than a modification to the nodes or connections in the
        ///     underlying NEAT network).
        /// </summary>
        public double ModifySubstrateResolutionProbability { get; }

        /// <summary>
        ///     A RouletteWheelLayout that represents the probabilities of each type of substrate mutation.
        /// </summary>
        public DiscreteDistribution RouletteWheelLayout { get; }

        #endregion

        #region Instance variables

        /// <summary>
        ///     Probability of increasing the substrate resolution (assuming a substrate modification is selected).
        /// </summary>
        private readonly double _increaseSubstrateResolutionProbability;

        /// <summary>
        ///     Probability of decreasing the substrate resolution (assuming a substrate modification is selected).
        /// </summary>
        private readonly double _decreaseSubstrateResolutionProbability;

        #endregion
    }
}