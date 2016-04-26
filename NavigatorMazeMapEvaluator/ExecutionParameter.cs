namespace NavigatorMazeMapEvaluator
{
    /// <summary>
    ///     Runtime parameters that can be set upon application invocation.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     The fixed number of input neurons in an agent neural network.
        /// </summary>
        AgentNeuronInputCount,

        /// <summary>
        ///     The fixed number of output neurons in an agent neural network.
        /// </summary>
        AgentNeuronOutputCount,

        /// <summary>
        ///     Whether or not to write the results of the maze/agent simulations to the experiment database.
        /// </summary>
        WriteResultsToDatabase,

        /// <summary>
        ///     Whether or not to generate bitmap images of the trajectory of each agent through each applicable maze.
        /// </summary>
        GenerateAgentTrajectoryBitmaps,

        /// <summary>
        ///     The base directory into which to write the bitmap image trajectories.
        /// </summary>
        BitmapOutputBaseDirectory,

        /// <summary>
        ///     The names of the applicable experiment configurations.
        /// </summary>
        ExperimentNames
    }
}