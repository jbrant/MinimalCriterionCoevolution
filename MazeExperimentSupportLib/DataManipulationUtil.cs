#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpNeat.Core;
using SharpNeat.Domains;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeExperimentSuppotLib
{
    /// <summary>
    ///     Contains utility methods for performaing miscellaneous data transformation and processing tasks.
    /// </summary>
    public static class DataManipulationUtil
    {
        #region Public utility methods

        /// <summary>
        ///     Extracts the specied number of evaluation unit samples from the population of evaluation units (trajectories).
        /// </summary>
        /// <param name="allEvaluationUnits">All evaluation during the given experiment/run/batch.</param>
        /// <param name="sampleSize">The sample size to extract from the collection of evaluation units.</param>
        /// <param name="extractEvenlyAcrossMazes">Whether to extract the given number of samples from each distinct maze.</param>
        /// <returns>The specified number of evaluation unit samples.</returns>
        public static IList<MazeNavigatorEvaluationUnit> ExtractEvaluationUnitSamplesFromPopulation(
            IList<MazeNavigatorEvaluationUnit> allEvaluationUnits, int sampleSize, bool extractEvenlyAcrossMazes)
        {
            List<MazeNavigatorEvaluationUnit> evalUnitSamples = new List<MazeNavigatorEvaluationUnit>();
            Random rnd = new Random();

            // If we're extract the sample size evenly across all extant/solvable mazes
            if (extractEvenlyAcrossMazes)
            {
                // Group agent trajectories by their applicable maze
                foreach (
                    var mazeEvalGroup in allEvaluationUnits.GroupBy(eu => eu.MazeId).Select(eu => eu.ToList()).ToList())
                {
                    // Get the actual sample size based on the minimum of the sample size or trajectories through the current maze
                    var groupSampleSize = Math.Min(sampleSize, mazeEvalGroup.Count());

                    // Gather sample of randomly selected evaluation units for the current maze
                    for (int cnt = 0; cnt < groupSampleSize; cnt++)
                    {
                        evalUnitSamples.Add(mazeEvalGroup[rnd.Next(groupSampleSize - 1)]);
                    }
                }
            }
            // Otherwise, just select randomly from the population
            else
            {
                // Gather sample of randomly selected evaluation units
                for (int cnt = 0; cnt < Math.Min(allEvaluationUnits.Count, sampleSize); cnt++)
                {
                    evalUnitSamples.Add(allEvaluationUnits[rnd.Next(allEvaluationUnits.Count - 1)]);
                }
            }

            // Return evaluation unit sample
            return evalUnitSamples;
        }

        /// <summary>
        ///     Extracts the specified number of evaluation unit samples from each navigator/maze species for subsuquent clustering
        ///     analysis.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="batch">The batch number of the given run.</param>
        /// <param name="allEvaluationUnits">All evaluation during the given experiment/run/batch.</param>
        /// <param name="sampleSize">The sample size to extract from each navigator/maze species.</param>
        /// <returns>The specified number of evaluation unit samples from each navigator/maze species.</returns>
        public static IList<MazeNavigatorEvaluationUnit> ExtractEvaluationUnitSamplesFromSpecies(int experimentId,
            int run,
            int batch, IList<MazeNavigatorEvaluationUnit> allEvaluationUnits, int sampleSize)
        {
            List<MazeNavigatorEvaluationUnit> evalUnitSamples = new List<MazeNavigatorEvaluationUnit>();

            // Extract all maze and navigator genome IDs
            var allMazeGenomeIds = allEvaluationUnits.Select(eu => eu.MazeId).Distinct().ToList();
            var allNavigatorGenomeIds = allEvaluationUnits.Select(eu => eu.AgentId).Distinct().ToList();

            // Get the species to which the mazes and navigators are assigned
            var mazeSpecieGenomesGroups = ExperimentDataHandler.GetSpecieAssignmentsForMazeGenomeIds(experimentId, run,
                batch, allMazeGenomeIds);
            var navigatorSpecieGenomesGroups =
                ExperimentDataHandler.GetSpecieAssignmentsForNavigatorGenomeIds(experimentId, run, batch,
                    RunPhase.Primary, allNavigatorGenomeIds);

            // Extract a sample of mazes and navigators for each of their respective species
            var sampleMazeIds = ExtractGenomeIdSample(mazeSpecieGenomesGroups, sampleSize);
            var sampleNavigatorIds = ExtractGenomeIdSample(navigatorSpecieGenomesGroups, sampleSize);

            // Collect maze and navigator samples
            CollectEvaluationSamples(sampleMazeIds, allEvaluationUnits, evalUnitSamples, true);
            CollectEvaluationSamples(sampleNavigatorIds, allEvaluationUnits, evalUnitSamples, true);

            return evalUnitSamples;
        }

        #endregion

        #region Private helper methods

        /// <summary>
        ///     Adds a distinct set of samples corresponding to the genome ID list to the given evaluation unit samples list.
        /// </summary>
        /// <param name="sampleIds">The set of genome IDs for which to find distinct samples.</param>
        /// <param name="allEvaluationUnits">All available evaluation units from which samples can be chosen.</param>
        /// <param name="evalUnitSamples">The running list of evaluation unit samples (for both mazes and navigators).</param>
        /// <param name="isMazeEvaluation">Flag indicating whether these are maze or navigator samples.</param>
        private static void CollectEvaluationSamples(IList<int> sampleIds,
            IList<MazeNavigatorEvaluationUnit> allEvaluationUnits, IList<MazeNavigatorEvaluationUnit> evalUnitSamples,
            bool isMazeEvaluation)
        {
            Random rnd = new Random();

            foreach (var sampleId in sampleIds)
            {
                MazeNavigatorEvaluationUnit curSampleEvalUnit;
                int curSampleEvalUnitIdx;

                // Get evaluation units matching the current ID
                var candidateEvalUnits = isMazeEvaluation
                    ? allEvaluationUnits.Where(eu => sampleId == eu.MazeId).ToList()
                    : allEvaluationUnits.Where(eu => sampleId == eu.AgentId).ToList();

                // If there are no matching evaluation units, move one to the next sample
                if (candidateEvalUnits.Count <= 0) continue;

                // Attempt to extract sample until we either get a unique sample or run out of candidate samples
                do
                {
                    // Randomly select evaluation unit index
                    curSampleEvalUnitIdx = rnd.Next(candidateEvalUnits.Count() - 1);

                    // Get sample evaluation unit
                    curSampleEvalUnit = candidateEvalUnits[curSampleEvalUnitIdx];

                    // Remove evaluation unit from candidate list
                    candidateEvalUnits.Remove(curSampleEvalUnit);
                } while (candidateEvalUnits.Any() &&
                         evalUnitSamples.Any(
                             us =>
                                 candidateEvalUnits[curSampleEvalUnitIdx].MazeId == us.MazeId &&
                                 candidateEvalUnits[curSampleEvalUnitIdx].AgentId == us.AgentId));

                // Add evaluation unit to overall sample list only if it constitutes a unique entry
                if (
                    evalUnitSamples.Any(
                        us => curSampleEvalUnit.MazeId == us.MazeId && curSampleEvalUnit.AgentId == us.AgentId) ==
                    false)
                {
                    evalUnitSamples.Add(curSampleEvalUnit);
                }
            }
        }

        /// <summary>
        ///     Extracts a sample of genomes from each of the given species based on the specified sample size.
        /// </summary>
        /// <param name="specieGenomesGroups">The groups of specie IDs and their constituent genome IDs.</param>
        /// <param name="sampleSize">The number of genome IDs to attempt to extract from each specie.</param>
        /// <returns>Sample of genomes from each of the given species based on the specified sample size.</returns>
        private static IList<int> ExtractGenomeIdSample(List<SpecieGenomesGroup> specieGenomesGroups, int sampleSize)
        {
            List<int> sampleGenomeIds = new List<int>();
            Random rnd = new Random();

            // Extract a sample of mazes for each species
            foreach (var specieGenomeGroup in specieGenomesGroups)
            {
                for (int idx = 0; idx < Math.Min(sampleSize, specieGenomeGroup.GenomeIds.Count()); idx++)
                {
                    // Randomly select genome ID
                    int mazeIdx = rnd.Next(specieGenomeGroup.GenomeIds.Count() - 1);

                    // Add genome ID to global list
                    sampleGenomeIds.Add(specieGenomeGroup.GenomeIds[mazeIdx]);

                    // Remove that genome ID as a candidate for sample selection
                    specieGenomeGroup.GenomeIds.RemoveAt(mazeIdx);
                }
            }

            return sampleGenomeIds;
        }

        /// <summary>
        ///     Converts the evolved walls into experiment domain walls so that experiment-specific calculations can be applied on
        ///     them.
        /// </summary>
        /// <param name="mazeStructureWalls">The evolved walls.</param>
        /// <returns>List of the experiment-specific walls.</returns>
        public static List<Wall> ExtractMazeWalls(List<MazeStructureWall> mazeStructureWalls)
        {
            List<Wall> mazeWalls = new List<Wall>(mazeStructureWalls.Count);

            // Convert each of the maze structure walls to the experiment domain wall
            // TODO: Can this also be parallelized?
            mazeWalls.AddRange(
                mazeStructureWalls.Select(
                    mazeStructureWall =>
                        new Wall(new DoubleLine(mazeStructureWall.StartMazeStructurePoint.X,
                            mazeStructureWall.StartMazeStructurePoint.Y,
                            mazeStructureWall.EndMazeStructurePoint.X, mazeStructureWall.EndMazeStructurePoint.Y))));

            return mazeWalls;
        }

        /// <summary>
        ///     Converts evolved point (start or finish) to experiment domain point for the navigator start location and the target
        ///     (goal).
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>The domain-specific point object.</returns>
        public static DoublePoint ExtractStartEndPoint(MazeStructurePoint point)
        {
            return new DoublePoint(point.X, point.Y);
        }

        #endregion
    }
}