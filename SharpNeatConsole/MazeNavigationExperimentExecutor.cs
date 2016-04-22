#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using ExperimentEntities;
using log4net;
using log4net.Config;
using SharpNeat.Core;
using SharpNeat.Domains;
using SharpNeat.Domains.MazeNavigation;
using SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment;
using SharpNeat.Domains.MazeNavigation.FitnessExperiment;
using SharpNeat.Domains.MazeNavigation.MCNSExperiment;
using SharpNeat.Domains.MazeNavigation.MCSExperiment;
using SharpNeat.Domains.MazeNavigation.NoveltyExperiment;
using SharpNeat.Domains.MazeNavigation.RandomExperiment;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;

#endregion

namespace SharpNeatConsole
{
    public enum ExecutionParameter
    {
        ExperimentSource,
        NumRuns,
        GeneratePopulation,
        SeedPopulationDirectory,
        ExperimentConfigDirectory,
        OutputFileDirectory,
        LogOrganismStateData,
        IsCoevolution,
        ExperimentNames
    }

    public class MazeNavigationExperimentExecutor
    {
        private static INeatEvolutionAlgorithm<NeatGenome> _ea;
        private static ICoevolutionAlgorithmContainer<NeatGenome, MazeGenome> _coevolutionEaContainer;
        private static ILog _executionLogger;

        private static readonly Dictionary<ExecutionParameter, String> _executionConfiguration =
            new Dictionary<ExecutionParameter, string>();

        private static void Main(string[] args)
        {
            // Initialise log4net (log to console and file).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            // Extract the execution parameters and check for any errors (exit application if any found)
            if (ParseAndValidateConfiguration(args) == false)
                Environment.Exit(0);

            // Set the execution source (file or database)
            string executionSource = _executionConfiguration[ExecutionParameter.ExperimentSource].ToLowerInvariant();

            // Read number of runs
            int numRuns = Int32.Parse(_executionConfiguration[ExecutionParameter.NumRuns]);

            // Determine whether to log organism state data
            bool logOrganismStateData = _executionConfiguration.ContainsKey(ExecutionParameter.LogOrganismStateData) &&
                                        Boolean.Parse(_executionConfiguration[ExecutionParameter.LogOrganismStateData]);

            // Array of initial genome population files
            string[] seedPopulationFiles = null;

            if (Boolean.Parse(_executionConfiguration[ExecutionParameter.GeneratePopulation]) == false)
            {
                // Read in the seed population files
                // Note that two assumptions are made here
                // 1. Files can be naturally sorted and match the naming convention "*Run01", "*Run02", etc.
                // 2. The number of files in the directory matches the number of runs (this is checked for below)
                seedPopulationFiles =
                    Directory.GetFiles(_executionConfiguration[ExecutionParameter.SeedPopulationDirectory]);

                // Make sure that the appropriate number of seed population have been specified
                if (seedPopulationFiles.Count() < numRuns)
                {
                    _executionLogger.Error(
                        string.Format(
                            "Number of seed population files [{0}] not sufficient for the specified number of runs [{1}]",
                            seedPopulationFiles.Count(), numRuns));
                    Environment.Exit(0);
                }
            }

            // Extract the experiment names
            string[] experimentNames = _executionConfiguration[ExecutionParameter.ExperimentNames].Split(',');

            foreach (string curExperimentName in experimentNames)
            {
                _executionLogger.Info(string.Format("Executing Experiment {0}", curExperimentName));

                // If these are non-coevolution experiments, proceed as normal using the base maze navigator experiment configuration
                if (Boolean.Parse(_executionConfiguration[ExecutionParameter.IsCoevolution]) == false)
                {
                    if ("file".Equals(executionSource))
                    {
                        // Execute file-based experiment
                        ExecuteFileBasedExperiment(
                            _executionConfiguration[ExecutionParameter.ExperimentConfigDirectory],
                            _executionConfiguration[ExecutionParameter.OutputFileDirectory], curExperimentName,
                            numRuns, seedPopulationFiles, logOrganismStateData);
                    }
                    else
                    {
                        // Execute database-based experiment
                        ExecuteDatabaseBasedExperiment(curExperimentName, numRuns, seedPopulationFiles);
                    }
                }
                // Otherwise, use the coevolution container interface
                else
                {
                    if ("file".Equals(executionSource))
                    {
                        // Execute file-based coevolution experiment
                        ExecuteFileBasedCoevolutionExperiment(
                            _executionConfiguration[ExecutionParameter.ExperimentConfigDirectory],
                            _executionConfiguration[ExecutionParameter.OutputFileDirectory], curExperimentName, numRuns,
                            seedPopulationFiles, logOrganismStateData);
                    }
                    else
                    {
                        throw new NotImplementedException(
                            "Database-based coevolution executor has not been implemented!");
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
            bool isConfigurationValid = executionArguments != null;

            // Only continue if there are execution arguments
            if (executionArguments != null && executionArguments.Length > 0)
            {
                foreach (string executionArgument in executionArguments)
                {
                    ExecutionParameter curParameter;

                    // Get the key/value pair
                    string[] parameterValuePair = executionArgument.Split('=');

                    // Attempt to parse the current parameter
                    isConfigurationValid = Enum.TryParse(parameterValuePair[0], true, out curParameter);

                    // If the current parameter is not valid, break out of the loop and return
                    if (isConfigurationValid == false)
                    {
                        _executionLogger.Error(string.Format("[{0}] is not a valid configuration parameter.",
                            parameterValuePair[0]));
                        break;
                    }

                    // If the parameter is valid but it already exists in the map, break out of the loop and return
                    if (_executionConfiguration.ContainsKey(curParameter))
                    {
                        _executionLogger.Error(
                            string.Format(
                                "Ambiguous configuration - parameter [{0}] has been specified more than once.",
                                curParameter));
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
                                    string.Format("The value for parameter [{0}] must be either file or database.",
                                        curParameter));
                                isConfigurationValid = false;
                            }
                            break;

                        // Ensure that a valid number of runs was specified
                        case ExecutionParameter.NumRuns:
                            int testInt;
                            if (Int32.TryParse(parameterValuePair[1], out testInt) == false)
                            {
                                _executionLogger.Error(string.Format(
                                    "The value for parameter [{0}] must be an integer.",
                                    curParameter));
                                isConfigurationValid = false;
                            }
                            break;

                        // Ensure that valid boolean values were given
                        case ExecutionParameter.GeneratePopulation:
                        case ExecutionParameter.LogOrganismStateData:
                        case ExecutionParameter.IsCoevolution:
                            bool testBool;
                            if (Boolean.TryParse(parameterValuePair[1], out testBool) == false)
                            {
                                _executionLogger.Error(string.Format("The value for parameter [{0}] must be a boolean.",
                                    curParameter));
                                isConfigurationValid = false;
                            }
                            break;

                        // Ensure that the seed population and experiments configuration directories actually exist
                        case ExecutionParameter.SeedPopulationDirectory:
                        case ExecutionParameter.ExperimentConfigDirectory:
                            if (Directory.Exists(parameterValuePair[1]) == false)
                            {
                                _executionLogger.Error(
                                    string.Format("The given experiment configuration directory [{0}] does not exist",
                                        parameterValuePair[1]));
                                isConfigurationValid = false;
                            }
                            break;
                    }

                    // If all else checks out, add the parameter to the map
                    _executionConfiguration.Add(curParameter, parameterValuePair[1]);
                }
            }
            // If there are no execution arguments, the configuration is invalid
            else
            {
                isConfigurationValid = false;
            }

            // If the per-parameter configuration is valid but not a full list of parameters were specified, makes sure the necessary ones are present
            if (isConfigurationValid && (_executionConfiguration.Count ==
                                         Enum.GetNames(typeof (ExecutionParameter)).Length) == false)
            {
                // Check for existence of experiment source
                if (_executionConfiguration.ContainsKey(ExecutionParameter.ExperimentSource) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.ExperimentSource));
                    isConfigurationValid = false;
                }

                // Check for existence of experiment names
                if (_executionConfiguration.ContainsKey(ExecutionParameter.ExperimentNames) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.ExperimentNames));
                    isConfigurationValid = false;
                }

                // Check for existence of number of runs
                if (_executionConfiguration.ContainsKey(ExecutionParameter.NumRuns) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.NumRuns));
                    isConfigurationValid = false;
                }

                // Check for existence of output file directory
                if (_executionConfiguration.ContainsKey(ExecutionParameter.OutputFileDirectory) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.OutputFileDirectory));
                    isConfigurationValid = false;
                }

                // If this is a file-based experiment, then the experiment configuration directory must be specified
                if ("file".Equals(_executionConfiguration[ExecutionParameter.ExperimentSource].ToLowerInvariant()) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.ExperimentConfigDirectory) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.ExperimentConfigDirectory));
                    isConfigurationValid = false;
                }

                // If the executor is told not to generate the population, then the seed population directory must be specified
                if (Convert.ToBoolean(_executionConfiguration[ExecutionParameter.GeneratePopulation]) == false &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.SeedPopulationDirectory) == false)
                {
                    _executionLogger.Error(
                        "If the executor is being run without generating a population, the directory containing the seed population must be specified.");
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            _executionLogger.Error("The experiment executor invocation must take the following form:");
            _executionLogger.Error(
                string.Format(
                    "SharpNeatConsole.exe {0}=[{9}] {1}=[{11}] {2}=[{10}] {3}=[{12}] {4}=[{12}] {5}=[{12}] {6}=[{10}] {7}=[{10}] {8}=[{13}]",
                    "ExperimentSource", "NumRuns", "GeneratePopulation", "SeedPopulationDirectory",
                    "ExperimentConfigDirectory", "OutputFileDirectory", "LogOrganismStateData", "IsCoevolution",
                    "ExperimentNames", "file|database", "true|false", "# runs", "directory", "experiment,experiment,..."));

            // If we've reached this point, the configuration is indeed invalid
            return false;
        }

        /// <summary>
        ///     Executes all runs of a given experiment using a configuration file as the configuration source and generated flat
        ///     files as the result destination.
        /// </summary>
        /// <param name="experimentConfigurationDirectory">The directory containing the XML experiment configuration file.</param>
        /// <param name="logFileDirectory">The directory into which to write the evolution/evaluation log files.</param>
        /// <param name="experimentName">The name of the experiment to execute.</param>
        /// <param name="numRuns">The number of runs to execute for that experiment.</param>
        /// <param name="seedPopulationFiles">The seed population XML file names.</param>
        /// <param name="writeOrganismStateData">Flag indicating whether organism state data should be written to an output file.</param>
        private static void ExecuteFileBasedExperiment(string experimentConfigurationDirectory, string logFileDirectory,
            string experimentName,
            int numRuns, string[] seedPopulationFiles, bool writeOrganismStateData)
        {
            // Read in the configuration files that match the given experiment name (should only be 1 file)
            string[] experimentConfigurationFiles = Directory.GetFiles(experimentConfigurationDirectory,
                string.Format("{0}.*", experimentName));

            // Make sure there's only one configuration file that matches the experiment name
            // (otherwise, we don't know for sure which configuration to use)
            if (experimentConfigurationFiles.Count() != 1)
            {
                _executionLogger.Error(
                    string.Format(
                        "Experiment configuration ambiguous: expeted a single possible configuration, but found {0} possible configurations",
                        experimentConfigurationFiles.Count()));
                Environment.Exit(0);
            }

            // Instantiate XML reader for configuration file
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(experimentConfigurationFiles[0]);

            // Determine which experiment to execute
            BaseMazeNavigationExperiment experiment = DetermineMazeNavigationExperiment(xmlConfig.DocumentElement);

            // Execute the experiment for the specified number of runs
            for (int runIdx = 0; runIdx < numRuns; runIdx++)
            {
                // Initialize the data loggers for the given run
                IDataLogger evolutionDataLogger =
                    new FileDataLogger(string.Format("{0}\\{1} - Run{2} - Evolution.csv", logFileDirectory,
                        experimentName,
                        runIdx + 1));
                IDataLogger evaluationDataLogger = writeOrganismStateData
                    ? new FileDataLogger(string.Format("{0}\\{1} - Run{2} - Evaluation.csv", logFileDirectory,
                        experimentName,
                        runIdx + 1))
                    : null;

                // Initialize new experiment
                experiment.Initialize(experimentName, xmlConfig.DocumentElement, evolutionDataLogger,
                    evaluationDataLogger);

                _executionLogger.Info(string.Format("Initialized experiment {0}.", experiment.GetType()));

                // This will hold the number of evaluations executed for each "attempt" of the EA within the current run
                ulong curEvaluations = 0;

                // This will hold the number of restarts of the algorithm
                int curRestarts = 0;

                do
                {
                    IGenomeFactory<NeatGenome> genomeFactory;
                    List<NeatGenome> genomeList;

                    // If there were seed population files specified, read them in
                    if (seedPopulationFiles != null)
                    {
                        // Open and load population XML file.
                        using (XmlReader xr = XmlReader.Create(seedPopulationFiles[runIdx]))
                        {
                            genomeList = experiment.LoadPopulation(xr);
                        }

                        // Grab the specified genome factory on the first genome in the list
                        genomeFactory = genomeList[0].GenomeFactory;
                    }

                    // Otherwise, generate the starting population
                    else
                    {
                        // Create a new genome factory
                        genomeFactory = experiment.CreateGenomeFactory();

                        // Generate the initial population
                        genomeList = genomeFactory.CreateGenomeList(experiment.SeedGenomeCount, 0);
                    }

                    _executionLogger.Info(string.Format("Loaded [{0}] genomes as initial population.", genomeList.Count));

                    _executionLogger.Info("Kicking off Experiment initialization/execution");

                    // Kick off the experiment run
                    RunExperiment(genomeFactory, genomeList, experimentName, experiment, numRuns, runIdx, curEvaluations);

                    // Increment the evaluations
                    curEvaluations = _ea.CurrentEvaluations;

                    // Increment the restart count
                    curRestarts++;
                } while (_ea.StopConditionSatisfied == false && experiment.MaxRestarts >= curRestarts);

                // Close the data loggers
                evolutionDataLogger.Close();
                evaluationDataLogger?.Close();
            }
        }

        /// <summary>
        ///     Executes all runs of a given experiment using the database as both the configuration source and the results
        ///     destination.
        /// </summary>
        /// <param name="experimentName">The name of the experiment to execute.</param>
        /// <param name="numRuns">The number of runs to execute for that experiment.</param>
        /// <param name="seedPopulationFiles">The seed population XML file names.</param>
        private static void ExecuteDatabaseBasedExperiment(string experimentName, int numRuns,
            string[] seedPopulationFiles)
        {
            // Create new database context and read in configuration for the given experiment
            ExperimentDataEntities experimentContext = new ExperimentDataEntities();
            var name = experimentName;
            ExperimentDictionary experimentConfiguration =
                experimentContext.ExperimentDictionaries.Single(
                    expName => expName.ExperimentName == name);

            // Determine which experiment to execute
            BaseMazeNavigationExperiment experiment =
                DetermineMazeNavigationExperiment(experimentConfiguration.Primary_SearchAlgorithmName,
                    experimentConfiguration.Primary_SelectionAlgorithmName, false);

            // Execute the experiment for the specified number of runs
            for (int runIdx = 0; runIdx < numRuns; runIdx++)
            {
                // Initialize the experiment
                experiment.Initialize(experimentConfiguration);

                _executionLogger.Error(string.Format("Initialized experiment {0}.", experiment.GetType()));

                // This will hold the number of evaluations executed for each "attempt" of the EA within the current run
                ulong curEvaluations = 0;

                // This will hold the number of restarts of the algorithm
                int curRestarts = 0;

                do
                {
                    List<NeatGenome> genomeList;

                    // Open and load population XML file.
                    using (XmlReader xr = XmlReader.Create(seedPopulationFiles[runIdx]))
                    {
                        genomeList = experiment.LoadPopulation(xr);
                    }
                    IGenomeFactory<NeatGenome> genomeFactory = genomeList[0].GenomeFactory;
                    _executionLogger.Info(string.Format("Loaded [{0}] genomes as initial population.", genomeList.Count));

                    _executionLogger.Info("Kicking off Experiment initialization/execution");

                    // Kick off the experiment run
                    RunExperiment(genomeFactory, genomeList, experimentName, experiment, numRuns, runIdx, curEvaluations);

                    // Increment the evaluations
                    curEvaluations = _ea.CurrentEvaluations;

                    // Increment the restart count
                    curRestarts++;
                } while (_ea.StopConditionSatisfied == false && experiment.MaxRestarts >= curRestarts);
            }

            // Dispose of the database context
            experimentContext.Dispose();
        }

        /// <summary>
        ///     Executes all runs of a given coevolution experiment using a configuration file as the configuration source and
        ///     generated flat files as the result destination.
        /// </summary>
        /// <param name="experimentConfigurationDirectory">The directory containing the XML experiment configuration file.</param>
        /// <param name="logFileDirectory">The directory into which to write the evolution/evaluation log files.</param>
        /// <param name="experimentName">The name of the experiment to execute.</param>
        /// <param name="numRuns">The number of runs to execute for that experiment.</param>
        /// <param name="seedPopulationFiles">The seed population XML file names.</param>
        /// <param name="writeOrganismStateData">Flag indicating whether organism state data should be written to an output file.</param>
        private static void ExecuteFileBasedCoevolutionExperiment(string experimentConfigurationDirectory,
            string logFileDirectory,
            string experimentName,
            int numRuns, string[] seedPopulationFiles, bool writeOrganismStateData)
        {
            // Read in the configuration files that match the given experiment name (should only be 1 file)
            string[] experimentConfigurationFiles = Directory.GetFiles(experimentConfigurationDirectory,
                string.Format("{0}.*", experimentName));

            // Make sure there's only one configuration file that matches the experiment name
            // (otherwise, we don't know for sure which configuration to use)
            if (experimentConfigurationFiles.Count() != 1)
            {
                _executionLogger.Error(
                    string.Format(
                        "Experiment configuration ambiguous: expeted a single possible configuration, but found {0} possible configurations",
                        experimentConfigurationFiles.Count()));
                Environment.Exit(0);
            }

            // Instantiate XML reader for configuration file
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(experimentConfigurationFiles[0]);

            // Determine which experiment to execute
            // TODO: If there are more coevolution experiments added, we'll have to figure out which experiment to run dynamically
            ICoevolutionExperiment experiment = new CoevolutionMazeNavigationMCSExperiment();

            // Execute the experiment for the specified number of runs
            for (int runIdx = 0; runIdx < numRuns; runIdx++)
            {
                // Initialize the data loggers for the given run
                IDataLogger navigatorDataLogger =
                    new FileDataLogger(string.Format("{0}\\{1} - Run{2} - NavigatorEvolution.csv", logFileDirectory,
                        experimentName,
                        runIdx + 1));
                IDataLogger navigatorGenomesLogger =
                    new FileDataLogger(string.Format("{0}\\{1} - Run{2} - NavigatorGenomes.csv", logFileDirectory,
                        experimentName,
                        runIdx + 1));
                IDataLogger mazeDataLogger =
                    new FileDataLogger(string.Format("{0}\\{1} - Run{2} - MazeEvolution.csv", logFileDirectory,
                        experimentName,
                        runIdx + 1));
                IDataLogger mazeGenomesLogger =
                    new FileDataLogger(string.Format("{0}\\{1} - Run{2} - MazeGenomes.csv", logFileDirectory,
                        experimentName,
                        runIdx + 1));

                // Initialize new experiment
                experiment.Initialize(experimentName, xmlConfig.DocumentElement, navigatorDataLogger,
                    navigatorGenomesLogger, mazeDataLogger, mazeGenomesLogger);

                _executionLogger.Info(string.Format("Initialized experiment {0}.", experiment.GetType()));

                // If there were seed population files specified, read them in
                if (Boolean.Parse(_executionConfiguration[ExecutionParameter.GeneratePopulation]) == false)
                {
                    // TODO: Need to implement ability to load maze seed genomes
                    throw new NotImplementedException("Currently unable to read in serialized maze genomes.");
                }

                // Otherwise, generate the starting population
                // Create a new agent genome factory
                var agentGenomeFactory = experiment.CreateAgentGenomeFactory();

                // Create a new maze genome factory
                var mazeGenomeFactory = experiment.CreateMazeGenomeFactory();

                // Generate the initial agent population
                var agentGenomeList = agentGenomeFactory.CreateGenomeList(experiment.AgentSeedGenomeCount, 0);

                // Generate the initial maze population
                var mazeGenomeList = mazeGenomeFactory.CreateGenomeList(experiment.MazeSeedGenomeCount, 0);

                _executionLogger.Info(string.Format("Loaded [{0}] genomes as initial population.",
                    agentGenomeList.Count));

                _executionLogger.Info("Kicking off Experiment initialization/execution");

                // Kick off the experiment run
                RunExperiment(agentGenomeFactory, mazeGenomeFactory, agentGenomeList, mazeGenomeList, experimentName,
                    experiment, numRuns, runIdx);

                // Close the data loggers
                navigatorDataLogger.Close();
                navigatorGenomesLogger.Close();
                mazeDataLogger.Close();
                mazeGenomesLogger.Close();
            }
        }

        /// <summary>
        ///     Executes a single run of the given experiment.
        /// </summary>
        /// <param name="genomeFactory">The factory for producing NEAT genomes.</param>
        /// <param name="genomeList">The list of initial genomes (population).</param>
        /// <param name="experimentName">The name of the experiment to execute.</param>
        /// <param name="experiment">Reference to the initialized experiment.</param>
        /// <param name="numRuns">Total number of runs being executed.</param>
        /// <param name="runIdx">The current run being executed.</param>
        /// <param name="startingEvaluation">
        ///     The number of evaluations from which execution is starting (this is only applicable in
        ///     the event of a restart).
        /// </param>
        private static void RunExperiment(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList,
            string experimentName, BaseMazeNavigationExperiment experiment, int numRuns,
            int runIdx, ulong startingEvaluation)
        {
            // Trap initialization exceptions (which, if applicable, could be due to initialization algorithm not
            // finding a viable seed) and continue to the next run if an exception does occur
            try
            {
                // Create evolution algorithm and attach update event.
                _ea = experiment.CreateEvolutionAlgorithm(genomeFactory, genomeList, startingEvaluation);
                _ea.UpdateEvent += ea_UpdateEvent;
            }
            catch (Exception)
            {
                _executionLogger.Error(string.Format("Experiment {0}, Run {1} of {2} failed to initialize",
                    experimentName,
                    runIdx + 1, numRuns));
                Environment.Exit(0);
            }

            _executionLogger.Info(string.Format(
                "Executing Experiment {0}, Run {1} of {2} from {3} starting evaluations", experimentName, runIdx + 1,
                numRuns, startingEvaluation));

            // Start algorithm (it will run on a background thread).
            _ea.StartContinue();

            while (RunState.Terminated != _ea.RunState && RunState.Paused != _ea.RunState)
            {
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        ///     Executes a single run of the given coevolution experiment.
        /// </summary>
        /// <param name="agentGenomeFactory">The factory for producing NEAT genomes.</param>
        /// <param name="mazeGenomeFactory">The factory for producing maze genomes.</param>
        /// <param name="agentGenomeList">The list of initial agent genomes (navigator population).</param>
        /// <param name="mazeGenomeList">The list of initial maze genomes (maze population).</param>
        /// <param name="experimentName">The name of the coevolution experiment to execute.</param>
        /// <param name="experiment">Reference to the initialized experiment.</param>
        /// <param name="numRuns">Total number of runs being executed.</param>
        /// <param name="runIdx">The current run being executed.</param>
        private static void RunExperiment(IGenomeFactory<NeatGenome> agentGenomeFactory,
            IGenomeFactory<MazeGenome> mazeGenomeFactory, List<NeatGenome> agentGenomeList,
            List<MazeGenome> mazeGenomeList, string experimentName, ICoevolutionExperiment experiment, int numRuns,
            int runIdx)
        {
            try
            {
                // Create evolution algorithm and attach update event.
                _coevolutionEaContainer = experiment.CreateCoevolutionAlgorithmContainer(agentGenomeFactory,
                    mazeGenomeFactory, agentGenomeList,
                    mazeGenomeList);
                _coevolutionEaContainer.UpdateEvent += coevolutionContainer_UpdateEvent;
            }
            catch (Exception)
            {
                _executionLogger.Error(string.Format("Experiment {0}, Run {1} of {2} failed to initialize",
                    experimentName,
                    runIdx + 1, numRuns));
                Environment.Exit(0);
            }

            _executionLogger.Info(string.Format("Executing Experiment {0}, Run {1} of {2}", experimentName, runIdx + 1,
                numRuns));

            // Start algorithm (it will run on a background thread).
            _coevolutionEaContainer.StartContinue();

            while (RunState.Terminated != _coevolutionEaContainer.RunState &&
                   RunState.Paused != _coevolutionEaContainer.RunState)
            {
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        ///     Evolutionary algorithm update delegate - logs current state of algorithm.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private static void ea_UpdateEvent(object sender, EventArgs e)
        {
            if (_ea.CurrentChampGenome != null)
            {
                double champGenomeAuxFitness = _ea.CurrentChampGenome.EvaluationInfo.AuxFitnessArr.Length > 0
                    ? _ea.CurrentChampGenome.EvaluationInfo.AuxFitnessArr[0]._value
                    : 0;

                if (champGenomeAuxFitness > 0)
                {
                    _executionLogger.Info(
                        string.Format("Generation={0:N0} Evaluations={1:N0} BestFitness={2:N6} BestAuxFitness={3:N6}",
                            _ea.CurrentGeneration,
                            _ea.CurrentEvaluations, _ea.CurrentChampGenome.EvaluationInfo.Fitness, champGenomeAuxFitness));
                }
                else
                {
                    _executionLogger.Info(string.Format("Generation={0:N0} Evaluations={1:N0} BestFitness={2:N6}",
                        _ea.CurrentGeneration,
                        _ea.CurrentEvaluations, _ea.CurrentChampGenome.EvaluationInfo.Fitness));
                }
            }
            else
            {
                _executionLogger.Info(string.Format("Generation={0:N0} Evaluations={1:N0} BestFitness={2:N6}",
                    _ea.CurrentGeneration,
                    _ea.CurrentEvaluations, _ea.Statistics._maxFitness));
            }
        }

        private static void coevolutionContainer_UpdateEvent(object sender, EventArgs e)
        {
            if (_coevolutionEaContainer.Population1CurrentChampGenome != null &&
                _coevolutionEaContainer.Population2CurrentChampGenome != null)
            {
                _executionLogger.Info(
                    string.Format(
                        "Generation={0:N0} Evaluations={1:N0} Population1BestFitness={2:N2} Population1MaxComplexity={3:N2} Population2BestFitness={4:N2}, Population2MaxComplexity={5:N2}",
                        _coevolutionEaContainer.CurrentGeneration, _coevolutionEaContainer.CurrentEvaluations,
                        _coevolutionEaContainer.Population1CurrentChampGenome.EvaluationInfo.Fitness,
                        _coevolutionEaContainer.Population1Statistics._maxComplexity,
                        _coevolutionEaContainer.Population2CurrentChampGenome.EvaluationInfo.Fitness,
                        _coevolutionEaContainer.Population2Statistics._maxComplexity));
            }
        }

        /// <summary>
        ///     Determine the maze navigation experiment to run based on the search and selection algorithms specified in the
        ///     configuration file.
        /// </summary>
        /// <param name="xmlConfig">The reference to the root node of the XML configuration file.</param>
        /// <returns>The appropriate maze navigation experiment class.</returns>
        private static BaseMazeNavigationExperiment DetermineMazeNavigationExperiment(XmlElement xmlConfig)
        {
            // Get the search and selection algorithm types
            string searchAlgorithm = XmlUtils.TryGetValueAsString(xmlConfig, "SearchAlgorithm");
            string selectionAlgorithm = XmlUtils.TryGetValueAsString(xmlConfig, "SelectionAlgorithm");
            bool isDynamicMC = XmlUtils.TryGetValueAsInt(xmlConfig, "MinimalCriteriaUpdateInterval") != null;

            // Make sure both the search algorithm and selection algorithm have been set in the configuration file
            if (searchAlgorithm == null || selectionAlgorithm == null)
            {
                _executionLogger.Error(
                    "Both the search algorithm and selection algorithm must be specified in the XML configuration file.");
                Environment.Exit(0);
            }

            // Get the appropriate experiment class
            return DetermineMazeNavigationExperiment(searchAlgorithm, selectionAlgorithm, isDynamicMC);
        }

        /// <summary>
        ///     Determine the maze navigation experiment to run based on the given search and selection algorithms.
        /// </summary>
        /// <param name="searchAlgorithmName">The search algorithm to run.</param>
        /// <param name="selectionAlgorithmName">The selection algorithm to run.</param>
        /// <returns></returns>
        private static BaseMazeNavigationExperiment DetermineMazeNavigationExperiment(string searchAlgorithmName,
            string selectionAlgorithmName, bool isDynamicMC)
        {
            // Extract the corresponding search and selection algorithm domain types
            SearchType searchType = AlgorithmTypeUtil.ConvertStringToSearchType(searchAlgorithmName);
            SelectionType selectionType = AlgorithmTypeUtil.ConvertStringToSelectionType(selectionAlgorithmName);

            // Match up with the correct experiment
            switch (searchType)
            {
                // Fitness experiment
                case SearchType.Fitness:
                    return new MazeNavigationFitnessExperiment();

                // Novelty search experiment (generational or steady state)
                case SearchType.NoveltySearch:
                    if (SelectionType.Generational.Equals(selectionType))
                    {
                        return new GenerationalMazeNavigationNoveltyExperiment();
                    }
                    return new SteadyStateMazeNavigationNoveltyExperiment();

                // Minimal criteria novelty search experiment (steady-state only)
                case SearchType.MinimalCriteriaNoveltySearch:
                    return new SteadyStateMazeNavigationMCNSExperiment();

                // Minimal criteria search experiment (steady state or queueing)
                case SearchType.MinimalCriteriaSearch:
                    if (SelectionType.SteadyState.Equals(selectionType))
                    {
                        return new SteadyStateMazeNavigationMCSExperiment();
                    }
                    if (SelectionType.QueueingWithNiching.Equals(selectionType))
                    {
                        return new QueueingNichedMazeNavigationMCSExperiment();
                    }
                    if (SelectionType.Queueing.Equals(selectionType) && isDynamicMC)
                    {
                        return new QueueingMazeNavigationDynamicMCSExperiment();
                    }
                    return new QueueingMazeNavigationMCSExperiment();

                // If none of the above were matched, return the steady state experiment with
                // randomly assigned fitness
                default:
                    return new SteadyStateMazeNavigationRandomExperiment();
            }
        }
    }
}