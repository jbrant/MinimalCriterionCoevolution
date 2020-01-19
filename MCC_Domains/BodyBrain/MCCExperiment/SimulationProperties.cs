namespace MCC_Domains.BodyBrain.MCCExperiment
{
    /// <summary>
    ///     Encapsulates simulator configuration properties.
    /// </summary>
    public struct SimulationProperties
    {
        /// <summary>
        ///     The output directory for generated simulation configuration files.
        /// </summary>
        public readonly string SimConfigOutputDirectory;

        /// <summary>
        ///     The output directory for simulation results.
        /// </summary>
        public readonly string SimResultsDirectory;

        /// <summary>
        ///     The type of brain controller (e.g. neural network or phase offset controller).
        /// </summary>
        public readonly BrainType BrainType;

        /// <summary>
        ///     The simulation configuration template file which is dynamically modified based on the configuration of the body and
        ///     brain undergoing evaluation.
        /// </summary>
        public readonly string SimConfigTemplateFile;

        /// <summary>
        ///     The location and filename of the simulator executable.
        /// </summary>
        public readonly string SimExecutableFile;

        /// <summary>
        ///     The minimum percentage of the voxel structure space that contains material (i.e. is not empty).
        /// </summary>
        public readonly double MinPercentMaterial;

        /// <summary>
        ///     The minimum percentage of voxels that are muscle (rather than bone/rigid).
        /// </summary>
        public readonly double MinPercentActive;

        /// <summary>
        ///     The starting X-axis dimensionality.
        /// </summary>
        public readonly int InitialXDimension;

        /// <summary>
        ///     The starting Y-axis dimensionality.
        /// </summary>
        public readonly int InitialYDimension;

        /// <summary>
        ///     The starting Z-axis dimensionality.
        /// </summary>
        public readonly int InitialZDimension;

        /// <summary>
        ///     The number of connections in the voxel controller neural network.
        /// </summary>
        public readonly int NumBrainConnections;

        /// <summary>
        ///     The maximum number of simulated seconds for which to execute the simulation.
        /// </summary>
        public readonly double SimulationSeconds;

        /// <summary>
        ///     The maximum number of simulated seconds dedicated to initialization.
        /// </summary>
        public readonly double InitializationSeconds;

        /// <summary>
        ///     The number of actuations (voxel expansion/contraction) that execute within a single, simulated second.
        /// </summary>
        public readonly int ActuationsPerSecond;

        /// <summary>
        ///     The slope (incline or decline) of the simulation terrain in degrees.
        /// </summary>
        public readonly double FloorSlope;

        /// <summary>
        ///     The XPath to the location in the simulation configuration file containing the simulation output filename.
        /// </summary>
        public readonly string SimOutputXPath;

        /// <summary>
        ///     The XPath to the location in the simulation configuration file containing the simulation termination condition
        ///     values.
        /// </summary>
        public readonly string SimStopConditionXPath;

        /// <summary>
        ///     The XPath to the location in the simulation configuration file containing the environment thermal properties.
        /// </summary>
        public readonly string EnvThermalXPath;

        /// <summary>
        ///     The XPath to the location in the simulation configuration file containing the environment gravity properties.
        /// </summary>
        public readonly string EnvGravityXPath;

        /// <summary>
        ///     The XPath to the location in the simulation configuration file containing structure properties.
        /// </summary>
        public readonly string StructurePropertiesXPath;

        /// <summary>
        ///     The XPath to the location in the simulation configuration file containing the minimal criterion properties.
        /// </summary>
        public readonly string MinimalCriterionXPath;

        /// <summary>
        ///     SimulationProperties constructor.
        /// </summary>
        /// <param name="simConfigOutputDirectory">The output directory for generated simulation configuration files.</param>
        /// <param name="simResultsDirectory">The output directory for simulation results.</param>
        /// <param name="brainType">The type of brain controller (e.g. neural network or phase offset controller).</param>
        /// <param name="simConfigTemplateFile">
        ///     The simulation configuration template file which is dynamically modified based on
        ///     the configuration of the body and brain undergoing evaluation.
        /// </param>
        /// <param name="simExecutableFile">The location and filename of the simulator executable.</param>
        /// <param name="minPercentMaterial">
        ///     The minimum percentage of the voxel structure space that contains material (i.e. is
        ///     not empty).
        /// </param>
        /// <param name="minPercentActive">The minimum percentage of voxels that are muscle (rather than bone/rigid).</param>
        /// <param name="initialXDimension">The starting X-axis dimensionality.</param>
        /// <param name="initialYDimension">The starting Y-axis dimensionality.</param>
        /// <param name="initialZDimension">The starting Z-axis dimensionality.</param>
        /// <param name="numBrainConnections">The number of connections in the voxel controller neural network.</param>
        /// <param name="simulationSeconds">The maximum number of simulated seconds for which to execute the simulation.</param>
        /// <param name="initializationSeconds">The maximum number of simulated seconds dedicated to initialization.</param>
        /// <param name="actuationsPerSecond">
        ///     The number of actuations (voxel expansion/contraction) that execute within a single,
        ///     simulated second.
        /// </param>
        /// <param name="floorSlope">The slope (incline or decline) of the simulation terrain in degrees.</param>
        /// <param name="simOutputXPath">
        ///     The XPath to the location in the simulation configuration file containing the simulation output filename.
        /// </param>
        /// <param name="simStopConditionXPath">
        ///     The XPath to the location in the simulation configuration file containing the
        ///     simulation termination condition values.
        /// </param>
        /// <param name="envThermalXPath">
        ///     The XPath to the location in the simulation configuration file containing the environment
        ///     thermal properties.
        /// </param>
        /// <param name="envGravityXPath">
        ///     The XPath to the location in the simulation configuration file containing the environment
        ///     gravity properties.
        /// </param>
        /// <param name="structurePropertiesXPath">
        ///     The XPath to the location in the simulation configuration file containing
        ///     structure properties.
        /// </param>
        /// <param name="minimalCriterionXPath">
        ///     The XPath to the location in the simulation configuration file containing the
        ///     minimal criterion properties.
        /// </param>
        public SimulationProperties(string simConfigOutputDirectory, string simResultsDirectory, BrainType brainType,
            string simConfigTemplateFile, string simExecutableFile, double minPercentMaterial, double minPercentActive,
            int initialXDimension, int initialYDimension, int initialZDimension, int numBrainConnections,
            double simulationSeconds, double initializationSeconds, int actuationsPerSecond, double floorSlope,
            string simOutputXPath, string simStopConditionXPath, string envThermalXPath, string envGravityXPath,
            string structurePropertiesXPath, string minimalCriterionXPath)
        {
            SimConfigOutputDirectory = simConfigOutputDirectory;
            SimResultsDirectory = simResultsDirectory;
            BrainType = brainType;
            SimConfigTemplateFile = simConfigTemplateFile;
            SimExecutableFile = simExecutableFile;
            MinPercentMaterial = minPercentMaterial;
            MinPercentActive = minPercentActive;
            InitialXDimension = initialXDimension;
            InitialYDimension = initialYDimension;
            InitialZDimension = initialZDimension;
            NumBrainConnections = numBrainConnections;
            SimulationSeconds = simulationSeconds;
            InitializationSeconds = initializationSeconds;
            ActuationsPerSecond = actuationsPerSecond;
            FloorSlope = floorSlope;
            SimOutputXPath = simOutputXPath;
            SimStopConditionXPath = simStopConditionXPath;
            EnvThermalXPath = envThermalXPath;
            EnvGravityXPath = envGravityXPath;
            StructurePropertiesXPath = structurePropertiesXPath;
            MinimalCriterionXPath = minimalCriterionXPath;
        }
    }
}