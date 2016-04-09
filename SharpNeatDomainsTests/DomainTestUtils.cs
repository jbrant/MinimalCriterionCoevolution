#region

using System.Drawing;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeatDomainsTests
{
    public class DomainTestUtils
    {
        public static void PrintMazeAndTrajectory(MazeStructure maze, double[] trajectory,
            string outputFile)
        {
            Pen blackPen = new Pen(Color.Black, 0.0001f);
            Bitmap mazeBitmap = new Bitmap(maze.ScaledMazeWidth + 1, maze.ScaledMazeHeight + 1);

            using (Graphics graphics = Graphics.FromImage(mazeBitmap))
            {
                // Fill with white
                Rectangle imageSize = new Rectangle(0, 0, maze.ScaledMazeWidth + 1, maze.ScaledMazeHeight + 1);
                graphics.FillRectangle(Brushes.White, imageSize);

                // Draw start and end points
                graphics.FillEllipse(Brushes.Green, maze.StartLocation.X, maze.StartLocation.Y, 5, 5);
                graphics.FillEllipse(Brushes.Red, maze.TargetLocation.X, maze.TargetLocation.Y, 5, 5);
            }

            // Draw all of the walls
            foreach (MazeStructureWall wall in maze.Walls)
            {
                // Convert line start/end points to Point objects from drawing namespace
                Point startPoint = new Point(wall.StartMazeStructurePoint.X, wall.StartMazeStructurePoint.Y);
                Point endPoint = new Point(wall.EndMazeStructurePoint.X, wall.EndMazeStructurePoint.Y);

                using (Graphics graphics = Graphics.FromImage(mazeBitmap))
                {
                    // Draw wall
                    graphics.DrawLine(blackPen, startPoint, endPoint);
                }
            }

            // Plot the navigator trajectory
            for (int i = 0; i < trajectory.Length; i = i + 2)
            {
                using (Graphics graphics = Graphics.FromImage(mazeBitmap))
                {
                    // Draw trajectory point
                    graphics.FillRectangle(Brushes.Gray, (float) trajectory[i], (float) trajectory[i + 1], 1, 1);
                }
            }

            mazeBitmap.Save(outputFile);
        }
    }
}