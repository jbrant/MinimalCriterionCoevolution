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

            // Get boolean indicator dictating whether to analyze the whole run or just the last batch (default is true - full run)
            AnalysisScope analysisScope =
                AnalysisScopeUtil.ConvertStringToAnalysisScope(_executionConfiguration[ExecutionParameter.AnalysisScope]);

            // Get input and output neurons counts for navigator agent
            int inputNeuronCount = Int32.Parse(_executionConfiguration[ExecutionParameter.AgentNeuronInputCount]);
            int outputNeuronCount = Int32.Parse(_executionConfiguration[ExecutionParameter.AgentNeuronOutputCount]);

            // Get boolean indicator dictating whether to generate trajectory data (default is false)
            bool generateTrajectoryData =
                _executionConfiguration.ContainsKey(ExecutionParameter.GenerateTrajectoryData) &&
                Boolean.Parse(_executionConfiguration[ExecutionParameter.GenerateTrajectoryData]);

            // Get boolean indicator dictating whether to write out numeric results of the batch simulations (default is true)
            bool generateSimulationResults =
                _executionConfiguration.ContainsKey(ExecutionParameter.GenerateSimulationResults) == false ||
                Boolean.Parse(_executionConfiguration[ExecutionParameter.GenerateSimulationResults]);

            // Get boolean indicator dictating whether to generate bitmaps of mazes (default is true)
            bool generateMazeBitmaps = _executionConfiguration.ContainsKey(ExecutionParameter.GenerateMazeBitmaps) ==
                                       false ||
                                       Boolean.Parse(_executionConfiguration[ExecutionParameter.GenerateMazeBitmaps]);

            // Get boolean indicator dictating whether to generate bitmaps of agent trajectories (default is true)
            bool generateTrajectoryBitmaps =
                _executionConfiguration.ContainsKey(ExecutionParameter.GenerateAgentTrajectoryBitmaps) == false ||
                Boolean.Parse(_executionConfiguration[ExecutionParameter.GenerateAgentTrajectoryBitmaps]);

            // Get boolean indicator dictating whether to write simulation results to database (default is false)
            bool writeResultsToDatabase =
                _executionConfiguration.ContainsKey(ExecutionParameter.WriteResultsToDatabase) &&
                Boolean.Parse(_executionConfiguration[ExecutionParameter.WriteResultsToDatabase]);

            // Determine whether this is a distributed execution
            bool isDistributedExecution =
                _executionConfiguration.ContainsKey(ExecutionParameter.IsDistributedExecution) &&
                Boolean.Parse(_executionConfiguration[ExecutionParameter.IsDistributedExecution]);

            // Get boolean indicator dictating whether to write out trajectory diversity scores (default is false)
            bool generateTrajectoryDiversityScores =
                _executionConfiguration.ContainsKey(ExecutionParameter.GenerateDiversityScores) &&
                Boolean.Parse(_executionConfiguration[ExecutionParameter.GenerateDiversityScores]);

            // Get boolean indicator dictating whether to write out natural population clusters (default is false)
            bool generateNaturalClusters =
                _executionConfiguration.ContainsKey(ExecutionParameter.GenerateNaturalClusters) &&
                Boolean.Parse(_executionConfiguration[ExecutionParameter.GenerateNaturalClusters]);

            // If the generate natural clusters flag is set, get the cluster improvement threshold and specie sample size
            int clusterImprovementThreshold = generateNaturalClusters
                ? Int32.Parse(_executionConfiguration[ExecutionParameter.ClusterImprovementThreshold])
                : 0;
            int specieSampleSize = generateNaturalClusters
                ? Int32.Parse(_executionConfiguration[ExecutionParameter.ClusterSampleSize])
                : 0;

            // Get boolean indicator dictating whether to sample the clustering data points from 
            // species or from the population (default is false, meaning sample from population)
            bool sampleClusterObservationsFromSpecies =
                _executionConfiguration.ContainsKey(ExecutionParameter.SampleFromSpecies) &&
                Boolean.Parse(_executionConfiguration[ExecutionParameter.SampleFromSpecies]);

            // Get boolean indicator dictating whether to execute initialization trial analysis (default is false)
            bool runInitializationAnalysis =
                _executionConfiguration.ContainsKey(ExecutionParameter.ExecuteInitializationTrials) &&
                Boolean.Parse(_executionConfiguration[ExecutionParameter.ExecuteInitializationTrials]);

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

                // Get the number of runs in the experiment. Note that if this is a distributed execution, each node
                // will only execute a single run analysis, so the number of runs will be equivalent to the run 
                // to start from (this ensures that the ensuing loop that executes all of the runs executes exactly once)
                int numRuns = isDistributedExecution
                    ? startingRun
                    : ExperimentDataHandler.GetNumRuns(curExperimentConfiguration.ExperimentDictionaryID);

                _executionLogger.Info(string.Format("Preparing to execute analysis for [{0}] runs of experiment [{1}]",
                    numRuns,
                    curExperimentConfiguration.ExperimentName));

                // Process each experiment run
                for (int curRun = startingRun; curRun <= numRuns; curRun++)
                {
                    // If simulation result generation is enabled and we're not writing to 
                    // the database, open the simulation result file writer
                    if (generateSimulationResults && writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.OpenFileWriter(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                string.Format("{0} - Run{1}.csv", experimentName, curRun)),
                            OutputFileType.NavigatorMazeEvaluationData);
                    }

                    // If trajectory data generation is enabled and we're not writing
                    // to the database, open the trajectory data file writer
                    if (generateTrajectoryData && writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.OpenFileWriter(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                string.Format("{0} - TrajectoryData - Run{1}.csv", experimentName, curRun)),
                            OutputFileType.TrajectoryData);
                    }

                    // If trajectory diversity score generation is enabled and we're not writing to 
                    // the database, open the trajectory diversity score file writer
                    if (generateTrajectoryDiversityScores && writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.OpenFileWriter(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                string.Format("{0} - TrajectoryDiversity - {1} - Run{2}.csv", experimentName,
                                    analysisScope, curRun)),
                            OutputFileType.TrajectoryDiversityData);
                    }

                    if (generateNaturalClusters && writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.OpenFileWriter(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                string.Format("{0} - NaturalClusters - {1} - Run{2}.csv", experimentName, analysisScope,
                                    curRun)),
                            OutputFileType.NaturalClusterData);
                    }

                    // If we're analyzing the entire run, go ahead and process through the initialization phase
                    // and all primary phase batch results
                    if (AnalysisScope.Full == analysisScope)
                    {
                        // Get the number of initialization batches in the current run
                        IList<int> initializationBatchesWithGenomeData =
                            ExperimentDataHandler.GetBatchesWithGenomeData(
                                curExperimentConfiguration.ExperimentDictionaryID, curRun, RunPhase.Initialization);

                        // If we're running initialization analysis and there was an initialization phase, analyze those results
                        if (runInitializationAnalysis && initializationBatchesWithGenomeData.Count > 0)
                        {
                            _executionLogger.Info(
                                string.Format(
                                    "Executing initialization phase analysis for run [{0}/{1}] with [{2}] batches",
                                    curRun,
                                    numRuns, initializationBatchesWithGenomeData.Count));

                            // Begin initialization phase results processing
                            ProcessAndLogPerBatchResults(initializationBatchesWithGenomeData,
                                RunPhase.Initialization,
                                experimentParameters, inputNeuronCount, outputNeuronCount, curRun, numRuns,
                                curExperimentConfiguration, generateSimulationResults, generateTrajectoryData,
                                generateTrajectoryDiversityScores, generateNaturalClusters, writeResultsToDatabase,
                                generateMazeBitmaps, generateTrajectoryBitmaps, baseImageOutputDirectory,
                                clusterImprovementThreshold, specieSampleSize, sampleClusterObservationsFromSpecies);
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
                            inputNeuronCount, outputNeuronCount, curRun, numRuns, curExperimentConfiguration,
                            generateSimulationResults, generateTrajectoryData, generateTrajectoryDiversityScores,
                            generateNaturalClusters, writeResultsToDatabase, generateMazeBitmaps,
                            generateTrajectoryBitmaps, baseImageOutputDirectory, clusterImprovementThreshold,
                            specieSampleSize, sampleClusterObservationsFromSpecies);
                    }
                    // If we want to analyze the entirety of the run as a whole
                    else if (AnalysisScope.Aggregate == analysisScope)
                    {
                        _executionLogger.Info(
                            string.Format("Executing aggregate analysis for run [{0}/{1}]", curRun, numRuns));

                        // Get the number of primary batches in the current run
                        IList<int> batchesWithGenomeData =
                            ExperimentDataHandler.GetBatchesWithGenomeData(
                                curExperimentConfiguration.ExperimentDictionaryID, curRun, RunPhase.Primary);

                        // Process aggregate run
                        ProcessAndLogAggregateRunResults(batchesWithGenomeData, experimentParameters, inputNeuronCount,
                            outputNeuronCount,
                            curRun, numRuns, curExperimentConfiguration, generateSimulationResults,
                            generateTrajectoryData, generateTrajectoryDiversityScores, generateNaturalClusters,
                            writeResultsToDatabase, generateMazeBitmaps, generateTrajectoryBitmaps,
                            baseImageOutputDirectory, clusterImprovementThreshold, specieSampleSize);
                    }
                    // Otherwise, we're just analyzing the ending population
                    else
                    {
                        // Get the last batch in the current run
                        int finalBatch =
                            ExperimentDataHandler.GetNumBatchesForRun(
                                curExperimentConfiguration.ExperimentDictionaryID, curRun);

                        _executionLogger.Info(
                            string.Format(
                                "Executing analysis of end-stage mazes and navigator trajectories for run [{0}/{1}] batch [{2}]",
                                curRun, numRuns, finalBatch));

                        // Begin maze/navigator trajectory image generation
                        ProcessAndLogPerBatchResults(new List<int>(1) {finalBatch}, RunPhase.Primary,
                            experimentParameters, inputNeuronCount, outputNeuronCount, curRun, numRuns,
                            curExperimentConfiguration, generateSimulationResults, generateTrajectoryData,
                            generateTrajectoryDiversityScores, generateNaturalClusters, writeResultsToDatabase,
                            generateMazeBitmaps, generateTrajectoryBitmaps, baseImageOutputDirectory,
                            clusterImprovementThreshold, specieSampleSize, sampleClusterObservationsFromSpecies);
                    }

                    // If we're not writing to the database, close the simulation result file writer
                    // and write the sentinel file for the run
                    if (generateSimulationResults && writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.CloseFileWriter(OutputFileType.NavigatorMazeEvaluationData);
                        ExperimentDataHandler.WriteSentinelFile(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                experimentName), curRun);
                    }

                    // If we're not writing to the database, close the trajectory data file writer
                    // and write the sentinel file for the run
                    if (generateTrajectoryData && writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.CloseFileWriter(OutputFileType.TrajectoryData);
                        ExperimentDataHandler.WriteSentinelFile(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                string.Format("{0} - TrajectoryData", experimentName)), curRun);
                    }

                    // If we're not writing to the database, close the trajectory diversity 
                    // score file writer and write the sentinel file for the run
                    if (generateTrajectoryDiversityScores && writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.CloseFileWriter(OutputFileType.TrajectoryDiversityData);
                        ExperimentDataHandler.WriteSentinelFile(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                string.Format("{0} - TrajectoryDiversity - {1}", experimentName, analysisScope)), curRun);
                    }

                    // If we're not writing to the database, close the natural clustering file writer
                    // and write the sentinel file for the run
                    if (generateNaturalClusters && writeResultsToDatabase == false)
                    {
                        ExperimentDataHandler.CloseFileWriter(OutputFileType.NaturalClusterData);
                        ExperimentDataHandler.WriteSentinelFile(
                            Path.Combine(_executionConfiguration[ExecutionParameter.DataFileOutputDirectory],
                                string.Format("{0} - NaturalClusters - {1}", experimentName, analysisScope)), curRun);
                    }
                }
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
        /// <param name="generateSimulationResults">Indicates whether to write out the results of the batch simulation.</param>
        /// <param name="writeResultsToDatabase">
        ///     Indicates whether to write results directly into a database (if not, results are
        ///     written to a flat file).
        /// </param>
        /// <param name="generateTrajectoryData">Indicates whether the full navigator trajectory should be simulated and persisted.</param>
        /// <param name="generateTrajectoryDiversityScore">
        ///     Indicates whether quantification of navigator trajectory diversity
        ///     should be written out.
        /// </param>
        /// <param name="generateNaturalClustering">Indicates whether the natural population clusters should be analyzed.</param>
        /// <param name="generateMazeBitmaps">Indicates whether bitmap files of the distinct mazes should be written out.</param>
        /// <param name="generateTrajectoryBitmaps">
        ///     Indicates whether bitmap files depicting the navigator trajectory should be
        ///     written out.
        /// </param>
        /// <param name="baseImageOutputDirectory">The path to the output directory for the trajectory images.</param>
        /// <param name="clusterImprovementThreshold">
        ///     The number of cluster additions that are permitted without further maximization of silhouette width.
        /// </param>
        /// <param name="sampleSize">The number of genomes sampled from the extant species for clustering analysis.</param>
        /// <param name="sampleClusterObservationsFromSpecies">
        ///     Indicates whether to sample observations used in clustering analysis
        ///     from species or from the population as a whole.
        /// </param>
        private static void ProcessAndLogPerBatchResults(IList<int> batchesWithGenomeData, RunPhase runPhase,
            ExperimentParameters experimentParameters, int inputNeuronCount, int outputNeuronCount, int curRun,
            int numRuns, ExperimentDictionary curExperimentConfiguration, bool generateSimulationResults,
            bool generateTrajectoryData, bool generateTrajectoryDiversityScore, bool generateNaturalClustering,
            bool writeResultsToDatabase, bool generateMazeBitmaps, bool generateTrajectoryBitmaps,
            string baseImageOutputDirectory, int clusterImprovementThreshold, int sampleSize,
            bool sampleClusterObservationsFromSpecies)
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

                // TODO: Check first to see if trajectory evaluations already exist

                // Get any existing navigation results (this avoids rerunning failed combinations)
                var successfulNavigations =
                    ExperimentDataHandler.GetSuccessfulNavigations(curExperimentConfiguration.ExperimentDictionaryID,
                        curRun, curBatch);

                // If successful navigation results were found and we're not re-running the simulations,
                // initialize the map evaluator with only those combinations known to be successful
                if (generateSimulationResults == false && successfulNavigations != null &&
                    successfulNavigations.Count > 0)
                {
                    List<Tuple<CoevolutionMCSMazeExperimentGenome, CoevolutionMCSNavigatorExperimentGenome>>
                        successfulGenomeCombos =
                            new List<Tuple<CoevolutionMCSMazeExperimentGenome, CoevolutionMCSNavigatorExperimentGenome>>
                                (successfulNavigations.Count());

                    // Get distinct maze and navigator genomes
                    var mazeGenomeData =
                        ExperimentDataHandler.GetMazeGenomeData(curExperimentConfiguration.ExperimentDictionaryID,
                            curRun, curBatch, successfulNavigations.Select(n => n.MazeGenomeID).Distinct().ToList());
                    var navigatorGenomeData =
                        ExperimentDataHandler.GetNavigatorGenomeData(curExperimentConfiguration.ExperimentDictionaryID,
                            curRun, curBatch, runPhase,
                            successfulNavigations.Select(n => n.NavigatorGenomeID).Distinct().ToList());

                    // Build list of successful maze/navigator combinations
                    successfulGenomeCombos.AddRange(
                        successfulNavigations.Select(
                            successfulNav =>
                                new Tuple<CoevolutionMCSMazeExperimentGenome, CoevolutionMCSNavigatorExperimentGenome>(
                                    mazeGenomeData.First(gd => successfulNav.MazeGenomeID == gd.GenomeID),
                                    navigatorGenomeData.First(gd => successfulNav.NavigatorGenomeID == gd.GenomeID))));

                    // Initialize the maze/navigator map with combinations that are known to be successful
                    mapEvaluator.Initialize(successfulGenomeCombos);
                }
                // Otherwise, just initialize with all combinations
                else
                {
                    // Initialize the maze/navigator map with the serialized maze and navigator data (this does the parsing)
                    mapEvaluator.Initialize(
                        runPhase == RunPhase.Initialization
                            ? staticInitializationMazes
                            : ExperimentDataHandler.GetMazeGenomeData(curExperimentConfiguration.ExperimentDictionaryID,
                                curRun, curBatch), ExperimentDataHandler.GetNavigatorGenomeData(
                                    curExperimentConfiguration.ExperimentDictionaryID, curRun, curBatch, runPhase));
                }

                // Evaluate all of the maze/navigator combinations in the batch
                mapEvaluator.RunTrajectoryEvaluations();

                if (generateSimulationResults)
                {
                    // Save the evaluation results
                    ExperimentDataHandler.WriteNavigatorMazeEvaluationData(
                        curExperimentConfiguration.ExperimentDictionaryID, curRun, curBatch, runPhase,
                        mapEvaluator.EvaluationUnits, CommitPageSize, writeResultsToDatabase);
                }

                if (generateMazeBitmaps)
                {
                    // Generate bitmaps of distinct mazes extant at the current point in time
                    ImageGenerationHandler.GenerateMazeBitmaps(baseImageOutputDirectory,
                        curExperimentConfiguration.ExperimentName, curExperimentConfiguration.ExperimentDictionaryID,
                        curRun, curBatch, mapEvaluator.EvaluationUnits);
                }

                if (generateTrajectoryBitmaps)
                {
                    // Generate bitmaps of trajectory for all successful trials
                    ImageGenerationHandler.GenerateBitmapsForSuccessfulTrials(
                        baseImageOutputDirectory, curExperimentConfiguration.ExperimentName,
                        curExperimentConfiguration.ExperimentDictionaryID,
                        curRun, curBatch, mapEvaluator.EvaluationUnits, runPhase);
                }

                if (generateTrajectoryData && runPhase != RunPhase.Initialization)
                {
                    // Write out the full trajectory of all agents through all solved mazes
                    ExperimentDataHandler.WriteTrajectoryData(curExperimentConfiguration.ExperimentDictionaryID, curRun,
                        curBatch, mapEvaluator.EvaluationUnits, CommitPageSize, writeResultsToDatabase);
                }

                // Compare trajectories of agents through maze to get quantitative sense of solution diversity
                // Mean euclidean distance will be calculated for selected trajectory against:
                // 1. Other agent trajectories in the current maze only
                // 2. Other agent trajectories on *another* maze only
                // 3. All other agent trajectories (regardless of maze)
                if (generateTrajectoryDiversityScore && runPhase != RunPhase.Initialization)
                {
                    ExperimentDataHandler.WriteTrajectoryDiversityData(
                        curExperimentConfiguration.ExperimentDictionaryID, curRun, curBatch,
                        EvaluationHandler.CalculateTrajectoryDiversity(mapEvaluator.EvaluationUnits),
                        writeResultsToDatabase);
                }

                // Only write clustering results for primary runs when the number of trajectories have surpassed 
                // the minimum cluster count of 3
                if (generateNaturalClustering && runPhase != RunPhase.Initialization)
                {
                    // Extract uniform samples of maze and navigator genomes from each extant specie
                    // on which to run clustering analysis
                    var evaluationSamples = sampleClusterObservationsFromSpecies
                        ? DataManipulationUtil.ExtractEvaluationUnitSamplesFromSpecies(
                            curExperimentConfiguration.ExperimentDictionaryID, curRun, curBatch,
                            mapEvaluator.EvaluationUnits.Where(eu => eu.IsMazeSolved).ToList(), sampleSize)
                        : DataManipulationUtil.ExtractEvaluationUnitSamplesFromPopulation(
                            mapEvaluator.EvaluationUnits.Where(eu => eu.IsMazeSolved).ToList(),
                            sampleSize);

                    // Calculate natural clustering of the population trajectories at each point in time and persist
                    ExperimentDataHandler.WriteClusteringDiversityData(
                        curExperimentConfiguration.ExperimentDictionaryID, curRun, curBatch,
                        EvaluationHandler.CalculateNaturalClustering(evaluationSamples, clusterImprovementThreshold),
                        writeResultsToDatabase);
                }
            }
        }

        /// <summary>
        ///     Runs post-hoc analysis for the entire experiment run in aggregate (i.e. not per batch).  This analyzes the primary
        ///     run phase only.
        /// </summary>
        /// <param name="batchesWithGenomeData">The total number of batches containing genome data.</param>
        /// <param name="experimentParameters">Experiment configuration parameters.</param>
        /// <param name="inputNeuronCount">Count of neurons in controller input layer.</param>
        /// <param name="outputNeuronCount">Count of neurons in controller output layer.</param>
        /// <param name="curRun">The run number.</param>
        /// <param name="numRuns">The total number of runs.</param>
        /// <param name="curExperimentConfiguration">The experiment configuration parameters.</param>
        /// <param name="generateSimulationResults">Indicates whether to write out the results of the batch simulation.</param>
        /// <param name="writeResultsToDatabase">
        ///     Indicates whether to write results directly into a database (if not, results are
        ///     written to a flat file).
        /// </param>
        /// <param name="generateTrajectoryData">Indicates whether the full navigator trajectory should be simulated and persisted.</param>
        /// <param name="generateTrajectoryDiversityScore">
        ///     Indicates whether quantification of navigator trajectory diversity
        ///     should be written out.
        /// </param>
        /// <param name="generateNaturalClustering">Indicates whether the natural population clusters should be analyzed.</param>
        /// <param name="generateMazeBitmaps">Indicates whether bitmap files of the distinct mazes should be written out.</param>
        /// <param name="generateTrajectoryBitmaps">
        ///     Indicates whether bitmap files depicting the navigator trajectory should be
        ///     written out.
        /// </param>
        /// <param name="baseImageOutputDirectory">The path to the output directory for the trajectory images.</param>
        /// <param name="clusterImprovementThreshold">
        ///     The number of cluster additions that are permitted without further maximization of silhouette width.
        /// </param>
        /// <param name="specieSampleSize">The number of genomes sampled from the extant species for clustering analysis.</param>
        private static void ProcessAndLogAggregateRunResults(IList<int> batchesWithGenomeData,
            ExperimentParameters experimentParameters,
            int inputNeuronCount, int outputNeuronCount, int curRun,
            int numRuns, ExperimentDictionary curExperimentConfiguration, bool generateSimulationResults,
            bool generateTrajectoryData, bool generateTrajectoryDiversityScore, bool generateNaturalClustering,
            bool writeResultsToDatabase, bool generateMazeBitmaps, bool generateTrajectoryBitmaps,
            string baseImageOutputDirectory, int clusterImprovementThreshold, int specieSampleSize)
        {
            // Create the maze/navigator map
            MapEvaluator mapEvaluator =
                new MapEvaluator(experimentParameters, inputNeuronCount, outputNeuronCount);

            _executionLogger.Info(string.Format("Executing aggregate run analysis for run [{0}/{1}]", curRun, numRuns));

            // TODO: Might need to consider the option of pinning the mazes in memory and batching through the agents
            // TODO: as pulling both into memory simultaneously is probably going to use too much memory

            // Initialize the evaluator with the maze genomes discovered throughout the entire run
            mapEvaluator.Initialize(
                ExperimentDataHandler.GetMazeGenomeData(curExperimentConfiguration.ExperimentDictionaryID, curRun));

            // Then evaluate each agent against each maze on a batch-by-batch basis
            foreach (int curBatch in batchesWithGenomeData)
            {
                _executionLogger.Info(string.Format("Executing trajectory for batch [{0}] and run [{1}/{2}]", curBatch,
                    curRun, numRuns));

                // Initialize with the agent genomes from the current batch
                mapEvaluator.Initialize(
                    ExperimentDataHandler.GetNavigatorGenomeData(curExperimentConfiguration.ExperimentDictionaryID,
                        curRun, curBatch, RunPhase.Primary));

                // Evaluate all of the maze/navigator combinations in the batch
                mapEvaluator.RunTrajectoryEvaluations();

                if (generateSimulationResults)
                {
                    // Save the evaluation results
                    // TODO: Implement - not sure if this adds a whole lot of value beyond per-batch approach
                }

                if (generateMazeBitmaps)
                {
                    // Generate bitmaps of distinct mazes extant throughout the run
                    // TODO: Implement - not sure if this adds a whole lot of value beyond per-batch approach
                }

                if (generateTrajectoryBitmaps)
                {
                    // Generate bitmaps of trajectory for all successful trials
                    // TODO: Implement - not sure if this adds a whole lot of value beyond per-batch approach
                }

                if (generateTrajectoryData)
                {
                    // Write out the full trajectory of all agents through all solved mazes
                    // TODO: Implement - may end up removing this as it takes up an enormous amount of space
                }

                // Compare trajectories of agents through maze to get quantitative sense of solution diversity
                // Mean euclidean distance will be calculated for selected trajectory against:
                // 1. Other agent trajectories in the current maze only
                // 2. Other agent trajectories on *another* maze only
                // 3. All other agent trajectories (regardless of maze)
                if (generateTrajectoryDiversityScore)
                {
                    // TODO: Implement
                    ExperimentDataHandler.WriteTrajectoryDiversityData(
                        curExperimentConfiguration.ExperimentDictionaryID, curRun,
                        EvaluationHandler.CalculateTrajectoryDiversity(mapEvaluator.EvaluationUnits),
                        writeResultsToDatabase);
                }

                if (generateNaturalClustering)
                {
                    // Calculate natural clustering of the population trajectories throughout the run and persist
                    ExperimentDataHandler.WriteClusteringDiversityData(
                        curExperimentConfiguration.ExperimentDictionaryID, curRun,
                        EvaluationHandler.CalculateNaturalClustering(mapEvaluator.EvaluationUnits,
                            clusterImprovementThreshold), writeResultsToDatabase);
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
                        case ExecutionParameter.ClusterImprovementThreshold:
                        case ExecutionParameter.ClusterSampleSize:
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
                        case ExecutionParameter.GenerateTrajectoryData:
                        case ExecutionParameter.GenerateSimulationResults:
                        case ExecutionParameter.WriteResultsToDatabase:
                        case ExecutionParameter.GenerateMazeBitmaps:
                        case ExecutionParameter.GenerateAgentTrajectoryBitmaps:
                        case ExecutionParameter.GenerateDiversityScores:
                        case ExecutionParameter.IsDistributedExecution:
                        case ExecutionParameter.ExecuteInitializationTrials:
                        case ExecutionParameter.SampleFromSpecies:
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

                // If we're generating experiment result data (default is true) and logging to a flat file instead of the database 
                // (default is true), the output directory must be set
                if ((_executionConfiguration.ContainsKey(ExecutionParameter.GenerateSimulationResults) == false ||
                     Convert.ToBoolean(_executionConfiguration[ExecutionParameter.GenerateSimulationResults])) &&
                    (_executionConfiguration.ContainsKey(ExecutionParameter.WriteResultsToDatabase) == false ||
                     Convert.ToBoolean(_executionConfiguration[ExecutionParameter.WriteResultsToDatabase]) == false) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.DataFileOutputDirectory) == false)
                {
                    _executionLogger.Error(
                        "The data file output directory must be specified when generating experiment result data and writing results to a flat file instead of the database.");
                    isConfigurationValid = false;
                }

                // If natural clustering is being generated, then the improvement threshold must be specified
                if ((_executionConfiguration.ContainsKey(ExecutionParameter.GenerateNaturalClusters) &&
                     Convert.ToBoolean(_executionConfiguration[ExecutionParameter.GenerateNaturalClusters])) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.ClusterImprovementThreshold) == false)
                {
                    _executionLogger.Error(
                        "The cluster improvement threshold must be specified when generating natural clustering statistics.");
                    isConfigurationValid = false;
                }

                // If natural clustering is being generated, then the specie sample size must be specified
                if ((_executionConfiguration.ContainsKey(ExecutionParameter.GenerateNaturalClusters) &&
                     Convert.ToBoolean(_executionConfiguration[ExecutionParameter.GenerateNaturalClusters])) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.ClusterSampleSize) == false)
                {
                    _executionLogger.Error(
                        "The specie sample size must be specified when generating natural clustering statistics.");
                    isConfigurationValid = false;
                }

                // If the executor is going to produce maze bitmap images (default is true), then the base output directory must be specified
                if ((_executionConfiguration.ContainsKey(ExecutionParameter.GenerateMazeBitmaps) == false ||
                     Convert.ToBoolean(_executionConfiguration[ExecutionParameter.GenerateMazeBitmaps])) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.BitmapOutputBaseDirectory) == false)
                {
                    _executionLogger.Error(
                        "The bitmap image base directory must be specified when producing maze images.");
                    isConfigurationValid = false;
                }

                // If the executor is going to produce navigator trajectory bitmap images (default is true), 
                // then the base output directory must be specified
                if ((_executionConfiguration.ContainsKey(ExecutionParameter.GenerateAgentTrajectoryBitmaps) == false ||
                     Convert.ToBoolean(_executionConfiguration[ExecutionParameter.GenerateAgentTrajectoryBitmaps])) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.BitmapOutputBaseDirectory) == false)
                {
                    _executionLogger.Error(
                        "The bitmap image base directory must be specified when producing navigator trajectory images.");
                    isConfigurationValid = false;
                }

                // If this is distributed execution, the StartFromRun parameter must be specified as this
                // is used to control which node is executing which experiment analysis run
                if (_executionConfiguration.ContainsKey(ExecutionParameter.IsDistributedExecution) &&
                    Convert.ToBoolean(_executionConfiguration[ExecutionParameter.IsDistributedExecution]) &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.StartFromRun) == false)
                {
                    _executionLogger.Error(
                        "If this is a distributed execution, the StartFromRun parameter must be specified via the invoking job.");
                    isConfigurationValid = false;
                }

                // Ensure that the analysis scope was specified and that it matches one of the defined scopes
                if (_executionConfiguration.ContainsKey(ExecutionParameter.AnalysisScope) == false ||
                    (_executionConfiguration[ExecutionParameter.AnalysisScope].Equals(AnalysisScope.Full.ToString(),
                        StringComparison.InvariantCultureIgnoreCase) &&
                     _executionConfiguration[ExecutionParameter.AnalysisScope].Equals(
                         AnalysisScope.Aggregate.ToString(), StringComparison.InvariantCultureIgnoreCase) &&
                     _executionConfiguration[ExecutionParameter.AnalysisScope].Equals(AnalysisScope.Last.ToString(),
                         StringComparison.InvariantCultureIgnoreCase)))
                {
                    _executionLogger.Error(
                        "The AnalysisScope parameter must be well-specified and via the invoking job.");
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            _executionLogger.Error("The experiment executor invocation must take the following form:");
            _executionLogger.Error(
                string.Format(
                    "NavigatorMazeMapEvaluator.exe {0}=[{19}] {1}=[{20}] (Optional: {2}=[{27}]) (Optional: {3}=[{21}] {4}=[{21}] (Required: {5}=[{22}])) (Optional: {6}=[{21}] (Required: {9}=[{22}])) (Optional: {7}=[{21}] (Required: {9}=[{22}])) (Optional: {8}=[{21}] (Required: {5}=[{22}]) (Required: {16}=[{25}] {17}=[{26}]) (Optional: {18}=[{21}])) (Optional: {11}=[{21}] (Required: {5}=[{22}])) (Optional: {12}=[{21}] (Required: {5}=[{22}])) (Optional: {13}=[{24}]) (Optional: {14}=[{21}]) (Optional: {15}=[{21}] {10}=[{23}]",
                    ExecutionParameter.AgentNeuronInputCount, ExecutionParameter.AgentNeuronOutputCount,
                    ExecutionParameter.AnalysisScope, ExecutionParameter.GenerateSimulationResults,
                    ExecutionParameter.WriteResultsToDatabase, ExecutionParameter.DataFileOutputDirectory,
                    ExecutionParameter.GenerateMazeBitmaps, ExecutionParameter.GenerateAgentTrajectoryBitmaps,
                    ExecutionParameter.GenerateNaturalClusters, ExecutionParameter.BitmapOutputBaseDirectory,
                    ExecutionParameter.ExperimentNames, ExecutionParameter.GenerateDiversityScores,
                    ExecutionParameter.GenerateTrajectoryData, ExecutionParameter.StartFromRun,
                    ExecutionParameter.ExecuteInitializationTrials,
                    ExecutionParameter.IsDistributedExecution, ExecutionParameter.ClusterImprovementThreshold,
                    ExecutionParameter.ClusterSampleSize, ExecutionParameter.SampleFromSpecies,
                    "# Input Neurons", "# Output Neurons", "true|false",
                    "directory", "experiment,experiment,...", "starting run #", "improvement #", "sample #",
                    "Full, Aggregate, Last"));

            return false;
        }
    }
}