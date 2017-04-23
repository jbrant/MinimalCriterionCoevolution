#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Accord.MachineLearning;
using SharpNeat.Behaviors;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation;
using SharpNeat.Genomes.Maze;

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
                new MazeConfiguration(DataManipulationUtil.ExtractMazeWalls(evaluationUnit.MazePhenome.Walls),
                    DataManipulationUtil.ExtractStartEndPoint(evaluationUnit.MazePhenome.StartLocation),
                    DataManipulationUtil.ExtractStartEndPoint(evaluationUnit.MazePhenome.TargetLocation),
                    evaluationUnit.MazePhenome.MaxTimesteps);

            // Create trajectory behavior characterization (in order to capture full trajectory of navigator)
            IBehaviorCharacterization behaviorCharacterization = new TrajectoryBehaviorCharacterization();

            // Create the maze navigation world
            MazeNavigationWorld<BehaviorInfo> world = new MazeNavigationWorld<BehaviorInfo>(mazeConfiguration.Walls,
                mazeConfiguration.NavigatorLocation, mazeConfiguration.GoalLocation,
                experimentParameters.MinSuccessDistance, mazeConfiguration.MaxSimulationTimesteps, behaviorCharacterization);

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
        ///     Computes the population entropy (shannon entropy) based on the given list of trajectories and number of clusters in
        ///     which to cluster those behaviors using the k-means algorithm.
        /// </summary>
        /// <param name="evaluationUnits">The agent/maze evaluations (trajectories) to cluster.</param>
        /// <param name="numClusters">The number of clusters into which to segregate agent trajectories.</param>
        /// <returns>The population entropy.</returns>
        public static PopulationEntropyUnit CalculatePopulationEntropy(
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int numClusters)
        {
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

            // Initialize k-Means algorithm with the given number of clusters
            var kmeans = new KMeans(numClusters);

            // TODO: The below logic is in support of a work-around to an Accord.NET bug wherein
            // TODO: an internal random number generator sometimes generates out-of-bounds values
            // TODO: (i.e. a probability that is not between 0 and 1)
            // TODO: https://github.com/accord-net/framework/issues/259
            // Use uniform initialization
            kmeans.UseSeeding = Seeding.Uniform;

            // Determine cluster assignments
            kmeans.Learn(trajectoryMatrix);

            // Compute shannon entropy given clustering
            double shannonEntropy = ComputeShannonEntropy(kmeans);

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
                MazeGenomeFactory tempMazeGenomeFactory = new MazeGenomeFactory();

                // Convert genome XML to genome object
                using (XmlReader xr = XmlReader.Create(new StringReader(genomeXml)))
                {
                    curMazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xr, tempMazeGenomeFactory);
                }

                // Add to the genome object list
                mazeGenomes.Add(curMazeGenome);
            }

            // Define observation matrix in which to store all gene coordinate vectors for each maze
            double[][] observationMatrix = new double[mazeGenomes.Count()][];

            // Get the maximum observation vector length (max number of maze genes)
            var maxObservationLength = mazeGenomes.Max(g => g.GeneList.Count());

            for (int idx = 0; idx < mazeGenomes.Count(); idx++)
            {
                // If there are more observations than the total elements in the observation vector, 
                // zero out the rest of the vector
                if (mazeGenomes[idx].GeneList.Count() < maxObservationLength)
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
            double shannonEntropy = ComputeShannonEntropy(optimalClustering);

            // Compute the silhouette width given optimal clustering
            double silhouetteWidth = ComputeSilhouetteWidth(optimalClustering.Clusters, observationMatrix, false);

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
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, bool isGreedySilhouetteCalculation, int clusterRange = 0)
        {
            // Always start with one cluster
            const int initClusterCnt = 1;

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

            // Determine the optimal number of clusters to fit these data
            var optimalClustering = DetermineOptimalClusters(trajectoryMatrix, initClusterCnt, true,
                isGreedySilhouetteCalculation, clusterRange);

            // Compute shannon entropy given optimal clustering
            double shannonEntropy = ComputeShannonEntropy(optimalClustering);

            // Compute the silhouette width given optimal clustering
            double silhouetteWidth = ComputeSilhouetteWidth(optimalClustering.Clusters, trajectoryMatrix, true);

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
            double sumLogProportion = 0.0;

            // Compute the shannon entropy of the population
            for (int idx = 0; idx < clusters.Clusters.Count; idx++)
            {
                sumLogProportion += clusters.Clusters[idx].Proportion > 0
                    ? clusters.Clusters[idx].Proportion*
                      Math.Log(clusters.Clusters[idx].Proportion, 2)
                    : 0;
            }

            // Multiply by negative one to get the Shannon entropy
            return sumLogProportion*-1;
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
            Dictionary<int, double> clusterSilhouetteMap = new Dictionary<int, double>();
            double maxSilhouetteWidth;

            // Initialize current silhouette width
            double curSilhouetteWidth = 0;

            // Set the initial cluster count
            int clusterCount = initialClusterCount;

            do
            {
                // Set max silhouette with the value on the previous loop iteration (since it was an improvement)
                maxSilhouetteWidth = curSilhouetteWidth;

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

            // Rerun kmeans for the final cluster count
            var optimalClustering = new KMeans(clusterCount);

            // TODO: The below logic is in support of a work-around to an Accord.NET bug wherein
            // TODO: an internal random number generator sometimes generates out-of-bounds values
            // TODO: (i.e. a probability that is not between 0 and 1)
            // TODO: https://github.com/accord-net/framework/issues/259
            // Use uniform initialization
            optimalClustering.UseSeeding = Seeding.Uniform;

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
            Object lockObj = new object();
            double totalSilhouetteWidth = 0;

            // Get cluster assignments for all of the observations
            int[] clusterAssignments = clusters.Decide(observations);

            Parallel.For(0, observations.Length, observationIdx =>
            {
                double obsIntraclusterDissimilarity = 0;

                // Get the cluster assignment of the current observation
                int curObsClusterAssignment = clusterAssignments[observationIdx];

                // Only add observation silhouette width if it is NOT the sole member of its assigned cluster
                if (clusterAssignments.Count(ca => ca == curObsClusterAssignment) > 1)
                {
                    // Setup list to hold average distance from current observation to every other neighboring cluster
                    List<double> neighboringClusterDistances = new List<double>(clusters.Count);

                    for (int clusterIdx = 0; clusterIdx < clusters.Count; clusterIdx++)
                    {
                        // Handle the case where the current cluster is the cluster of which the observation is a member
                        if (clusterIdx == curObsClusterAssignment)
                        {
                            // Sum the distance between current observation and every other observation in the same cluster
                            for (int caIdx = 0; caIdx < clusterAssignments.Length; caIdx++)
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
                            for (int caIdx = 0; caIdx < clusterAssignments.Length; caIdx++)
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
                            neighboringClusterDistances.Add(curObsNeighboringClusterDissimilarity/
                                                            clusterAssignments.Where(ca => ca == clusterIdx).Count());
                        }
                    }

                    // Compute the average intracluster dissimilarity (local variance)
                    obsIntraclusterDissimilarity = obsIntraclusterDissimilarity/
                                                   clusterAssignments.Where(ca => ca == curObsClusterAssignment)
                                                       .Count();

                    // Get the minimum intercluster dissimilarity (0 if there are no centroid differences)
                    var obsInterClusterDissimilarity = neighboringClusterDistances.Any()
                        ? neighboringClusterDistances.Min()
                        : 0;

                    // Compute the silhouette width for the current observation
                    // If its the only point in the cluster, then the silhouette width is 0
                    var curSilhouetteWidth = Math.Abs(obsIntraclusterDissimilarity) < 0.0000001
                        ? 0
                        : (obsInterClusterDissimilarity -
                           obsIntraclusterDissimilarity)/
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
            return totalSilhouetteWidth/observations.Length;
        }

        /// <summary>
        ///     Generic euclidean distance calculation that computes the euclidean distance between two observation vectors.
        /// </summary>
        /// <param name="observation1">The first observation vector.</param>
        /// <param name="observation2">The second observation vector.</param>
        /// <returns></returns>
        private static double ComputeEuclideanObservationDifference(double[] observation1, double[] observation2)
        {
            double distance = 0;

            // The length of the longest observation vector
            int maxObservationLength = Math.Max(observation1.Length, observation2.Length);

            // Compute the euclidean distance between each element of the observation vectors
            for (int idx = 0; idx < maxObservationLength; idx++)
            {
                // Handle the case where there are no more observations in observation vector 1
                if (idx >= observation1.Length)
                {
                    distance += Math.Pow(observation2[idx], 2);
                }
                // Handle the case where there are no more observations in observation vector 2
                else if (idx >= observation2.Length)
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
                    trajectoryDistance += Math.Sqrt(
                        Math.Pow(
                            (trajectory2[idx] - trajectory1[trajectory1.Length - 2]),
                            2) +
                        Math.Pow(
                            (trajectory2[idx + 1] -
                             trajectory1[trajectory1.Length - 1]), 2));
                }
                // Handle the case where the second trajectory has ended
                else if (idx >= trajectory2.Length)
                {
                    trajectoryDistance += Math.Sqrt(
                        Math.Pow(
                            (trajectory2[trajectory2.Length - 2] - trajectory1[idx]),
                            2) +
                        Math.Pow(
                            (trajectory2[trajectory2.Length - 1] -
                             trajectory1[idx + 1]), 2));
                }
                // Otherwise, we're still in the simulation time frame for both trajectories
                else
                {
                    trajectoryDistance += Math.Sqrt(
                        Math.Pow(
                            (trajectory2[idx] - trajectory1[idx]),
                            2) +
                        Math.Pow(
                            (trajectory2[idx + 1] -
                             trajectory1[idx + 1]), 2));
                }
            }

            // Return the euclidean distance between the two trajectories
            return trajectoryDistance/timesteps;
        }

        #endregion
    }
}