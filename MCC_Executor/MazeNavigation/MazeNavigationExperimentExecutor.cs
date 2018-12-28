#region

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
using MCC_Domains.MazeNavigation;
using MCC_Domains.MazeNavigation.MCCExperiment;
using MCC_Domains.Utils;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;

#endregion

namespace MCC_Executor.MazeNavigation
{
    /// <summary>
    ///     Encapsulates and standardizes experiment execution parameters.
    /// </summary>
    public enum ExecutionParameter
    {
        /// <summary>
        ///     The source of the experiment configuration and results (i.e. file or database).
        /// </summary>
        ExperimentSource,

        /// <summary>
        ///     The number of experiment runs to execute.
        /// </summary>
        NumRuns,

        /// <summary>
        ///     The run from which execution should begin.
        /// </summary>
        StartFromRun,

        /// <summary>
        ///     Directory containing the experiment configurations.
        /// </summary>
        ExperimentConfigDirectory,

        /// <summary>
        ///     Directory into which to write experiment results.
        /// </summary>
        OutputFileDirectory,

        /// <summary>
        ///     Whether or not to log detailed information about individual navigator evaluations.
        /// </summary>
        LogOrganismStateData,

        /// <summary>
        ///     Flag indicating whether different runs will be distributed on separate cluster nodes.  If this is true, the
        ///     "StartFromRun" parameter must be set as this will indicate the run that's being executed on a particular node.
        ///     Additionally, each node will execute one run only.
        /// </summary>
        IsDistributedExecution,

        /// <summary>
        ///     The path (directory and file name) of the seed agents (navigators). It is assumed that all seed agents are able to
        ///     solve at least one of the seed mazes (i.e. or that they meet the specific MC constraints). If this is not
        ///     specified, a bootstrap process will be executed to evolve these agents to seed the agent population queue.
        /// </summary>
        SeedAgentFile,

        /// <summary>
        ///     The path (directory and file name) of the seed mazes.  This is only required/used in the case of MCC
        ///     experiments, and is strictly required for said experiments.
        /// </summary>
        SeedMazeFile,

        /// <summary>
        ///     The names of the experiments to execute (this will be matched up against the experiment configurations in the
        ///     experiment configs directory).
        /// </summary>
        ExperimentNames
    }

    /// <summary>
    ///     Handles automated, command-line execution for all maze navigation based experiments.
    /// </summary>
    public static class MazeNavigationExperimentExecutor
    {
        /// <summary>
        ///     MCC algorithm container, encapsulating two population queues.
        /// </summary>
        private static IMCCAlgorithmContainer<NeatGenome, MazeGenome> _mccEaContainer;

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
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            // Extract the execution parameters and check for any errors (exit application if any found)
            if (ParseAndValidateConfiguration(args) == false)
                Environment.Exit(0);

            // Determine whether this is a distributed execution
            var isDistributedExecution =
                ExecutionConfiguration.ContainsKey(ExecutionParameter.IsDistributedExecution) &&
                bool.Parse(ExecutionConfiguration[ExecutionParameter.IsDistributedExecution]);

            // Set the execution source (file or database)
            var executionSource = ExecutionConfiguration[ExecutionParameter.ExperimentSource].ToLowerInvariant();

            // Read number of runs and run to start from.  Note that if this is a distributed execution, each node
            // will only execute a single run, so the number of runs will be equivalent to the run to start from
            // (this ensures that the ensuing loop that executes all of the runs executes exactly once)
            var startFromRun = ExecutionConfiguration.ContainsKey(ExecutionParameter.StartFromRun)
                ? int.Parse(ExecutionConfiguration[ExecutionParameter.StartFromRun])
                : 1;
            var numRuns = isDistributedExecution
                ? startFromRun
                : int.Parse(ExecutionConfiguration[ExecutionParameter.NumRuns]);

            // Extract path to seed agent file
            var seedAgentFile = ExecutionConfiguration.ContainsKey(ExecutionParameter.SeedAgentFile)
                ? ExecutionConfiguration[ExecutionParameter.SeedAgentFile]
                : null;
            
            // Extract the experiment names
            var experimentNames = ExecutionConfiguration[ExecutionParameter.ExperimentNames].Split(',');

            foreach (var curExperimentName in experimentNames)
            {
                _executionLogger.Info($"Executing Experiment {curExperimentName}");

                if ("file".Equals(executionSource))
                {
                    // Execute file-based MCC experiment
                    ExecuteFileBasedMCCExperiment(
                        ExecutionConfiguration[ExecutionParameter.ExperimentConfigDirectory],
                        ExecutionConfiguration[ExecutionParameter.OutputFileDirectory], seedAgentFile,
                        ExecutionConfiguration[ExecutionParameter.SeedMazeFile], curExperimentName, numRuns,
                        startFromRun);
                }
                else
                {
                    throw new NotImplementedException(
                        "Database-based MCC executor has not been implemented!");
                }

                // If this is a distributed execution, write out a sentinel file for every run (since each node is only
                // executing one run)
                if (isDistributedExecution)
                {
                    using (
                        File.Create(
                            $"{Path.Combine(ExecutionConfiguration[ExecutionParameter.OutputFileDirectory], curExperimentName)} - Run {startFromRun} - COMPLETE")
                    )
                    {
                    }
                }
                // Otherwise, Write a sentinel file to indicate analysis completion
                else if ("file".Equals(executionSource))
                {
                    // Write sentinel file to the given output directory
                    using (
                        File.Create(
                            $"{Path.Combine(ExecutionConfiguration[ExecutionParameter.OutputFileDirectory], curExperimentName)} - COMPLETE")
                    )
                    {
                    }
                }
            }
        }

        /// <summary>
        ///     Populates the execution configuration and checks for any errors in said configuration.
        /// </summary>
        /// <param name="executionArguments">The arguments with which the experiment executor is being invoked.</param>
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
                        // Ensure that experiment source is either file or database
                        case ExecutionParameter.ExperimentSource:
                            if ("file".Equals(parameterValuePair[1].ToLowerInvariant()) == false &&
                                "database".Equals(parameterValuePair[1].ToLowerInvariant()) == false)
                            {
                                _executionLogger.Error(
                                    $"The value for parameter [{curParameter}] must be either file or database.");
                                isConfigurationValid = false;
                            }

                            break;

                        // Ensure that a valid number of runs was specified
                        case ExecutionParameter.NumRuns:
                        case ExecutionParameter.StartFromRun:
                            if (int.TryParse(parameterValuePair[1], out _) == false)
                            {
                                _executionLogger.Error($"The value for parameter [{curParameter}] must be an integer.");
                                isConfigurationValid = false;
                            }

                            break;

                        // Ensure that valid boolean values were given
                        case ExecutionParameter.LogOrganismStateData:
                        case ExecutionParameter.IsDistributedExecution:
                            if (bool.TryParse(parameterValuePair[1], out _) == false)
                            {
                                _executionLogger.Error($"The value for parameter [{curParameter}] must be a boolean.");
                                isConfigurationValid = false;
                            }

                            break;

                        // Ensure that the seed population and experiments configuration directories actually exist
                        case ExecutionParameter.SeedAgentFile:
                        case ExecutionParameter.ExperimentConfigDirectory:
                            if (Directory.Exists(parameterValuePair[1]) == false)
                            {
                                _executionLogger.Error(
                                    $"The given experiment configuration directory [{parameterValuePair[1]}] does not exist");
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
            if (isConfigurationValid && (ExecutionConfiguration.Count ==
                                         Enum.GetNames(typeof(ExecutionParameter)).Length) == false)
            {
                // Check for existence of experiment source
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.ExperimentSource) == false)
                {
                    _executionLogger.Error($"Parameter [{ExecutionParameter.ExperimentSource}] must be specified.");
                    isConfigurationValid = false;
                }

                // Check for existence of experiment names
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.ExperimentNames) == false)
                {
                    _executionLogger.Error($"Parameter [{ExecutionParameter.ExperimentNames}] must be specified.");
                    isConfigurationValid = false;
                }

                // Check for existence of number of runs (only required if non-distributed execution)
                if ((ExecutionConfiguration.ContainsKey(ExecutionParameter.IsDistributedExecution) == false ||
                     Convert.ToBoolean(ExecutionConfiguration[ExecutionParameter.IsDistributedExecution]) == false) &&
                    ExecutionConfiguration.ContainsKey(ExecutionParameter.NumRuns) == false)
                {
                    _executionLogger.Error($"Parameter [{ExecutionParameter.NumRuns}] must be specified.");
                    isConfigurationValid = false;
                }

                // Check for existence of output file directory
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.OutputFileDirectory) == false)
                {
                    _executionLogger.Error($"Parameter [{ExecutionParameter.OutputFileDirectory}] must be specified.");
                    isConfigurationValid = false;
                }

                // If this is a file-based experiment, then the experiment configuration directory must be specified
                if ("file".Equals(ExecutionConfiguration[ExecutionParameter.ExperimentSource].ToLowerInvariant()) &&
                    ExecutionConfiguration.ContainsKey(ExecutionParameter.ExperimentConfigDirectory) == false)
                {
                    _executionLogger.Error(
                        $"Parameter [{ExecutionParameter.ExperimentConfigDirectory}] must be specified.");
                    isConfigurationValid = false;
                }

                // If this is a MCC experiment, the seed maze must be specified
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.SeedMazeFile) == false ||
                    ExecutionConfiguration[ExecutionParameter.SeedMazeFile] == null)
                {
                    _executionLogger.Error(
                        "If a MCC experiment is being executed, the full path and filename of the seed maze must be specified.");
                    isConfigurationValid = false;
                }

                // If this is distributed execution, the StartFromRun parameter must be specified as this
                // is used to control which node is executing which run of the experiment
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.IsDistributedExecution) &&
                    Convert.ToBoolean(ExecutionConfiguration[ExecutionParameter.IsDistributedExecution]) &&
                    ExecutionConfiguration.ContainsKey(ExecutionParameter.StartFromRun) == false)
                {
                    _executionLogger.Error(
                        "If this is a distributed execution, the StartFromRun parameter must be specified via the invoking job.");
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            _executionLogger.Error("The experiment executor invocation must take the following form:");
            _executionLogger.Error(
                string.Format(
                    "SharpNeatConsole.exe {0}=[{10}] {1}=[{12}] {2}=[{13}] {3}=[{14}] {4}=[{14}] {5}=[{11}] {6}=[{15}] {7}=[{16}] {8}=[{11}] {9}=[{17}]",
                    ExecutionParameter.ExperimentSource, ExecutionParameter.NumRuns, ExecutionParameter.StartFromRun,
                    ExecutionParameter.ExperimentConfigDirectory, ExecutionParameter.OutputFileDirectory,
                    ExecutionParameter.LogOrganismStateData, ExecutionParameter.SeedAgentFile,
                    ExecutionParameter.SeedMazeFile, ExecutionParameter.IsDistributedExecution,
                    ExecutionParameter.ExperimentNames, "file|database", "true|false", "# runs", "starting run #", 
                    "directory", "agent genome file", "maze genome file", "experiment,experiment,..."));

            // If we've reached this point, the configuration is indeed invalid
            return false;
        }

        /// <summary>
        ///     Executes all runs of a given MCC experiment using a configuration file as the configuration source and
        ///     generated flat files as the result destination.
        /// </summary>
        /// <param name="experimentConfigurationDirectory">The directory containing the XML experiment configuration file.</param>
        /// <param name="logFileDirectory">The directory into which to write the evolution/evaluation log files.</param>
        /// <param name="seedAgentPath">The path to the XML agent genome file.</param>
        /// <param name="seedMazePath">The path to the XML maze genome file.</param>
        /// <param name="experimentName">The name of the experiment to execute.</param>
        /// <param name="numRuns">The number of runs to execute for that experiment.</param>
        /// <param name="startFromRun">The run to start from (1 by default).</param>
        private static void ExecuteFileBasedMCCExperiment(string experimentConfigurationDirectory,
            string logFileDirectory, string seedAgentPath, string seedMazePath,
            string experimentName,
            int numRuns, int startFromRun)
        {
            // Read in the configuration files that match the given experiment name (should only be 1 file)
            var experimentConfigurationFiles =
                Directory.GetFiles(experimentConfigurationDirectory, $"{experimentName}.*");

            // Make sure there's only one configuration file that matches the experiment name
            // (otherwise, we don't know for sure which configuration to use)
            if (experimentConfigurationFiles.Count() != 1)
            {
                _executionLogger.Error(
                    $"Experiment configuration ambiguous: expected a single possible configuration, but found {experimentConfigurationFiles.Count()} possible configurations");
                Environment.Exit(0);
            }

            // Instantiate XML reader for configuration file
            var xmlConfig = new XmlDocument();
            xmlConfig.Load(experimentConfigurationFiles[0]);

            // Determine which experiment to execute
            var experiment = DetermineMCCMazeNavigationExperiment(xmlConfig.DocumentElement);

            // Execute the experiment for the specified number of runs
            for (int runIdx = startFromRun; runIdx <= numRuns; runIdx++)
            {
                // Initialize the data loggers for the given run
                IDataLogger navigatorDataLogger =
                    new FileDataLogger($"{logFileDirectory}\\{experimentName} - Run{runIdx} - NavigatorEvolution.csv");
                IDataLogger navigatorPopulationLogger =
                    new FileDataLogger($"{logFileDirectory}\\{experimentName} - Run{runIdx} - NavigatorPopulation.csv");
                IDataLogger navigatorGenomesLogger =
                    new FileDataLogger($"{logFileDirectory}\\{experimentName} - Run{runIdx} - NavigatorGenomes.csv");
                IDataLogger mazeDataLogger =
                    new FileDataLogger($"{logFileDirectory}\\{experimentName} - Run{runIdx} - MazeEvolution.csv");
                IDataLogger mazePopulationLogger =
                    new FileDataLogger($"{logFileDirectory}\\{experimentName} - Run{runIdx} - MazePopulation.csv");
                IDataLogger mazeGenomesLogger =
                    new FileDataLogger($"{logFileDirectory}\\{experimentName} - Run{runIdx} - MazeGenomes.csv");

                // Initialize new experiment
                experiment.Initialize(experimentName, xmlConfig.DocumentElement, navigatorDataLogger,
                    navigatorPopulationLogger, navigatorGenomesLogger, mazeDataLogger, mazePopulationLogger,
                    mazeGenomesLogger);

                _executionLogger.Info($"Initialized experiment {experiment.GetType()}.");

                // Create a new agent genome factory
                var agentGenomeFactory = experiment.CreateAgentGenomeFactory();

                // Create a new maze genome factory
                var mazeGenomeFactory = experiment.CreateMazeGenomeFactory();

                // Read in the seed agent population or generate the initial agent population if no seeds were specified
                var agentGenomeList = seedAgentPath != null
                    ? ExperimentUtils.ReadSeedNeatGenomes(seedAgentPath, (NeatGenomeFactory) agentGenomeFactory)
                    : agentGenomeFactory.CreateGenomeList(experiment.AgentInitializationGenomeCount, 0);

                // Read in the seed maze population
                var mazeGenomeList = ExperimentUtils
                    .ReadSeedMazeGenomes(seedMazePath, (MazeGenomeFactory) mazeGenomeFactory)
                    .Take(experiment.MazeSeedGenomeCount).ToList();

                // Check for insufficient number of genomes in the seed maze file
                if (mazeGenomeList.Count < experiment.MazeSeedGenomeCount)
                {
                    throw new SharpNeatException(
                        $"The maze genome input file contains only {mazeGenomeList.Count} genomes while the experiment configuration requires {experiment.MazeDefaultPopulationSize} genomes.");
                }
                
                _executionLogger.Info(
                    $"Loaded [{agentGenomeList.Count}] navigator genomes and [{mazeGenomeList.Count}] seed maze genomes as initial populations.");

                _executionLogger.Info("Kicking off Experiment initialization/execution");

                // Kick off the experiment run
                RunExperiment(agentGenomeFactory, mazeGenomeFactory, agentGenomeList, mazeGenomeList,
                    seedAgentPath != null, experimentName, experiment, numRuns, runIdx);

                // Close the data loggers
                navigatorDataLogger.Close();
                navigatorPopulationLogger.Close();
                navigatorGenomesLogger.Close();
                mazeDataLogger.Close();
                mazePopulationLogger.Close();
                mazeGenomesLogger.Close();
            }
        }

        /// <summary>
        ///     Executes a single run of the given MCC experiment.
        /// </summary>
        /// <param name="agentGenomeFactory">The factory for producing NEAT genomes.</param>
        /// <param name="mazeGenomeFactory">The factory for producing maze genomes.</param>
        /// <param name="agentGenomeList">The list of initial agent genomes (navigator population).</param>
        /// <param name="mazeGenomeList">The list of initial maze genomes (maze population).</param>
        /// <param name="areAgentsPreevolved">Indicates whether the agents have been pre-evolved and loaded from a file or another source.</param>
        /// <param name="experimentName">The name of the MCC experiment to execute.</param>
        /// <param name="experiment">Reference to the initialized experiment.</param>
        /// <param name="numRuns">Total number of runs being executed.</param>
        /// <param name="runIdx">The current run being executed.</param>
        private static void RunExperiment(IGenomeFactory<NeatGenome> agentGenomeFactory, IGenomeFactory<MazeGenome> mazeGenomeFactory, List<NeatGenome> agentGenomeList, List<MazeGenome> mazeGenomeList, bool areAgentsPreevolved, string experimentName, IMCCExperiment experiment, int numRuns, int runIdx)
        {
            try
            {
                // Create evolution algorithm and attach update event.
                _mccEaContainer = experiment.CreateMCCAlgorithmContainer(agentGenomeFactory,
                    mazeGenomeFactory, agentGenomeList,
                    mazeGenomeList, areAgentsPreevolved);
                _mccEaContainer.UpdateEvent += MccContainerUpdateEvent;
            }
            catch (Exception exception)
            {
                _executionLogger.Error($"Experiment {experimentName}, Run {runIdx} of {numRuns} failed to initialize");
                _executionLogger.Error(exception.Message);
                Environment.Exit(0);
            }

            _executionLogger.Info($"Executing Experiment {experimentName}, Run {runIdx} of {numRuns}");

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
            if (_mccEaContainer.Population1CurrentChampGenome != null &&
                _mccEaContainer.Population2CurrentChampGenome != null)
            {
                _executionLogger.Info(
                    $"Generation={_mccEaContainer.CurrentGeneration:N0} Evaluations={_mccEaContainer.CurrentEvaluations:N0} Population1BestFitness={_mccEaContainer.Population1CurrentChampGenome.EvaluationInfo.Fitness:N2} Population1MaxComplexity={_mccEaContainer.Population1Statistics.MaxComplexity:N2} Population2BestFitness={_mccEaContainer.Population2CurrentChampGenome.EvaluationInfo.Fitness:N2}, Population2MaxComplexity={_mccEaContainer.Population2Statistics.MaxComplexity:N2}");
            }
        }

        /// <summary>
        ///     Determine the MCC maze navigation experiment to run based on the search and selection algorithms specified
        ///     in the configuration file.
        /// </summary>
        /// <param name="xmlConfig">The reference to the root node of the XML configuration file.</param>
        /// <returns>The appropriate maze navigation experiment class.</returns>
        private static BaseMCCMazeNavigationExperiment DetermineMCCMazeNavigationExperiment(XmlElement xmlConfig)
        {
            // Get the search and selection algorithm types
            var searchAlgorithm = XmlUtils.TryGetValueAsString(xmlConfig, "SearchAlgorithm");
            var selectionAlgorithm = XmlUtils.TryGetValueAsString(xmlConfig, "SelectionAlgorithm");

            // Make sure both the search algorithm and selection algorithm have been set in the configuration file
            if (searchAlgorithm == null || selectionAlgorithm == null)
            {
                _executionLogger.Error(
                    "Both the search algorithm and selection algorithm must be specified in the XML configuration file.");
                Environment.Exit(0);
            }

            // Get the appropriate experiment class
            return DetermineMCCMazeNavigationExperiment(searchAlgorithm, selectionAlgorithm);
        }

        /// <summary>
        ///     Determines the MCC maze navigation experiment to run based on the given search and selection algorithm.
        /// </summary>
        /// <param name="searchAlgorithmName">The search algorithm to run.</param>
        /// <param name="selectionAlgorithmName">The selection algorithm to run.</param>
        /// <returns>The applicable MCC maze navigation experiment.</returns>
        private static BaseMCCMazeNavigationExperiment DetermineMCCMazeNavigationExperiment(
            string searchAlgorithmName, string selectionAlgorithmName)
        {
            // Extract the corresponding search and selection algorithm domain types
            var searchType = AlgorithmTypeUtil.ConvertStringToSearchType(searchAlgorithmName);
            var selectionType = AlgorithmTypeUtil.ConvertStringToSelectionType(selectionAlgorithmName);

            // Queueing MCC experiment with separate (currently per-species) queues
            if (SelectionType.MultipleQueueing.Equals(selectionType))
            {
                return new MCCSpeciationExperiment();
            }

            // Otherwise, go with the single maze, single queue MCC experiment
            return new MCCControlExperiment();
        }
    }
}