#region

using System.Collections.Generic;
using System.Linq;
using ExperimentEntities;

#endregion

namespace NavigatorMazeMapEvaluator
{
    /// <summary>
    ///     Provides methods for interfacing with and writing reuslts to the experiment database.
    /// </summary>
    public static class ExperimentDataHandler
    {
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
        ///     Writes the given evaluation results to the experiment database.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="evaluationUnits">The evaluation results to persist.</param>
        public static async void WriteNavigatorMazeEvaluationData(int experimentId, int run, int batch,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits)
        {
            IList<CoevolutionMCSMazeNavigatorResult> serializedResults =
                new List<CoevolutionMCSMazeNavigatorResult>(evaluationUnits.Count);

            // Convert each evaluation unit into an entity representation to be persisted
            foreach (MazeNavigatorEvaluationUnit evaluationUnit in evaluationUnits)
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

            // Persist the evaluation results
            using (ExperimentDataEntities context = new ExperimentDataEntities())
            {
                context.CoevolutionMCSMazeNavigatorResults.AddRange(serializedResults);
                await context.SaveChangesAsync();
            }
        }
    }
}