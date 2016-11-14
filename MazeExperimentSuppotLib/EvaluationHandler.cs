#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.MachineLearning;
using SharpNeat.Behaviors;
using SharpNeat.Core;
using SharpNeat.Domains;
using SharpNeat.Domains.MazeNavigation;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeExperimentSuppotLib
{
    public class EvaluationHandler
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
            bool isGoalReached;

            // Build maze configuration
            MazeConfiguration mazeConfiguration =
                new MazeConfiguration(ExtractMazeWalls(evaluationUnit.MazePhenome.Walls),
                    ExtractStartEndPoint(evaluationUnit.MazePhenome.StartLocation),
                    ExtractStartEndPoint(evaluationUnit.MazePhenome.TargetLocation));

            // Create trajectory behavior characterization (in order to capture full trajectory of navigator)
            IBehaviorCharacterization behaviorCharacterization = new TrajectoryBehaviorCharacterization();

            // Create the maze navigation world
            MazeNavigationWorld<BehaviorInfo> world = new MazeNavigationWorld<BehaviorInfo>(mazeConfiguration.Walls,
                mazeConfiguration.NavigatorLocation, mazeConfiguration.GoalLocation,
                experimentParameters.MinSuccessDistance, experimentParameters.MaxTimesteps, behaviorCharacterization);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(evaluationUnit.AgentPhenome, SearchType.MinimalCriteriaSearch,
                out isGoalReached);

            // Set maze solved status
            evaluationUnit.IsMazeSolved = isGoalReached;

            // The number of time steps is effectively the number of 2-dimensional points in the behaviors array
            evaluationUnit.NumTimesteps = trialInfo.Behaviors.Count()/2;

            // Set the trajectory of the agent
            evaluationUnit.AgentTrajectory = trialInfo.Behaviors;
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

            foreach (MazeNavigatorEvaluationUnit evaluationUnit in evaluationUnits.Where(u => u.IsMazeSolved))
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

                    // Caculate trajectory difference for same maze
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
                int intraMazeTrajectoryCount = evaluationUnits.Count(unit =>
                    unit.IsMazeSolved && unit.Equals(evaluationUnit) == false &&
                    unit.MazeId == evaluationUnit.MazeId);
                int interMazeTrajectoryCount = evaluationUnits.Count(
                    unit =>
                        unit.IsMazeSolved && unit.MazeId != evaluationUnit.MazeId);
                int globalMazeTrajectoryCount = evaluationUnits.Count(unit => unit.IsMazeSolved);

                // Calculate intra-maze, inter-maze, and global trajectory diversity scores 
                // and instantiate trajectory diversity unit
                TrajectoryDiversityUnit trajectoryDiversityUnit = new TrajectoryDiversityUnit(evaluationUnit.AgentId,
                    evaluationUnit.MazeId, intraMazeTrajectoryCount == 0
                        ? 0
                        : intraMazeTotalTrajectoryDifference/intraMazeTrajectoryCount, interMazeTrajectoryCount == 0
                            ? 0
                            : interMazeTotalTrajectoryDifference/interMazeTrajectoryCount,
                    globalMazeTrajectoryCount == 0
                        ? 0
                        : (intraMazeTotalTrajectoryDifference + interMazeTotalTrajectoryDifference)/
                          globalMazeTrajectoryCount);

                // Add trajectory diversity unit to list
                trajectoryDiversityUnits.Add(trajectoryDiversityUnit);
            }

            return trajectoryDiversityUnits;
        }

        /// <summary>
        ///     Computes the natural clustering of the resulting trajectories (behaviors) along with the behavioral entropy
        ///     (diversity).
        /// </summary>
        /// <param name="evaluationUnits">The agent/maze evaluations to cluster and compute entropy.</param>
        /// <param name="clusterImprovementThreshold">
        ///     The number of cluster additions that are permitted without further
        ///     maximization of silhouette width.  When this is exceeded, the incremental cluster additions will stop and the
        ///     number of clusters resulting in the highest silhouette width will be considered optimal.
        /// </param>
        /// <returns></returns>
        public static ClusterDiversityUnit CalculateNaturalClustering(
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int clusterImprovementThreshold)
        {
            Dictionary<int, double> clusterSilhoutteMap = new Dictionary<int, double>();
            Tuple<int, double> clusterWithMaxSilhouetteWidth = null;

            // Always start with zero clusters and increment on first iteration of loop
            const int initClusterCnt = 0;

            // Only consider successful trials
            IList<MazeNavigatorEvaluationUnit> successfulEvaluations =
                evaluationUnits.Where(eu => eu.IsMazeSolved).ToList();

            // Define the trajectory matrix in which to store all trajectory points for each trajectory
            // (this becomes a collection of observation vectors that's fed into k-means)
            double[][] trajectoryMatrix = new double[successfulEvaluations.Count][];

            // Get the maximum observation vector length (max simulation runtime)
            // (multiplied by 2 to account for each timestep containing a 2-dimensional position)
            int maxObservationLength = successfulEvaluations.Max(x => x.NumTimesteps)*2;

            for (int idx = 0; idx < successfulEvaluations.Count; idx++)
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

            // Set the initial cluster count
            int clusterCount = initClusterCnt;

            // Continue loop until the maximum number of iterations without silhouette width improvement has elapsed
            // (also don't allow number of clusters to match the number of observations)
            while (clusterWithMaxSilhouetteWidth == null || (clusterSilhoutteMap.Count <= clusterImprovementThreshold ||
                                                             clusterSilhoutteMap.Where(
                                                                 csm =>
                                                                     (csm.Key - initClusterCnt) >=
                                                                     clusterSilhoutteMap.Count -
                                                                     clusterImprovementThreshold)
                                                                 .Any(
                                                                     csm =>
                                                                         csm.Value >=
                                                                         clusterWithMaxSilhouetteWidth.Item2)) &&
                   clusterCount < trajectoryMatrix.Length - 1)
            {
                // Increment cluster count
                clusterCount++;

                // Create a new k-means instance with the specified number of clusters
                var kmeans = new KMeans(clusterCount);

                // TODO: The below logic is in support of a work-around to an Accord.NET bug wherein
                // TODO: an internal random number generator sometimes generates out-of-bounds values
                // TODO: (i.e. a probability that is not between 0 and 1)
                // TODO: https://github.com/accord-net/framework/issues/259
                // Use uniform initialization
                kmeans.UseSeeding = Seeding.Uniform;

                // Determine the resulting clusters
                var clusters = kmeans.Learn(trajectoryMatrix);

                // Compute the silhouette width for the current number of clusters
                double silhouetteWidth = ComputeSilhouetteWidth(clusters, trajectoryMatrix);

                // Compute silhouette width and add to map with the current cluster count
                clusterSilhoutteMap.Add(clusterCount, silhouetteWidth);

                // If greater than the max silhouette width, reset the cluster with the max
                if (clusterWithMaxSilhouetteWidth == null || silhouetteWidth > clusterWithMaxSilhouetteWidth.Item2)
                {
                    clusterWithMaxSilhouetteWidth = new Tuple<int, double>(clusterCount, silhouetteWidth);
                }                
            }

            // Rerun kmeans for the final cluster count
            var optimalClustering = new KMeans(clusterCount);

            // TODO: The below logic is in support of a work-around to an Accord.NET bug wherein
            // TODO: an internal random number generator sometimes generates out-of-bounds values
            // TODO: (i.e. a probability that is not between 0 and 1)
            // TODO: https://github.com/accord-net/framework/issues/259
            // Use uniform initialization
            optimalClustering.UseSeeding = Seeding.Uniform;

            // Determine cluster assignments
            optimalClustering.Learn(trajectoryMatrix);

            double sumLogProportion = 0.0;

            // Compute the shannon entropy of the population
            for (int idx = 0; idx < optimalClustering.Clusters.Count; idx++)
            {
                sumLogProportion += optimalClustering.Clusters[idx].Proportion*
                                    Math.Log(optimalClustering.Clusters[idx].Proportion, 2);
            }

            // Multiply by negative one to get the Shannon entropy
            double shannonEntropy = sumLogProportion*-1;

            // Return the resulting cluster diversity info
            return new ClusterDiversityUnit(optimalClustering.Clusters.Count, shannonEntropy);
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Computes the silhouette width for the given set of clusters and observations.
        /// </summary>
        /// <param name="clusters">The clusters in the dataset.</param>
        /// <param name="observations">The observation vectors.</param>
        /// <returns>The silhouette width for the given set of clusters and observations.</returns>
        private static double ComputeSilhouetteWidth(KMeansClusterCollection clusters, double[][] observations)
        {
            Object lockObj = new object();
            double totalSilhouetteWidth = 0;

            // Get cluster assignments for all of the observations
            int[] clusterAssignments = clusters.Decide(observations);

            Parallel.For(0, observations.Length, observationIdx =>
            {
                double obsIntraclusterDissimilarity = 0;
                double obsInterClusterDissimilarity = 0;

                // Sum the distance between current observation and every other observation in the same cluster
                for (int caIdx = 0; caIdx < clusterAssignments.Length; caIdx++)
                {
                    if (clusterAssignments[caIdx] == clusterAssignments[observationIdx])
                    {
                        obsIntraclusterDissimilarity +=
                            ComputeEuclideanTrajectoryDifference(observations[observationIdx], observations[caIdx]);
                    }
                }

                // Compute the average intracluster dissimilarity (local variance)
                obsIntraclusterDissimilarity = obsIntraclusterDissimilarity/
                                               clusterAssignments.Where(ca => ca == clusterAssignments[observationIdx])
                                                   .Count();

                // Setup list to hold distance from current observation to every other cluster centroid
                List<double> centroidDistances = new List<double>(clusters.Count);

                // Sum the distance between current observation and cluster centroids to which the current
                // observation is NOT assigned
                for (int idx = 0; idx < clusters.Count; idx++)
                {
                    // Only compute distance when observation is not assigned to the current cluster
                    if (idx != clusterAssignments[observationIdx])
                    {
                        centroidDistances.Add(ComputeEuclideanTrajectoryDifference(observations[observationIdx],
                            clusters[idx].Centroid));
                    }
                }

                // Get the minimum intercluster dissimilarity (0 if there are no centroid differences)
                obsInterClusterDissimilarity = centroidDistances.Any() ? centroidDistances.Min() : 0;

                // Add the silhoutte width for the current observation
                var curSilhouetteWidth = (Math.Abs(obsIntraclusterDissimilarity) < 0.0000001 &&
                                          Math.Abs(obsInterClusterDissimilarity) < 0.0000001)
                    ? 0
                    : (obsInterClusterDissimilarity -
                       obsIntraclusterDissimilarity)/
                      Math.Max(obsIntraclusterDissimilarity,
                          obsInterClusterDissimilarity);

                lock (lockObj)
                {
                    totalSilhouetteWidth += curSilhouetteWidth;
                }
            });

            // Return the silhoutte width
            return totalSilhouetteWidth/observations.Length;
        }

        /// <summary>
        ///     Computes the euclidean distance between two trajectories by summing the euclidean distance between each point in
        ///     their simulation and dividing by the number of simulation timesteps.
        /// </summary>
        /// <param name="trajectory1">The first trajectory.</param>
        /// <param name="trajectory2">The second trajectory.</param>
        /// <returns>The euclidean distance between the two trajectories.</returns>
        private static double ComputeEuclideanTrajectoryDifference(double[] trajectory1, double[] trajectory2)
        {
            double trajectoryDistance = 0;

            // Number of timesteps is equivalent to the longest simulation
            int timesteps = Math.Max(trajectory1.Length/2, trajectory2.Length/2);

            // Compute the euclidean distance between the two trajectories at each point 
            // during the simulation
            for (int idx = 0;
                idx < timesteps;
                idx = idx + 2)
            {
                // Handle the case where the first trajectory has ended
                if (idx >= trajectory1.Length)
                {
                    trajectoryDistance +=
                        Math.Pow(
                            (trajectory2[idx] - trajectory1[trajectory1.Length - 2]),
                            2) +
                        Math.Pow(
                            (trajectory2[idx + 1] -
                             trajectory1[trajectory1.Length - 1]), 2);
                }
                // Handle the case where the second trajectory has ended
                else if (idx >= trajectory2.Length)
                {
                    trajectoryDistance +=
                        Math.Pow(
                            (trajectory2[trajectory2.Length - 2] - trajectory1[idx]),
                            2) +
                        Math.Pow(
                            (trajectory2[trajectory2.Length - 1] -
                             trajectory1[idx + 1]), 2);
                }
                // Otherwise, we're still in the simulation time frame for both trajectories
                else
                {
                    trajectoryDistance +=
                        Math.Pow(
                            (trajectory2[idx] - trajectory1[idx]),
                            2) +
                        Math.Pow(
                            (trajectory2[idx + 1] -
                             trajectory1[idx + 1]), 2);
                }
            }

            // Return the euclidean distance between the two trajectories
            return Math.Sqrt(trajectoryDistance);
        }

        /// <summary>
        ///     Converts the evolved walls into experiment domain walls so that experiment-specific calculations can be applied on
        ///     them.
        /// </summary>
        /// <param name="mazeStructureWalls">The evolved walls.</param>
        /// <returns>List of the experiment-specific walls.</returns>
        private static List<Wall> ExtractMazeWalls(List<MazeStructureWall> mazeStructureWalls)
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
        private static DoublePoint ExtractStartEndPoint(MazeStructurePoint point)
        {
            return new DoublePoint(point.X, point.Y);
        }

        #endregion
    }
}