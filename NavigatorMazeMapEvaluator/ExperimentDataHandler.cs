#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExperimentEntities;

#endregion

namespace NavigatorMazeMapEvaluator
{
    /// <summary>
    ///     Provides methods for interfacing with and writing reuslts to the experiment database or flat file.
    /// </summary>
    public static class ExperimentDataHandler
    {
        private const string FileDelimiter = ",";
        private static StreamWriter _fileWriter;

        /// <summary>
        ///     Looks up an experiment configuration given the unique experiment name.
        /// </summary>
        /// <param name="experimentName">The experiment name whose configuration to lookup.</param>
        /// <returns>The corresponding experiment configuration (i.e. experiment dictionary).</returns>
        public static ExperimentDictionary LookupExperimentConfiguration(string experimentName)
        {
            ExperimentDictionary experimentConfiguration;

            // Query for the experiment configuration given the name (which is guaranteed to be unique)
            using (ExperimentDataEntities context = new ExperimentDataEntities())
            {
                experimentConfiguration =
                    context.ExperimentDictionaries.Single(expName => expName.ExperimentName == experimentName);
            }

            return experimentConfiguration;
        }

        /// <summary>
        ///     Retrieves the number of runs that were executed for a given experiment.
        /// </summary>
        /// <param name="experimentId">The experiment for which to compute the number of corresponding runs.</param>
        /// <returns>The number of runs that were executed for the given experiment.</returns>
        public static int GetNumRuns(int experimentId)
        {
            int numRuns;

            // Query for the number of runs for the given experiment
            using (ExperimentDataEntities context = new ExperimentDataEntities())
            {
                numRuns = context.CoevolutionMCSMazeExperimentEvaluationDatas.Where(
                    expData => expData.ExperimentDictionaryID == experimentId).Select(row => row.Run).Distinct().Count();
            }

            return numRuns;
        }

        /// <summary>
        ///     Retrieves the number of batches that were executed during a given run of a given experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The number of batches in the given run.</returns>
        public static int GetNumBatchesForRun(int experimentId, int run)
        {
            int numBatches;

            // Query for the number of batches in the current run of the given experiment
            using (ExperimentDataEntities context = new ExperimentDataEntities())
            {
                numBatches = context.CoevolutionMCSMazeExperimentEvaluationDatas.Where(
                    expData => expData.ExperimentDictionaryID == experimentId && expData.Run == run)
                    .Select(row => row.Generation)
                    .Max();
            }

            return numBatches;
        }

        /// <summary>
        ///     Retrieves the batches during a given run of a given experiment that have associated genome data.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The collection of batch numbers in the given experiment/run that have associated genome data.</returns>
        public static IList<int> GetBatchesWithGenomeData(int experimentId, int run)
        {
            IList<int> batchesWithGenomeData;

            // Query for the distinct batches in the current run of the given experiment
            using (ExperimentDataEntities context = new ExperimentDataEntities())
            {
                batchesWithGenomeData = context.CoevolutionMCSMazeExperimentGenomes.Where(
                    expData => expData.ExperimentDictionaryID == experimentId && expData.Run == run)
                    .Select(row => row.Generation)
                    .Distinct().ToList();
            }

            return batchesWithGenomeData;
        }

        /// <summary>
        ///     Retrieves the maze genome data (i.e. evaluation statistics and XML) for a particular batch of a given
        ///     run/experiemnt.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <returns>The maze genome data.</returns>
        public static IList<CoevolutionMCSMazeExperimentGenome> GetMazeGenomeData(int experimentId, int run, int batch)
        {
            IList<CoevolutionMCSMazeExperimentGenome> mazeGenomes;

            // Query for maze genomes logged during the current batch
            using (ExperimentDataEntities context = new ExperimentDataEntities())
            {
                mazeGenomes = context.CoevolutionMCSMazeExperimentGenomes.Where(
                    expData =>
                        expData.ExperimentDictionaryID == experimentId && expData.Run == run &&
                        expData.Generation == batch).ToList();
            }

            return mazeGenomes;
        }

        /// <summary>
        ///     Retrieves the navigator genome data (i.e. evaluation statistics and XML) for a particular batch of a given
        ///     run/experiemnt.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <returns>The navigator genome data.</returns>
        public static IList<CoevolutionMCSNavigatorExperimentGenome> GetNavigatorGenomeData(int experimentId, int run,
            int batch)
        {
            IList<CoevolutionMCSNavigatorExperimentGenome> navigatorGenomes;

            // Query for navigator genomes logged during the current batch
            using (ExperimentDataEntities context = new ExperimentDataEntities())
            {
                navigatorGenomes =
                    context.CoevolutionMCSNavigatorExperimentGenomes.Where(
                        expData =>
                            expData.ExperimentDictionaryID == experimentId && expData.Run == run &&
                            expData.Generation == batch).ToList();
            }

            return navigatorGenomes;
        }

        /// <summary>
        ///     Writes the given evaluation results to the experiment database or to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        /// <param name="commitPageSize">The number of records that are committed within a single batch/context.</param>
        /// <param name="writeToDatabase">
        ///     Indicates whether evaluation results should be written directly to the database or to a
        ///     flat file.
        /// </param>
        public static void WriteNavigatorMazeEvaluationData(int experimentId, int run, int batch,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int commitPageSize, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                WriteNavigatorMazeEvaluationDataToDatabase(experimentId, run, batch, evaluationUnits, commitPageSize);
            }
            // Otherwise, write to the flat file output
            else
            {
                WriteNavigatorMazeEvaluationDataToFile(experimentId, run, batch, evaluationUnits);
            }
        }

        /// <summary>
        ///     Writes the given evaluation results to the experiment database.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        /// <param name="commitPageSize">The number of records that are committed within a single batch/context.</param>
        private static void WriteNavigatorMazeEvaluationDataToDatabase(int experimentId, int run, int batch,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int commitPageSize)
        {
            // Page through the result set, committing each in the specified batch size
            for (int curPage = 0; curPage <= evaluationUnits.Count/commitPageSize; curPage++)
            {
                IList<CoevolutionMCSMazeNavigatorResult> serializedResults =
                    new List<CoevolutionMCSMazeNavigatorResult>(commitPageSize);

                // Build a list of serialized results
                foreach (
                    MazeNavigatorEvaluationUnit evaluationUnit in
                        evaluationUnits.Skip(curPage*commitPageSize).Take(commitPageSize))
                {
                    serializedResults.Add(new CoevolutionMCSMazeNavigatorResult
                    {
                        ExperimentDictionaryID = experimentId,
                        Run = run,
                        Generation = batch,
                        MazeGenomeID = evaluationUnit.MazeId,
                        NavigatorGenomeID = evaluationUnit.AgentId,
                        IsMazeSolved = evaluationUnit.IsMazeSolved,
                        NumTimesteps = evaluationUnit.NumTimesteps
                    });
                }

                // Create a new context and persist the batch
                using (ExperimentDataEntities context = new ExperimentDataEntities())
                {
                    // Auto-detect changes and save validation are switched off to speed things up
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Configuration.ValidateOnSaveEnabled = false;

                    context.CoevolutionMCSMazeNavigatorResults.AddRange(serializedResults);
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        ///     Opens the file stream writer.
        /// </summary>
        /// <param name="fileName">The name of the flat file to write into.</param>
        public static void OpenFileWriter(string fileName)
        {
            // Open the stream writer
            _fileWriter = new StreamWriter(fileName) {AutoFlush = true};
        }

        /// <summary>
        ///     Closes the file stream writer.
        /// </summary>
        public static void CloseFileWriter()
        {
            _fileWriter.Close();
            _fileWriter.Dispose();
        }

        /// <summary>
        ///     Writes the given evaluation results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        private static void WriteNavigatorMazeEvaluationDataToFile(int experimentId, int run, int batch,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits)
        {
            // Loop through the evaluation units and write each row
            foreach (MazeNavigatorEvaluationUnit evaluationUnit in evaluationUnits)
            {
                _fileWriter.WriteLine(string.Join(FileDelimiter, new List<string>
                {
                    experimentId.ToString(),
                    run.ToString(),
                    batch.ToString(),
                    evaluationUnit.MazeId.ToString(),
                    evaluationUnit.AgentId.ToString(),
                    evaluationUnit.IsMazeSolved.ToString(),
                    evaluationUnit.NumTimesteps.ToString()
                }));
            }

            // Immediately flush to the log file
            _fileWriter.Flush();
        }

        /// <summary>
        ///     Writes empty "sentinel" file so that data transfer agent on cluster can easily identify that a given experiment is
        ///     complete.
        /// </summary>
        /// <param name="experimentFilename">The base path and filename of the experiment under execution.</param>
        public static void WriteSentinelFile(string experimentFilename)
        {
            // Write sentinel file to the given output directory
            using (File.Create(string.Format("{0} - COMPLETE", experimentFilename)))
            {
            }
        }
    }
}