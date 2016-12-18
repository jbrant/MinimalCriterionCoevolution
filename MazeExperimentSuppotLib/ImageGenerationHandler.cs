#region

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpNeat.Core;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeExperimentSuppotLib
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
        /// <param name="batch">The batch during which the trial took place.</param>
        /// <param name="evaluationUnits">The maze/agent combinations to trace.</param>
        /// <param name="runPhase">The run phase (initialization or primary) for the current set of trials.</param>
        public static void GenerateBitmapsForSuccessfulTrials(string baseDirectory, string experimentName,
            int experimentId, int run,
            int batch, IList<MazeNavigatorEvaluationUnit> evaluationUnits, RunPhase runPhase)
        {
            // Construct the output directory path
            string outputDirectory = Path.Combine(baseDirectory, experimentName,
                string.Format("Run {0}", run), "Trajectories", runPhase.ToString(), string.Format("Batch {0}", batch));

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
                            string.Format("{0}_ExperimentID_{1}_Run_{2}_Batch_{3}_MazeID_{4}_NavigatorID_{5}.bmp",
                                experimentName, experimentId, run, batch, evaluationUnit.MazeId, evaluationUnit.AgentId)),
                        evaluationUnit.MazePhenome, evaluationUnit.AgentTrajectory);
                });
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
            string outputDirectory = Path.Combine(baseDirectory, experimentName,
                string.Format("Run {0}", run), "Mazes", string.Format("Batch {0}", batch));

            // Create the output directory if it doesn't yet exist
            if (Directory.Exists(outputDirectory) == false)
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Get the distinct list of maze IDs
            List<int> mazeIds = evaluationUnits.Select(unit => unit.MazeId).Distinct().ToList();

            // Generate bitmap image for each distinct maze
            Parallel.ForEach(mazeIds, delegate(int mazeId)
            {
                GenerateMazeStructureImage(
                    Path.Combine(outputDirectory,
                        string.Format("{0}_ExperimentID_{1}_Run_{2}_Batch_{3}_MazeID_{4}.bmp", experimentName,
                            experimentId, run, batch, mazeId)),
                    evaluationUnits.Where(unit => unit.MazeId == mazeId).Select(unit => unit.MazePhenome).First());
            });
        }

        /// <summary>
        ///     Generates a single bitmap image of the trajectory of an agent through the given maze.
        /// </summary>
        /// <param name="imagePathName">Image path and filename.</param>
        /// <param name="mazeStructure">The structure of the maze on which the trial was run.</param>
        /// <param name="agentTrajectory">The trajectory of the agent through the maze.</param>
        private static void GenerateSingleMazeTrajectoryImage(string imagePathName, MazeStructure mazeStructure,
            double[] agentTrajectory)
        {
            // Create pen and initialize bitmap canvas
            Pen blackPen = new Pen(Color.Black, 0.0001f);
            Bitmap mazeBitmap = new Bitmap(mazeStructure.ScaledMazeWidth + 1, mazeStructure.ScaledMazeHeight + 1);

            using (Graphics graphics = Graphics.FromImage(mazeBitmap))
            {
                // Fill with white
                Rectangle imageSize = new Rectangle(0, 0, mazeStructure.ScaledMazeWidth + 1,
                    mazeStructure.ScaledMazeHeight + 1);
                graphics.FillRectangle(Brushes.White, imageSize);

                // Draw start and end points
                graphics.FillEllipse(Brushes.Green, mazeStructure.StartLocation.X, mazeStructure.StartLocation.Y, 5, 5);
                graphics.FillEllipse(Brushes.Red, mazeStructure.TargetLocation.X, mazeStructure.TargetLocation.Y, 5, 5);

                // Draw all of the walls
                foreach (MazeStructureWall wall in mazeStructure.Walls)
                {
                    // Convert line start/end points to Point objects from drawing namespace
                    Point startPoint = new Point(wall.StartMazeStructurePoint.X, wall.StartMazeStructurePoint.Y);
                    Point endPoint = new Point(wall.EndMazeStructurePoint.X, wall.EndMazeStructurePoint.Y);

                    // Draw wall
                    graphics.DrawLine(blackPen, startPoint, endPoint);
                }

                // Plot the navigator trajectory
                for (int i = 0; i < agentTrajectory.Length; i = i + 2)
                {
                    // Draw trajectory point
                    graphics.FillRectangle(Brushes.Gray, (float) agentTrajectory[i], (float) agentTrajectory[i + 1], 1,
                        1);
                }
            }

            // Save the bitmap image
            mazeBitmap.Save(imagePathName);
        }

        /// <summary>
        ///     Generates a bitmap image of the given maze structure (no agent trajectory).
        /// </summary>
        /// <param name="imagePathName">Image path and filename.</param>
        /// <param name="mazeStructure">The structure of the maze.</param>
        public static void GenerateMazeStructureImage(string imagePathName, MazeStructure mazeStructure)
        {
            // Create pen and initialize bitmap canvas
            Pen blackPen = new Pen(Color.Black, 0.0001f);
            Bitmap mazeBitmap = new Bitmap(mazeStructure.ScaledMazeWidth + 1, mazeStructure.ScaledMazeHeight + 1);

            using (Graphics graphics = Graphics.FromImage(mazeBitmap))
            {
                // Fill with white
                Rectangle imageSize = new Rectangle(0, 0, mazeStructure.ScaledMazeWidth + 1,
                    mazeStructure.ScaledMazeHeight + 1);
                graphics.FillRectangle(Brushes.White, imageSize);

                // Draw start and end points
                graphics.FillEllipse(Brushes.Green, mazeStructure.StartLocation.X, mazeStructure.StartLocation.Y, 5, 5);
                graphics.FillEllipse(Brushes.Red, mazeStructure.TargetLocation.X, mazeStructure.TargetLocation.Y, 5, 5);

                // Draw all of the walls
                foreach (MazeStructureWall wall in mazeStructure.Walls)
                {
                    // Convert line start/end points to Point objects from drawing namespace
                    Point startPoint = new Point(wall.StartMazeStructurePoint.X, wall.StartMazeStructurePoint.Y);
                    Point endPoint = new Point(wall.EndMazeStructurePoint.X, wall.EndMazeStructurePoint.Y);

                    // Draw wall
                    graphics.DrawLine(blackPen, startPoint, endPoint);
                }
            }

            // Save the bitmap image
            mazeBitmap.Save(imagePathName);
        }
    }
}