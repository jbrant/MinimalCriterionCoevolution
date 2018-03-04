#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ExperimentEntities;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace MazeExperimentSupportLib
{
    /// <summary>
    ///     Provides methods for interfacing with and writing reuslts to the experiment database or flat file.
    /// </summary>
    public static class ExperimentDataHandler
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

        #region Public generic writer methods

        /// <summary>
        ///     Writes the given evaluation results to the experiment database or to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        /// <param name="commitPageSize">The number of records that are committed within a single batch/context.</param>
        /// <param name="writeToDatabase">
        ///     Indicates whether evaluation results should be written directly to the database or to a
        ///     flat file.
        /// </param>
        public static void WriteNavigatorMazeEvaluationData(int experimentId, int run, int batch, RunPhase runPhase,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int commitPageSize, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                WriteNavigatorMazeEvaluationDataToDatabase(experimentId, run, batch, runPhase, evaluationUnits,
                    commitPageSize);
            }
            // Otherwise, write to the flat file output
            else
            {
                WriteNavigatorMazeEvaluationDataToFile(experimentId, run, batch, runPhase, evaluationUnits);
            }
        }

        /// <summary>
        ///     Writes the given evaluation results containing the full trajectory characterization to the experiment database or
        ///     to a flat file.
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
        public static void WriteTrajectoryData(int experimentId, int run, int batch,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int commitPageSize, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                WriteTrajectoryDataToDatabase(experimentId, run, batch, evaluationUnits, commitPageSize);
            }
            // Otherwise, write to the flat file output
            WriteTrajectoryDataToFile(experimentId, run, batch, evaluationUnits);
        }

        /// <summary>
        ///     Writes the given trajectory diversity results to the experiment database or to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="trajectoryDiversityUnits">The trajectory diversity results.</param>
        /// <param name="writeToDatabase">
        ///     Indicates whether evaluation results should be written directly to the database or to a
        ///     flat file.
        /// </param>
        public static void WriteTrajectoryDiversityData(int experimentId, int run,
            IList<TrajectoryDiversityUnit> trajectoryDiversityUnits, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                throw new NotImplementedException(
                    "Direct write to database for trajectory diversity not yet implemented!");
            }
            // Otherwise, write to the flat file output
            WriteTrajectoryDiversityDataToFile(experimentId, run, trajectoryDiversityUnits);
        }

        /// <summary>
        ///     Writes the given trajectory diversity results to the experiment database or to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="trajectoryDiversityUnits">The trajectory diversity results.</param>
        /// <param name="writeToDatabase">
        ///     Indicates whether evaluation results should be written directly to the database or to a
        ///     flat file.
        /// </param>
        public static void WriteTrajectoryDiversityData(int experimentId, int run, int batch,
            IList<TrajectoryDiversityUnit> trajectoryDiversityUnits, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                throw new NotImplementedException(
                    "Direct write to database for trajectory diversity not yet implemented!");
            }
            // Otherwise, write to the flat file output
            WriteTrajectoryDiversityDataToFile(experimentId, run, batch, trajectoryDiversityUnits);
        }

        /// <summary>
        ///     Writes the cluster diversity results to the experiment database or to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="clusterDiversityUnit">The cluster diversity results.</param>
        /// <param name="writeToDatabase">
        ///     Indicates whether evaluation results should be written directly to the database or to a
        ///     flat file.
        /// </param>
        public static void WriteClusteringDiversityData(int experimentId, int run,
            ClusterDiversityUnit clusterDiversityUnit, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                throw new NotImplementedException(
                    "Direct write to database for clustering diversity not yet implemented!");
            }
            // Otherwise, write to the flat file output
            WriteClusteringDiversityDataToFile(experimentId, run, clusterDiversityUnit);
        }

        /// <summary>
        ///     Writes the cluster diversity results to the experiment database or to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="clusterDiversityUnit">The cluster diversity results.</param>
        /// <param name="clusteringOutputType">The type of clustering output (e.g. agent trajectory maze).</param>
        /// <param name="writeToDatabase">
        ///     Indicates whether evaluation results should be written directly to the database or to a
        ///     flat file.
        /// </param>
        public static void WriteClusteringDiversityData(int experimentId, int run, int batch,
            ClusterDiversityUnit clusterDiversityUnit, OutputFileType clusteringOutputType, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                throw new NotImplementedException(
                    "Direct write to database for clustering diversity not yet implemented!");
            }
            // Otherwise, write to the flat file output
            WriteClusteringDiversityDataToFile(experimentId, run, batch, clusterDiversityUnit, clusteringOutputType);
        }

        /// <summary>
        ///     Writes the population entropy results to the experiment database or to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="populationEntropyUnit">The population entropy results.</param>
        /// <param name="writeToDatabase">
        ///     Indicates whether evaluation results should be written directly to the database or to a
        ///     flat file.
        /// </param>
        public static void WritePopulationEntropyData(int experimentId, int run, int batch,
            PopulationEntropyUnit populationEntropyUnit, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                throw new NotImplementedException(
                    "Direct write to database for population entropy not yet implemented!");
            }
            // Otherwise, write to the flat file output
            WritePopulationEntropyDataToFile(experimentId, run, batch, populationEntropyUnit);
        }

        /// <summary>
        ///     Writes the coevolution vs. novelty search comparison results to the experiment database or to a flat file.
        /// </summary>
        /// <param name="coEvoExperimentId">The coevolution experiment that was executed.</param>
        /// <param name="nsExperimentId">The novelty search experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="mazeGenomeId">
        ///     The unique identifier of the maze domain on which the novelty search comparison was
        ///     executed.
        /// </param>
        /// <param name="mazeBirthBatch">The birth batch (generation) of the maze.</param>
        /// <param name="nsAgentMinComplexity">The minimum complexity of the novelty search population at the end of the run.</param>
        /// <param name="nsAgentMaxComplexity">The maximum complexity of the novelty search population at the end of the run.</param>
        /// <param name="nsAgentMeanComplexity">The mean complexity of the novelty search population at the end of the run.</param>
        /// <param name="coEvoEvaluations">
        ///     The number of evaluations executed by the coevolution algorithm in order to arrive at
        ///     the given maze structure.
        /// </param>
        /// <param name="nsEvaluations">
        ///     The total number of evaluations executed by the novelty search algorithm in order to solve
        ///     (or attempt to solve) the coevolution-discovered maze structure.
        /// </param>
        /// <param name="isSolved">Flag indicating whether novelty search was successful in solving the maze.</param>
        /// <param name="writeToDatabase">Flag indicating whether to write directly into the experiment database or to a flat file.</param>
        public static void WriteNoveltySearchComparisonResults(int coEvoExperimentId, int nsExperimentId, int run,
            int batch, int mazeGenomeId,
            int mazeBirthBatch, int nsAgentMinComplexity, int nsAgentMaxComplexity, double nsAgentMeanComplexity,
            int coEvoEvaluations, int nsEvaluations, bool isSolved, bool writeToDatabase)
        {
            // Write results to the database if the option has been specified
            if (writeToDatabase)
            {
                throw new NotImplementedException(
                    "Direct write to database for novelty search comparison not yet implemented!");
            }
            // Otherwise, write to the flat file output
            WriteNoveltySearchComparisonResultsToFile(coEvoExperimentId, nsExperimentId, run, batch, mazeGenomeId,
                mazeBirthBatch, nsAgentMinComplexity, nsAgentMaxComplexity, nsAgentMeanComplexity, coEvoEvaluations,
                nsEvaluations, isSolved);
        }

        #endregion

        #region Public database read methods

        /// <summary>
        ///     Looks up an experiment configuration given the unique experiment name.
        /// </summary>
        /// <param name="experimentName">The experiment name whose configuration to lookup.</param>
        /// <returns>The corresponding experiment configuration (i.e. experiment dictionary).</returns>
        public static ExperimentDictionary LookupExperimentConfiguration(string experimentName)
        {
            ExperimentDictionary experimentConfiguration = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the experiment configuration given the name (which is guaranteed to be unique)
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        experimentConfiguration =
                            context.ExperimentDictionaries.Single(expName => expName.ExperimentName == experimentName);
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
        ///     Retrieves the number of runs that were executed for a given experiment.
        /// </summary>
        /// <param name="experimentId">The experiment for which to compute the number of corresponding runs.</param>
        /// <returns>The number of runs that were executed for the given experiment.</returns>
        public static int GetNumRuns(int experimentId)
        {
            int numRuns = 0;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the number of runs for the given experiment
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        numRuns = context.MCCExperimentMazeEvaluationDatas.Where(
                            expData => expData.ExperimentDictionaryID == experimentId)
                            .Select(row => row.Run)
                            .Distinct()
                            .Count();
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

            return numRuns;
        }

        /// <summary>
        ///     Retrieves the number of batches that were executed during a given run of a given experiment for the specified run
        ///     phase.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="runPhase">The run phase (i.e. initialization or primary) for which to get the associated batches.</param>
        /// <returns>The number of batches in the given run/run phase.</returns>
        public static int GetNumBatchesForRun(int experimentId, int run, RunPhase runPhase)
        {
            int numBatches = 0;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the number of batches in the current run of the given experiment
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        // Only attempt to get max if batches if records exist for experiment, run, and run phase
                        if (
                            context.MCCExperimentNavigatorEvaluationDatas.Any(
                                expData => expData.ExperimentDictionaryID == experimentId && expData.Run == run &&
                                           expData.RunPhase.RunPhaseName == runPhase.ToString()))
                        {
                            numBatches = context.MCCExperimentNavigatorEvaluationDatas.Where(
                                expData =>
                                    expData.ExperimentDictionaryID == experimentId && expData.Run == run &&
                                    expData.RunPhase.RunPhaseName == runPhase.ToString()).Max(row => row.Generation);
                        }
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

            return numBatches;
        }

        /// <summary>
        ///     Retrieves the maze genome IDs for a particular run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The maze genome IDs.</returns>
        public static IList<int> GetMazeGenomeIds(int experimentId, int run)
        {
            IList<int> mazeGenomeIds = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the distinct maze genome IDs logged during the run
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        mazeGenomeIds =
                            context.MCCExperimentMazeGenomes.Where(
                                expData => expData.ExperimentDictionaryID == experimentId && expData.Run == run)
                                .Select(m => m.GenomeID)
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

            return mazeGenomeIds;
        }

        /// <summary>
        ///     Retrieves the maze genome XML for a particular batch of a given run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <returns>The maze genome XML.</returns>
        public static IList<string> GetMazeGenomeXml(int experimentId, int run, int batch)
        {
            return GetMazeGenomeData(experimentId, run, batch).Select(mazeGenome => mazeGenome.GenomeXml).ToList();
        }

        /// <summary>
        ///     Retrieves the distinct maze genome data between the given start and end batch.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="startBatch">The starting batch.</param>
        /// <param name="endBatch">The ending batch.</param>
        /// <param name="mazeGenomeIdsInclusive">The maze genome IDs by which to filter the query result set (optional).</param>
        /// <param name="mazeGenomeIdsExclusive">The maze genome IDs to exclude from the query result set (optional).</param>
        /// <returns>The distinct maze genomes between the start and end batch.</returns>
        public static IList<MCCExperimentMazeGenome> GetMazeGenomeData(int experimentId, int run, int startBatch,
            int endBatch, IList<int> mazeGenomeIdsInclusive = null, IList<int> mazeGenomeIdsExclusive = null)
        {
            List<MCCExperimentMazeGenome> mazeGenomes = new List<MCCExperimentMazeGenome>();

            // Get all maze genome objects between the start and end batch
            for (int curBatch = startBatch; curBatch <= endBatch; curBatch++)
            {
                mazeGenomes.AddRange(GetMazeGenomeData(experimentId, run, curBatch, mazeGenomeIdsInclusive,
                    mazeGenomes.Count != 0
                        ? mazeGenomeIdsExclusive?.Concat(mazeGenomes.Select(g => g.GenomeID)).ToList() ??
                          mazeGenomes.Select(g => g.GenomeID).ToList()
                        : mazeGenomeIdsExclusive));
            }

            return mazeGenomes;
        }

        /// <summary>
        ///     Retrieves the maze genome data (i.e. evaluation statistics and XML) for the entirety of a given run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The maze genome data.</returns>
        public static IList<MCCExperimentMazeGenome> GetMazeGenomeData(int experimentId, int run)
        {
            IList<MCCExperimentMazeGenome> mazeGenomes = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the distinct maze genomes logged during the run
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        mazeGenomes =
                            context.MCCExperimentMazeGenomes.Where(
                                expData => expData.ExperimentDictionaryID == experimentId && expData.Run == run)
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

            return mazeGenomes;
        }

        /// <summary>
        ///     Retrieves the maze genome data (i.e. evaluation statistics and XML) for the entirety of a given run/experiment,
        ///     constrained by the specified genome IDs.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="mazeGenomeIds">The maze genome IDs by which to constrain.</param>
        /// <returns>The maze genome data.</returns>
        public static IList<MCCExperimentMazeGenome> GetMazeGenomeData(int experimentId, int run,
            IList<int> mazeGenomeIds)
        {
            IList<MCCExperimentMazeGenome> mazeGenomes = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the maze genomes corresponding to the specified genome IDs
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        mazeGenomes =
                            context.MCCExperimentMazeGenomes.Where(
                                expData =>
                                    expData.ExperimentDictionaryID == experimentId && expData.Run == run &&
                                    mazeGenomeIds.Contains(expData.GenomeID))
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

            return mazeGenomes;
        }

        /// <summary>
        ///     Retrieves the maze genome data (i.e. evaluation statistics and XML) for a particular batch of a given
        ///     run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="mazeGenomeIdsInclusive">The maze genome IDs by which to filter the query result set (optional).</param>
        /// <param name="mazeGenomeIdsExclusive">The maze genome IDs to exclude from the query result set (optional).</param>
        /// <returns>The maze genome data.</returns>
        public static IList<MCCExperimentMazeGenome> GetMazeGenomeData(int experimentId, int run, int batch,
            IList<int> mazeGenomeIdsInclusive = null, IList<int> mazeGenomeIdsExclusive = null)
        {
            IList<MCCExperimentMazeGenome> mazeGenomes = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        // Query for maze genomes logged during the current batch with the specified genome IDs included and filtered out respectively
                        if (mazeGenomeIdsInclusive != null && mazeGenomeIdsExclusive != null)
                        {
                            mazeGenomes =
                                context.MCCExperimentExtantMazePopulations.Where(
                                    popData =>
                                        mazeGenomeIdsInclusive.Contains(popData.GenomeID) &&
                                        mazeGenomeIdsExclusive.Contains(popData.GenomeID) == false &&
                                        popData.ExperimentDictionaryID == experimentId && popData.Run == run &&
                                        popData.Generation == batch)
                                    .Join(context.MCCExperimentMazeGenomes,
                                        popData => new {popData.ExperimentDictionaryID, popData.Run, popData.GenomeID},
                                        expData => new {expData.ExperimentDictionaryID, expData.Run, expData.GenomeID},
                                        (popData, expData) => new {popData, expData})
                                    .Select(data => data.expData)
                                    .ToList();
                        }

                        // Query for maze genomes logged during the current batch and constrained by the given set of genome IDs
                        else if (mazeGenomeIdsInclusive != null)
                        {
                            mazeGenomes =
                                context.MCCExperimentExtantMazePopulations.Where(
                                    popData =>
                                        mazeGenomeIdsInclusive.Contains(popData.GenomeID) &&
                                        popData.ExperimentDictionaryID == experimentId && popData.Run == run &&
                                        popData.Generation == batch)
                                    .Join(context.MCCExperimentMazeGenomes,
                                        popData => new {popData.ExperimentDictionaryID, popData.Run, popData.GenomeID},
                                        expData => new {expData.ExperimentDictionaryID, expData.Run, expData.GenomeID},
                                        (popData, expData) => new {popData, expData})
                                    .Select(data => data.expData)
                                    .ToList();
                        }

                        // Query for maze genomes logged during the current batch with the specified genome IDs filtered out
                        else if (mazeGenomeIdsExclusive != null)
                        {
                            mazeGenomes =
                                context.MCCExperimentExtantMazePopulations.Where(
                                    popData =>
                                        mazeGenomeIdsExclusive.Contains(popData.GenomeID) == false &&
                                        popData.ExperimentDictionaryID == experimentId && popData.Run == run &&
                                        popData.Generation == batch)
                                    .Join(context.MCCExperimentMazeGenomes,
                                        popData => new {popData.ExperimentDictionaryID, popData.Run, popData.GenomeID},
                                        expData => new {expData.ExperimentDictionaryID, expData.Run, expData.GenomeID},
                                        (popData, expData) => new {popData, expData})
                                    .Select(data => data.expData)
                                    .ToList();
                        }

                        // Otherwise, query for all maze genomes logged during the current batch
                        else
                        {
                            mazeGenomes =
                                context.MCCExperimentExtantMazePopulations.Where(
                                    popData =>
                                        popData.ExperimentDictionaryID == experimentId && popData.Run == run &&
                                        popData.Generation == batch)
                                    .Join(context.MCCExperimentMazeGenomes,
                                        popData => new {popData.ExperimentDictionaryID, popData.Run, popData.GenomeID},
                                        expData => new {expData.ExperimentDictionaryID, expData.Run, expData.GenomeID},
                                        (popData, expData) => new {popData, expData})
                                    .Select(data => data.expData)
                                    .ToList();
                        }
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

            return mazeGenomes;
        }

        /// <summary>
        ///     Retrieves the distinct navigator genome data between the given start and end batch.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="startBatch">The starting batch.</param>
        /// <param name="endBatch">The ending batch.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <param name="navigatorGenomeIdsInclusive">The navigator genome IDs by which to filter the query result set (optional).</param>
        /// <param name="navigatorGenomeIdsExclusive">The navigator genome IDs to exclude from the query result set (optional).</param>
        /// <returns>The distinct navigator genomes between the start and end batch.</returns>
        public static IList<MCCExperimentNavigatorGenome> GetNavigatorGenomeData(int experimentId, int run,
            int startBatch, int endBatch, RunPhase runPhase, IList<int> navigatorGenomeIdsInclusive = null,
            IList<int> navigatorGenomeIdsExclusive = null)
        {
            List<MCCExperimentNavigatorGenome> navigatorGenomes = new List<MCCExperimentNavigatorGenome>();

            // Get all navigator genome objects between the start and end batch
            for (int curBatch = startBatch; curBatch <= endBatch; curBatch++)
            {
                navigatorGenomes.AddRange(GetNavigatorGenomeData(experimentId, run, curBatch, runPhase,
                    navigatorGenomeIdsInclusive,
                    navigatorGenomes.Count != 0
                        ? navigatorGenomeIdsExclusive?.Concat(navigatorGenomes.Select(g => g.GenomeID)).ToList() ??
                          navigatorGenomes.Select(g => g.GenomeID).ToList()
                        : navigatorGenomeIdsExclusive));
            }

            return navigatorGenomes;
        }

        /// <summary>
        ///     Retrieves the navigator genome data (i.e. evaluation statistics and XML) for the entirety of a given
        ///     run/experiment.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <returns>The navigator genome data.</returns>
        public static IList<MCCExperimentNavigatorGenome> GetNavigatorGenomeData(int experimentId, int run,
            RunPhase runPhase)
        {
            IList<MCCExperimentNavigatorGenome> navigatorGenomes = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the distinct navigator genomes logged during the run
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        navigatorGenomes =
                            context.MCCExperimentNavigatorGenomes.Where(
                                expData =>
                                    expData.ExperimentDictionaryID == experimentId && expData.Run == run &&
                                    expData.RunPhase.RunPhaseName == runPhase.ToString()).ToList();
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

            return navigatorGenomes;
        }

        /// <summary>
        ///     Retrieves the navigator genome data (i.e. evaluation statistics and XML) for the entirety of a given
        ///     run/experiment, constrained by the specified genome IDs.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <param name="navigatorGenomeIds">The navigator genome IDs by which to constrain.</param>
        /// <returns>The navigator genome data.</returns>
        public static IList<MCCExperimentNavigatorGenome> GetNavigatorGenomeData(int experimentId, int run,
            RunPhase runPhase, IList<int> navigatorGenomeIds)
        {
            IList<MCCExperimentNavigatorGenome> navigatorGenomes = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the navigator genomes corresponding to the specified genome IDs
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        navigatorGenomes =
                            context.MCCExperimentNavigatorGenomes.Where(
                                expData =>
                                    expData.ExperimentDictionaryID == experimentId && expData.Run == run &&
                                    expData.RunPhase.RunPhaseName == runPhase.ToString() &&
                                    navigatorGenomeIds.Contains(expData.GenomeID)).ToList();
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

            return navigatorGenomes;
        }

        /// <summary>
        ///     Retrieves the navigator genome data (i.e. evaluation statistics and XML) for a particular batch of a given
        ///     run/experiemnt.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <param name="navigatorGenomeIdsInclusive">The navigator genome IDs by which to filter the query result set (optional).</param>
        /// <param name="navigatorGenomeIdsExclusive">The navigator genome IDs to exclude from the query result set (optional).</param>
        /// <returns>The navigator genome data.</returns>
        public static IList<MCCExperimentNavigatorGenome> GetNavigatorGenomeData(int experimentId, int run,
            int batch, RunPhase runPhase, IList<int> navigatorGenomeIdsInclusive = null,
            IList<int> navigatorGenomeIdsExclusive = null)
        {
            IList<MCCExperimentNavigatorGenome> navigatorGenomes = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        // Query for navigator genomes logged during the current batch with the specified genome IDs included and filtered out respectively
                        if (navigatorGenomeIdsInclusive != null && navigatorGenomeIdsExclusive != null)
                        {
                            navigatorGenomes =
                                context.MCCExperimentExtantNavigatorPopulations.Where(
                                    popData =>
                                        navigatorGenomeIdsInclusive.Contains(popData.GenomeID) &&
                                        navigatorGenomeIdsExclusive.Contains(popData.GenomeID) == false &&
                                        popData.ExperimentDictionaryID == experimentId && popData.Run == run &&
                                        popData.Generation == batch)
                                    .Join(context.MCCExperimentNavigatorGenomes,
                                        popData => new {popData.ExperimentDictionaryID, popData.Run, popData.GenomeID},
                                        expData => new {expData.ExperimentDictionaryID, expData.Run, expData.GenomeID},
                                        (popData, expData) => new {popData, expData})
                                    .Select(data => data.expData)
                                    .ToList();
                        }

                        // Query for navigator genomes logged during the current batch and constrained by the given set of genome IDs
                        else if (navigatorGenomeIdsInclusive != null)
                        {
                            navigatorGenomes =
                                context.MCCExperimentExtantNavigatorPopulations.Where(
                                    popData =>
                                        navigatorGenomeIdsInclusive.Contains(popData.GenomeID) &&
                                        popData.ExperimentDictionaryID == experimentId && popData.Run == run &&
                                        popData.Generation == batch)
                                    .Join(context.MCCExperimentNavigatorGenomes,
                                        popData => new {popData.ExperimentDictionaryID, popData.Run, popData.GenomeID},
                                        expData => new {expData.ExperimentDictionaryID, expData.Run, expData.GenomeID},
                                        (popData, expData) => new {popData, expData})
                                    .Select(data => data.expData)
                                    .ToList();
                        }

                        // Query for navigator genomes logged during the current batch with the specified genome IDs filtered out
                        else if (navigatorGenomeIdsExclusive != null)
                        {
                            navigatorGenomes =
                                context.MCCExperimentExtantNavigatorPopulations.Where(
                                    popData =>
                                        navigatorGenomeIdsExclusive.Contains(popData.GenomeID) == false &&
                                        popData.ExperimentDictionaryID == experimentId && popData.Run == run &&
                                        popData.Generation == batch)
                                    .Join(context.MCCExperimentNavigatorGenomes,
                                        popData => new {popData.ExperimentDictionaryID, popData.Run, popData.GenomeID},
                                        expData => new {expData.ExperimentDictionaryID, expData.Run, expData.GenomeID},
                                        (popData, expData) => new {popData, expData})
                                    .Select(data => data.expData)
                                    .ToList();
                        }

                        // Otherwise, query for all navigator genomes logged during the current batch
                        else
                        {
                            navigatorGenomes =
                                context.MCCExperimentExtantNavigatorPopulations.Where(
                                    popData =>
                                        popData.ExperimentDictionaryID == experimentId && popData.Run == run &&
                                        popData.Generation == batch)
                                    .Join(context.MCCExperimentNavigatorGenomes,
                                        popData => new {popData.ExperimentDictionaryID, popData.Run, popData.GenomeID},
                                        expData => new {expData.ExperimentDictionaryID, expData.Run, expData.GenomeID},
                                        (popData, expData) => new {popData, expData})
                                    .Select(data => data.expData)
                                    .ToList();
                        }
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

            return navigatorGenomes;
        }

        /// <summary>
        ///     Retrieves the total number of initialization evaluations that were performed for the given experiment run.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <returns>The total number of initialization evaluations.</returns>
        public static int GetInitializationEvaluationsForRun(int experimentId, int run)
        {
            int initEvaluations = 0;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the maximum initialization evaluations
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        initEvaluations =
                            context.MCCExperimentNavigatorEvaluationDatas.Where(
                                navigatorData =>
                                    navigatorData.ExperimentDictionaryID == experimentId && navigatorData.Run == run &&
                                    navigatorData.RunPhase.RunPhaseName == RunPhase.Initialization.ToString())
                                .Max(navigatorData => navigatorData.TotalEvaluations);
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

            return initEvaluations;
        }

        /// <summary>
        ///     Retrieves the total number of primary evaluations executes by mazes and navigators at the given experiment
        ///     run/batch.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <returns>The total number of primary evaluations from both the maze and navigator queues.</returns>
        public static int GetTotalPrimaryEvaluationsAtBatch(int experimentId, int run, int batch)
        {
            int mazeEvaluations = 0, navigatorEvaluations = 0;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        // Get the total maze evaluations at the given batch
                        mazeEvaluations =
                            context.MCCExperimentMazeEvaluationDatas.Where(
                                mazeData =>
                                    mazeData.ExperimentDictionaryID == experimentId && mazeData.Run == run &&
                                    mazeData.Generation == batch).Select(mazeData => mazeData.TotalEvaluations).First();

                        // Get the total navigator evaluations at the given batch
                        navigatorEvaluations =
                            context.MCCExperimentNavigatorEvaluationDatas.Where(
                                navigatorData =>
                                    navigatorData.ExperimentDictionaryID == experimentId && navigatorData.Run == run &&
                                    navigatorData.Generation == batch &&
                                    navigatorData.RunPhase.RunPhaseName == RunPhase.Primary.ToString())
                                .Select(navigatorData => navigatorData.TotalEvaluations)
                                .Single();
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

            return mazeEvaluations + navigatorEvaluations;
        }

        /// <summary>
        ///     Retrieves the list of maze/navigator combinations that were successful for the given experiment, run, and batch.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <returns>The list of maze/navigator combinations that were successful for the given experiment, run, and batch.</returns>
        public static IList<MCCMazeNavigatorResult> GetSuccessfulNavigations(int experimentId, int run,
            int batch)
        {
            IList<MCCMazeNavigatorResult> navigationResults = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        // Get only the combination of navigators and mazes that resulted in a successful navigation
                        navigationResults = context.MCCMazeNavigatorResults.Where(
                            nav =>
                                experimentId == nav.ExperimentDictionaryID && run == nav.Run && batch == nav.Generation &&
                                RunPhase.Primary.ToString().Equals(nav.RunPhase.RunPhaseName) && nav.IsMazeSolved)
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

            return navigationResults;
        }

        /// <summary>
        ///     For each maze, retrieves the first navigator that solved within the given experiment and run.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="mazeGenomeIds">The list of maze genome IDs by which to filter the navigation results.</param>
        /// <returns>The list of distinct maze/navigator combinations that were successful for the given experiment and run.</returns>
        public static IList<MCCMazeNavigatorResult> GetSuccessfulNavigationPerMaze(int experimentId, int run,
            IList<int> mazeGenomeIds)
        {
            IList<MCCMazeNavigatorResult> navigationResults = null;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        // Get single navigation result for each of the specified maze IDs over the entirety of the run
                        navigationResults = context.MCCMazeNavigatorResults.Where(
                            nav =>
                                experimentId == nav.ExperimentDictionaryID && run == nav.Run &&
                                RunPhase.Primary.ToString().Equals(nav.RunPhase.RunPhaseName) &&
                                nav.IsMazeSolved && mazeGenomeIds.Contains(nav.MazeGenomeID))
                            .GroupBy(nav => nav.MazeGenomeID)
                            .Select(m => m.OrderBy(x => x.MazeGenomeID).FirstOrDefault())
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

            return navigationResults;
        }

        /// <summary>
        ///     Retrieves specie assignments for the given maze genome IDs and experiment, run, and batch.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="mazeGenomeIds">The maze genome IDs to group into species.</param>
        /// <returns>Specie assignments for the given maze genome IDs and experiment, run, and batch.</returns>
        public static List<SpecieGenomesGroup> GetSpecieAssignmentsForMazeGenomeIds(int experimentId, int run, int batch,
            IList<int> mazeGenomeIds)
        {
            var mazeSpecieGenomesGroups = new List<SpecieGenomesGroup>();
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        // Get the species to which the mazes are assigned and group by species
                        var specieGroupedMazes =
                            context.MCCExperimentExtantMazePopulations.Where(
                                maze =>
                                    experimentId == maze.ExperimentDictionaryID && run == maze.Run &&
                                    batch == maze.Generation && mazeGenomeIds.Contains(maze.GenomeID))
                                .Select(maze => new {maze.SpecieID, maze.GenomeID})
                                .GroupBy(maze => maze.SpecieID)
                                .ToList();

                        // Build list of maze specie genome groups
                        mazeSpecieGenomesGroups.AddRange(
                            specieGroupedMazes.Select(
                                specieGenomesGroup =>
                                    specieGenomesGroup.Key != null
                                        ? new SpecieGenomesGroup((int) specieGenomesGroup.Key,
                                            specieGenomesGroup.Select(gg => gg.GenomeID).ToList())
                                        : new SpecieGenomesGroup()));
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

            return mazeSpecieGenomesGroups;
        }

        /// <summary>
        ///     Retrieves specie assignments for the given navigator genome IDs and experiment, run, and batch.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="runPhase">Indicates whether this is part of the initialization or primary experiment phase.</param>
        /// <param name="navigatorGenomeIds">The navigator genome IDs to group into species.</param>
        /// <returns>Specie assignments for the given navigator genome IDs and experiment, run, and batch.</returns>
        public static List<SpecieGenomesGroup> GetSpecieAssignmentsForNavigatorGenomeIds(int experimentId, int run,
            int batch, RunPhase runPhase, IList<int> navigatorGenomeIds)
        {
            var navigatorSpecieGenomesGroups = new List<SpecieGenomesGroup>();
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        // Get the species to which the navigators are assigned and group by species
                        var specieGroupedNavigators =
                            context.MCCExperimentExtantNavigatorPopulations.Where(
                                nav =>
                                    experimentId == nav.ExperimentDictionaryID && run == nav.Run &&
                                    batch == nav.Generation && runPhase.ToString().Equals(nav.RunPhase.RunPhaseName) &&
                                    navigatorGenomeIds.Contains(nav.GenomeID))
                                .Select(nav => new {nav.SpecieID, nav.GenomeID})
                                .GroupBy(nav => nav.SpecieID)
                                .ToList();

                        // Build list of navigator specie genome groups
                        navigatorSpecieGenomesGroups.AddRange(
                            specieGroupedNavigators.Select(
                                specieGenomesGroup =>
                                    specieGenomesGroup.Key != null
                                        ? new SpecieGenomesGroup((int) specieGenomesGroup.Key,
                                            specieGenomesGroup.Select(gg => gg.GenomeID).ToList())
                                        : new SpecieGenomesGroup()));
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

            return navigatorSpecieGenomesGroups;
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
                throw new Exception(string.Format("File writer for type {0} already opened.", fileType));
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
                throw new Exception(
                    string.Format("Cannot close file writer as no file writer of type {0} has been created.", fileType));
            }

            // Close the file writer and dispose of the stream
            FileWriters[fileType].Close();
            FileWriters[fileType].Dispose();
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

        /// <summary>
        ///     Writes empty "sentinel" file so that data transfer agent on cluster can easily identify that a given experiment run
        ///     is complete.
        /// </summary>
        /// <param name="experimentFilename">The base path and filename of the experiment under execution.</param>
        /// <param name="run">The completed run.</param>
        public static void WriteSentinelFile(string experimentFilename, int run)
        {
            // Write sentinel file to the given output directory
            using (File.Create(string.Format("{0} - Run {1} - COMPLETE", experimentFilename, run)))
            {
            }
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
                typeof (ExperimentDataHandler).FullName, methodName, retryCnt);
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
                typeof (ExperimentDataHandler).FullName, methodName, retryCnt);

            // Throw exception if we've no exceeded the retry count
            if (retryCnt + 1 > MaxQueryRetryCnt)
            {
                throw e;
            }
        }

        /// <summary>
        ///     Retrieves the primary key associated with the given run phase.
        /// </summary>
        /// <param name="runPhase">The run phase for which to lookup the key.</param>
        /// <returns>The key (in the "RunPhase" database table) for the given run phase object.</returns>
        private static int GetRunPhaseKey(RunPhase runPhase)
        {
            int runPhaseKey = 0;
            bool querySuccess = false;
            int retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the run phase key
                    using (ExperimentDataEntities context = new ExperimentDataEntities())
                    {
                        runPhaseKey =
                            context.RunPhases.First(runPhaseData => runPhaseData.RunPhaseName == runPhase.ToString())
                                .RunPhaseID;
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

            return runPhaseKey;
        }

        /// <summary>
        ///     Writes the given evaluation results to the experiment database.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        /// <param name="commitPageSize">The number of records that are committed within a single batch/context.</param>
        private static void WriteNavigatorMazeEvaluationDataToDatabase(int experimentId, int run, int batch,
            RunPhase runPhase,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int commitPageSize)
        {
            // Page through the result set, committing each in the specified batch size
            for (int curPage = 0; curPage <= evaluationUnits.Count/commitPageSize; curPage++)
            {
                IList<MCCMazeNavigatorResult> serializedResults =
                    new List<MCCMazeNavigatorResult>(commitPageSize);

                // Go ahead and lookup the run phase key for all of the records
                // (instead of hitting the database on every iteration of the below loop)
                int runPhaseKey = GetRunPhaseKey(runPhase);

                // Build a list of serialized results
                foreach (
                    MazeNavigatorEvaluationUnit evaluationUnit in
                        evaluationUnits.Skip(curPage*commitPageSize).Take(commitPageSize))
                {
                    serializedResults.Add(new MCCMazeNavigatorResult
                    {
                        ExperimentDictionaryID = experimentId,
                        Run = run,
                        Generation = batch,
                        RunPhase_FK = runPhaseKey,
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

                    context.MCCMazeNavigatorResults.AddRange(serializedResults);
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        ///     Writes the given evaluation results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        private static void WriteNavigatorMazeEvaluationDataToFile(int experimentId, int run, int batch,
            RunPhase runPhase, IList<MazeNavigatorEvaluationUnit> evaluationUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.NavigatorMazeEvaluationData) == false)
            {
                throw new Exception(
                    string.Format("Cannot write to output stream as no file writer of type {0} has been created.",
                        OutputFileType.NavigatorMazeEvaluationData));
            }

            // Loop through the evaluation units and write each row
            foreach (MazeNavigatorEvaluationUnit evaluationUnit in evaluationUnits)
            {
                FileWriters[OutputFileType.NavigatorMazeEvaluationData].WriteLine(string.Join(FileDelimiter,
                    new List<string>
                    {
                        experimentId.ToString(),
                        run.ToString(),
                        batch.ToString(),
                        runPhase.ToString(),
                        evaluationUnit.MazeId.ToString(),
                        evaluationUnit.AgentId.ToString(),
                        evaluationUnit.IsMazeSolved.ToString(),
                        evaluationUnit.NumTimesteps.ToString()
                    }));
            }

            // Immediately flush to the output file
            FileWriters[OutputFileType.NavigatorMazeEvaluationData].Flush();
        }

        /// <summary>
        ///     Writes each position within each trajectory to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        private static void WriteTrajectoryDataToFile(int experimentId, int run, int batch,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.TrajectoryData) == false)
            {
                throw new Exception(
                    string.Format("Cannot write to output stream as no file writer of type {0} has been created.",
                        OutputFileType.TrajectoryData));
            }

            // Loop through evaluation units and through each trajectory unit and write each point on the trajectory
            foreach (MazeNavigatorEvaluationUnit evaluationUnit in evaluationUnits.Where(eu => eu.IsMazeSolved))
            {
                for (int idx = 0; idx < evaluationUnit.AgentTrajectory.Count(); idx += 2)
                {
                    FileWriters[OutputFileType.TrajectoryData].WriteLine(string.Join(FileDelimiter,
                        new List<string>
                        {
                            experimentId.ToString(),
                            run.ToString(),
                            batch.ToString(),
                            ((idx/2) + 1).ToString(), // this is the timestep
                            evaluationUnit.MazeId.ToString(),
                            evaluationUnit.AgentId.ToString(),
                            evaluationUnit.AgentTrajectory[idx].ToString(CultureInfo.InvariantCulture),
                            evaluationUnit.AgentTrajectory[idx + 1].ToString(CultureInfo.InvariantCulture)
                        }));
                }
            }
        }

        /// <summary>
        ///     Writes the given trajectory diversity results to the experiment database.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        /// <param name="commitPageSize">The number of records that are committed within a single batch/context.</param>
        private static void WriteTrajectoryDataToDatabase(int experimentId, int run, int batch,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int commitPageSize)
        {
            // Page through the result set, committing each in the specified batch size
            for (int curPage = 0; curPage <= evaluationUnits.Count/commitPageSize; curPage++)
            {
                IList<MCCFullTrajectory> serializedResults =
                    new List<MCCFullTrajectory>(commitPageSize);

                // Build a list of serialized results
                foreach (
                    MazeNavigatorEvaluationUnit evaluationUnit in
                        evaluationUnits.Skip(curPage*commitPageSize).Take(commitPageSize))
                {
                    for (int idx = 0; idx < evaluationUnit.AgentTrajectory.Count(); idx += 2)
                    {
                        serializedResults.Add(new MCCFullTrajectory
                        {
                            ExperimentDictionaryID = experimentId,
                            Run = run,
                            Generation = batch,
                            Timestep = ((idx/2) + 1), // this is the timestep
                            MazeGenomeID = evaluationUnit.MazeId,
                            NavigatorGenomeID = evaluationUnit.AgentId,
                            XPosition = Convert.ToDecimal(evaluationUnit.AgentTrajectory[idx]),
                            YPosition = Convert.ToDecimal(evaluationUnit.AgentTrajectory[idx + 1])
                        });
                    }
                }

                // Create a new context and persist the batch
                using (ExperimentDataEntities context = new ExperimentDataEntities())
                {
                    // Auto-detect changes and save validation are switched off to speed things up
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Configuration.ValidateOnSaveEnabled = false;

                    context.MCCFullTrajectories.AddRange(serializedResults);
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        ///     Writes the given trajectory diversity results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="trajectoryDiversityUnits">The trajectory diversity data to persist.</param>
        private static void WriteTrajectoryDiversityDataToFile(int experimentId, int run,
            IList<TrajectoryDiversityUnit> trajectoryDiversityUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.TrajectoryDiversityData) == false)
            {
                throw new Exception(
                    string.Format("Cannot write to output stream as no file writer of type {0} has been created.",
                        OutputFileType.TrajectoryDiversityData));
            }

            // Loop through the trajectory diversity units and write each row
            foreach (TrajectoryDiversityUnit diversityUnit in trajectoryDiversityUnits)
            {
                FileWriters[OutputFileType.TrajectoryDiversityData].WriteLine(string.Join(FileDelimiter,
                    new List<string>
                    {
                        experimentId.ToString(),
                        run.ToString(),
                        diversityUnit.MazeId.ToString(),
                        diversityUnit.AgentId.ToString(),
                        diversityUnit.IntraMazeDiversityScore.ToString(CultureInfo.InvariantCulture),
                        diversityUnit.InterMazeDiversityScore.ToString(CultureInfo.InvariantCulture),
                        diversityUnit.GlobalDiversityScore.ToString(CultureInfo.InvariantCulture)
                    }));
            }

            // Immediately flush to the output file
            FileWriters[OutputFileType.TrajectoryDiversityData].Flush();
        }

        /// <summary>
        ///     Writes the given trajectory diversity results to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="trajectoryDiversityUnits">The trajectory diversity data to persist.</param>
        private static void WriteTrajectoryDiversityDataToFile(int experimentId, int run, int batch,
            IList<TrajectoryDiversityUnit> trajectoryDiversityUnits)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.TrajectoryDiversityData) == false)
            {
                throw new Exception(
                    string.Format("Cannot write to output stream as no file writer of type {0} has been created.",
                        OutputFileType.TrajectoryDiversityData));
            }

            // Loop through the trajectory diversity units and write each row
            foreach (TrajectoryDiversityUnit diversityUnit in trajectoryDiversityUnits)
            {
                FileWriters[OutputFileType.TrajectoryDiversityData].WriteLine(string.Join(FileDelimiter,
                    new List<string>
                    {
                        experimentId.ToString(),
                        run.ToString(),
                        batch.ToString(),
                        diversityUnit.MazeId.ToString(),
                        diversityUnit.AgentId.ToString(),
                        diversityUnit.IntraMazeDiversityScore.ToString(CultureInfo.InvariantCulture),
                        diversityUnit.InterMazeDiversityScore.ToString(CultureInfo.InvariantCulture),
                        diversityUnit.GlobalDiversityScore.ToString(CultureInfo.InvariantCulture)
                    }));
            }

            // Immediately flush to the output file
            FileWriters[OutputFileType.TrajectoryDiversityData].Flush();
        }

        /// <summary>
        ///     Writes the given cluster diversity unit to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="clusterDiversityUnit">The clustering and population entropy data to persist.</param>
        private static void WriteClusteringDiversityDataToFile(int experimentId, int run,
            ClusterDiversityUnit clusterDiversityUnit)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.NaturalClusterData) == false)
            {
                throw new Exception(
                    string.Format("Cannot write to output stream as no file writer of type {0} has been created.",
                        OutputFileType.NaturalClusterData));
            }

            FileWriters[OutputFileType.NaturalClusterData].WriteLine(string.Join(FileDelimiter,
                new List<string>
                {
                    experimentId.ToString(),
                    run.ToString(),
                    clusterDiversityUnit.NumClusters.ToString(),
                    clusterDiversityUnit.PopulationEntropy.ToString(CultureInfo.InvariantCulture)
                }));
        }

        /// <summary>
        ///     Writes the given cluster diversity unit to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="clusterDiversityUnit">The clustering and population entropy data to persist.</param>
        /// <param name="clusteringOutputType">The type of clustering output (e.g. agent trajectory maze).</param>
        private static void WriteClusteringDiversityDataToFile(int experimentId, int run, int batch,
            ClusterDiversityUnit clusterDiversityUnit, OutputFileType clusteringOutputType)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(clusteringOutputType) == false)
            {
                throw new Exception(
                    string.Format("Cannot write to output stream as no file writer of type {0} has been created.",
                        clusteringOutputType));
            }

            FileWriters[clusteringOutputType].WriteLine(string.Join(FileDelimiter,
                new List<string>
                {
                    experimentId.ToString(),
                    run.ToString(),
                    batch.ToString(),
                    clusterDiversityUnit.NumClusters.ToString(),
                    clusterDiversityUnit.SilhouetteWidth.ToString(CultureInfo.InvariantCulture),
                    clusterDiversityUnit.PopulationEntropy.ToString(CultureInfo.InvariantCulture)
                }));
        }

        /// <summary>
        ///     Writes the given population entropy unit to a flat file.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="populationEntropyUnit">The population entropy data to persist.</param>
        private static void WritePopulationEntropyDataToFile(int experimentId, int run, int batch,
            PopulationEntropyUnit populationEntropyUnit)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.PopulationEntropyData) == false)
            {
                throw new Exception(
                    string.Format("Cannot write to output stream as no file writer of type {0} has been created.",
                        OutputFileType.PopulationEntropyData));
            }

            FileWriters[OutputFileType.PopulationEntropyData].WriteLine(string.Join(FileDelimiter,
                new List<string>
                {
                    experimentId.ToString(),
                    run.ToString(),
                    batch.ToString(),
                    populationEntropyUnit.PopulationEntropy.ToString(CultureInfo.InvariantCulture)
                }));
        }

        /// <summary>
        ///     Writes the coevolution vs. novelty search comparison results to a flat file.
        /// </summary>
        /// <param name="coEvoExperimentId">The coevolution experiment that was executed.</param>
        /// <param name="nsExperimentId">The novelty search experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="mazeGenomeId">
        ///     The unique identifier of the maze domain on which the novelty search comparison was
        ///     executed.
        /// </param>
        /// <param name="mazeBirthBatch">The birth batch (generation) of the maze.</param>
        /// <param name="nsAgentMinComplexity">The minimum complexity of the novelty search population at the end of the run.</param>
        /// <param name="nsAgentMaxComplexity">The maximum complexity of the novelty search population at the end of the run.</param>
        /// <param name="nsAgentMeanComplexity">The mean complexity of the novelty search population at the end of the run.</param>
        /// <param name="coEvoEvaluations">
        ///     The number of evaluations executed by the coevolution algorithm in order to arrive at
        ///     the given maze structure.
        /// </param>
        /// <param name="nsEvaluations">
        ///     The total number of evaluations executed by the novelty search algorithm in order to solve
        ///     (or attempt to solve) the coevolution-discovered maze structure.
        /// </param>
        /// <param name="isSolved">Flag indicating whether novelty search was successful in solving the maze.</param>
        private static void WriteNoveltySearchComparisonResultsToFile(int coEvoExperimentId, int nsExperimentId, int run,
            int batch, int mazeGenomeId, int mazeBirthBatch, int nsAgentMinComplexity, int nsAgentMaxComplexity,
            double nsAgentMeanComplexity, int coEvoEvaluations, int nsEvaluations, bool isSolved)
        {
            // Make sure the file writer actually exists before attempting to write to it
            if (FileWriters.ContainsKey(OutputFileType.NoveltySearchComparisonData) == false)
            {
                throw new Exception(
                    string.Format("Cannot write to output stream as no file writer of type {0} has been created.",
                        OutputFileType.NoveltySearchComparisonData));
            }

            // Write comparison results row to flat file with the specified delimiter
            FileWriters[OutputFileType.NoveltySearchComparisonData].WriteLine(string.Join(FileDelimiter,
                new List<string>
                {
                    coEvoExperimentId.ToString(),
                    nsExperimentId.ToString(),
                    run.ToString(),
                    batch.ToString(),
                    mazeGenomeId.ToString(),
                    mazeBirthBatch.ToString(),
                    nsAgentMinComplexity.ToString(),
                    nsAgentMaxComplexity.ToString(),
                    nsAgentMeanComplexity.ToString(CultureInfo.InvariantCulture),
                    coEvoEvaluations.ToString(),
                    nsEvaluations.ToString(),
                    isSolved.ToString()
                }));

            // Immediately flush to the output file
            FileWriters[OutputFileType.NoveltySearchComparisonData].Flush();
        }

        #endregion
    }
}