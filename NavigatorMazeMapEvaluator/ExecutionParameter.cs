namespace NavigatorMazeMapEvaluator
{
    /// <summary>
    ///     Runtime parameters that can be set upon application invocation.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     Whether or not to evaluate mazes/navigator trajectories for the entirety of the run.  If false,
        ///     only the last batch is analyzed.
        /// </summary>
        AnalyzeFullRun,

        /// <summary>
        ///     The fixed number of input neurons in an agent neural network.
        /// </summary>
        AgentNeuronInputCount,

        /// <summary>
        ///     The fixed number of output neurons in an agent neural network.
        /// </summary>
        AgentNeuronOutputCount,

        /// <summary>
        ///     Whether or not to generate data/results about the simulation.  If enabled, either the WriteResultsToDatabase or
        ///     DataFileOutputDirectory must be specified.
        /// </summary>
        GenerateSimulationResults,

        /// <summary>
        ///     Whether or not to write the results of the maze/agent simulations to the experiment database.
        /// </summary>
        WriteResultsToDatabase,

        /// <summary>
        ///     The directory of the output data files (if we're not writing directly into the database).
        /// </summary>
        DataFileOutputDirectory,

        /// <summary>
        ///     Whether or not to generate bitmap images of the distinct mazes extant in the analyzed batches (no navigator
        ///     trajectory included by default).
        /// </summary>
        GenerateMazeBitmaps,

        /// <summary>
        ///     Whether or not to generate bitmap images of the trajectory of each agent through each applicable maze.
        /// </summary>
        GenerateAgentTrajectoryBitmaps,

        /// <summary>
        ///     The base directory into which to write the bitmap image trajectories.
        /// </summary>
        BitmapOutputBaseDirectory,

        /// <summary>
        ///     Allows starting from an arbitrary run number of the experiment under analysis.
        /// </summary>
        StartFromRun,

        /// <summary>
        ///     Flag indicating whether different runs will be distributed on separate cluster nodes.  If this is true, the
        ///     "StartFromRun" parameter must be set as this will indicate the run that's being executed on a particular node.
        ///     Additionally, each node will execute an analysis of one run only.
        /// </summary>
        IsDistributedExecution,

        /// <summary>
        ///     The names of the applicable experiment configurations.
        /// </summary>
        ExperimentNames
    }
}