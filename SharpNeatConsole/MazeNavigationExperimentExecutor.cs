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
using SharpNeat.Domains.MazeNavigation.FitnessExperiment;
using SharpNeat.Domains.MazeNavigation.MCNSExperiment;
using SharpNeat.Domains.MazeNavigation.MCSExperiment;
using SharpNeat.Domains.MazeNavigation.NoveltyExperiment;
using SharpNeat.Domains.MazeNavigation.RandomExperiment;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;

#endregion

namespace SharpNeatConsole
{
    public class MazeNavigationExperimentExecutor
    {
        private static IGenomeFactory<NeatGenome> _genomeFactory;
        private static List<NeatGenome> _genomeList;
        private static INeatEvolutionAlgorithm<NeatGenome> _ea;
        private static ILog _executionLogger;

        private static void Main(string[] args)
        {
            // Initialise log4net (log to console and file).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            if (args == null || args.Length < 5)
            {
                _executionLogger.Error(
                    "The following invocations are supported for a file source/destination:");
                _executionLogger.Error(
                    "SharpNeatConsole.exe <Experiment source (file)> <seed population directory> <# of runs> <experiment config directory> <output file directory> <enable organism state logging> <experiment names>");
                _executionLogger.Error(
                    "SharpNeatConsole.exe <Experiment source (file)> generate_population <# of runs> <experiment config directory> <output file directory> <enable organism state logging> <experiment names>");
                _executionLogger.Error("The following invocation is required for a database source/destination:");
                _executionLogger.Error(
                    "SharpNeatConsole.exe <Experiment source (database)> <seed population directory> <# of runs> <enable organism state logging> <experiment names>");
                Environment.Exit(0);
            }

            if ("file".Equals(args[0].ToLowerInvariant()) == false &&
                "database".Equals(args[0].ToLowerInvariant()) == false)
            {
                _executionLogger.Error("Argument 1 is the source type, which should be either \"File\" or \"Database\"");
                Environment.Exit(0);
            }

            // Set the execution source (file or database)
            string executionSource = args[0].ToLowerInvariant();

            // Read seed populuation file and number of runs
            string seedPopulationFileDirectory = args[1];
            int numRuns = Int32.Parse(args[2]);

            // Read experiment configuration directory and log file output directory (only applicable if file-based)
            string experimentConfigurationDirectory = "file".Equals(executionSource) ? args[3] : null;
            string outputFileDirectory = "file".Equals(executionSource) ? args[4] : null;

            // Read flag indicating whether or not to output organism state data
            bool writeOrganismStateData = args[5] == "true";

            // Make sure that the experiment configuration directory actually exists (if one is being used)
            if (seedPopulationFileDirectory.Equals("generate_population") == false &&
                Directory.Exists(seedPopulationFileDirectory) == false)
            {
                _executionLogger.Error(string.Format("The given seed population file directory [{0}] does not exist",
                    seedPopulationFileDirectory));
                Environment.Exit(0);
            }

            // Array of initial genome population files
            string[] seedPopulationFiles = null;

            if (seedPopulationFileDirectory.Equals("generate_population") == false)
            {
                // Read in the seed population files
                // Note that two assumptions are made here
                // 1. Files can be naturally sorted and match the naming convention "*Run01", "*Run02", etc.
                // 2. The number of files in the directory matches the number of runs (this is checked for below)
                seedPopulationFiles = Directory.GetFiles(seedPopulationFileDirectory);

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

            // The default offset for the experiment names in the argument list
            int experimentNameOffset = 4;

            if ("file".Equals(executionSource))
            {
                // Offset by 4 to make room for experiment configuration directory parameter
                experimentNameOffset = 6;
            }

            // Create experiment names array with the defined size
            string[] experimentNames = new string[args.Length - experimentNameOffset];

            // Read all experiments
            for (int cnt = 0; cnt < experimentNames.Length; cnt++)
            {
                experimentNames[cnt] = args[cnt + experimentNameOffset];
            }

            foreach (string curExperimentName in experimentNames)
            {
                _executionLogger.Info(string.Format("Executing Experiment {0}", curExperimentName));

                if ("file".Equals(executionSource))
                {
                    // Execute file-based experiment
                    ExecuteFileBasedExperiment(experimentConfigurationDirectory, outputFileDirectory, curExperimentName,
                        numRuns, seedPopulationFiles, writeOrganismStateData);
                }
                else
                {
                    // Execute database-based experiment
                    ExecuteDatabaseBasedExperiment(curExperimentName, numRuns, seedPopulationFiles);
                }
            }
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
            if (Directory.Exists(experimentConfigurationDirectory) == false)
            {
                _executionLogger.Error(string.Format(
                    "The given experiment configuration directory [{0}] does not exist",
                    experimentConfigurationDirectory));
                Environment.Exit(0);
            }

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

                // Initialize new steady state novelty experiment
                experiment.Initialize(experimentName, xmlConfig.DocumentElement, evolutionDataLogger,
                    evaluationDataLogger);

                _executionLogger.Info(string.Format("Initialized experiment {0}.", experiment.GetType()));

                // This will hold the number of evaluations executed for each "attempt" of the EA within the current run
                ulong curEvaluations = 0;

                // This will hold the number of restarts of the algorithm
                int curRestarts = 0;

                do
                {
                    // If there were seed population files specified, read them in
                    if (seedPopulationFiles != null)
                    {
                        // Open and load population XML file.
                        using (XmlReader xr = XmlReader.Create(seedPopulationFiles[runIdx]))
                        {
                            _genomeList = experiment.LoadPopulation(xr);
                        }

                        // Grab the specified genome factory on the first genome in the list
                        _genomeFactory = _genomeList[0].GenomeFactory;
                    }

                    // Otherwise, generate the starting population
                    else
                    {
                        // Create a new genome factory
                        _genomeFactory = experiment.CreateGenomeFactory();

                        // Generate the initial population
                        _genomeList = _genomeFactory.CreateGenomeList(experiment.SeedGenomeCount, 0);
                    }

                    _executionLogger.Info(string.Format("Loaded [{0}] genomes as initial population.", _genomeList.Count));

                    _executionLogger.Info("Kicking off Experiment initialization/execution");

                    // Kick off the experiment run
                    RunExperiment(experimentName, experiment, numRuns, runIdx, curEvaluations);

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
                    // Open and load population XML file.
                    using (XmlReader xr = XmlReader.Create(seedPopulationFiles[runIdx]))
                    {
                        _genomeList = experiment.LoadPopulation(xr);
                    }
                    _genomeFactory = _genomeList[0].GenomeFactory;
                    _executionLogger.Info(string.Format("Loaded [{0}] genomes as initial population.", _genomeList.Count));

                    _executionLogger.Info("Kicking off Experiment initialization/execution");

                    // Kick off the experiment run
                    RunExperiment(experimentName, experiment, numRuns, runIdx, curEvaluations);

                    // Increment the evaluations
                    curEvaluations = _ea.CurrentEvaluations;

                    // Increment the restart count
                    curRestarts++;
                } while (_ea.StopConditionSatisfied == false && experiment.MaxRestarts >= curRestarts);
            }

            // Dispose of the database context
            experimentContext.Dispose();
        }

        private static void RunExperiment(string experimentName, BaseMazeNavigationExperiment experiment, int numRuns,
            int runIdx, ulong startingEvaluation)
        {
            // Trap initialization exceptions (which, if applicable, could be due to initialization algorithm not
            // finding a viable seed) and continue to the next run if an exception does occur
            try
            {
                // Create evolution algorithm and attach update event.
                _ea = experiment.CreateEvolutionAlgorithm(_genomeFactory, _genomeList, startingEvaluation);
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