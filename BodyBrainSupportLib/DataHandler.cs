using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ExperimentEntities;
using ExperimentEntities.entities;
using RunPhase = SharpNeat.Core.RunPhase;

namespace BodyBrainSupportLib
{
    /// <summary>
    ///     Provides methods for interfacing with the body/brain experiment database.
    /// </summary>
    public static class DataHandler
    {
        /// <summary>
        ///     Field delimiter used in output files.
        /// </summary>
        private const string FileDelimiter = ",";

        /// <summary>
        ///     Number of times query should be retried before finally throwing exception.  This is to deal with connection
        ///     resiliency issues and issues with connections in shared pool being disposed while another connection is using it.
        /// </summary>
        private const int MaxQueryRetryCnt = 100;

        /// <summary>
        ///     Map of output file stream writers.
        /// </summary>
        private static readonly Dictionary<OutputFileType, StreamWriter> FileWriters =
            new Dictionary<OutputFileType, StreamWriter>();

        #region Public database read methods

        /// <summary>
        ///     Looks up an experiment configuration given the unique experiment name.
        /// </summary>
        /// <param name="experimentName">The experiment name whose configuration to lookup.</param>
        /// <returns>The corresponding experiment configuration (i.e. experiment dictionary).</returns>
        public static ExperimentDictionaryBodyBrain LookupExperimentConfiguration(string experimentName)
        {
            ExperimentDictionaryBodyBrain experimentConfiguration = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the experiment configuration given the name (which is guaranteed to be unique)
                    using (var context = new ExperimentDataContext())
                    {
                        experimentConfiguration =
                            context.ExperimentDictionaryBodyBrain.Single(expName =>
                                expName.ExperimentName == experimentName);
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return experimentConfiguration;
        }

        /// <summary>
        ///     Retrieves the body genome IDs for a particular run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The body genome IDs.</returns>
        public static IList<int> GetBodyGenomeIds(int experimentId, int run)
        {
            IList<int> bodyGenomeIds = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the distinct body genome IDs logged during the run
                    using (var context = new ExperimentDataContext())
                    {
                        bodyGenomeIds =
                            context.MccexperimentVoxelBodyGenomes.Where(
                                    expData => expData.ExperimentDictionaryId == experimentId && expData.Run == run)
                                .Select(m => m.GenomeId)
                                .ToList();
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return bodyGenomeIds;
        }

        /// <summary>
        ///     Extracts successful body and brain genome pairs from experiment body trials.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="bodyGenomeIds">The list of body genome IDs for which to find successful brains.</param>
        /// <returns>Successful body and brain genome pairs.</returns>
        public static List<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>>
            GetSuccessfulGenomeCombosFromBodyTrials(int experimentId, int run, IList<int> bodyGenomeIds)
        {
            // Get successful ambulation trials during experiments (if produced)
            var perBodySuccessfulTrials = GetSuccessfulBrainTrialPerBody(experimentId, run, bodyGenomeIds);

            var successfulGenomeCombos =
                new List<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>>(perBodySuccessfulTrials
                    .Count());

            // Get distinct body and brain genomes
            var bodyGenomeData = GetBodyGenomeData(experimentId, run,
                perBodySuccessfulTrials.Select(trial => trial.BodyGenomeId).Distinct().ToList());
            var brainGenomeData = GetBrainGenomeData(experimentId, run, RunPhase.Primary,
                perBodySuccessfulTrials.Select(trial => trial.PairedBrainGenomeId).Distinct().ToList());

            // Build list of successful body/brain combinations
            successfulGenomeCombos.AddRange(
                perBodySuccessfulTrials.Select(
                    successfulTrial =>
                        new Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>(
                            bodyGenomeData.First(gd => successfulTrial.BodyGenomeId == gd.GenomeId),
                            brainGenomeData.First(gd => successfulTrial.PairedBrainGenomeId == gd.GenomeId))));

            return successfulGenomeCombos;
        }

        #endregion

        #region Private static methods

        /// <summary>
        ///     Handles logging of query success state after one or more failed attempts.
        /// </summary>
        /// <param name="methodName">The name of the method executing the query.</param>
        /// <param name="retryCnt">The number of times the query has been retried.</param>
        private static void LogFailedQuerySuccess(string methodName, int retryCnt)
        {
            Console.Error.WriteLine("Successfully executed {0}.{1} query on batch retry {2}",
                typeof(DataHandler).FullName, methodName, retryCnt);
        }

        /// <summary>
        ///     Handles logging and retry boundary checking for exceptions that are thrown during query execution.
        /// </summary>
        /// <param name="methodName">The name of the method executing the query.</param>
        /// <param name="retryCnt">The number of times the query has been retried.</param>
        /// <param name="e">The exception object that was thrown.</param>
        private static void HandleQueryException(string methodName, int retryCnt, Exception e)
        {
            Console.Error.WriteLine("{0}.{1} failed to execute query on retry {2}",
                typeof(DataHandler).FullName, methodName, retryCnt);

            // Throw exception if we've no exceeded the retry count
            if (retryCnt + 1 > MaxQueryRetryCnt)
            {
                throw e;
            }
        }

        #endregion

        #region Private database methods

        /// <summary>
        ///     For each body, retrieves the first brain that solved within the given experiment and run.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="bodyGenomeIds">The list of body genome IDs by which to filter the brain results.</param>
        /// <returns>The list of distinct body ambulation trials that were successful for the given experiment and run.</returns>
        private static IList<MccexperimentVoxelBodyTrials> GetSuccessfulBrainTrialPerBody(int experimentId, int run,
            IList<int> bodyGenomeIds)
        {
            IList<MccexperimentVoxelBodyTrials> bodyTrials = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (var context = new ExperimentDataContext())
                    {
                        // Get single maze trial for each of the specified maze IDs over the entirety of the run
                        bodyTrials = context.MccexperimentVoxelBodyTrials.Where(
                                nav =>
                                    experimentId == nav.ExperimentDictionaryId && run == nav.Run &&
                                    nav.IsBodySolved && bodyGenomeIds.Contains(nav.BodyGenomeId))
                            .GroupBy(nav => nav.BodyGenomeId)
                            .Select(m => m.OrderBy(x => x.BodyGenomeId).FirstOrDefault())
                            .ToList();
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return bodyTrials;
        }

        /// <summary>
        ///     Retrieves the body genome data (i.e. evaluation statistics and XML) for the entirety of a given run/experiment,
        ///     constrained by the specified genome IDs.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="bodyGenomeIds">The body genome IDs by which to constrain.</param>
        /// <returns>The body genome data.</returns>
        private static IList<MccexperimentVoxelBodyGenome> GetBodyGenomeData(int experimentId, int run,
            IList<int> bodyGenomeIds)
        {
            IList<MccexperimentVoxelBodyGenome> bodyGenomes = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the body genomes corresponding to the specified genome IDs
                    using (var context = new ExperimentDataContext())
                    {
                        bodyGenomes =
                            context.MccexperimentVoxelBodyGenomes.Where(
                                    expData =>
                                        expData.ExperimentDictionaryId == experimentId && expData.Run == run &&
                                        bodyGenomeIds.Contains(expData.GenomeId))
                                .ToList();
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return bodyGenomes;
        }

        /// <summary>
        ///     Retrieves the brain genome data (i.e. evaluation statistics and XML) for the entirety of a given
        ///     run/experiment, constrained by the specified genome IDs.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <param name="brainGenomeIds">The brain genome IDs by which to constrain.</param>
        /// <returns>The navigator genome data.</returns>
        private static IList<MccexperimentVoxelBrainGenome> GetBrainGenomeData(int experimentId, int run,
            RunPhase runPhase, IList<int> brainGenomeIds)
        {
            IList<MccexperimentVoxelBrainGenome> brainGenomes = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the brain genomes corresponding to the specified genome IDs
                    using (var context = new ExperimentDataContext())
                    {
                        brainGenomes =
                            context.MccexperimentVoxelBrainGenomes.Where(
                                expData =>
                                    expData.ExperimentDictionaryId == experimentId && expData.Run == run &&
                                    expData.RunPhaseFkNavigation.RunPhaseName == runPhase.ToString() &&
                                    brainGenomeIds.Contains(expData.GenomeId)).ToList();
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return brainGenomes;
        }

        #endregion

        #region Public file writer methods

        /// <summary>
        ///     Opens the file stream writer.
        /// </summary>
        /// <param name="fileName">The name of the flat file to write into.</param>
        /// <param name="fileType">The type of data being written.</param>
        public static void OpenFileWriter(string fileName, OutputFileType fileType)
        {
            // Make sure a writer of the given type has not already been opened.
            if (FileWriters.ContainsKey(fileType))
            {
                throw new Exception($"File writer for type {fileType} already opened.");
            }

            // Open the stream writer
            FileWriters.Add(fileType, new StreamWriter(fileName) {AutoFlush = true});
        }

        /// <summary>
        ///     Closes the file stream writer.
        /// </summary>
        public static void CloseFileWriter(OutputFileType fileType)
        {
            // Make sure the file writer actually exists before attempting to close it
            if (FileWriters.ContainsKey(fileType) == false)
            {
                throw new Exception($"Cannot close file writer as no file writer of type {fileType} has been created.");
            }

            // Close the file writer, dispose of the stream and remove from the file writers dictionary
            FileWriters[fileType].Close();
            FileWriters[fileType].Dispose();
            FileWriters.Remove(fileType);
        }

        /// <summary>
        ///     Writes empty "sentinel" file so that data transfer agent on cluster can easily identify that a given experiment run
        ///     is complete.
        /// </summary>
        /// <param name="experimentFilename">The base path and filename of the experiment under execution.</param>
        /// <param name="run">The completed run.</param>
        public static void WriteSentinelFile(string experimentFilename, int run)
        {
            // Write sentinel file to the given output directory
            using (File.Create($"{experimentFilename} - Run {run} - COMPLETE"))
            {
            }
        }

        /// <summary>
        ///     Writes body/brain simulation results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="simulationUnits">The body/brain simulation log data to write.</param>
        public static void WriteSimulationLogDataToFile(int experimentId, int run,
            IEnumerable<BodyBrainSimulationUnit> simulationUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.SimulationLogData) == false)
            {
                throw new Exception(
                    $"Cannot write to output stream as no file writer of type {OutputFileType.SimulationLogData} has been created.");
            }

            // Loop through body brain simulation units and write each recorded simulation state
            foreach (var simulationUnit in simulationUnits)
            {
                foreach (var timestepUnit in simulationUnit.BodyBrainSimulationTimestepUnits)
                {
                    FileWriters[OutputFileType.SimulationLogData].WriteLine(string.Join(FileDelimiter, new List<string>
                    {
                        experimentId.ToString(),
                        run.ToString(),
                        simulationUnit.BodyId.ToString(),
                        simulationUnit.BrainId.ToString(),
                        timestepUnit.Timestep.ToString(),
                        timestepUnit.Position.X.ToString(CultureInfo.InvariantCulture),
                        timestepUnit.Position.Y.ToString(CultureInfo.InvariantCulture),
                        timestepUnit.Position.Z.ToString(CultureInfo.InvariantCulture),
                        timestepUnit.Distance.ToString(CultureInfo.InvariantCulture),
                        timestepUnit.VoxelsTouchingFloor.ToString(),
                        timestepUnit.MaxVoxelVelocity.ToString(CultureInfo.InvariantCulture),
                        timestepUnit.MaxVoxelDisplacement.ToString(CultureInfo.InvariantCulture),
                        timestepUnit.Displacement.X.ToString(CultureInfo.InvariantCulture),
                        timestepUnit.Displacement.Y.ToString(CultureInfo.InvariantCulture),
                        timestepUnit.Displacement.Z.ToString(CultureInfo.InvariantCulture)
                    }));
                }
            }
        }

        #endregion
    }
}