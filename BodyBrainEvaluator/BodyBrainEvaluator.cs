using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BodyBrainSupportLib;
using ExperimentEntities.entities;
using log4net;
using log4net.Config;
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

            // Get boolean indicator dictating whether to generate run body diversity data
            var generateRunBodyDiversity =
                ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateRunBodyDiversityData) &&
                bool.Parse(ExecutionConfiguration[ExecutionParameter.GenerateRunBodyDiversityData]);

            // Get boolean indicator dictating whether to generate batch body diversity data
            var generateBatchBodyDiversity =
                ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateBatchBodyDiversityData) &&
                bool.Parse(ExecutionConfiguration[ExecutionParameter.GenerateBatchBodyDiversityData]);

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

                ProcessIndependentResultChunks(curExperimentConfiguration, run,
                    EvaluationHandler.GenerateSimulationLogData);

                DataHandler.CloseFileWriter(OutputFileType.SimulationLogData);
                DataHandler.WriteSentinelFile(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - SimulationLog"), run);
            }

            // Run simulation configuration file generation
            if (generateSimConfigs)
            {
                ProcessIndependentResultChunks(curExperimentConfiguration, run,
                    EvaluationHandler.GenerateSimulationConfigs);
            }

            // Run incremental upscale evaluation
            if (generateUpscaleResults)
            {
                DataHandler.OpenFileWriter(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - UpscaleResults - Run{run}.csv"), OutputFileType.UpscaleResultData);

                ProcessIndependentResultChunks(curExperimentConfiguration, run,
                    EvaluationHandler.GenerateUpscaleResultData);

                DataHandler.CloseFileWriter(OutputFileType.UpscaleResultData);
                DataHandler.WriteSentinelFile(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - UpscaleResults"), run);
            }

            // Run body diversity analysis across the entire run
            if (generateRunBodyDiversity)
            {
                DataHandler.OpenFileWriter(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - RunBodyDiversity - Run{run}.csv"), OutputFileType.RunBodyDiversityData);

                ProcessComparativeBodyResultChunks(curExperimentConfiguration, run,
                    EvaluationHandler.GenerateRunBodyDiversityData);

                DataHandler.CloseFileWriter(OutputFileType.RunBodyDiversityData);
                DataHandler.WriteSentinelFile(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - RunBodyDiversity"), run);
            }

            // Run per-batch body diversity analysis
            if (generateBatchBodyDiversity)
            {
                DataHandler.OpenFileWriter(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - BatchBodyDiversity - Run{run}.csv"),
                    OutputFileType.BatchBodyDiversityData);

                ProcessComparativePerBatchBodyResultChunks(curExperimentConfiguration, run,
                    EvaluationHandler.GenerateBatchBodyDiversityData);

                DataHandler.CloseFileWriter(OutputFileType.BatchBodyDiversityData);
                DataHandler.WriteSentinelFile(
                    Path.Combine(ExecutionConfiguration[ExecutionParameter.DataOutputDirectory],
                        $"{experimentName} - BatchBodyDiversity"), run);
            }

            _executionLogger.Info($"Result processing for experiment [{experimentName}] and run [{run}] complete");
        }

        /// <summary>
        ///     Iterate through discrete result chunks and evaluate each against the full population of bodies evolved in the given
        ///     run.
        /// </summary>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="comparativeBodyEvalMethod">The evaluation method to apply to each chunk against the full population.</param>
        /// <param name="chunkSize">The number of body/brain combinations to process at one time (optional).</param>
        private static void ProcessComparativeBodyResultChunks(ExperimentDictionaryBodyBrain experimentConfig, int run,
            ComparativeBodyEvalMethod comparativeBodyEvalMethod, int chunkSize = 100)
        {
            var allBodiesBag = new ConcurrentBag<VoxelBody>();
            var experimentId = experimentConfig.ExperimentDictionaryId;

            // Create container object for body/brain factories and decoders
            var voxelPack = new VoxelFactoryDecoderPack(experimentConfig.VoxelyzeConfigInitialXdimension,
                experimentConfig.VoxelyzeConfigInitialYdimension, experimentConfig.VoxelyzeConfigInitialZdimension,
                experimentConfig.MaxBodySize, experimentConfig.ActivationIters);

            // Read all body genome XMLs
            var serializedBodyGenomes = DataHandler.GetBodyGenomeXml(experimentId, run);

            // Decode each serialized body genome XML strings to voxel body objects
            Parallel.ForEach(serializedBodyGenomes,
                bodyXml =>
                {
                    allBodiesBag.Add(DecodeHandler.DecodeBodyGenome(bodyXml, voxelPack.BodyDecoder,
                        voxelPack.BodyGenomeFactory));
                });

            // Convert bodies bag to list so it can be enumerated below
            var allBodies = allBodiesBag.ToList();

            for (var curChunk = 0; curChunk < allBodies.Count; curChunk += chunkSize)
            {
                _executionLogger.Info(
                    $"Evaluating bodies [{curChunk}] through [{chunkSize + curChunk}] of [{allBodies.Count}]");

                // Get voxel bodies for the current chunk
                var curBodies = allBodiesBag.Skip(curChunk).Take(chunkSize).ToList();

                // Invoke the given evaluation method
                comparativeBodyEvalMethod(curBodies, allBodies, experimentConfig, run, voxelPack);
            }
        }

        private static void ProcessComparativePerBatchBodyResultChunks(ExperimentDictionaryBodyBrain experimentConfig,
            int run, ComparativePerBatchBodyEvalMethod comparativePerBatchBodyEvalMethod)
        {
            var experimentId = experimentConfig.ExperimentDictionaryId;
            var numBatches = experimentConfig.MaxBatches;

            // Create container object for body/brain factories and decoders
            var voxelPack = new VoxelFactoryDecoderPack(experimentConfig.VoxelyzeConfigInitialXdimension,
                experimentConfig.VoxelyzeConfigInitialYdimension, experimentConfig.VoxelyzeConfigInitialZdimension,
                experimentConfig.MaxBodySize, experimentConfig.ActivationIters);

            for (var batch = 0; batch < numBatches; batch++)
            {
                _executionLogger.Info($"Evaluating bodies for batch [{batch}] of [{numBatches}]");

                var allBatchBodiesBag = new ConcurrentBag<VoxelBody>();

                // Read all body genome XMLs for the current batch
                var serializedBodyGenomes = DataHandler.GetBodyGenomeXml(experimentId, run, batch);

                // Decode each serialized body genome XML string to voxel body objects
                Parallel.ForEach(serializedBodyGenomes,
                    bodyXml =>
                    {
                        allBatchBodiesBag.Add(DecodeHandler.DecodeBodyGenome(bodyXml, voxelPack.BodyDecoder,
                            voxelPack.BodyGenomeFactory));
                    });

                // Invoke the given evaluation method
                comparativePerBatchBodyEvalMethod(allBatchBodiesBag.ToList(), experimentConfig, run, batch, voxelPack);
            }
        }

        /// <summary>
        ///     Iterate through discrete result chunks of body/brain combinations and apply the selected evaluation to each.
        /// </summary>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="independentEvalMethod">The evaluation method to apply to each chunk.</param>
        /// <param name="chunkSize">The number of body/brain combinations to process at one time (optional).</param>
        private static void ProcessIndependentResultChunks(ExperimentDictionaryBodyBrain experimentConfig, int run,
            IndependentEvalMethod independentEvalMethod, int chunkSize = 100)
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
                independentEvalMethod(successfulGenomeCombos, experimentConfig, run, ExecutionConfiguration, voxelPack);
            }
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

                // Data output directory must be specified if run body diversity data is being generated
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateRunBodyDiversityData) &&
                    Convert.ToBoolean(ExecutionConfiguration[ExecutionParameter.GenerateRunBodyDiversityData]) &&
                    ExecutionConfiguration.ContainsKey(ExecutionParameter.DataOutputDirectory) == false)
                {
                    _executionLogger.Error(
                        $"Parameter [{ExecutionParameter.DataOutputDirectory}] must be specified if run body diversity data is being generated.");
                    isConfigurationValid = false;
                }

                // Data output directory must be specified if run batch diversity data is being generated
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.GenerateBatchBodyDiversityData) &&
                    Convert.ToBoolean(ExecutionConfiguration[ExecutionParameter.GenerateBatchBodyDiversityData]) &&
                    ExecutionConfiguration.ContainsKey(ExecutionParameter.DataOutputDirectory) == false)
                {
                    _executionLogger.Error(
                        $"Parameter [{ExecutionParameter.DataOutputDirectory}] must be specified if batch body diversity data is being generated.");
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
                $"Optional: {ExecutionParameter.GenerateIncrementalUpscaleResults} (Required: {ExecutionParameter.MaxBodySize}=integer {ExecutionParameter.ConfigTemplateFilePath}=file {ExecutionParameter.SimExecutablePath}=file {ExecutionParameter.ConfigOutputDirectory}=directory {ExecutionParameter.ResultsOutputDirectory}=directory {ExecutionParameter.DataOutputDirectory}=directory) \n\t" +
                $"Optional: {ExecutionParameter.GenerateRunBodyDiversityData} (Required: {ExecutionParameter.DataOutputDirectory}=directory) \n\t" +
                $"Optional: {ExecutionParameter.GenerateBatchBodyDiversityData} (Required: {ExecutionParameter.DataOutputDirectory}=directory)");

            return false;
        }

        /// <summary>
        ///     Independent evaluation delegate.
        /// </summary>
        /// <param name="viableBodyBrainCombos">Successful combinations of bodies and brains.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="executionConfiguration">Encapsulates configuration parameters specified at runtime.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        private delegate void IndependentEvalMethod(
            List<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> viableBodyBrainCombos,
            ExperimentDictionaryBodyBrain experimentConfig, int run,
            Dictionary<ExecutionParameter, string> executionConfiguration, VoxelFactoryDecoderPack voxelPack);

        /// <summary>
        ///     Comparative body evaluation delegate.
        /// </summary>
        /// <param name="bodies1">The first list of bodies to compare to the second.</param>
        /// <param name="bodies2">The second list of bodies to compare to the first.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        private delegate void ComparativeBodyEvalMethod(List<VoxelBody> bodies1, List<VoxelBody> bodies2,
            ExperimentDictionaryBodyBrain experimentConfig, int run, VoxelFactoryDecoderPack voxelPack);

        /// <summary>
        ///     Comparative per-batch body evaluation delegate.
        /// </summary>
        /// <param name="bodies1">The list of bodies to compare with each other.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="batch">The current batch.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        private delegate void ComparativePerBatchBodyEvalMethod(List<VoxelBody> bodies,
            ExperimentDictionaryBodyBrain experimentConfig, int run, int batch, VoxelFactoryDecoderPack voxelPack);
    }
}