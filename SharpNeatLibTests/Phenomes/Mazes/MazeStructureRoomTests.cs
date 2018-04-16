#region

using System.Drawing;
using System.IO;
using System.Linq;
using MazeExperimentSupportLib;
using MCC_Domains.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Genomes.Maze;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Phenomes.Mazes.Tests
{
    [TestClass]
    public class MazeStructureRoomTests
    {
        private void produceTrajectory(MazeGenome seedMazeGenome, MazeStructureGridCell[,] grid, int scaleFactor,
            int startEndPointSize, int startLocationXY, string outputPath, string baseName)
        {
            // Compute scaled height/width
            var scaledWidth = seedMazeGenome.MazeBoundaryWidth*scaleFactor;
            var scaledHeight = seedMazeGenome.MazeBoundaryHeight*scaleFactor;

            // Compute the target location points
            var targetLocationX = (seedMazeGenome.MazeBoundaryWidth*scaleFactor) - (scaleFactor/2);
            var targetLocationY = (seedMazeGenome.MazeBoundaryHeight*scaleFactor) - (scaleFactor/2);

            // Create pen and initialize bitmap canvas
            Pen blackPen = new Pen(Color.Black, 0.0001f);
            Bitmap mazeBitmap = new Bitmap(scaledWidth + 1, scaledHeight + 1);

            using (Graphics graphics = Graphics.FromImage(mazeBitmap))
            {
                // Fill with white
                Rectangle imageSize = new Rectangle(0, 0, scaledWidth + 1,
                    scaledHeight + 1);
                graphics.FillRectangle(Brushes.White, imageSize);

                // Draw the path
                for (int y = 0; y < seedMazeGenome.MazeBoundaryHeight; y++)
                {
                    for (int x = 0; x < seedMazeGenome.MazeBoundaryWidth; x++)
                    {
                        if (PathDirection.None != grid[y, x].PathDirection)
                        {
                            if (grid[y, x].IsJuncture)
                            {
                                graphics.FillEllipse(Brushes.Coral, x*scaleFactor + 16, y*scaleFactor + 16,
                                    startEndPointSize, startEndPointSize);
                            }
                            else
                            {
                                graphics.FillEllipse(Brushes.DarkSlateGray, x*scaleFactor + 16, y*scaleFactor + 16,
                                    startEndPointSize, startEndPointSize);
                            }
                        }
                    }
                }

                // Draw start and end points
                graphics.FillEllipse(Brushes.Green, startLocationXY, startLocationXY,
                    startEndPointSize, startEndPointSize);
                graphics.FillEllipse(Brushes.Red, targetLocationX, targetLocationY,
                    startEndPointSize, startEndPointSize);

                // Draw juncture points
                foreach (var pathGene in seedMazeGenome.PathGeneList)
                {
                    if (grid[pathGene.Waypoint.Y, pathGene.Waypoint.X].IsJuncture)
                    {
                        graphics.FillEllipse(Brushes.DarkViolet, (pathGene.Waypoint.X*scaleFactor) + 16,
                            (pathGene.Waypoint.Y*scaleFactor) + 16, startEndPointSize, startEndPointSize);
                    }
                    else
                    {
                        graphics.FillEllipse(Brushes.CornflowerBlue, (pathGene.Waypoint.X*scaleFactor) + 16,
                            (pathGene.Waypoint.Y*scaleFactor) + 16, startEndPointSize, startEndPointSize);
                    }
                }

                // Save the file
                mazeBitmap.Save(Path.Combine(
                    outputPath,
                    string.Format("{0}_Trajectory.bmp", baseName)));
            }
        }

        [TestMethod]
        public void DivideRoomTest()
        {
            var baseName = "25_MutationIter_6_Waypoints_4_Walls_16_Units";
            int mazeHeight = 1;
            int mazeWidth = 1;
            //var seedMazePath = @"F:\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Development\MCC_Projects\MCC_Executor\MazeNavigation\SeedMazes\" + baseName + ".xml";
            var seedMazePath =
                @"\\JONATHAN-PC\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Minimal Criteria Search\Analysis\MCC Expand Maze and Pathway Evolution\MCC Mazes - Pathway Complexification - FullMovement\Genomes\" +
                baseName + ".xml";
            var outputPath =
                @"\\JONATHAN-PC\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Minimal Criteria Search\ExperimentData\DebugOutput";

            var scaleFactor = 32;
            var startEndPointSize = 10;
            var startLocationXY = 16;

            var genomeParameters = new MazeGenomeParameters();

            genomeParameters.MutateExpandMazeProbability = 0;
            genomeParameters.MutateDeleteWallProbability = 0;
            genomeParameters.MutateAddWallProbability = 0;
            genomeParameters.MutateWallStartLocationProbability = 0;
            genomeParameters.MutatePassageStartLocationProbability = 0;
            genomeParameters.MutateAddPathWaypointProbability = 0;
            genomeParameters.MutatePathWaypointLocationProbability = 1;

            var mazeGenomeFactory = new MazeGenomeFactory(genomeParameters, mazeHeight, mazeWidth);

            var seedMazeGenome = ExperimentUtils.ReadSeedMazeGenomes(seedMazePath, mazeGenomeFactory).First();
            
            var grid = MazeUtils.BuildMazeSolutionPath(seedMazeGenome);

            // Write trajectory image out to file
            produceTrajectory(seedMazeGenome, grid, scaleFactor, startEndPointSize, startLocationXY, outputPath,
                baseName);

            var structure = MazeUtils.BuildMazeStructureAroundPath(seedMazeGenome, grid);

            var mazePhenotype = new MazeStructure(seedMazeGenome.MazeBoundaryWidth, seedMazeGenome.MazeBoundaryHeight, scaleFactor);
            mazePhenotype.ConvertGridArrayToWalls(structure);

            ImageGenerationHandler.GenerateMazeStructureImage(
                Path.Combine(outputPath, string.Format("{0}_Maze.bmp", baseName)), mazePhenotype, true);

            //seedMazeGenome.CreateOffspring(2);
        }
    }
}