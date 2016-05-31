#region

using SharpNeat.Decoders;

#endregion

namespace MazeSimulatorSupport
{
    /// <summary>
    ///     Encapsulates experiment properties utilized by the maze navigation simulation.
    /// </summary>
    internal struct SimulatorExperimentConfiguration
    {
        /// <summary>
        ///     SimulatorExperimentConfiguration constructor.
        /// </summary>
        /// <param name="experimentName">The experiment name.</param>
        /// <param name="mazeHeight">The height of the maze.</param>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <param name="mazeScaleMultiplier">
        ///     The maze scale multiplication factor (this is the scale on which the navigator was
        ///     evaluated, not at which the maze was evolved).
        /// </param>
        /// <param name="navigatorActivationScheme">The activation scheme (e.g. acyclic, cyclic) of the navigator ANN controller.</param>
        /// <param name="maxTimesteps">The maximum number of timesteps for one simulation trial.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful trial.</param>
        public SimulatorExperimentConfiguration(string experimentName, int mazeHeight, int mazeWidth,
            int mazeScaleMultiplier, NetworkActivationScheme navigatorActivationScheme, int maxTimesteps,
            int minSuccessDistance)
        {
            ExperimentName = experimentName;
            MazeHeight = mazeHeight*mazeScaleMultiplier;
            MazeWidth = mazeWidth*mazeScaleMultiplier;
            NavigatorAnnActivationScheme = navigatorActivationScheme;
            MaxTimesteps = maxTimesteps;
            MinSuccessDistance = minSuccessDistance;
        }

        /// <summary>
        ///     The simulated experiment name.
        /// </summary>
        public string ExperimentName { get; }

        /// <summary>
        ///     The height of the maze (this is the height at which navigator evolution was performed, not the height at which the
        ///     maze is displayed).
        /// </summary>
        public int MazeHeight { get; }

        /// <summary>
        ///     The width of the maze (this is the width at which navigator evolution was performed, not the width at which the
        ///     maze is displayed).
        /// </summary>
        public int MazeWidth { get; }

        /// <summary>
        ///     The navigator ANN controller activation scheme (e.g. acyclic or cyclic).
        /// </summary>
        public NetworkActivationScheme NavigatorAnnActivationScheme { get; }

        /// <summary>
        ///     The maximum number of discrete timesteps allotted for the simulation.
        /// </summary>
        public int MaxTimesteps { get; }

        /// <summary>
        ///     The minimum distance from the target for the trial to be considered successful.
        /// </summary>
        public int MinSuccessDistance { get; }
    }
}