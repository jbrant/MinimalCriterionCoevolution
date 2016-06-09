#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ExperimentEntities;
using log4net;
using log4net.Config;
using MazeExperimentSuppotLib;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace NavigatorMazeMapEvaluator
{
    internal class NavigatorMazeMapEvaluatorExecutor
    {
        /// <summary>
        ///     This is the number of records that are written to the database in one pass.
        /// </summary>
        private const int CommitPageSize = 1000;

        private static readonly Dictionary<ExecutionParameter, String> _executionConfiguration =
            new Dictionary<ExecutionParameter, string>();

        private static ILog _executionLogger;

        private static void Main(string[] args)
        {
            string baseImageOutputDirectory = null;

            // Initialise log4net (log to console and file).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            // Extract the execution parameters and check for any errors (exit application if any found)
            if (ParseAndValidateConfiguration(args) == false)
                Environment.Exit(0);

            _executionLogger.Info("Invocation parameters validated - continuing with experiment execution.");

            // Get input and output neurons counts for navigator agent
            int inputNeuronCount = Int32.Parse(_executionConfiguration[ExecutionParameter.AgentNeuronInputCount]);
            int outputNeuronCount = Int32.Parse(_executionConfiguration[ExecutionParameter.AgentNeuronOutputCount]);

            // Get boolean indicators dictating whether to write simulation results to database and/or generate bitmaps of agent trajectories
            bool writeResultsToDatabase =
                !_executionConfiguration.ContainsKey(ExecutionParameter.WriteResultsToDatabase) ||
                Boolean.Parse(_executionConfiguration[ExecutionParameter.WriteResultsToDatabase]);
            bool generateTrajectoryBitmaps =
                !_executionConfiguration.ContainsKey(ExecutionParameter.GenerateAgentTrajectoryBitmaps) ||
                Boolean.Parse(_executionConfiguration[ExecutionParameter.GenerateAgentTrajectoryBitmaps]);

            // If bitmap generation was enabled, grab the base output directory
            if (generateTrajectoryBitmaps)
            {
                baseImageOutputDirectory = _executionConfiguration[ExecutionParameter.BitmapOutputBaseDirectory];
            }

            // Extract the experiment names
            string[] experimentNames = _executionConfiguration[ExecutionParameter.ExperimentNames].Split(',');

            _executionLogger.Info(string.Format("[{0}] experiments specified for analysis.", experimentNames.Count()));

            // Process each experiment
            foreach (string experimentName in experimentNames)
            {
                // Get the run from which to start execution (if specified)
                int startingRun = _executionConfiguration.ContainsKey(ExecutionParameter.StartFromRun)
                    ? Int32.Parse(_executionConfiguration[ExecutionParameter.StartFromRun])
                    : 1;

                // Lookup the current experiment configuration
                ExperimentDictionary curExperimentConfiguration =
                    ExperimentDataHandler.LookupExperimentConfiguration(experimentName);

                // Ensure that experiment configuration was found
                if (curExperimentConfiguration == null)
                {
                    _executionLogger.Error(
                        string.Format("Unable to lookup experiment configuration for experiment with name [{0}]",
                            experimentName));
                    Environment.Exit(0);
                }

                // Construct the experiment parameters
                ExperimentParameters experimentParameters =
                    new ExperimentParameters(curExperimentConfiguration.MaxTimesteps,
                        curExperimentConfiguration.MinSuccessDistance,
                        curExperimentConfiguration.Primary_Maze_MazeHeight,
                        curExperimentConfiguration.Primary_Maze_MazeWidth,
                        curExperimentConfiguration.Primary_Maze_MazeScaleMultiplier);

                // Get the number of runs in the experiment
                int numRuns = ExperimentDataHandler.GetNumRuns(curExperimentConfiguration.ExperimentDictionaryID);

                _executionLogger.Info(string.Format("Preparing to execute analysis for [{0}] runs of experiment [{1}]",
                    numRuns,
                    curExperimentConfiguration.ExperimentName));

                // Get the number of batches (generations) in the experiment
                for (int curRun = startingRun; curRun <= numRuns; curRun++)
                {
                    // If we're not writing to the database, open the file writer
                    if (writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.OpenFileWriter(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                string.Format("{0} - Run{1}.csv", experimentName, curRun)));
                    }

                    // Get the number of initialization batches in the current run
                    IList<int> initializationBatchesWithGenomeData =
                        ExperimentDataHandler.GetBatchesWithGenomeData(
                            curExperimentConfiguration.ExperimentDictionaryID, curRun, RunPhase.Initialization);

                    // If there was an initialization phase, analyze those results
                    if (initializationBatchesWithGenomeData.Count > 0)
                    {
                        _executionLogger.Info(
                            string.Format(
                                "Executing initialization phase analysis for run [{0}/{1}] with [{2}] batches",
                                curRun,
                                numRuns, initializationBatchesWithGenomeData.Count));

                        // Begin initialization phase results processing
                        ProcessAndLogPerBatchResults(initializationBatchesWithGenomeData, RunPhase.Initialization,
                            experimentParameters,
                            inputNeuronCount, outputNeuronCount, curRun, numRuns, curExperimentConfiguration,
                            writeResultsToDatabase, generateTrajectoryBitmaps, baseImageOutputDirectory);
                    }

                    // Get the number of primary batches in the current run
                    IList<int> batchesWithGenomeData =
                        ExperimentDataHandler.GetBatchesWithGenomeData(
                            curExperimentConfiguration.ExperimentDictionaryID, curRun, RunPhase.Primary);

                    _executionLogger.Info(
                        string.Format("Executing primary phase analysis for run [{0}/{1}] with [{2}] batches",
                            curRun,
                            numRuns, batchesWithGenomeData.Count));

                    // Begin primary phase results processing
                    ProcessAndLogPerBatchResults(batchesWithGenomeData, RunPhase.Primary, experimentParameters,
                        inputNeuronCount,
                        outputNeuronCount, curRun, numRuns, curExperimentConfiguration, writeResultsToDatabase,
                        generateTrajectoryBitmaps, baseImageOutputDirectory);

                    // If we're not writing to the database, close the file writer since the run is over
                    if (writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.CloseFileWriter();
                    }
                }

                // Write a sentinel file to indicate analysis completion
                if (writeResultsToDatabase == false)
                    ExperimentDataHandler.WriteSentinelFile(
                        Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory], experimentName));
            }
        }

        /// <summary>
        ///     Runs post-hoc analysis for all batches in the given experiment/run.  This can be part of either the initialization
        ///     or primary run phase.
        /// </summary>
        /// <param name="batchesWithGenomeData">The total number of batches containing genome data.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary run phase.
        /// </param>
        /// <param name="experimentParameters">Experiment configuration parameters.</param>
        /// <param name="inputNeuronCount">Count of neurons in controller input layer.</param>
        /// <param name="outputNeuronCount">Count of neurons in controller output layer.</param>
        /// <param name="curRun">The run number.</param>
        /// <param name="numRuns">The total number of runs.</param>
        /// <param name="curExperimentConfiguration">The experiment configuration parameters.</param>
        /// <param name="writeResultsToDatabase">
        ///     Indicates whether to write results directly into a database (if not, results are
        ///     written to a flat file).
        /// </param>
        /// <param name="generateTrajectoryBitmaps">
        ///     Indicates whether bitmap files depicting the navigator trajectory should be
        ///     written out.
        /// </param>
        /// <param name="baseImageOutputDirectory">The path to the output directory for the trajectory images.</param>
        private static void ProcessAndLogPerBatchResults(IList<int> batchesWithGenomeData, RunPhase runPhase,
            ExperimentParameters experimentParameters, int inputNeuronCount, int outputNeuronCount, int curRun,
            int numRuns, ExperimentDictionary curExperimentConfiguration, bool writeResultsToDatabase,
            bool generateTrajectoryBitmaps, string baseImageOutputDirectory)
        {
            IList<CoevolutionMCSMazeExperimentGenome> staticInitializationMazes = null;

            // If this invocation is processing initialization results, just get the maze up front as it will remain
            // the same throughout the initialization process
            if (runPhase == RunPhase.Initialization)
            {
                staticInitializationMazes =
                    ExperimentDataHandler.GetMazeGenomeData(curExperimentConfiguration.ExperimentDictionaryID,
                        curRun, 0);
            }

            // Iterate through each batch and evaluate maze/navigator combinations
            foreach (int curBatch in batchesWithGenomeData)
            {
                // Create the maze/navigator map
                MapEvaluator mapEvaluator =
                    new MapEvaluator(experimentParameters, inputNeuronCount, outputNeuronCount);

                _executionLogger.Info(string.Format("Executing {0} run phase analysis for batch [{1}] of run [{2}/{3}]",
                    runPhase, curBatch, curRun, numRuns));

                // Initialize the maze/navigator map with the serialized maze and navigator data (this does the parsing)
                mapEvaluator.Initialize(
                    runPhase == RunPhase.Initialization
                        ? staticInitializationMazes
                        : ExperimentDataHandler.GetMazeGenomeData(curExperimentConfiguration.ExperimentDictionaryID,
                            curRun, curBatch), ExperimentDataHandler.GetNavigatorGenomeData(
                                curExperimentConfiguration.ExperimentDictionaryID, curRun, curBatch, runPhase));

                // Evaluate all of the maze/navigator combinations in the batch
                mapEvaluator.RunBatchEvaluation();

                // Save the evaluation results
                ExperimentDataHandler.WriteNavigatorMazeEvaluationData(
                    curExperimentConfiguration.ExperimentDictionaryID, curRun, curBatch, runPhase,
                    mapEvaluator.EvaluationUnits, CommitPageSize, writeResultsToDatabase);

                if (generateTrajectoryBitmaps)
                {
                    // Generate bitmaps of trajectory for all successful trials
                    ImageGenerationHandler.GenerateBitmapsForSuccessfulTrials(
                        baseImageOutputDirectory, curExperimentConfiguration.ExperimentName,
                        curExperimentConfiguration.ExperimentDictionaryID,
                        curRun, curBatch, mapEvaluator.EvaluationUnits, runPhase);
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
                        // Ensure valid agent input/output neuron counts were specified
                        case ExecutionParameter.AgentNeuronInputCount:
                        case ExecutionParameter.AgentNeuronOutputCount:
                        case ExecutionParameter.StartFromRun:
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
                        case ExecutionParameter.WriteResultsToDatabase:
                        case ExecutionParameter.GenerateAgentTrajectoryBitmaps:
                            bool testBool;
                            if (Boolean.TryParse(parameterValuePair[1], out testBool) == false)
                            {
                                _executionLogger.Error(string.Format("The value for parameter [{0}] must be a boolean.",
                                    curParameter));
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
                // Check for existence of experiment names to execute
                if (_executionConfiguration.ContainsKey(ExecutionParameter.ExperimentNames) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.ExperimentNames));
                    isConfigurationValid = false;
                }

                // Check for existence of input neuron count
                if (_executionConfiguration.ContainsKey(ExecutionParameter.AgentNeuronInputCount) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.AgentNeuronInputCount));
                    isConfigurationValid = false;
                }

                // Check for existence of output neuron count
                if (_executionConfiguration.ContainsKey(ExecutionParameter.AgentNeuronOutputCount) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.AgentNeuronOutputCount));
                    isConfigurationValid = false;
                }

                // If we're logging to a flat file instead of the database, the output directory must be set
                if ((_executionConfiguration.ContainsKey(ExecutionParameter.WriteResultsToDatabase) &&
                     Convert.ToBoolean(_executionConfiguration[ExecutionParameter.WriteResultsToDatabase]) == false) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.DataFileOutputDirectory) == false)
                {
                    _executionLogger.Error(
                        "The data file output directory must be specified when writing results to a flat file instead of the database.");
                    isConfigurationValid = false;
                }

                // If the executor is going to produce bitmap images, then the base output directory must be specified
                if ((_executionConfiguration.ContainsKey(ExecutionParameter.GenerateAgentTrajectoryBitmaps) == false ||
                     Convert.ToBoolean(_executionConfiguration[ExecutionParameter.GenerateAgentTrajectoryBitmaps])) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.BitmapOutputBaseDirectory) == false)
                {
                    _executionLogger.Error(
                        "The bitmap image base directory must be specified when producing navigator trajectory images.");
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            _executionLogger.Error("The experiment executor invocation must take the following form:");
            _executionLogger.Error(
                string.Format(
                    "NavigatorMazeMapEvaluator.exe {0}=[{7}] {1}=[{8}] (Optional: {2}=[{9}]) (Optional: {3}=[{10}]) (Optional: {4}=[{9}] {5}=[{10}]) {6}=[{11}]",
                    ExecutionParameter.AgentNeuronInputCount, ExecutionParameter.AgentNeuronOutputCount,
                    ExecutionParameter.WriteResultsToDatabase, ExecutionParameter.DataFileOutputDirectory,
                    ExecutionParameter.GenerateAgentTrajectoryBitmaps,
                    ExecutionParameter.BitmapOutputBaseDirectory, ExecutionParameter.ExperimentNames, "# Input Neurons",
                    "# Output Neurons", "true|false", "directory", "experiment,experiment,..."));

            return false;
        }
    }
}