using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Decoders.Maze;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;
using System.Drawing;

namespace SharpNeat.Decoders.Maze.Tests
{
    [TestClass()]
    public class MazeDecoderTests
    {
        [TestMethod()]
        public void HardCodedGenomeDecodeTest()
        {
            // Mock up maze genome (just use null genome factory)
            MazeGenome mazeGenome = new MazeGenome((MazeGenomeFactory)null, 1, 1);

            // Add some genes
            mazeGenome.GeneList.Add(new MazeGene(0.6, 0.3, false));
            mazeGenome.GeneList.Add(new MazeGene(0.7, 0.4, false));
            mazeGenome.GeneList.Add(new MazeGene(0.3, 0.8, true));
            mazeGenome.GeneList.Add(new MazeGene(0.9, 0.2, false));
            mazeGenome.GeneList.Add(new MazeGene(0.5, 0.3, false));
            mazeGenome.GeneList.Add(new MazeGene(0.2, 0.5, false));
            mazeGenome.GeneList.Add(new MazeGene(0.4, 0.1, true));
            mazeGenome.GeneList.Add(new MazeGene(0.7, 0.8, true));
            mazeGenome.GeneList.Add(new MazeGene(0.3, 0.2, false));

            // Create the maze decoder
            MazeDecoder mazeDecoder = new MazeDecoder(20, 20);

            MazeGrid mazeGrid = mazeDecoder.Decode(mazeGenome);

            //DisplayMaze(mazeGrid.MazeArray);
        }

        [TestMethod()]
        public void MutatedGenomeDecodeTest()
        {
            int scaleMultiplier = 16;

            // Mock up maze genome (just use null genome factory)
            MazeGenome mazeGenome = new MazeGenome(new MazeGenomeFactory(), 1, 1);

            uint birthGeneration = 1;

            do
            {
                // Generate an offspring (perform mutation)
                mazeGenome.CreateOffspring(++birthGeneration);
            } while (mazeGenome.GeneList.Count < 200);
            
            // Create the maze decoder
            MazeDecoder mazeDecoder = new MazeDecoder(20, 20, scaleMultiplier);

            MazeGrid mazeGrid = mazeDecoder.Decode(mazeGenome);

            DisplayMaze(mazeGrid.MazeArray);

            mazeGrid.ConvertGridArrayToWalls(mazeGrid.MazeArray);

            PrintBitmapMaze(mazeGrid.Walls, 20 * scaleMultiplier, 20 * scaleMultiplier);
        }

        private void DisplayMaze(int[,] grid)
        {
            // Write space at top border
            Debug.Write(" ");

            // Write top border itself 
            // (note that this is doubled for display purposes - to have both '|' and '_' character in same "cell")
            for (int i = 0; i < (grid.GetLength(0) * 2 - 1); i++)
            {
                Debug.Write("_");
            }

            // Write new line
            Debug.WriteLine("");

            // Actually iterate through the rows of the maze grid
            for (int curRowNum = 0; curRowNum < grid.GetLength(0); curRowNum++)
            {
                // Write left border for the current row
                Debug.Write("|");

                // Iterate through each cell (column) in the current row
                for (int curColNum = 0; curColNum < grid.GetLength(1); curColNum++)
                {
                    // We're at the bottom if 1 + current row is greater than or equal to the height
                    bool isBottom = curRowNum + 1 >= grid.GetLength(0);

                    // Southern wall is enabled if one is specified in the grid or this is the bottom row
                    bool isSouthernWall = (grid[curRowNum, curColNum] & (int)WallDirection.South) != 0 || isBottom;

                    // Enable the southern wall at the second half of the current cell if its still part of the same line
                    // (this prevents gaps due to doubling the number of columns for display purposes)
                    bool isInternalSouthernWall = curColNum + 1 < grid.GetLength(1) &&
                                                  (grid[curRowNum, curColNum + 1] & (int)WallDirection.South) != 0 ||
                                                  isBottom;

                    // Eastern wall is enabled if specified on the cell or if this is the last column in the grid
                    bool isEasternWall = (grid[curRowNum, curColNum] & (int)WallDirection.East) != 0 ||
                                         curColNum + 1 >= grid.GetLength(1);

                    Debug.Write(isSouthernWall ? "_" : " ");
                    Debug.Write(isEasternWall ? "|" : (isSouthernWall && isInternalSouthernWall) ? "_" : " ");
                }

                // Write new line
                Debug.WriteLine("");
            }
        }

        private void PrintBitmapMaze(List<MazeWall> walls, int mazeWidth, int mazeHeight)
        {
            Pen blackPen = new Pen(Color.Black, 0.0001f);
            Bitmap mazeBitmap = new Bitmap(mazeWidth + 1, mazeHeight + 1);

            // Fill with white
            using (Graphics graphics = Graphics.FromImage(mazeBitmap))
            {
                Rectangle imageSize = new Rectangle(0, 0, mazeWidth + 1, mazeHeight + 1);
                graphics.FillRectangle(Brushes.White, imageSize);
                graphics.FillEllipse(Brushes.Green, 5, 5, 5, 5);
                graphics.FillEllipse(Brushes.Red, mazeWidth - 10, mazeHeight - 10, 5, 5);
            }

            foreach (MazeWall wall in walls)
            {
                using (Graphics graphics = Graphics.FromImage(mazeBitmap))
                {
                    graphics.DrawLine(blackPen, new Point(wall.StartMazePoint.X, wall.StartMazePoint.Y) , new Point(wall.EndMazePoint.X, wall.EndMazePoint.Y));
                }
            }

            mazeBitmap.Save("RecursiveDivision_TestImage.bmp");
        }
    }
}