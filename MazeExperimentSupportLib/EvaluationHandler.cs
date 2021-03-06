﻿#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Accord.MachineLearning;
using MCC_Domains.MazeNavigation;
using SharpNeat.Behaviors;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;
using SharpNeat.Utility;

#endregion

namespace MazeExperimentSupportLib
{
    /// <summary>
    ///     Provides methods for computing experiment evaluation metrics.
    /// </summary>
    public static class EvaluationHandler
    {
        #region Public methods

        /// <summary>
        ///     Runs the navigator through its corresponding maze and records data about said evaluation (e.g. solved status, time
        ///     steps, and trajectory).
        /// </summary>
        /// <param name="evaluationUnit">A single unit of evaluation containing a paired maze and navigator.</param>
        /// <param name="experimentParameters">Parameters that control the navigation simulation.</param>
        public static void EvaluateMazeNavigatorUnit(MazeNavigatorEvaluationUnit evaluationUnit,
            ExperimentParameters experimentParameters)
        {
            // Build maze configuration
            var mazeConfiguration =
                new MazeConfiguration(DataManipulationUtil.ExtractMazeWalls(evaluationUnit.MazePhenome.Walls),
                    DataManipulationUtil.ExtractStartEndPoint(evaluationUnit.MazePhenome.ScaledStartLocation),
                    DataManipulationUtil.ExtractStartEndPoint(evaluationUnit.MazePhenome.ScaledTargetLocation),
                    evaluationUnit.MazePhenome.MaxTimesteps);

            // Create trajectory behavior characterization (in order to capture full trajectory of navigator)
            IBehaviorCharacterization behaviorCharacterization = new TrajectoryBehaviorCharacterization();

            // Create the maze navigation world
            var world = new MazeNavigationWorld(mazeConfiguration.Walls,
                mazeConfiguration.NavigatorLocation, mazeConfiguration.GoalLocation,
                experimentParameters.MinSuccessDistance, mazeConfiguration.MaxSimulationTimesteps,
                behaviorCharacterization);

            // Run a single trial
            var trialBehavior = world.RunBehaviorTrial(evaluationUnit.AgentPhenome, out var isGoalReached);

            // Set maze solved status
            evaluationUnit.IsMazeSolved = isGoalReached;

            // The number of time steps is effectively the number of 2-dimensional points in the behaviors array
            evaluationUnit.NumTimesteps = world.GetSimulationTimesteps();

            // Set the trajectory of the agent
            evaluationUnit.AgentTrajectory = trialBehavior;
        }

        /// <summary>
        ///     Computes the number of "deceptive" turns in the maze by finding juncture locations where there is more than one
        ///     possible direction to turn, leading to a potentially deceptive trap.
        /// </summary>
        /// <param name="curChunkMazes">The collection of mazes being evaluated during the current chunk.</param>
        /// <returns>The collection of maze genome IDs along with the number of deceptive turns in their solution path.</returns>
        public static IEnumerable<Tuple<uint, int>> CalculateDeceptiveTurnCount(
            IEnumerable<MazeStructure> curChunkMazes)
        {
            var mazeDeceptiveTurns = new ConcurrentBag<Tuple<uint, int>>();

            // Loop through each solution path and tally the number of deceptive turns
            Parallel.ForEach(curChunkMazes, mazeStructure =>
            {
                var mazeGrid = mazeStructure.MazeGrid.Grid;

                // Initialize deceptive turns to 0
                var numDeceptiveTurns = 0;

                // Initialize the previous cell to the start location and the current cell to the the second location
                var prevCell = mazeStructure.UnscaledStartLocation;
                var curCell = mazeStructure.GetNextPathCell(prevCell);

                do
                {
                    // Check to see if there are deceptive offshoots for the current turn 
                    if (mazeGrid[curCell.Y, curCell.X].IsJuncture)
                    {
                        switch (mazeGrid[curCell.Y, curCell.X].PathDirection)
                        {
                            case PathDirection.North when prevCell.X <= curCell.X && MazeUtils.IsEastOpening(curCell,
                                                              mazeStructure.UnscaledMazeWidth, mazeGrid) ||
                                                          // South opening
                                                          prevCell.Y <= curCell.Y && MazeUtils.IsSouthOpening(curCell,
                                                              mazeStructure.UnscaledMazeHeight, mazeGrid) ||
                                                          // West opening
                                                          prevCell.X >= curCell.X &&
                                                          MazeUtils.IsWestOpening(curCell, mazeGrid):
                            case PathDirection.East
                                when prevCell.Y >= curCell.Y && MazeUtils.IsNorthOpening(curCell, mazeGrid) ||
                                     // South opening
                                     prevCell.Y <= curCell.Y && MazeUtils.IsSouthOpening(curCell,
                                         mazeStructure.UnscaledMazeHeight, mazeGrid) ||
                                     // West opening
                                     prevCell.X >= curCell.X && MazeUtils.IsWestOpening(curCell, mazeGrid):
                            case PathDirection.South
                                when prevCell.Y >= curCell.Y && MazeUtils.IsNorthOpening(curCell, mazeGrid) ||
                                     // East opening
                                     prevCell.X <= curCell.X && MazeUtils.IsEastOpening(curCell,
                                         mazeStructure.UnscaledMazeWidth, mazeGrid) ||
                                     // West opening
                                     prevCell.X >= curCell.X && MazeUtils.IsWestOpening(curCell, mazeGrid):
                            case PathDirection.West
                                when prevCell.Y >= curCell.Y && MazeUtils.IsNorthOpening(curCell, mazeGrid) ||
                                     // East opening
                                     prevCell.X <= curCell.X && MazeUtils.IsEastOpening(curCell,
                                         mazeStructure.UnscaledMazeWidth, mazeGrid) ||
                                     // South opening
                                     prevCell.Y <= curCell.Y && MazeUtils.IsSouthOpening(curCell,
                                         mazeStructure.UnscaledMazeHeight, mazeGrid):
                                numDeceptiveTurns++;
                                break;
                            case PathDirection.None:
                                break;
                        }
                    }

                    // Walk the path to the next cell
                    prevCell = curCell;
                    curCell = mazeStructure.GetNextPathCell(curCell);
                } while (curCell != mazeStructure.UnscaledTargetLocation);

                // Add the current maze genome ID and the corresponding number of deceptive turns
                mazeDeceptiveTurns.Add(new Tuple<uint, int>(mazeStructure.GenomeId, numDeceptiveTurns));
            });

            return mazeDeceptiveTurns;
        }

        /// <summary>
        ///     Computes the maze diversity by computing the manhattan distance between points on the solution paths.
        /// </summary>
        /// <param name="curChunkMazes">The collection of mazes being evaluated during the current chunk.</param>
        /// <param name="allMazes">The list of all maze structures undergoing evaluation/comparison.</param>
        /// <returns>The collection of maze diversity units recording the solution path distances between mazes.</returns>
        public static IEnumerable<MazeDiversityUnit> CalculateMazeDiversity(IEnumerable<MazeStructure> curChunkMazes,
            IList<MazeStructure> allMazes)
        {
            var mazeDiversityUnits = new List<MazeDiversityUnit>();

            // Loop through every maze in the population, comparing its solution path
            foreach (var curMaze in curChunkMazes)
            {
                var curMazeDiversityScores = new ConcurrentBag<double>();

                // Compute the solution path diversity for all mazes in the current chunk against the entire population
                Parallel.ForEach(allMazes, comparisonMaze =>
                {
                    // Path cell counter
                    var pathCellCount = 0;

                    // Distance accumulator for the current maze
                    var pathDistance = 0.0;

                    // Don't compare the current maze to itself
                    if (curMaze == comparisonMaze) return;

                    // Initialize current cells to one location beyond the maze start location
                    var curCell = curMaze.GetNextPathCell(curMaze.UnscaledStartLocation);
                    var curCmprCell = comparisonMaze.GetNextPathCell(comparisonMaze.UnscaledStartLocation);

                    do
                    {
                        // Calculate manhattan distance between cells of the two mazes
                        pathDistance += Math.Abs(curCell.X - curCmprCell.X) + Math.Abs(curCell.Y - curCmprCell.Y);

                        try
                        {
                            // Increment to the next cell of both mazes
                            curCell = curMaze.GetNextPathCell(curCell);
                            curCmprCell = comparisonMaze.GetNextPathCell(curCmprCell);

                            // Increment the path cell count
                            pathCellCount++;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    } while (curCell != curMaze.UnscaledTargetLocation ||
                             curCmprCell != comparisonMaze.UnscaledTargetLocation);

                    // Record the distance between the two maze solution paths
                    curMazeDiversityScores.Add(pathDistance / pathCellCount);
                });

                // Compute overall solution path diversity for the current maze
                mazeDiversityUnits.Add(new MazeDiversityUnit(curMaze.GenomeId, curMazeDiversityScores.Average()));
            }

            return mazeDiversityUnits;
        }

        /// <summary>
        ///     Computes the diversity of trajectories for the given list of evaluations.
        /// </summary>
        /// <param name="evaluationUnits">The agent/maze evaluations for which to compute trajectory diversity.</param>
        /// <returns>The resulting trajectory diversity scores.</returns>
        public static IList<TrajectoryDiversityUnit> CalculateTrajectoryDiversity(
            IList<MazeNavigatorEvaluationUnit> evaluationUnits)
        {
            IList<TrajectoryDiversityUnit> trajectoryDiversityUnits = new List<TrajectoryDiversityUnit>();

            foreach (var evaluationUnit in evaluationUnits.Where(u => u.IsMazeSolved))
            {
                double intraMazeTotalTrajectoryDifference = 0;
                double interMazeTotalTrajectoryDifference = 0;

                // Copy current evaluation unit for use in closure below
                var evaluationUnitCopy = evaluationUnit;

                // Iterates through every other agent trajectory and computes the trajectory difference                
                Parallel.ForEach(evaluationUnits.Where(u => u.IsMazeSolved), otherEvaluationUnit =>
                {
                    // Don't evaluate current agent trajectory against itself
                    if (otherEvaluationUnit.Equals(evaluationUnitCopy))
                        return;

                    // Calculate trajectory difference for same maze
                    if (otherEvaluationUnit.MazeId.Equals(evaluationUnitCopy.MazeId))
                    {
                        intraMazeTotalTrajectoryDifference +=
                            ComputeEuclideanTrajectoryDifference(evaluationUnitCopy.AgentTrajectory,
                                otherEvaluationUnit.AgentTrajectory);
                    }
                    // Calculate trajectory difference for other maze
                    else
                    {
                        interMazeTotalTrajectoryDifference +=
                            ComputeEuclideanTrajectoryDifference(evaluationUnitCopy.AgentTrajectory,
                                otherEvaluationUnit.AgentTrajectory);
                    }
                });

                // Get the count of intramaze, intermaze, and globla trajectories
                var intraMazeTrajectoryCount = evaluationUnits.Count(unit =>
                    unit.IsMazeSolved && unit.Equals(evaluationUnit) == false &&
                    unit.MazeId == evaluationUnit.MazeId);
                var interMazeTrajectoryCount = evaluationUnits.Count(
                    unit =>
                        unit.IsMazeSolved && unit.MazeId != evaluationUnit.MazeId);
                var globalMazeTrajectoryCount = evaluationUnits.Count(unit => unit.IsMazeSolved);

                // Calculate intra-maze, inter-maze, and global trajectory diversity scores 
                // and instantiate trajectory diversity unit
                var trajectoryDiversityUnit = new TrajectoryDiversityUnit(evaluationUnit.AgentId,
                    evaluationUnit.MazeId, intraMazeTrajectoryCount == 0
                        ? 0
                        : intraMazeTotalTrajectoryDifference / intraMazeTrajectoryCount, interMazeTrajectoryCount == 0
                        ? 0
                        : interMazeTotalTrajectoryDifference / interMazeTrajectoryCount,
                    globalMazeTrajectoryCount == 0
                        ? 0
                        : (intraMazeTotalTrajectoryDifference + interMazeTotalTrajectoryDifference) /
                          globalMazeTrajectoryCount);

                // Add trajectory diversity unit to list
                trajectoryDiversityUnits.Add(trajectoryDiversityUnit);
            }

            return trajectoryDiversityUnits;
        }

        /// <summary>
        ///     Computes the population entropy (shannon entropy) based on the given list of trajectories and number of clusters in
        ///     which to cluster those behaviors using the k-means algorithm.
        /// </summary>
        /// <param name="evaluationUnits">The agent/maze evaluations (trajectories) to cluster.</param>
        /// <param name="numClusters">The number of clusters into which to segregate agent trajectories.</param>
        /// <returns>The population entropy.</returns>
        public static PopulationEntropyUnit CalculatePopulationEntropy(
            IEnumerable<MazeNavigatorEvaluationUnit> evaluationUnits, int numClusters)
        {
            // Only consider successful trials
            IList<MazeNavigatorEvaluationUnit> successfulEvaluations =
                evaluationUnits.Where(eu => eu.IsMazeSolved).ToList();

            // Define the trajectory matrix in which to store all trajectory points for each trajectory
            // (this becomes a collection of observation vectors that's fed into k-means)
            var trajectoryMatrix = new double[successfulEvaluations.Count][];

            // Get the maximum observation vector length (max simulation runtime)
            // (multiplied by 2 to account for each timestep containing a 2-dimensional position)
            var maxObservationLength = successfulEvaluations.Max(x => x.NumTimesteps) * 2;

            for (var idx = 0; idx < successfulEvaluations.Count; idx++)
            {
                // If there are few observations than the total elements in the observation vector,
                // fill out the vector with the existing observations and set the rest equal to the last
                // position in the simulation
                if (successfulEvaluations[idx].AgentTrajectory.Length < maxObservationLength)
                {
                    trajectoryMatrix[idx] =
                        successfulEvaluations[idx].AgentTrajectory.Concat(
                            Enumerable.Repeat(
                                successfulEvaluations[idx].AgentTrajectory[
                                    successfulEvaluations[idx].AgentTrajectory.Length - 1],
                                maxObservationLength - successfulEvaluations[idx].AgentTrajectory.Length)).ToArray();
                }
                // If they are equal, just set the trajectory points
                else
                {
                    trajectoryMatrix[idx] = successfulEvaluations[idx].AgentTrajectory;
                }
            }

            // TODO: The below logic is in support of a work-around to an Accord.NET bug wherein
            // TODO: an internal random number generator sometimes generates out-of-bounds values
            // TODO: (i.e. a probability that is not between 0 and 1)
            // TODO: https://github.com/accord-net/framework/issues/259

            // Initialize k-Means algorithm with the given number of clusters and uniform seeding
            var kmeans = new KMeans(numClusters) {UseSeeding = Seeding.Uniform};

            // Determine cluster assignments
            kmeans.Learn(trajectoryMatrix);

            // Compute shannon entropy given clustering
            var shannonEntropy = ComputeShannonEntropy(kmeans);

            // Return the resulting population entropy
            return new PopulationEntropyUnit(shannonEntropy);
        }

        /// <summary>
        ///     Computes the natural clustering of the maze genomes along with their respective entropy.
        /// </summary>
        /// <param name="mazeGenomeListXml">The list of serialized maze genome XML.</param>
        /// <param name="isGreedySilhouetteCalculation">
        ///     Dictates whether optimal clustering is based on number of clusters that
        ///     maximize silhouette score until the first decrease in score (true), or based on the silhouette score calculated for
        ///     a range of clusters with the number of clusters resulting in the maximum score used as the optimal number of
        ///     clusters (false).
        /// </param>
        /// <param name="clusterRange">The range of clusters values for which to compute the silhouette width (optional).</param>
        /// <returns>The cluster diversity unit for maze genomes.</returns>
        public static ClusterDiversityUnit CalculateMazeClustering(IList<string> mazeGenomeListXml,
            bool isGreedySilhouetteCalculation, int clusterRange = 0)
        {
            // Always start with one cluster
            const int initClusterCnt = 1;

            // Initialize list of maze genome objects
            var mazeGenomes = new List<MazeGenome>(mazeGenomeListXml.Count());

            // Convert all maze genome XML strings to maze genome objects
            foreach (var genomeXml in mazeGenomeListXml)
            {
                MazeGenome curMazeGenome;

                // Create a new, dummy maze genome factory
                var tempMazeGenomeFactory = new MazeGenomeFactory();

                // Convert genome XML to genome object
                using (var xr = XmlReader.Create(new StringReader(genomeXml)))
                {
                    curMazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xr, tempMazeGenomeFactory);
                }

                // Add to the genome object list
                mazeGenomes.Add(curMazeGenome);
            }

            // Define observation matrix in which to store all gene coordinate vectors for each maze
            var observationMatrix = new double[mazeGenomes.Count()][];

            // Get the maximum observation vector length (max number of maze genes)
            var maxObservationLength = mazeGenomes.Max(g => g.WallGeneList.Count());

            for (var idx = 0; idx < mazeGenomes.Count(); idx++)
            {
                // If there are more observations than the total elements in the observation vector, 
                // zero out the rest of the vector
                if (mazeGenomes[idx].WallGeneList.Count() < maxObservationLength)
                {
                    observationMatrix[idx] =
                        mazeGenomes[idx].Position.CoordArray.Select(ca => ca.Value)
                            .Concat(Enumerable.Repeat(0.0,
                                maxObservationLength - mazeGenomes[idx].Position.CoordArray.Length))
                            .ToArray();
                }
                // Otherwise, if the observation and vector length are the same, just set the elements
                else
                {
                    observationMatrix[idx] = mazeGenomes[idx].Position.CoordArray.Select(ca => ca.Value).ToArray();
                }
            }

            // Determine the optimal number of clusters to fit these data
            var optimalClustering = DetermineOptimalClusters(observationMatrix, initClusterCnt, false,
                isGreedySilhouetteCalculation, clusterRange);

            // Compute shannon entropy given optimal clustering
            var shannonEntropy = ComputeShannonEntropy(optimalClustering);

            // Compute the silhouette width given optimal clustering
            var silhouetteWidth = ComputeSilhouetteWidth(optimalClustering.Clusters, observationMatrix, false);

            // Return the resulting maze genome cluster diversity info
            return new ClusterDiversityUnit(optimalClustering.Clusters.Count, silhouetteWidth, shannonEntropy);
        }

        /// <summary>
        ///     Clusters the agent trajectories (behaviors) along with computing the behavioral entropy (diversity).
        /// </summary>
        /// <param name="evaluationUnits">The agent/maze evaluations to cluster and compute entropy.</param>
        /// <param name="isGreedySilhouetteCalculation">
        ///     Dictates whether optimal clustering is based on number of clusters that
        ///     maximize silhouette score until the first decrease in score (true), or based on the silhouette score calculated for
        ///     a range of clusters with the number of clusters resulting in the maximum score used as the optimal number of
        ///     clusters (false).
        /// </param>
        /// <param name="clusterRange">The range of clusters values for which to compute the silhouette width (optional).</param>
        /// <returns>The cluster diversity unit for agent trajectories.</returns>
        public static ClusterDiversityUnit CalculateAgentTrajectoryClustering(
            IEnumerable<MazeNavigatorEvaluationUnit> evaluationUnits, bool isGreedySilhouetteCalculation,
            int clusterRange = 0)
        {
            // Always start with one cluster
            const int initClusterCnt = 1;

            // Only consider successful trials
            IList<MazeNavigatorEvaluationUnit> successfulEvaluations =
                evaluationUnits.Where(eu => eu.IsMazeSolved).ToList();

            // Define the trajectory matrix in which to store all trajectory points for each trajectory
            // (this becomes a collection of observation vectors that's fed into k-means)
            var trajectoryMatrix = new double[successfulEvaluations.Count][];

            // Get the maximum observation vector length (max simulation runtime)
            // (multiplied by 2 to account for each timestep containing a 2-dimensional position)
            var maxObservationLength = successfulEvaluations.Max(x => x.NumTimesteps) * 2;

            for (var idx = 0; idx < successfulEvaluations.Count; idx++)
            {
                // If there are few observations than the total elements in the observation vector,
                // fill out the vector with the existing observations and set the rest equal to the last
                // position in the simulation
                if (successfulEvaluations[idx].AgentTrajectory.Length < maxObservationLength)
                {
                    trajectoryMatrix[idx] =
                        successfulEvaluations[idx].AgentTrajectory.Concat(
                            Enumerable.Repeat(
                                successfulEvaluations[idx].AgentTrajectory[
                                    successfulEvaluations[idx].AgentTrajectory.Length - 1],
                                maxObservationLength - successfulEvaluations[idx].AgentTrajectory.Length)).ToArray();
                }
                // If they are equal, just set the trajectory points
                else
                {
                    trajectoryMatrix[idx] = successfulEvaluations[idx].AgentTrajectory;
                }
            }

            // Determine the optimal number of clusters to fit these data
            var optimalClustering = DetermineOptimalClusters(trajectoryMatrix, initClusterCnt, true,
                isGreedySilhouetteCalculation, clusterRange);

            // Compute shannon entropy given optimal clustering
            var shannonEntropy = ComputeShannonEntropy(optimalClustering);

            // Compute the silhouette width given optimal clustering
            var silhouetteWidth = ComputeSilhouetteWidth(optimalClustering.Clusters, trajectoryMatrix, true);

            // Return the resulting agent trajectory cluster diversity info
            return new ClusterDiversityUnit(optimalClustering.Clusters.Count, silhouetteWidth, shannonEntropy);
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Computes the shannon entropy for the given clusters and their respective assignment proportions.
        /// </summary>
        /// <param name="clusters">The clusters resulting from K-means clustering process.</param>
        /// <returns>The shannon entropy (i.e. population diversity) based on cluster proportion assignments.</returns>
        private static double ComputeShannonEntropy(KMeans clusters)
        {
            var sumLogProportion = 0.0;

            // Compute the shannon entropy of the population
            foreach (var cluster in clusters.Clusters)
            {
                sumLogProportion += cluster.Proportion > 0 ? cluster.Proportion * Math.Log(cluster.Proportion, 2) : 0;
            }

            // Multiply by negative one to get the Shannon entropy
            return sumLogProportion * -1;
        }

        /// <summary>
        ///     Computes the optimal number of clusters for the given observations.
        /// </summary>
        /// <param name="observationMatrix">The matrix of observations.</param>
        /// <param name="initialClusterCount">The initial number of clusters to start with.</param>
        /// <param name="isAgentTrajectoryClustering">
        ///     Dictates whether the clustering that is being performed is specific to
        ///     two-dimensional agent trajectories (otherwise, distance calculations assume one-dimensional observation vectors).
        /// </param>
        /// <param name="isGreedySilhouetteCalculation">
        ///     Dictates whether optimal clustering is based on number of clusters that
        ///     maximize silhouette score until the first decrease in score (true), or based on the silhouette score calculated for
        ///     a range of clusters with the number of clusters resulting in the maximum score used as the optimal number of
        ///     clusters (false).
        /// </param>
        /// <param name="clusterRange">The range of clusters values for which to compute the silhouette width (optional).</param>
        /// <returns>The cluster assignments and their proportions (as well as other K-Means stats).</returns>
        private static KMeans DetermineOptimalClusters(double[][] observationMatrix, int initialClusterCount,
            bool isAgentTrajectoryClustering, bool isGreedySilhouetteCalculation, int clusterRange = 0)
        {
            var clusterSilhouetteMap = new Dictionary<int, double>();
            double maxSilhouetteWidth;

            // Initialize current silhouette width
            double curSilhouetteWidth = 0;

            // Set the initial cluster count
            var clusterCount = initialClusterCount;

            do
            {
                // Set max silhouette with the value on the previous loop iteration (since it was an improvement)
                maxSilhouetteWidth = curSilhouetteWidth;

                // Increment cluster count
                clusterCount++;

                // TODO: The below logic is in support of a work-around to an Accord.NET bug wherein
                // TODO: an internal random number generator sometimes generates out-of-bounds values
                // TODO: (i.e. a probability that is not between 0 and 1)
                // TODO: https://github.com/accord-net/framework/issues/259

                // Create a new k-means instance with the specified number of clusters and uniform seeding
                var kmeans = new KMeans(clusterCount) {UseSeeding = Seeding.Uniform};

                // Determine the resulting clusters
                var clusters = kmeans.Learn(observationMatrix);

                // Compute the silhouette width for the current number of clusters
                curSilhouetteWidth = ComputeSilhouetteWidth(clusters, observationMatrix, isAgentTrajectoryClustering);

                // If this is non-greedy silhouette calculation, record the silhouette width associated with
                // the current number of clusters
                if (isGreedySilhouetteCalculation == false)
                {
                    clusterSilhouetteMap.Add(clusterCount, curSilhouetteWidth);
                }

                // Continue to increment number of clusters while there is a silhouette width improvement 
                // (in the case of greedy silhouette calculation) or we have reached the max cluster range, 
                // and we have not yet reached a number of clusters equivalent to the number of observations
            } while ((isGreedySilhouetteCalculation
                         ? curSilhouetteWidth >= maxSilhouetteWidth
                         : clusterCount < clusterRange) &&
                     clusterCount < observationMatrix.Length);

            // If this is non-greedy silhouette calculation, set the cluster count to the number of clusters
            // within the given range that results in the highest silhouette width (i.e. highest cluster validity)
            if (isGreedySilhouetteCalculation == false)
            {
                clusterCount =
                    clusterSilhouetteMap.FirstOrDefault(csm => csm.Value == clusterSilhouetteMap.Values.Max()).Key;
            }

            // TODO: The below logic is in support of a work-around to an Accord.NET bug wherein
            // TODO: an internal random number generator sometimes generates out-of-bounds values
            // TODO: (i.e. a probability that is not between 0 and 1)
            // TODO: https://github.com/accord-net/framework/issues/259

            // Rerun kmeans for the final cluster count with uniform seeding
            var optimalClustering = new KMeans(clusterCount) {UseSeeding = Seeding.Uniform};

            // Determine cluster assignments
            optimalClustering.Learn(observationMatrix);

            return optimalClustering;
        }

        /// <summary>
        ///     Computes the silhouette width for the given set of clusters and observations.
        /// </summary>
        /// <param name="clusters">The clusters in the dataset.</param>
        /// <param name="observations">The observation vectors.</param>
        /// <param name="isTwoDimensionalObservations">
        ///     Indicates whether or not the observation vector consists of flattened,
        ///     two-dimensioal observations (which is how agent trajectories are stored), prompting special consideration for
        ///     euclidean distasnce calculation.
        /// </param>
        /// <returns>The silhouette width for the given set of clusters and observations.</returns>
        private static double ComputeSilhouetteWidth(KMeansClusterCollection clusters, double[][] observations,
            bool isTwoDimensionalObservations)
        {
            var lockObj = new object();
            double totalSilhouetteWidth = 0;

            // Get cluster assignments for all of the observations
            var clusterAssignments = clusters.Decide(observations);

            Parallel.For(0, observations.Length, observationIdx =>
            {
                double obsIntraclusterDissimilarity = 0;

                // Get the cluster assignment of the current observation
                var curObsClusterAssignment = clusterAssignments[observationIdx];

                // Only add observation silhouette width if it is NOT the sole member of its assigned cluster
                if (clusterAssignments.Count(ca => ca == curObsClusterAssignment) > 1)
                {
                    // Setup list to hold average distance from current observation to every other neighboring cluster
                    var neighboringClusterDistances = new List<double>(clusters.Count);

                    for (var clusterIdx = 0; clusterIdx < clusters.Count; clusterIdx++)
                    {
                        // Handle the case where the current cluster is the cluster of which the observation is a member
                        if (clusterIdx == curObsClusterAssignment)
                        {
                            // Sum the distance between current observation and every other observation in the same cluster
                            for (var caIdx = 0; caIdx < clusterAssignments.Length; caIdx++)
                            {
                                if (curObsClusterAssignment == clusterAssignments[caIdx])
                                {
                                    obsIntraclusterDissimilarity +=
                                        isTwoDimensionalObservations
                                            ? ComputeEuclideanTrajectoryDifference(observations[observationIdx],
                                                observations[caIdx])
                                            : ComputeEuclideanObservationDifference(observations[observationIdx],
                                                observations[caIdx]);
                                }
                            }
                        }
                        // Otherwise, handle the case where we're on a neighboring cluster
                        else
                        {
                            // Create new variable to hold sum of dissimilarities between observation and 
                            // neighboring cluster observations
                            double curObsNeighboringClusterDissimilarity = 0;

                            // Sum the distance between current observation and cluster centroids to which 
                            // the current observation is NOT assigned
                            for (var caIdx = 0; caIdx < clusterAssignments.Length; caIdx++)
                            {
                                if (curObsClusterAssignment != clusterAssignments[caIdx])
                                {
                                    curObsNeighboringClusterDissimilarity +=
                                        isTwoDimensionalObservations
                                            ? ComputeEuclideanTrajectoryDifference(observations[observationIdx],
                                                observations[caIdx])
                                            : ComputeEuclideanObservationDifference(observations[observationIdx],
                                                observations[caIdx]);
                                }
                            }

                            // Compute the average intercluster dissimilarity for the current neighboring 
                            // cluster and add to the list of average neighboring cluster distances
                            neighboringClusterDistances.Add(curObsNeighboringClusterDissimilarity /
                                                            clusterAssignments.Count(ca => ca == clusterIdx));
                        }
                    }

                    // Compute the average intracluster dissimilarity (local variance)
                    obsIntraclusterDissimilarity = obsIntraclusterDissimilarity /
                                                   clusterAssignments.Count(ca => ca == curObsClusterAssignment);

                    // Get the minimum intercluster dissimilarity (0 if there are no centroid differences)
                    var obsInterClusterDissimilarity = neighboringClusterDistances.Any()
                        ? neighboringClusterDistances.Min()
                        : 0;

                    // Compute the silhouette width for the current observation
                    // If its the only point in the cluster, then the silhouette width is 0
                    var curSilhouetteWidth = Math.Abs(obsIntraclusterDissimilarity) < 0.0000001
                        ? 0
                        : (obsInterClusterDissimilarity -
                           obsIntraclusterDissimilarity) /
                          Math.Max(obsIntraclusterDissimilarity,
                              obsInterClusterDissimilarity);

                    lock (lockObj)
                    {
                        // Add the silhoutte width for the current observation
                        totalSilhouetteWidth += curSilhouetteWidth;
                    }
                }
            });

            // Return the silhoutte width
            return totalSilhouetteWidth / observations.Length;
        }

        /// <summary>
        ///     Generic euclidean distance calculation that computes the euclidean distance between two observation vectors.
        /// </summary>
        /// <param name="observation1">The first observation vector.</param>
        /// <param name="observation2">The second observation vector.</param>
        /// <returns></returns>
        private static double ComputeEuclideanObservationDifference(IReadOnlyList<double> observation1,
            IReadOnlyList<double> observation2)
        {
            double distance = 0;

            // The length of the longest observation vector
            var maxObservationLength = Math.Max(observation1.Count, observation2.Count);

            // Compute the euclidean distance between each element of the observation vectors
            for (var idx = 0; idx < maxObservationLength; idx++)
            {
                // Handle the case where there are no more observations in observation vector 1
                if (idx >= observation1.Count)
                {
                    distance += Math.Pow(observation2[idx], 2);
                }
                // Handle the case where there are no more observations in observation vector 2
                else if (idx >= observation2.Count)
                {
                    distance += Math.Pow(observation1[idx], 2);
                }
                // Otherwise, there's an observation in both vectors for the current index
                else
                {
                    distance += Math.Pow(observation2[idx] - observation1[idx], 2);
                }
            }

            // Return the euclidean distance between the two observation vectors
            return Math.Sqrt(distance);
        }

        /// <summary>
        ///     Computes the euclidean distance between two trajectories by summing the euclidean distance between each point in
        ///     their simulation and dividing by the number of simulation timesteps.
        /// </summary>
        /// <param name="trajectory1">The first trajectory.</param>
        /// <param name="trajectory2">The second trajectory.</param>
        /// <returns>The euclidean distance between the two trajectories.</returns>
        private static double ComputeEuclideanTrajectoryDifference(IReadOnlyList<double> trajectory1,
            IReadOnlyList<double> trajectory2)
        {
            double trajectoryDistance = 0;

            // Number of timesteps is equivalent to the longest simulation
            var timesteps = Math.Max(trajectory1.Count / 2, trajectory2.Count / 2);

            // Compute the euclidean distance between the two trajectories at each point 
            // during the simulation
            for (var idx = 0;
                idx < timesteps;
                idx = idx + 2)
            {
                // Handle the case where the first trajectory has ended
                if (idx >= trajectory1.Count)
                {
                    trajectoryDistance += Math.Sqrt(
                        Math.Pow(
                            trajectory2[idx] - trajectory1[trajectory1.Count - 2],
                            2) +
                        Math.Pow(
                            trajectory2[idx + 1] -
                            trajectory1[trajectory1.Count - 1], 2));
                }
                // Handle the case where the second trajectory has ended
                else if (idx >= trajectory2.Count)
                {
                    trajectoryDistance += Math.Sqrt(
                        Math.Pow(
                            trajectory2[trajectory2.Count - 2] - trajectory1[idx],
                            2) +
                        Math.Pow(
                            trajectory2[trajectory2.Count - 1] -
                            trajectory1[idx + 1], 2));
                }
                // Otherwise, we're still in the simulation time frame for both trajectories
                else
                {
                    trajectoryDistance += Math.Sqrt(
                        Math.Pow(
                            trajectory2[idx] - trajectory1[idx],
                            2) +
                        Math.Pow(
                            trajectory2[idx + 1] -
                            trajectory1[idx + 1], 2));
                }
            }

            // Return the euclidean distance between the two trajectories
            return trajectoryDistance / timesteps;
        }

        #endregion
    }
}