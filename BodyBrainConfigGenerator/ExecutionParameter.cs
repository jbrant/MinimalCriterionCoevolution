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
        ///     The path to the configuration template file.
        /// </summary>
        ConfigTemplateFilePath,

        /// <summary>
        ///     The directory of the output configuration files.
        /// </summary>
        ConfigOutputDirectory
    }
}