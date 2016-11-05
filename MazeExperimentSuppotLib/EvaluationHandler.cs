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
        /// <param name="errorThreshold">
        ///     The maximum error (intracluster variance) threshold.  Additional clusters are added and
        ///     the population reclustered until the resulting error is below the threshold.
        /// </param>
        /// <returns></returns>
        public static ClusterDiversityUnit CalculateNaturalClustering(
            IList<MazeNavigatorEvaluationUnit> evaluationUnits, int errorThreshold)
        {
            KMeans kmeans = null;

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

            // Always start with the standard 3-cluster arrangement
            int clusterCount = 3;

            // Increment the number of clusters until the error (intracluster variance) is below some threshold
            do
            {
                // Create a new k-means instance with the specified number of clusters
                kmeans = new KMeans(clusterCount++);

                // Compute the cluster assignments
                kmeans.Learn(trajectoryMatrix);
            } while (kmeans.Error > errorThreshold);
            // Continue while error (intracluster variance) remains above threshold

            double sumLogProportion = 0.0;

            // Compute the shannon entropy of the population
            for (int idx = 0; idx < kmeans.Clusters.Count; idx++)
            {
                sumLogProportion += kmeans.Clusters[idx].Proportion*Math.Log(kmeans.Clusters[idx].Proportion, 2);
            }

            // Multiply by negative one to get the Shannon entropy
            double shannonEntropy = sumLogProportion*-1;

            // Return the resulting cluster diversity info
            return new ClusterDiversityUnit(kmeans.Clusters.Count, shannonEntropy);
        }

        #endregion

        #region Private methods

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
                        Math.Sqrt(
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
                    trajectoryDistance +=
                        Math.Sqrt(
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
                    trajectoryDistance +=
                        Math.Sqrt(
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