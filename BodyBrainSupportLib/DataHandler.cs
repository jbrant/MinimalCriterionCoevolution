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

        #endregion

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

        /// <summary>
        ///     Retrieves the body genome data (i.e. evaluation statistics and XML) for the entirety of a given run/experiment,
        ///     constrained by the specified genome IDs.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="bodyGenomeIds">The body genome IDs by which to constrain.</param>
        /// <returns>The body genome data.</returns>
        public static IList<MccexperimentVoxelBodyGenome> GetBodyGenomeData(int experimentId, int run,
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
        ///     Retrieves the body genome XML data (i.e. evaluation statistics and XML) for the entirety of a given run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The body genome XML data.</returns>
        public static IList<string> GetBodyGenomeXml(int experimentId, int run)
        {
            IList<string> bodyGenomesXml = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the body genomes XML
                    using (var context = new ExperimentDataContext())
                    {
                        bodyGenomesXml = context.MccexperimentVoxelBodyGenomes
                            .Where(expData => expData.ExperimentDictionaryId == experimentId && expData.Run == run)
                            .Select(x => x.GenomeXml).ToList();
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

            return bodyGenomesXml;
        }

        /// <summary>
        ///     Retrieves the body genome XML data (i.e. evaluation statistics and XML) for the population extant during the
        ///     current batch of a given run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch for which to get the extant population.</param>
        /// <returns>The body genome XML data.</returns>
        public static IList<string> GetBodyGenomeXml(int experimentId, int run, int batch)
        {
            IList<string> bodyGenomesXml = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the body genomes XML
                    using (var context = new ExperimentDataContext())
                    {
                        bodyGenomesXml = context.MccexperimentExtantVoxelBodyPopulation
                            .Where(popData =>
                                popData.ExperimentDictionaryId == experimentId && popData.Run == run &&
                                popData.Generation == batch).Join(context.MccexperimentVoxelBodyGenomes,
                                popData => new {popData.ExperimentDictionaryId, popData.Run, popData.GenomeId},
                                expData => new {expData.ExperimentDictionaryId, expData.Run, expData.GenomeId},
                                (popData, expData) => new {popData, expData}).Select(data => data.expData.GenomeXml)
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

            return bodyGenomesXml;
        }

        /// <summary>
        ///     Retrieves the body genome IDs (i.e. evaluation statistics and XML) for the entirety of a given run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The body genome ID data.</returns>
        public static IList<int> GetBodyGenomeIds(int experimentId, int run)
        {
            IList<int> bodyGenomeIds = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the body genome IDs
                    using (var context = new ExperimentDataContext())
                    {
                        bodyGenomeIds = context.MccexperimentVoxelBodyGenomes
                            .Where(expData => expData.ExperimentDictionaryId == experimentId && expData.Run == run)
                            .Select(x => x.GenomeId).ToList();
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
        ///     Retrieves the body genome IDs for the population extant during the current batch of a given run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch for which to get the extant population.</param>
        /// <returns>The body genome ID data.</returns>
        public static IList<int> GetBodyGenomeIds(int experimentId, int run, int batch)
        {
            IList<int> bodyGenomeIds = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the body genome IDs
                    using (var context = new ExperimentDataContext())
                    {
                        bodyGenomeIds = context.MccexperimentExtantVoxelBodyPopulation
                            .Where(expData =>
                                expData.ExperimentDictionaryId == experimentId && expData.Run == run &&
                                expData.Generation == batch).Select(x => x.GenomeId).ToList();
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
        public static IList<MccexperimentVoxelBrainGenome> GetBrainGenomeData(int experimentId, int run,
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

        /// <summary>
        ///     Retrieves all of the simulation log entries for the given experiment and run.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The list of body-brain simulation logs.</returns>
        public static IList<MccbodyBrainSimLog> GetSimulationLogs(int experimentId, int run)
        {
            IList<MccbodyBrainSimLog> simLogs = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (var context = new ExperimentDataContext())
                    {
                        // Get simulation logs for the given experiment and run
                        simLogs = context.MccbodyBrainSimLog.Where(sim =>
                            sim.ExperimentDictionaryId == experimentId && sim.Run == run).ToList();
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

            return simLogs;
        }

        /// <summary>
        ///     Retrieves all of the simulation log entries for the given experiment, run and batch.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch for which to get the extant population.</param>
        /// <returns>The list of body-brain simulation logs.</returns>
        public static IList<MccbodyBrainSimLog> GetSimulationLogs(int experimentId, int run, int batch)
        {
            IList<MccbodyBrainSimLog> simLogs = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (var context = new ExperimentDataContext())
                    {
                        // Get simulation logs for the given experiment, run and batch
                        simLogs = context.MccexperimentExtantVoxelBodyPopulation
                            .Where(popData =>
                                popData.ExperimentDictionaryId == experimentId && popData.Run == run &&
                                popData.Generation == batch)
                            .Join(
                                context.MccbodyBrainSimLog,
                                popData => new {popData.ExperimentDictionaryId, popData.Run, popData.GenomeId},
                                expData => new
                                    {expData.ExperimentDictionaryId, expData.Run, GenomeId = expData.BodyGenomeId},
                                (popData, expData) => new {popData, expData}).Select(x => x.expData).ToList();
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

            return simLogs;
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
                        timestepUnit.Time.ToString(CultureInfo.InvariantCulture),
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

        /// <summary>
        ///     Writes upscale results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="upscaleResultUnits">
        ///     The body/brain CPPN resolution increase (upscale) results that contains the maximum
        ///     resolution (size) at which the body is solvable by the paired brain.
        /// </param>
        public static void WriteUpscaleResultDataToFile(int experimentId, int run,
            IEnumerable<UpscaleResultUnit> upscaleResultUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.UpscaleResultData) == false)
            {
                throw new Exception(
                    $"Cannot write to output stream as no file writer of type {OutputFileType.SimulationLogData} has been created.");
            }

            // Write each upscale result as a separate entry
            foreach (var upscaleResultUnit in upscaleResultUnits)
            {
                FileWriters[OutputFileType.UpscaleResultData].WriteLine(string.Join(FileDelimiter, new List<string>
                {
                    experimentId.ToString(),
                    run.ToString(),
                    upscaleResultUnit.BodyId.ToString(),
                    upscaleResultUnit.BrainId.ToString(),
                    upscaleResultUnit.BaseSize.ToString(),
                    upscaleResultUnit.MaxSize.ToString()
                }));
            }
        }

        /// <summary>
        ///     Writes body diversity results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="bodyDiversityUnits">
        ///     The diversity of each body compared to the rest of the population in terms of its voxel placement.
        /// </param>
        public static void WriteRunBodyDiversityDataToFile(int experimentId, int run,
            IEnumerable<BodyDiversityUnit> bodyDiversityUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.RunBodyDiversityData) == false)
            {
                throw new Exception(
                    $"Cannot write to output stream as no file writer of type {OutputFileType.RunBodyDiversityData} has been created.");
            }

            // Write each run body diversity unit as a separate entry
            foreach (var bodyDiversityUnit in bodyDiversityUnits)
            {
                FileWriters[OutputFileType.RunBodyDiversityData].WriteLine(string.Join(FileDelimiter, new List<string>
                {
                    experimentId.ToString(),
                    run.ToString(),
                    bodyDiversityUnit.BodyId.ToString(),
                    bodyDiversityUnit.AvgVoxelDiff.ToString(CultureInfo.InvariantCulture),
                    bodyDiversityUnit.AvgVoxelMaterialDiff.ToString(CultureInfo.InvariantCulture),
                    bodyDiversityUnit.AvgVoxelActiveMaterialDiff.ToString(CultureInfo.InvariantCulture),
                    bodyDiversityUnit.AvgVoxelPassiveMaterialDiff.ToString(CultureInfo.InvariantCulture)
                }));
            }
        }

        /// <summary>
        ///     Writes body diversity results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The current batch in evolution.</param>
        /// <param name="bodyDiversityUnits">
        ///     The diversity of each body compared to the rest of the population (extant at the current batch) in terms of its
        ///     voxel placement.
        /// </param>
        public static void WriteBatchBodyDiversityDataToFile(int experimentId, int run, int batch,
            IEnumerable<BodyDiversityUnit> bodyDiversityUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.BatchBodyDiversityData) == false)
            {
                throw new Exception(
                    $"Cannot write to output stream as no file writer of type {OutputFileType.BatchBodyDiversityData} has been created.");
            }

            // Write each batch body diversity unit as a separate entry
            foreach (var bodyDiversityUnit in bodyDiversityUnits)
            {
                FileWriters[OutputFileType.BatchBodyDiversityData].WriteLine(string.Join(FileDelimiter, new List<string>
                {
                    experimentId.ToString(),
                    run.ToString(),
                    batch.ToString(),
                    bodyDiversityUnit.BodyId.ToString(),
                    bodyDiversityUnit.AvgVoxelDiff.ToString(CultureInfo.InvariantCulture),
                    bodyDiversityUnit.AvgVoxelMaterialDiff.ToString(CultureInfo.InvariantCulture),
                    bodyDiversityUnit.AvgVoxelActiveMaterialDiff.ToString(CultureInfo.InvariantCulture),
                    bodyDiversityUnit.AvgVoxelPassiveMaterialDiff.ToString(CultureInfo.InvariantCulture)
                }));
            }
        }

        /// <summary>
        ///     Writes trajectory diversity results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="trajectoryDiversityUnits">
        ///     The average trajectory distance compared to the rest of the population.
        /// </param>
        public static void WriteRunTrajectoryDiversityDataToFile(int experimentId, int run,
            IEnumerable<TrajectoryDiversityUnit> trajectoryDiversityUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.RunTrajectoryDiversityData) == false)
            {
                throw new Exception(
                    $"Cannot write to output stream as no file writer of type {OutputFileType.RunTrajectoryDiversityData} has been created.");
            }

            // Write each run trajectory diversity unit as a separate entry
            foreach (var trajectoryDiversityUnit in trajectoryDiversityUnits)
            {
                FileWriters[OutputFileType.RunTrajectoryDiversityData].WriteLine(string.Join(FileDelimiter,
                    new List<string>
                    {
                        experimentId.ToString(),
                        run.ToString(),
                        trajectoryDiversityUnit.BodyId.ToString(),
                        trajectoryDiversityUnit.BrainId.ToString(),
                        trajectoryDiversityUnit.TrajectoryDiversity.ToString(CultureInfo.InvariantCulture),
                        trajectoryDiversityUnit.EndPointDiversity.ToString(CultureInfo.InvariantCulture)
                    }));
            }
        }

        /// <summary>
        ///     Writes trajectory diversity results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The current batch in evolution.</param>
        /// <param name="trajectoryDiversityUnits">
        ///     The average trajectory distance compared to the rest of the  population (extant at the current batch).
        /// </param>
        public static void WriteBatchTrajectoryDiversityDataToFile(int experimentId, int run, int batch,
            IEnumerable<TrajectoryDiversityUnit> trajectoryDiversityUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.BatchTrajectoryDiversityData) == false)
            {
                throw new Exception(
                    $"Cannot write to output stream as no file writer of type {OutputFileType.BatchTrajectoryDiversityData} has been created.");
            }

            // Write each batch trajectory diversity unit as a separate entry
            foreach (var trajectoryDiversityUnit in trajectoryDiversityUnits)
            {
                FileWriters[OutputFileType.BatchTrajectoryDiversityData].WriteLine(string.Join(FileDelimiter,
                    new List<string>
                    {
                        experimentId.ToString(),
                        run.ToString(),
                        batch.ToString(),
                        trajectoryDiversityUnit.BodyId.ToString(),
                        trajectoryDiversityUnit.BrainId.ToString(),
                        trajectoryDiversityUnit.TrajectoryDiversity.ToString(CultureInfo.InvariantCulture),
                        trajectoryDiversityUnit.EndPointDiversity.ToString(CultureInfo.InvariantCulture)
                    }));
            }
        }

        #endregion
    }
}