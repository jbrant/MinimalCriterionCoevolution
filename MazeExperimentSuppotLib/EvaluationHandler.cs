#region

using System.Collections.Generic;
using System.Linq;
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
    }
}