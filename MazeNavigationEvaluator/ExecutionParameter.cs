namespace MazeNavigationEvaluator
{
    /// <summary>
    ///     Runtime parameters that can be set upon application invocation.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     The scope of the analysis (every batch in the run, the aggregate results of the run, or the last batch).
        /// </summary>
        AnalysisScope,

        /// <summary>
        ///     The fixed number of input neurons in an agent neural network.
        /// </summary>
        AgentNeuronInputCount,

        /// <summary>
        ///     The fixed number of output neurons in an agent neural network.
        /// </summary>
        AgentNeuronOutputCount,

        /// <summary>
        ///     Whether or not to run simulations and output full trajectory data for every point visited during each trajectory
        ///     simulation.
        /// </summary>
        GenerateTrajectoryData,

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
        ///     Whether or not to compute agent trajectory diversity scores.
        /// </summary>
        GenerateAgentDiversityScores,
        
        /// <summary>
        ///     Whether or not to compute maze solution path diversity scores.
        /// </summary>
        GenerateMazeDiversityScores,

        /// <summary>
        ///     Whether or not to analyze natural clustering of agent trajectories.
        /// </summary>
        GenerateAgentTrajectoryClusters,

        /// <summary>
        ///     Whether or not to analyze natural clustering of mazes.
        /// </summary>
        GenerateMazeClusters,

        /// <summary>
        ///     Whether or not to compute population entropy (using a fixed number of clusters that's based on the number of specie
        ///     clusters).
        /// </summary>
        GeneratePopulationEntropy,

        /// <summary>
        ///     Whether or not to use a greedy strategy when calculating silhouette width for optimal cluster determination.
        /// </summary>
        UseGreedySilhouetteCalculation,

        /// <summary>
        ///     Whether to distribute the stated sample size evenly across extant mazes.  If this is set, the number of
        ///     trajectories clustered will be (# mazes) x (# samples).
        /// </summary>
        UseEvenMazeTrajectoryDistribution,

        /// <summary>
        ///     Ceiling on range of cluster values to try when determining which cluster values maximizes the silhouette width
        ///     (used as part of a non-greedy strategy).
        /// </summary>
        ClusterRange,

        /// <summary>
        ///     The number of individuals to choose either from the population or from each maze/agent species (depending on the
        ///     experiment configuration) to use in cluster or trajectory analysis.
        /// </summary>
        SampleSize,

        /// <summary>
        ///     Whether or not to select samples evenly across species (not possible for non-speciated experiments) or from the
        ///     entire population.
        /// </summary>
        SampleFromSpecies,

        /// <summary>
        ///     The base directory into which to write the bitmap image trajectories.
        /// </summary>
        BitmapOutputBaseDirectory,

        /// <summary>
        ///     Allows starting from an arbitrary run number of the experiment under analysis.
        /// </summary>
        StartFromRun,

        /// <summary>
        ///     Flag indicating whether to run initialization trial analysis.
        /// </summary>
        ExecuteInitializationTrials,

        /// <summary>
        ///     The number of batches to skip in each iteration.
        /// </summary>
        BatchInterval,

        /// <summary>
        ///     The number of maze/navigator combinations to process at one time.
        /// </summary>
        ImageChunkSize,

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