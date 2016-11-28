#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using ExperimentEntities;
using log4net;
using log4net.Config;
using MazeExperimentSuppotLib;
using SharpNeat.Core;
using SharpNeat.Decoders.Maze;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Mazes;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace CoevolutionAlgorithmComparator
{
    /// <summary>
    ///     Handles execution of comparison between coevolution and reference algorithm for one or more coevolution experiment
    ///     configurations.
    /// </summary>
    internal class CoevolutionAlgorithmComparatorExecutor
    {
        /// <summary>
        ///     Map of execution parameters.
        /// </summary>
        private static readonly Dictionary<ExecutionParameter, String> ExecutionConfiguration =
            new Dictionary<ExecutionParameter, string>();

        /// <summary>
        ///     Execution file logger.
        /// </summary>
        private static ILog _executionLogger;

        private static void Main(string[] args)
        {
            // Initialise log4net (log to console and file).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            // Extract the execution parameters and check for any errors (exit application if any found)
            if (ParseAndValidateConfiguration(args) == false)
                Environment.Exit(0);

            _executionLogger.Info("Invocation parameters validated - continuing with experiment execution.");

            // Get input and output neurons counts for navigator agent
            int inputNeuronCount = Int32.Parse(ExecutionConfiguration[ExecutionParameter.AgentNeuronInputCount]);
            int outputNeuronCount = Int32.Parse(ExecutionConfiguration[ExecutionParameter.AgentNeuronOutputCount]);

            // Get the reference experiment name
            string referenceExperimentName = ExecutionConfiguration[ExecutionParameter.ReferenceExperimentName];

            // Extract the experiment names
            string[] coEvoExperimentNames = ExecutionConfiguration[ExecutionParameter.ExperimentNames].Split(',');

            _executionLogger.Info(
                string.Format(
                    "Baseline experiment [{0}] specified for comparison against {1} coevolution experiments.",
                    referenceExperimentName, coEvoExperimentNames.Length));

            // Process each experiment
            foreach (string coEvoExperimentName in coEvoExperimentNames)
            {
                // Get the run from which to start execution (if specified)
                int startingRun = ExecutionConfiguration.ContainsKey(ExecutionParameter.StartFromRun)
                    ? Int32.Parse(ExecutionConfiguration[ExecutionParameter.StartFromRun])
                    : 1;

                // Lookup the reference experiment configuration
                ExperimentDictionary referenceExperimentConfiguration =
                    ExperimentDataHandler.LookupExperimentConfiguration(referenceExperimentName);

                // Ensure that reference experiment configuration was found
                if (referenceExperimentConfiguration == null)
                {
                    _executionLogger.Error(
                        string.Format("Unable to lookup reference experiment configuration with name [{0}]",
                            referenceExperimentName));
                    Environment.Exit(0);
                }

                // Lookup current coevolution experiment configuration
                // Lookup the current experiment configuration
                ExperimentDictionary curCoEvoExperimentConfiguration =
                    ExperimentDataHandler.LookupExperimentConfiguration(coEvoExperimentName);

                // Ensure that coevolution experiment configuration was found
                if (curCoEvoExperimentConfiguration == null)
                {
                    _executionLogger.Error(
                        string.Format("Unable to lookup coevolution experiment configuration with name [{0}]",
                            coEvoExperimentName));
                    Environment.Exit(0);
                }

                // Ensure that the experiment contains the necessary maze structure configuration parameters
                Debug.Assert(curCoEvoExperimentConfiguration.Primary_Maze_MazeHeight != null,
                    "Maze Height configuration parameter cannot be null.");
                Debug.Assert(curCoEvoExperimentConfiguration.Primary_Maze_MazeWidth != null,
                    "Maze Width configuration parameter cannot be null.");
                Debug.Assert(curCoEvoExperimentConfiguration.Primary_Maze_MazeScaleMultiplier != null,
                    "Maze Scale Multiplier configuration parameter cannot be null.");

                // Instantiate maze decoder
                MazeDecoder mazeDecoder =
                    new MazeDecoder((int) curCoEvoExperimentConfiguration.Primary_Maze_MazeScaleMultiplier);

                // Get the number of runs in the coevolution experiment
                int numRuns = ExperimentDataHandler.GetNumRuns(curCoEvoExperimentConfiguration.ExperimentDictionaryID);

                _executionLogger.Info(
                    string.Format(
                        "Preparing to execute comparative analysis for [{0}] runs of coevolution experiment [{1}] against reference experiment [{2}]",
                        numRuns,
                        curCoEvoExperimentConfiguration.ExperimentName, referenceExperimentConfiguration.ExperimentName));

                // Get the number of batches (generations) in the experiment
                for (int curRun = startingRun; curRun <= numRuns; curRun++)
                {
                    // Open the file writer
                    ExperimentDataHandler.OpenFileWriter(
                        Path.Combine(ExecutionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                            string.Format("{0} - Run{1}.csv", coEvoExperimentName, curRun)),
                        OutputFileType.NoveltySearchComparisonData);

                    // Get the number of primary batches in the current run
                    IList<int> batchesWithGenomeData =
                        ExperimentDataHandler.GetBatchesWithGenomeData(
                            curCoEvoExperimentConfiguration.ExperimentDictionaryID, curRun, RunPhase.Primary);

                    _executionLogger.Info(
                        string.Format("Executing comparative analysis for run [{0}/{1}] with [{2}] batches",
                            curRun,
                            numRuns, batchesWithGenomeData.Count));

                    // Begin comparative analysis
                    RunPerBatchComparativeAnalysis(batchesWithGenomeData,
                        curCoEvoExperimentConfiguration.ExperimentDictionaryID,
                        curCoEvoExperimentConfiguration.ExperimentName, curRun, numRuns, mazeDecoder,
                        referenceExperimentConfiguration);

                    // Close the file writer after the comparative analysis for the current run is complete
                    ExperimentDataHandler.CloseFileWriter(OutputFileType.NoveltySearchComparisonData);
                }
            }
        }

        /// <summary>
        ///     Handles per-batch comparison of coevolution-generated mazes on a reference algorithm (e.g. novelty search).
        /// </summary>
        /// <param name="batchesWithGenomeData">List of the batches that contain maze genome data.</param>
        /// <param name="curCoevoExperimentId">Identifier of the coevolution experiment.</param>
        /// <param name="curCoevoExperimentName">Name of the coevolution experiment.</param>
        /// <param name="curRun">The current run.</param>
        /// <param name="numRuns">The total number of runs in the experiment.</param>
        /// <param name="mazeDecoder">Reference to the maze genome decoder.</param>
        /// <param name="noveltySearchExperimentConfig">The experiment configuration for the reference (novelty search) experiment.</param>
        private static void RunPerBatchComparativeAnalysis(IList<int> batchesWithGenomeData,
            int curCoevoExperimentId, string curCoevoExperimentName, int curRun, int numRuns, MazeDecoder mazeDecoder,
            ExperimentDictionary noveltySearchExperimentConfig)
        {
            // Declare list to hold maze IDs that have already been evaluated using the comparison algorithm
            List<int> evaluatedMazeGenomeIds = new List<int>();

            // Get the total number of initialization evaluations for the current run
            int coEvoInitEvaluations = ExperimentDataHandler.GetInitializationEvaluationsForRun(curCoevoExperimentId,
                curRun);

            // Iterate through each batch and run comparative algorithm on each maze
            foreach (int curBatch in batchesWithGenomeData)
            {
                _executionLogger.Info(string.Format("Executing comparative analysis for batch [{0}] of run [{1}/{2}]",
                    curBatch, curRun, numRuns));

                // Get list of all extant maze genomes for this batch
                IList<string> mazeGenomeXml = ExperimentDataHandler.GetMazeGenomeXml(curCoevoExperimentId, curRun,
                    curBatch);

                foreach (string genomeXml in mazeGenomeXml)
                {
                    MazeStructure curMazeStructure = null;
                    MazeGenome curMazeGenome = null;

                    // Create a new (mostly dummy) maze genome factory
                    MazeGenomeFactory curMazeGenomeFactory = new MazeGenomeFactory();

                    // Convert each genome string to an equivalent genome object
                    using (XmlReader xr = XmlReader.Create(new StringReader(genomeXml)))
                    {
                        curMazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xr, curMazeGenomeFactory);
                    }

                    // If the maze has already been processed, continue on to the next one
                    // (also exclude the initialization maze as a special case)
                    if (evaluatedMazeGenomeIds.Contains((int) curMazeGenome.BirthGeneration) ||
                        curMazeGenome.BirthGeneration == 0)
                    {
                        continue;
                    }

                    // Get the total number of evaluations required to solve this maze
                    // (this amounts to the total number of evaluations from both queues at the time of maze birth)
                    int coEvoTotalEvaluations = coEvoInitEvaluations +
                                                ExperimentDataHandler.GetTotalPrimaryEvaluationsAtBatch(
                                                    curCoevoExperimentId, curRun, (int) curMazeGenome.BirthGeneration);

                    // Decode the maze structure
                    curMazeStructure =
                        mazeDecoder.Decode(curMazeGenome);

                    // Instantiate new novelty search experiment for the current experiment/batch/maze
                    NoveltySearchRunner nsRunner = new NoveltySearchRunner(noveltySearchExperimentConfig,
                        curCoevoExperimentName, curMazeStructure, curRun, numRuns, _executionLogger);

                    _executionLogger.Info(string.Format(
                        "Executing novelty search on maze [{0}] with birth batch [{1}]", curMazeGenome.Id,
                        curMazeGenome.BirthGeneration));

                    // Run novelty search on that maze until the maze is solved or the max evals have been reached
                    INeatEvolutionAlgorithm<NeatGenome> eaFinalState = nsRunner.RunNoveltySearch();

                    // Write comparison results to output file
                    ExperimentDataHandler.WriteNoveltySearchComparisonResults(curCoevoExperimentId,
                        noveltySearchExperimentConfig.ExperimentDictionaryID, curRun, curBatch,
                        (int) curMazeGenome.Id, (int) curMazeGenome.BirthGeneration,
                        (int) eaFinalState.Statistics._minComplexity,
                        (int) eaFinalState.Statistics._maxComplexity, eaFinalState.Statistics._meanComplexity,
                        coEvoTotalEvaluations, (int) eaFinalState.CurrentEvaluations,
                        !((int) eaFinalState.CurrentEvaluations >= noveltySearchExperimentConfig.MaxEvaluations), false);

                    // Add maze genome ID to the list of processed mazes to its not considered for evaluation again
                    evaluatedMazeGenomeIds.Add((int) curMazeGenome.Id);
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
                    if (ExecutionConfiguration.ContainsKey(curParameter))
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
                                         Enum.GetNames(typeof (ExecutionParameter)).Length) == false)
            {
                // Check for existence of experiment names to execute
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.ExperimentNames) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.ExperimentNames));
                    isConfigurationValid = false;
                }

                // Check for existence of reference experiment
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.ReferenceExperimentName) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.ReferenceExperimentName));
                    isConfigurationValid = false;
                }

                // Check for existence of input neuron count
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.AgentNeuronInputCount) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.AgentNeuronInputCount));
                    isConfigurationValid = false;
                }

                // Check for existence of output neuron count
                if (ExecutionConfiguration.ContainsKey(ExecutionParameter.AgentNeuronOutputCount) == false)
                {
                    _executionLogger.Error(string.Format("Parameter [{0}] must be specified.",
                        ExecutionParameter.AgentNeuronOutputCount));
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            _executionLogger.Error("The experiment executor invocation must take the following form:");
            _executionLogger.Error(
                string.Format(
                    "CoevolutionAlgorithmComparator.exe {0}=[{5}] {1}=[{6}] {2}=[{7}] {3}=[{8}] {4}=[{9}]",
                    ExecutionParameter.AgentNeuronInputCount, ExecutionParameter.AgentNeuronOutputCount,
                    ExecutionParameter.DataFileOutputDirectory, ExecutionParameter.ReferenceExperimentName,
                    ExecutionParameter.ExperimentNames, "# Input Neurons", "# Output Neurons", "directory",
                    "reference experiment", "experiment,experiment,..."));

            return false;
        }
    }
}