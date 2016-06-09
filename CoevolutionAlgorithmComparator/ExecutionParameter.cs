namespace CoevolutionAlgorithmComparator
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
        ///     The directory of the output data files (if we're not writing directly into the database).
        /// </summary>
        DataFileOutputDirectory,

        /// <summary>
        ///     Allows starting from an arbitrary run number of the experiment under analysis.
        /// </summary>
        StartFromRun,

        /// <summary>
        ///     Experiment against which to compare coevolution (probably novelty search).
        /// </summary>
        ReferenceExperimentName,

        /// <summary>
        ///     The names of the applicable experiment configurations.
        /// </summary>
        ExperimentNames
    }
}