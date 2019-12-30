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
        ///     The number of time steps for which to execute a simulation replay (if creating trial data).
        /// </summary>
        SimulationTimesteps,

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
        ///     The directory of the simulation log files.
        /// </summary>
        SimLogOutputDirectory,

        /// <summary>
        ///     The directory of the output evaluation data files.
        /// </summary>
        DataOutputDirectory
    }
}