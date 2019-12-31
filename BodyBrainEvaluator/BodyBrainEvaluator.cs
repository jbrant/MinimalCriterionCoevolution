using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using BodyBrainSupportLib;
using ExperimentEntities.entities;
using log4net;
using log4net.Config;
using SharpNeat.Decoders.Neat;
using SharpNeat.Decoders.Substrate;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Genomes.Substrate;
using SharpNeat.Phenomes.Voxels;

namespace BodyBrainConfigGenerator
{
    internal static class BodyBrainEvaluator
    {
        /// <summary>
        ///     Encapsulates configuration parameters specified at runtime.
        /// </summary>
        private static readonly Dictionary<ExecutionParameter, string> ExecutionConfiguration =
            new Dictionary<ExecutionParameter, string>();

        /// <summary>
        ///     Console logger for reporting execution status.
        /// </summary>
        private static ILog _executionLogger;

        private static void Main(string[] args)
        {
            // Initialise log4net (log to console and file).
            XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetEntryAssembly()),
                new FileInfo("log4net.config"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            // Extract the execution parameters and check for any errors (exit application if any found)
            if (ParseAndValidateConfiguration(args) == false)
                Environment.Exit(0);

            _executionLogger.Info("Invocation parameters validated - continuing with configuration file generation.");

            // Extract experiment name and run number
            var experimentName = ExecutionConfiguration[ExecutionParameter.ExperimentName];
            var run = int.Parse(ExecutionConfiguration[ExecutionParameter.Run]);

            // Get boolean indicator dictating whether to generate simulation configs
            var generateSimConfigs = ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateSimulationConfigs) &&
                                     bool.Parse(ExecutionConfiguration[ExecutionParameter.GenerateSimulationConfigs]);

            // Get boolean indicator dictating whether to generate simulation log
            var generateSimLog = ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateSimLogData) &&
                                 bool.Parse(ExecutionConfiguration[ExecutionParameter.GenerateSimLogData]);

            // Get boolean indicator dictating whether to run CPPN incremental upscale evaluation
            var generateUpscaleResults =
                ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateIncrementalUpscaleResults) &&
                bool.Parse(ExecutionConfiguration[ExecutionParameter.GenerateIncrementalUpscaleResults]);

            // Lookup the current experiment configuration
            var curExperimentConfiguration = DataHandler.LookupExperimentConfiguration(experimentName);

            // Ensure that experiment configuration was found
            if (curExperimentConfiguration == null)
            {
                _executionLogger.Error(
                    $"Unable to lookup experiment configuration for experiment with name [{experimentName}]");
                Environment.Exit(0);
            }

            _executionLogger.Info(
                $"Preparing to execute configuration file generation for experiment [{curExperimentConfiguration.ExperimentName}] run [{run}]");

            // Run simulation log file generation
            if (generateSimLog)
            {
                DataHandler.OpenFileWriter(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - SimulationLog - Run{run}.csv"), OutputFileType.SimulationLogData);

                ProcessResultChunks(curExperimentConfiguration, run, GenerateSimulationLogData);

                // Close the output file and write the sentinel file
                DataHandler.CloseFileWriter(OutputFileType.SimulationLogData);
                DataHandler.WriteSentinelFile(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - SimulationLog"), run);
            }

            // Run simulation configuration file generation
            if (generateSimConfigs)
            {
                ProcessResultChunks(curExperimentConfiguration, run, GenerateSimulationConfigs);
            }

            // Run incremental upscale evaluation
            if (generateUpscaleResults)
            {
                DataHandler.OpenFileWriter(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - UpscaleResults - Run{run}.csv"), OutputFileType.UpscaleResultData);

                ProcessResultChunks(curExperimentConfiguration, run, GenerateUpscaleResultData);

                // Close the output file and write the sentinel file
                DataHandler.CloseFileWriter(OutputFileType.UpscaleResultData);
                DataHandler.WriteSentinelFile(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - UpscaleResults"), run);
            }

            _executionLogger.Info($"Result processing for experiment [{experimentName}] and run [{run}] complete");
        }

        /// <summary>
        ///     Iterate through discrete result chunks of body/brain combinations and apply the selected evaluation to each.
        /// </summary>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="evalMethod">The evaluation method to apply to each chunk.</param>
        /// <param name="chunkSize">The number of body/brain combinations to process at one time (optional).</param>
        private static void ProcessResultChunks(ExperimentDictionaryBodyBrain experimentConfig, int run,
            EvalMethod evalMethod, int chunkSize = 100)
        {
            var experimentId = experimentConfig.ExperimentDictionaryId;

            // Create container object for body/brain factories and decoders
            var voxelPack = new VoxelFactoryDecoderPack(experimentConfig.VoxelyzeConfigInitialXdimension,
                experimentConfig.VoxelyzeConfigInitialYdimension, experimentConfig.VoxelyzeConfigInitialZdimension,
                experimentConfig.MaxBodySize, experimentConfig.ActivationIters);

            // Get all viable bodies
            var bodyGenomeIds = DataHandler.GetBodyGenomeIds(experimentId, run);

            for (var curChunk = 0; curChunk < bodyGenomeIds.Count; curChunk += chunkSize)
            {
                _executionLogger.Info(
                    $"Evaluating bodies [{curChunk}] through [{chunkSize + curChunk}] of [{bodyGenomeIds.Count}]");

                // Get body genome IDs for the current chunk
                var curBodyGenomeIds = bodyGenomeIds.Skip(curChunk).Take(chunkSize).ToList();

                // Get combinations of brains that successfully ambulate a given body
                var successfulGenomeCombos =
                    DataHandler.GetSuccessfulGenomeCombosFromBodyTrials(experimentId, run, curBodyGenomeIds);

                // Invoke the given evaluation method
                evalMethod(successfulGenomeCombos, experimentConfig, run, voxelPack);
            }
        }

        /// <summary>
        ///     Generates configuration files for body/brain simulation.
        /// </summary>
        /// <param name="viableBodyBrainCombos">Successful combinations of bodies and brains.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        private static void GenerateSimulationConfigs(
            IEnumerable<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> viableBodyBrainCombos,
            ExperimentDictionaryBodyBrain experimentConfig, int run, VoxelFactoryDecoderPack voxelPack)
        {
            var experimentName = experimentConfig.ExperimentName;
            var numBrainConnections = experimentConfig.VoxelyzeConfigBrainNetworkConnections;
            var configTemplate = ExecutionConfiguration[ExecutionParameter.ConfigTemplateFilePath];
            var configDirectory = ExecutionConfiguration[ExecutionParameter.ConfigOutputDirectory];

            Parallel.ForEach(viableBodyBrainCombos,
                delegate(Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome> genomeCombo)
                {
                    // Read in voxel body XML and decode to phenotype
                    var body = DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                        voxelPack.BodyGenomeFactory);

                    // Read in voxel brain XML and decode to phenotype
                    var brain = DecodeBrainGenome(genomeCombo.Item2, voxelPack.BrainDecoder,
                        voxelPack.BrainGenomeFactory,
                        body, numBrainConnections);

                    // Construct the output directory
                    var configOutputDirectory =
                        SimulationHandler.GetConfigOutputDirectory(configDirectory, experimentName, run, body);

                    // Generate configuration file for the given body/brain combo
                    SimulationHandler.WriteConfigFile(body, brain, configOutputDirectory, experimentName, run,
                        configTemplate, experimentConfig.MinimalCriteriaValue);
                });
        }

        /// <summary>
        ///     Generates detailed, per-timestep log data for the body/brain simulation.
        /// </summary>
        /// <param name="viableBodyBrainCombos">Successful combinations of bodies and brains.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        private static void GenerateSimulationLogData(
            IEnumerable<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> viableBodyBrainCombos,
            ExperimentDictionaryBodyBrain experimentConfig, int run, VoxelFactoryDecoderPack voxelPack)
        {
            var bodyBrainSimulationUnits = new ConcurrentBag<BodyBrainSimulationUnit>();

            var experimentId = experimentConfig.ExperimentDictionaryId;
            var experimentName = experimentConfig.ExperimentName;
            var numBrainConnections = experimentConfig.VoxelyzeConfigBrainNetworkConnections;
            var simulationTime = double.Parse(ExecutionConfiguration[ExecutionParameter.SimulationTimesteps]);
            var simExecutablePath = ExecutionConfiguration[ExecutionParameter.SimExecutablePath];
            var configTemplate = ExecutionConfiguration[ExecutionParameter.ConfigTemplateFilePath];
            var configDirectory = ExecutionConfiguration[ExecutionParameter.ConfigOutputDirectory];
            var simLogDirectory = ExecutionConfiguration[ExecutionParameter.SimLogOutputDirectory];

            Parallel.ForEach(viableBodyBrainCombos,
                delegate(Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome> genomeCombo)
                {
                    // Read in voxel body XML and decode to phenotype
                    var body = DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                        voxelPack.BodyGenomeFactory);

                    // Read in voxel brain XML and decode to phenotype
                    var brain = DecodeBrainGenome(genomeCombo.Item2, voxelPack.BrainDecoder,
                        voxelPack.BrainGenomeFactory,
                        body, numBrainConnections);

                    // Construct the simulation configuration file path
                    var configFilePath = SimulationHandler.GetConfigFilePath(configDirectory, experimentName, run,
                        brain.GenomeId, body.GenomeId);

                    // Construct the simulation log file path
                    var simLogFilePath = SimulationHandler.GetSimLogFilePath(simLogDirectory, experimentName, run,
                        brain.GenomeId, body.GenomeId);

                    // Run the simulation
                    SimulationHandler.ExecuteTimeboundBodyBrainSimulation(configTemplate, configFilePath,
                        simExecutablePath,
                        simLogFilePath, simulationTime, brain, body);

                    // Extract simulation log data
                    bodyBrainSimulationUnits.Add(
                        SimulationHandler.ReadSimulationLog(brain.GenomeId, body.GenomeId, simLogFilePath));
                });

            // Write simulation log data from body/brain combinations
            DataHandler.WriteSimulationLogDataToFile(experimentId, run, bodyBrainSimulationUnits);
        }

        /// <summary>
        ///     For each successful body/brain combination, upscales the body phenotype (i.e. querying the CPPN at a higher
        ///     resolution) in increments of one unit per dimension, and evaluates the brains ability to control the body and still
        ///     meet the MC. This continues until the body reaches a size where the brain fails to ambulate the body the minimum
        ///     required distance, or the maximum allowable body size is reached.
        /// </summary>
        /// <param name="viableBodyBrainCombos">Successful combinations of bodies and brains.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        private static void GenerateUpscaleResultData(
            IEnumerable<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> viableBodyBrainCombos,
            ExperimentDictionaryBodyBrain experimentConfig, int run, VoxelFactoryDecoderPack voxelPack)
        {
            var upscaleResultUnits = new ConcurrentBag<UpscaleResultUnit>();

            var experimentId = experimentConfig.ExperimentDictionaryId;
            var experimentName = experimentConfig.ExperimentName;
            var numBrainConnections = experimentConfig.VoxelyzeConfigBrainNetworkConnections;
            var minDistance = experimentConfig.MinimalCriteriaValue;
            var maxBodySize = int.Parse(ExecutionConfiguration[ExecutionParameter.MaxBodySize]);
            var simExecutablePath = ExecutionConfiguration[ExecutionParameter.SimExecutablePath];
            var configTemplate = ExecutionConfiguration[ExecutionParameter.ConfigTemplateFilePath];
            var configDirectory = ExecutionConfiguration[ExecutionParameter.ConfigOutputDirectory];
            var simResultDirectory = ExecutionConfiguration[ExecutionParameter.ResultsOutputDirectory];

            Parallel.ForEach(viableBodyBrainCombos,
                delegate(Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome> genomeCombo)
                {
                    var curResolutionIncrease = 0;
                    bool isSuccessful;

                    // Copy off the body/brain genome IDs
                    var bodyGenomeId = (uint) genomeCombo.Item1.GenomeId;
                    var brainGenomeId = (uint) genomeCombo.Item2.GenomeId;

                    // Get starting size
                    var evolvedSize = DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                        voxelPack.BodyGenomeFactory).LengthX;

                    // Construct the simulation configuration file path
                    var configFilePath = SimulationHandler.GetConfigFilePath(configDirectory, experimentName, run,
                        brainGenomeId, bodyGenomeId);

                    // Construct the simulation result file path
                    var simResultFilePath = SimulationHandler.GetSimResultFilePath(simResultDirectory, experimentName,
                        run,
                        brainGenomeId, bodyGenomeId);

                    do
                    {
                        // Read in voxel body XML and decode to phenotype
                        var body = DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                            voxelPack.BodyGenomeFactory, curResolutionIncrease);

                        // Read in voxel brain XML and decode to phenotype
                        var brain = DecodeBrainGenome(genomeCombo.Item2, voxelPack.BrainDecoder,
                            voxelPack.BrainGenomeFactory,
                            body, numBrainConnections);

                        // Run the simulation
                        SimulationHandler.ExecuteDistanceBoundedBodyBrainSimulation(configTemplate, configFilePath,
                            simExecutablePath, simResultFilePath, minDistance, brain, body);

                        // Extract the simulation results
                        isSuccessful = SimulationHandler.ReadSimulationDistance(simResultFilePath) >= minDistance;
                    } while (isSuccessful && maxBodySize >= evolvedSize + ++curResolutionIncrease);

                    // Record the max resolution at which the body was solvable
                    upscaleResultUnits.Add(new UpscaleResultUnit(brainGenomeId, bodyGenomeId, evolvedSize,
                        evolvedSize + Math.Max(curResolutionIncrease - 1, 0)));
                });

            // Write results of upscale analysis
            DataHandler.WriteUpscaleResultDataToFile(experimentId, run, upscaleResultUnits);
        }

        /// <summary>
        ///     Reads the body genome XML and decodes into its voxel body phenotype.
        /// </summary>
        /// <param name="bodyGenome">The body genome to convert into its corresponding phenotype.</param>
        /// <param name="bodyDecoder">The body genome decoder.</param>
        /// <param name="bodyGenomeFactory">The body genome factory.</param>
        /// <param name="substrateResIncrease">
        ///     The amount by which to increase the resolution from that at which the body was
        ///     evolved.
        /// </param>
        /// <returns>The decoded voxel body.</returns>
        private static VoxelBody DecodeBodyGenome(MccexperimentVoxelBodyGenome bodyGenome,
            NeatSubstrateGenomeDecoder bodyDecoder, NeatSubstrateGenomeFactory bodyGenomeFactory,
            int substrateResIncrease = 0)
        {
            VoxelBody body;

            using (var xmlReader = XmlReader.Create(new StringReader(bodyGenome.GenomeXml)))
            {
                body = new VoxelBody(bodyDecoder.Decode(
                        NeatSubstrateGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false, bodyGenomeFactory)),
                    substrateResIncrease);
            }

            return body;
        }

        /// <summary>
        ///     Reads the brain genome XML and decodes into its voxel brain phenotype.
        /// </summary>
        /// <param name="brainGenome">The brain genome to convert into its corresponding phenotype.</param>
        /// <param name="brainDecoder">The brain genome decoder.</param>
        /// <param name="brainGenomeFactory">The brain genome factory.</param>
        /// <param name="body">The voxel body to which the brain is scaled.</param>
        /// <param name="numConnections">The number of connections in the brain controller network.</param>
        /// <returns>The decoded voxel brain.</returns>
        private static VoxelBrain DecodeBrainGenome(MccexperimentVoxelBrainGenome brainGenome,
            NeatGenomeDecoder brainDecoder, CppnGenomeFactory brainGenomeFactory, VoxelBody body, int numConnections)
        {
            VoxelBrain brain;

            using (var xmlReader = XmlReader.Create(new StringReader(brainGenome.GenomeXml)))
            {
                brain = new VoxelBrain(
                    brainDecoder.Decode(
                        NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false, brainGenomeFactory)), body.LengthX,
                    body.LengthY, body.LengthZ, numConnections);
            }

            return brain;
        }

        /// <summary>
        ///     Populates the execution configuration and checks for any errors in said configuration.
        /// </summary>
        /// <param name="executionArguments">The arguments with which the configuration file executor is being invoked.</param>
        /// <returns>Boolean status indicating whether parsing the configuration suceeded.</returns>
        private static bool ParseAndValidateConfiguration(string[] executionArguments)
        {
            var isConfigurationValid = executionArguments != null;

            // Only continue if there are execution arguments
            if (executionArguments != null && executionArguments.Length > 0)
            {
                foreach (var executionArgument in executionArguments)
                {
                    // Get the key/value pair
                    var parameterValuePair = executionArgument.Split('=');

                    // Attempt to parse the current parameter
                    isConfigurationValid =
                        Enum.TryParse(parameterValuePair[0], true, out ExecutionParameter curParameter);

                    // If the current parameter is not valid, break out of the loop and return
                    if (isConfigurationValid == false)
                    {
                        _executionLogger.Error($"[{parameterValuePair[0]}] is not a valid configuration parameter.");
                        break;
                    }

                    // If the parameter is valid but it already exists in the map, break out of the loop and return
                    if (ExecutionConfiguration.ContainsKey(curParameter))
                    {
                        _executionLogger.Error(
                            $"Ambiguous configuration - parameter [{curParameter}] has been specified more than once.");
                        break;
                    }

                    switch (curParameter)
                    {
                        // Ensure valid run number was specified
                        case ExecutionParameter.Run:
                        case ExecutionParameter.SimulationTimesteps:
                        case ExecutionParameter.MaxBodySize:
                            if (int.TryParse(parameterValuePair[1], out _) == false)
                            {
                                _executionLogger.Error($"The value for parameter [{curParameter}] must be an integer.");
                                isConfigurationValid = false;
                            }

                            break;

                        // Ensure that valid boolean values were given
                        case ExecutionParameter.GenerateSimulationConfigs:
                        case ExecutionParameter.GenerateSimLogData:
                        case ExecutionParameter.GenerateIncrementalUpscaleResults:
                            if (bool.TryParse(parameterValuePair[1], out _) == false)
                            {
                                _executionLogger.Error($"The value for parameter [{curParameter}] must be a boolean.");
                                isConfigurationValid = false;
                            }

                            break;
                    }

                    // If all else checks out, add the parameter to the map
                    ExecutionConfiguration.Add(curParameter, parameterValuePair[1]);
                }
            }
            // If there are no execution arguments, the configuration is invalid
            else
            {
                isConfigurationValid = false;
            }

            // If the per-parameter configuration is valid but not a full list of parameters were specified, makes sure the necessary ones are present
            if (isConfigurationValid && ExecutionConfiguration.Count ==
                Enum.GetNames(typeof(ExecutionParameter)).Length == false)
            {
                // Check for existence of experiment names to execute
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.ExperimentName) == false)
                {
                    _executionLogger.Error($"Parameter [{ExecutionParameter.ExperimentName}] must be specified.");
                    isConfigurationValid = false;
                }

                // Check for existence of run number to execute
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.Run) == false)
                {
                    _executionLogger.Error($"Parameter [{ExecutionParameter.Run}] must be specified.");
                    isConfigurationValid = false;
                }

                // Config template file and config output directory must be specified if simulation config files are being generated
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateSimulationConfigs) &&
                    Convert.ToBoolean(ExecutionConfiguration[ExecutionParameter.GenerateSimulationConfigs]) &&
                    (ExecutionConfiguration.ContainsKey(ExecutionParameter.ConfigTemplateFilePath) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.ConfigOutputDirectory) == false))
                {
                    _executionLogger.Error(
                        $"Parameters [{ExecutionParameter.ConfigTemplateFilePath}] and [{ExecutionParameter.ConfigOutputDirectory}] must be specified if simulation configuration files are being generated.");
                    isConfigurationValid = false;
                }

                // Number of simulation timesteps, config directory and file, simulation executable, simulation
                // log directory and data directory must be specified if simulation log data is being generated
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateSimLogData) &&
                    Convert.ToBoolean(ExecutionConfiguration[ExecutionParameter.GenerateSimLogData]) &&
                    (ExecutionConfiguration.ContainsKey(ExecutionParameter.SimulationTimesteps) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.ConfigTemplateFilePath) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.SimExecutablePath) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.ConfigOutputDirectory) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.SimLogOutputDirectory) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.DataOutputDirectory) == false))
                {
                    _executionLogger.Error(
                        $"Parameters [{ExecutionParameter.SimulationTimesteps}], [{ExecutionParameter.ConfigTemplateFilePath}], [{ExecutionParameter.SimExecutablePath}], [{ExecutionParameter.ConfigOutputDirectory}], [{ExecutionParameter.SimLogOutputDirectory}] and [{ExecutionParameter.DataOutputDirectory}] must be specified if verbose simulation log data is being generated.");
                    isConfigurationValid = false;
                }

                // Max body size, config directory and file, simulation executable, results directory and
                // data directory must be specified if incremental upscale evaluation is being executed
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateIncrementalUpscaleResults) &&
                    Convert.ToBoolean(ExecutionConfiguration[ExecutionParameter.GenerateIncrementalUpscaleResults]) &&
                    (ExecutionConfiguration.ContainsKey(ExecutionParameter.MaxBodySize) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.ConfigTemplateFilePath) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.SimExecutablePath) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.ConfigOutputDirectory) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.ResultsOutputDirectory) == false ||
                     ExecutionConfiguration.ContainsKey(ExecutionParameter.DataOutputDirectory) == false))
                {
                    _executionLogger.Error(
                        $"Parameters [{ExecutionParameter.MaxBodySize}], [{ExecutionParameter.ConfigTemplateFilePath}], [{ExecutionParameter.SimExecutablePath}], [{ExecutionParameter.ConfigOutputDirectory}], [{ExecutionParameter.ResultsOutputDirectory}] and [{ExecutionParameter.DataOutputDirectory}] must be specified if incremental upscale results are being generated.");
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            _executionLogger.Error(
                "The body/brain evaluator invocation must take the following form:");
            _executionLogger.Error(
                "BodyBrainEvaluator.exe \n\t" +
                $"Required: {ExecutionParameter.ExperimentName}=experiment {ExecutionParameter.Run}=run \n\t" +
                $"Optional: {ExecutionParameter.GenerateSimulationConfigs} (Required: {ExecutionParameter.ConfigTemplateFilePath}=file {ExecutionParameter.ConfigOutputDirectory}=directory) \n\t" +
                $"Optional: {ExecutionParameter.GenerateSimLogData} (Required: {ExecutionParameter.SimulationTimesteps}=timesteps {ExecutionParameter.ConfigTemplateFilePath}=file {ExecutionParameter.SimExecutablePath}=file {ExecutionParameter.ConfigOutputDirectory}=directory {ExecutionParameter.SimLogOutputDirectory}=directory {ExecutionParameter.DataOutputDirectory}=directory) \n\t" +
                $"Optional: {ExecutionParameter.GenerateIncrementalUpscaleResults} (Required: {ExecutionParameter.MaxBodySize}=integer {ExecutionParameter.ConfigTemplateFilePath}=file {ExecutionParameter.SimExecutablePath}=file {ExecutionParameter.ConfigOutputDirectory}=directory {ExecutionParameter.ResultsOutputDirectory}=directory {ExecutionParameter.DataOutputDirectory}=directory)");

            return false;
        }

        /// <summary>
        ///     Evaluation delegate.
        /// </summary>
        /// <param name="viableBodyBrainCombos">Successful combinations of bodies and brains.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        private delegate void EvalMethod(
            List<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> viableBodyBrainCombos,
            ExperimentDictionaryBodyBrain experimentConfig, int run, VoxelFactoryDecoderPack voxelPack);
    }
}