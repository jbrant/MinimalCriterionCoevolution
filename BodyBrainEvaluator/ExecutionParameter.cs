namespace BodyBrainConfigGenerator
{
    /// <summary>
    ///     Runtime parameters that can be set upon application invocation.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     The name of the applicable experiment configuration.
        /// </summary>
        ExperimentName,

        /// <summary>
        ///     The run number within the given experiment from which to start configuration file generation.
        /// </summary>
        Run,

        /// <summary>
        ///     Flag indicating whether to generate simulation configuration files.
        /// </summary>
        GenerateSimulationConfigs,

        /// <summary>
        ///     Flag indicating whether to generate verbose simulation data.
        /// </summary>
        GenerateSimLogData,

        /// <summary>
        ///     Flag indicating whether to evaluate ability of brain CPPN to scale to incrementally larger bodies.
        /// </summary>
        GenerateIncrementalUpscaleResults,

        /// <summary>
        ///     Flag indicating whether to compute voxel body similarity between each pair of bodies averaged over full run.
        /// </summary>
        GenerateRunBodyDiversityData,

        /// <summary>
        ///     Flag indicating whether to compute voxel body similarity between each pair of bodies of the same dimensions and
        ///     averaged over full run.
        /// </summary>
        GenerateRunSizeBodyDiversityData,

        /// <summary>
        ///     Flag indicating whether to compute voxel body similarity between each pair of bodies averaged over extant
        ///     population at each batch.
        /// </summary>
        GenerateBatchBodyDiversityData,

        /// <summary>
        ///     Flag indicating whether to compute trajectory similarity between each pair of trajectories averaged over full run.
        /// </summary>
        GenerateRunTrajectoryDiversityData,

        /// <summary>
        ///     Flag indicating whether to compute trajectory similarity between each pair of trajectories between bodies of the
        ///     same dimensions and averaged over full run.
        /// </summary>
        GenerateRunSizeTrajectoryDiversityData,

        /// <summary>
        ///     Flag indicating whether to compute trajectory similarity between each pair of trajectories averaged over extant
        ///     population at each batch.
        /// </summary>
        GenerateBatchTrajectoryDiversityData,

        /// <summary>
        ///     The number of time steps for which to execute a simulation replay (if creating trial data).
        /// </summary>
        SimulationTimesteps,

        /// <summary>
        ///     The maximum body size to evaluate (used for brain CPPN incremental upscale test).
        /// </summary>
        MaxBodySize,

        /// <summary>
        ///     The path to the simulator executable file.
        /// </summary>
        SimExecutablePath,

        /// <summary>
        ///     The path to the configuration template file.
        /// </summary>
        ConfigTemplateFilePath,

        /// <summary>
        ///     The directory of the output configuration files.
        /// </summary>
        ConfigOutputDirectory,

        /// <summary>
        ///     The directory of the simulation results.
        /// </summary>
        ResultsOutputDirectory,

        /// <summary>
        ///     The directory of the simulation log files.
        /// </summary>
        SimLogOutputDirectory,

        /// <summary>
        ///     The directory of the output evaluation data files.
        /// </summary>
        DataOutputDirectory
    }
}