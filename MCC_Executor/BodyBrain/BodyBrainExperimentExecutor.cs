using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using log4net;
using log4net.Config;
using MCC_Domains;
using MCC_Domains.BodyBrain;
using MCC_Domains.MazeNavigation;
using MCC_Domains.Utils;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;

namespace MCC_Executor.BodyBrain
{
    /// <summary>
    ///     Encapsulates and standardizes experiment execution parameters.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     The name of the experiment to execute (this will be matched up against the experiment configurations in the
        ///     experiment configs directory).
        /// </summary>
        ExperimentName,

        /// <summary>
        ///     The current run number.
        /// </summary>
        Run,

        /// <summary>
        ///     Directory containing the experiment configurations.
        /// </summary>
        ExperimentConfigDirectory,

        /// <summary>
        /// Directory into which to write generated simulation configuration files.
        /// </summary>
        SimulationConfigFileDirectory,
        
        /// <summary>
        ///     Directory into which to write simulation results.
        /// </summary>
        SimulationResultsDirectory,

        /// <summary>
        /// Simulator executable path and file.
        /// </summary>
        SimulatorExecutable,
        
        /// <summary>
        ///     Directory into which to write experiment results.
        /// </summary>
        OutputFileDirectory,
        
        /// <summary>
        ///     The path (directory and file name) of the seed brains (CPPN controllers). It is assumed that all brains are
        ///     able to control at least one body such that it is able to satisfy its locomotion MC. If this is not specified, a
        ///     bootstrap process will be executed to evolve these brains to seed the brain population queue.
        /// </summary>
        SeedBrainFile,

        /// <summary>
        ///     The path (directory and file name) of the seed robot bodies. This is required.
        /// </summary>
        SeedBodyFile
    }

    public static class BodyBrainExperimentExecutor
    {
        /// <summary>
        ///     MCC algorithm container, encapsulating two population queues.
        /// </summary>
        private static IMCCAlgorithmContainer<NeatGenome, NeatGenome> _mccEaContainer;
        
        /// <summary>
        ///     Console logger for reporting execution status.
        /// </summary>
        private static ILog _executionLogger;

        /// <summary>
        ///     Encapsulates configuration parameters specified at runtime.
        /// </summary>
        private static readonly Dictionary<ExecutionParameter, string> ExecutionConfiguration =
            new Dictionary<ExecutionParameter, string>();

        public static void Execute(string[] args)
        {
            // Initialise log4net (log to console and file).
            XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetEntryAssembly()),
                new FileInfo("log4net.config"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            
            // Extract the execution parameters and check for any errors (exit application if any found)
            if (ParseAndValidateConfiguration(args) == false)
                Environment.Exit(0);

            // Extract experiment name and run
            var experimentName = ExecutionConfiguration[ExecutionParameter.ExperimentName];
            var run = int.Parse(ExecutionConfiguration[ExecutionParameter.Run]);
            
            // Extract simulation output directories
            var simConfigDirectory = ExecutionConfiguration[ExecutionParameter.SimulationConfigFileDirectory];
            var simResultsDirectory = ExecutionConfiguration[ExecutionParameter.SimulationResultsDirectory];
            var simExecutable = ExecutionConfiguration[ExecutionParameter.SimulatorExecutable];
            
            // Extract directory for writing experiment output logs
            var outputFileDirectory = ExecutionConfiguration[ExecutionParameter.OutputFileDirectory];
            
            // Extract path to seed brain file
            var seedBrainFile = ExecutionConfiguration.ContainsKey(ExecutionParameter.SeedBrainFile)
                ? ExecutionConfiguration[ExecutionParameter.SeedBrainFile]
                : null;
            
            // Extract path to seed body file
            var seedBodyFile = ExecutionConfiguration.ContainsKey(ExecutionParameter.SeedBodyFile)
                ? ExecutionConfiguration[ExecutionParameter.SeedBodyFile]
                : null;

            // Read in the configuration files that match the given experiment name (should only be 1 file)
            var experimentConfigurationFiles =
                Directory.GetFiles(ExecutionConfiguration[ExecutionParameter.ExperimentConfigDirectory],
                    $"{ExecutionConfiguration[ExecutionParameter.ExperimentName]}.*");

            // Make sure there's only one configuration file that matches the experiment name
            // (otherwise, we don't know for sure which configuration to use)
            if (experimentConfigurationFiles.Count() != 1)
            {
                _executionLogger.Error(
                    $"Experiment configuration ambiguous: expected a single possible configuration, but found {experimentConfigurationFiles.Count()} possible configurations");
                Environment.Exit(0);
            }
            
            _executionLogger.Info($"Executing Experiment {experimentName}");
            
            // Execute the current configuration
            ExecuteExperimentConfiguration(experimentConfigurationFiles[0], experimentName, run, seedBrainFile,
                seedBodyFile, simConfigDirectory, simResultsDirectory, simExecutable, outputFileDirectory);

            // TODO: Write sentinel file to indicate completion
        }

        private static void ExecuteExperimentConfiguration(string experimentConfiguration, string experimentName,
            int run, string seedBrainPath, string seedBodyPath, string simConfigDirectory, string simResultsDirectory,
            string simExecutable, string outputFileDirectory)
        {
            // Instantiate XML reader for configuration file
            var xmlConfig = new XmlDocument();
            xmlConfig.Load(experimentConfiguration);

            // TODO: When there are other experiment types, this should come back as a base experiment
            var experiment = new BodyBrainExperiment();
            
            // Initialize new experiment
            experiment.Initialize(experimentName, run, simConfigDirectory, simResultsDirectory, simExecutable,
                xmlConfig.DocumentElement, outputFileDirectory);

            _executionLogger.Info($"Initialized experiment {experiment.GetType()}.");
            
            // Create a new brain and body genome factory
            var brainGenomeFactory = experiment.CreateBrainGenomeFactory();
            var bodyGenomeFactory = experiment.CreateBodyGenomeFactory();
            
            // Read in the seed brain population or generate the initial brain population if no seeds were specified
            var brainGenomeList = seedBrainPath != null
                ? ExperimentUtils.ReadSeedNeatGenomes(seedBrainPath, (CppnGenomeFactory) brainGenomeFactory)
                    .Take(experiment.BrainSeedGenomeCount).ToList()
                : null;
            
            // Read in the seed maze population
            var bodyGenomeList = seedBodyPath != null
                ? ExperimentUtils.ReadSeedNeatGenomes(seedBodyPath, (CppnGenomeFactory) bodyGenomeFactory)
                    .Take(experiment.BodySeedGenomeCount).ToList()
                : null;

            // Check for insufficient number of brain seed genomes
            if (brainGenomeList != null && brainGenomeList.Count < experiment.BrainSeedGenomeCount)
            {
                throw new SharpNeatException(
                    $"The brain genome input file contains only {brainGenomeList.Count} genomes while the experiment configuration requires {experiment.BrainSeedGenomeCount} seed genomes");
            }

            if (bodyGenomeList != null && bodyGenomeList.Count < experiment.BodySeedGenomeCount)
            {
                throw new SharpNeatException(
                    $"The body genome input file contains only {bodyGenomeList.Count} genomes while the experiment configuration requires {experiment.BodySeedGenomeCount} seed genomes");
            }

            _executionLogger.Info(
                $"Loaded [{brainGenomeList?.Count ?? 0}] seed brain genomes and [{bodyGenomeList?.Count ?? 0}] seed body genomes as initial populations");
            
            _executionLogger.Info($"Kicking off Experiment initialization/execution for run [{run}]");
            
            // Kick off the experiment run
            RunExperiment(brainGenomeFactory, bodyGenomeFactory, brainGenomeList, bodyGenomeList, experimentName,
                experiment, run);
        }

        private static void RunExperiment(IGenomeFactory<NeatGenome> brainGenomeFactory,
            IGenomeFactory<NeatGenome> bodyGenomeFactory, List<NeatGenome> brainGenomeList,
            List<NeatGenome> bodyGenomeList, string experimentName, IBodyBrainExperiment experiment, int run)
        {
            try
            {
                // Create evolution algorithm and attach update event.
                _mccEaContainer = experiment.CreateMCCAlgorithmContainer(brainGenomeFactory,
                    bodyGenomeFactory, brainGenomeList,
                    bodyGenomeList);
                _mccEaContainer.UpdateEvent += MccContainerUpdateEvent;
            }
            catch (Exception exception)
            {
                _executionLogger.Error($"Experiment {experimentName}, Run {run} failed to initialize");
                _executionLogger.Error(exception.Message);
                Environment.Exit(0);
            }
            
            _executionLogger.Info($"Executing Experiment {experimentName}, Run {run}");

            // Start algorithm (it will run on a background thread).
            _mccEaContainer.StartContinue();

            while (RunState.Terminated != _mccEaContainer.RunState &&
                   RunState.Paused != _mccEaContainer.RunState)
            {
                Thread.Sleep(2000);
            }
        }
        
        /// <summary>
        ///     Print update event specific to MCC algorithm.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private static void MccContainerUpdateEvent(object sender, EventArgs e)
        {
            if (_mccEaContainer.AgentChampGenome != null &&
                _mccEaContainer.EnvironmentChampGenome != null)
            {
                _executionLogger.Info(
                    $"Generation={_mccEaContainer.CurrentGeneration:N0} Evaluations={_mccEaContainer.CurrentEvaluations:N0} Population1BestFitness={_mccEaContainer.AgentChampGenome.EvaluationInfo.Fitness:N2} Population1MaxComplexity={_mccEaContainer.AgentPopulationStats.MaxComplexity:N2} Population2BestFitness={_mccEaContainer.EnvironmentChampGenome.EvaluationInfo.Fitness:N2}, Population2MaxComplexity={_mccEaContainer.EnvironmentPopulationStats.MaxComplexity:N2}");
            }
        }

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
                    
                    // TODO: Do parameter-specific validity checking...
                
                    // If all else checks out, add the parameter to the map
                    ExecutionConfiguration.Add(curParameter, parameterValuePair[1]);
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;
            
            // If we've reached this point, the configuration is indeed invalid
            return false;
        }
    }
}