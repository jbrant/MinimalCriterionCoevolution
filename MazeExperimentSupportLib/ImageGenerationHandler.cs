#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using SharpNeat.Core;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeExperimentSupportLib
{
    /// <summary>
    ///     Handles generation of bitmap images of agent trajectories through the given maze structures.
    /// </summary>
    public static class ImageGenerationHandler
    {
        /// <summary>
        ///     Generates bitmap images depicting the path for all successful trials during a given batch.
        /// </summary>
        /// <param name="baseDirectory">The base directory into which write images.</param>
        /// <param name="experimentName">The name of the experiment being analyzed.</param>
        /// <param name="experimentId">The ID of the experiment being analyzed.</param>
        /// <param name="run">The run during which the trial took place.</param>
        /// <param name="evaluationUnits">The maze/agent combinations to trace.</param>
        /// <param name="runPhase">The run phase (initialization or primary) for the current set of trials.</param>
        /// <param name="startEndPointSize">The size of the start and end location points (optional).</param>
        /// <param name="trajectoryPointSize">The size of a point on the agent's trajectory (optional).</param>
        public static void GenerateBitmapsForSuccessfulTrials(string baseDirectory, string experimentName,
            int experimentId, int run, IList<MazeNavigatorEvaluationUnit> evaluationUnits, RunPhase runPhase,
            int startEndPointSize = 10, double trajectoryPointSize = 2.5)
        {
            GenerateBitmapsForSuccessfulTrials(baseDirectory, experimentName, experimentId, run, 0, evaluationUnits,
                runPhase, startEndPointSize, trajectoryPointSize);
        }

        /// <summary>
        ///     Generates bitmap images depicting the path for all successful trials during a given batch.
        /// </summary>
        /// <param name="baseDirectory">The base directory into which write images.</param>
        /// <param name="experimentName">The name of the experiment being analyzed.</param>
        /// <param name="experimentId">The ID of the experiment being analyzed.</param>
        /// <param name="run">The run during which the trial took place.</param>
        /// <param name="batch">The batch during which the trial took place.</param>
        /// <param name="evaluationUnits">The maze/agent combinations to trace.</param>
        /// <param name="runPhase">The run phase (initialization or primary) for the current set of trials.</param>
        /// <param name="startEndPointSize">The size of the start and end location points (optional).</param>
        /// <param name="trajectoryPointSize">The size of a point on the agent's trajectory (optional).</param>
        public static void GenerateBitmapsForSuccessfulTrials(string baseDirectory, string experimentName,
            int experimentId, int run, int batch, IList<MazeNavigatorEvaluationUnit> evaluationUnits, RunPhase runPhase,
            int startEndPointSize = 10, double trajectoryPointSize = 2.5)
        {
            // Construct the output directory path
            var outputDirectory = Path.Combine(baseDirectory, experimentName,
                string.Format("Run {0}", run), "Trajectories", runPhase.ToString(),
                batch != 0 ? string.Format("Batch {0}", batch) : "");

            // Create the output directory if it doesn't yet exist
            if (Directory.Exists(outputDirectory) == false)
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Evaluate only one successful agent trial per distinct maze
            // (otherwise, there would be millions of generated images)
            var distinctMazeEvaluationUnits =
                evaluationUnits.Where(eu => eu.IsMazeSolved).GroupBy(eu => eu.MazeId).Select(eu => eu.First()).ToList();

            // Generate bitmap images for each maze/navigator combination that resulted in a successful trial
            Parallel.ForEach(distinctMazeEvaluationUnits,
                delegate(MazeNavigatorEvaluationUnit evaluationUnit)
                {
                    GenerateSingleMazeTrajectoryImage(
                        Path.Combine(outputDirectory,
                            batch != 0
                                ? string.Format("{0}_ExperimentID_{1}_Run_{2}_Batch_{3}_MazeID_{4}_NavigatorID_{5}.png",
                                    experimentName, experimentId, run, batch, evaluationUnit.MazeId,
                                    evaluationUnit.AgentId)
                                : string.Format("{0}_ExperimentID_{1}_Run_{2}_MazeID_{3}_NavigatorID_{4}.png",
                                    experimentName, experimentId, run, evaluationUnit.MazeId, evaluationUnit.AgentId)),
                        evaluationUnit.MazePhenome, evaluationUnit.AgentTrajectory, startEndPointSize,
                        trajectoryPointSize);
                });
        }

        /// <summary>
        ///     Generates bitmap images of the extant maze population.
        /// </summary>
        /// <param name="baseDirectory">The base directory into which write images.</param>
        /// <param name="experimentName">The name of the experiment being analyzed.</param>
        /// <param name="experimentId">The ID of the experiment being analyzed.</param>
        /// <param name="run">The run during which the trial took place.</param>
        /// <param name="evaluationUnits">The maze/agent combinations to trace.</param>
        public static void GenerateMazeBitmaps(string baseDirectory, string experimentName, int experimentId, int run,
            IList<MazeNavigatorEvaluationUnit> evaluationUnits)
        {
            GenerateMazeBitmaps(baseDirectory, experimentName, experimentId, run, 0, evaluationUnits);
        }

        /// <summary>
        ///     Generates bitmap images of the extant maze population.
        /// </summary>
        /// <param name="baseDirectory">The base directory into which write images.</param>
        /// <param name="experimentName">The name of the experiment being analyzed.</param>
        /// <param name="experimentId">The ID of the experiment being analyzed.</param>
        /// <param name="run">The run during which the trial took place.</param>
        /// <param name="batch">The batch during which the trial took place.</param>
        /// <param name="evaluationUnits">The maze/agent combinations to trace.</param>
        public static void GenerateMazeBitmaps(string baseDirectory, string experimentName, int experimentId, int run,
            int batch, IList<MazeNavigatorEvaluationUnit> evaluationUnits)
        {
            // Construct the output directory path
            var outputDirectory = Path.Combine(baseDirectory, experimentName,
                string.Format("Run {0}", run), "Mazes", batch != 0 ? string.Format("Batch {0}", batch) : "");

            // Create the output directory if it doesn't yet exist
            if (Directory.Exists(outputDirectory) == false)
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Get the distinct list of maze IDs
            var mazeIds = evaluationUnits.Select(unit => unit.MazeId).Distinct().ToList();

            // Generate bitmap image for each distinct maze
            Parallel.ForEach(mazeIds, delegate(int mazeId)
            {
                GenerateMazeStructureImage(
                    Path.Combine(outputDirectory,
                        batch != 0
                            ? string.Format("{0}_ExperimentID_{1}_Run_{2}_Batch_{3}_MazeID_{4}.png", experimentName,
                                experimentId, run, batch, mazeId)
                            : string.Format("{0}_ExperimentID_{1}_Run_{2}_MazeID_{3}.png", experimentName, experimentId,
                                run, mazeId)),
                    evaluationUnits.Where(unit => unit.MazeId == mazeId).Select(unit => unit.MazePhenome).First(),
                    false);
            });
        }

        /// <summary>
        ///     Generates a single image of the trajectory of an agent through the given maze.
        /// </summary>
        /// <param name="imagePathName">Image path and filename.</param>
        /// <param name="mazeStructure">The structure of the maze on which the trial was run.</param>
        /// <param name="agentTrajectory">The trajectory of the agent through the maze.</param>
        /// <param name="startEndPointSize">The size of the start and end location points.</param>
        /// <param name="trajectoryPointSize">The size of a point on the agent's trajectory.</param>
        private static void GenerateSingleMazeTrajectoryImage(string imagePathName, MazeStructure mazeStructure,
            double[] agentTrajectory, int startEndPointSize, double trajectoryPointSize)
        {
            // Compute radius from start/end point size
            var startEndPointRadius = startEndPointSize / 2;

            using (var image = new MagickImage(MagickColors.White,
                mazeStructure.ScaledMazeWidth + 1, mazeStructure.ScaledMazeHeight + 1))
            {
                // Draw border around maze
                image.Draw(new DrawableFillColor(MagickColors.Transparent),
                    new DrawableRectangle(0, 0, mazeStructure.ScaledMazeWidth + 1, mazeStructure.ScaledMazeHeight + 1));

                // Draw start and end points
                image.Draw(new DrawableFillColor(MagickColors.Green),
                    new DrawableEllipse(mazeStructure.StartLocation.X, mazeStructure.StartLocation.Y,
                        startEndPointRadius, startEndPointRadius, 0, 360));
                image.Draw(new DrawableFillColor(MagickColors.Red),
                    new DrawableEllipse(mazeStructure.TargetLocation.X, mazeStructure.TargetLocation.Y,
                        startEndPointRadius, startEndPointRadius, 0, 360));

                // Draw walls
                foreach (var wall in mazeStructure.Walls)
                {
                    image.Draw(new DrawableLine(wall.StartMazeStructurePoint.X, wall.StartMazeStructurePoint.Y,
                        wall.EndMazeStructurePoint.X, wall.EndMazeStructurePoint.Y));
                }

                // Plot the navigator trajectory
                for (var i = 0; i < agentTrajectory.Length; i = i + 2)
                {
                    var topX = agentTrajectory[i] - trajectoryPointSize / 2;
                    var topY = agentTrajectory[i + 1] - trajectoryPointSize / 2;
                    var bottomX = agentTrajectory[i] + trajectoryPointSize / 2;
                    var bottomY = agentTrajectory[i + 1] + trajectoryPointSize / 2;

                    image.Draw(new DrawableFillColor(MagickColors.Gray),
                        new DrawableRectangle(topX, topY, bottomX, bottomY));
                }

                // Save the image
                image.Write(imagePathName);
            }
        }

        /// <summary>
        ///     Generates an image of the given maze structure (no agent trajectory).
        /// </summary>
        /// <param name="imagePathName">Image path and filename.</param>
        /// <param name="mazeStructure">The structure of the maze.</param>
        /// <param name="drawSolutionPath">Flag which controls whether or not solution trajectory is rendered.</param>
        public static void GenerateMazeStructureImage(string imagePathName, MazeStructure mazeStructure,
            bool drawSolutionPath)
        {
            var defaultStartEndPointRadius = 5;
            var defaultTrajectoryPointSize = 2.5;

            using (var image = new MagickImage(MagickColors.White,
                mazeStructure.ScaledMazeWidth + 1, mazeStructure.ScaledMazeHeight + 1))
            {
                // Draw border around maze
                image.Draw(new DrawableFillColor(MagickColors.Transparent),
                    new DrawableRectangle(0, 0, mazeStructure.ScaledMazeWidth + 1, mazeStructure.ScaledMazeHeight + 1));

                // Draw start and end points
                image.Draw(new DrawableFillColor(MagickColors.Green),
                    new DrawableEllipse(mazeStructure.StartLocation.X, mazeStructure.StartLocation.Y,
                        defaultStartEndPointRadius, defaultStartEndPointRadius, 0, 360));
                image.Draw(new DrawableFillColor(MagickColors.Red),
                    new DrawableEllipse(mazeStructure.TargetLocation.X, mazeStructure.TargetLocation.Y,
                        defaultStartEndPointRadius, defaultStartEndPointRadius, 0, 360));

                // Draw walls
                foreach (var wall in mazeStructure.Walls)
                {
                    image.Draw(new DrawableLine(wall.StartMazeStructurePoint.X, wall.StartMazeStructurePoint.Y,
                        wall.EndMazeStructurePoint.X, wall.EndMazeStructurePoint.Y));
                }

                // Add solution path to image if enabled
                if (drawSolutionPath)
                {
                    // Draw the solution trajectory/path
                    for (var y = 0; y < mazeStructure.MazeGrid.Grid.GetLength(0); y++)
                    {
                        for (var x = 0; x < mazeStructure.MazeGrid.Grid.GetLength(1); x++)
                        {
                            if (PathDirection.None != mazeStructure.MazeGrid.Grid[y, x].PathDirection)
                            {
                                var topX = x * mazeStructure.ScaleMultiplier + mazeStructure.ScaleMultiplier / 2 -
                                           defaultTrajectoryPointSize / 2;
                                var topY = y * mazeStructure.ScaleMultiplier + mazeStructure.ScaleMultiplier / 2 -
                                           defaultTrajectoryPointSize / 2;
                                var bottomX = x * mazeStructure.ScaleMultiplier + mazeStructure.ScaleMultiplier / 2 +
                                              defaultTrajectoryPointSize / 2;
                                var bottomY = y * mazeStructure.ScaleMultiplier + mazeStructure.ScaleMultiplier / 2 +
                                              defaultTrajectoryPointSize / 2;

                                image.Draw(new DrawableFillColor(MagickColors.DarkViolet),
                                    new DrawableRectangle(topX, topY, bottomX, bottomY));
                            }
                        }
                    }
                }

                // Save the image
                image.Write(imagePathName);
            }
        }
    }
}