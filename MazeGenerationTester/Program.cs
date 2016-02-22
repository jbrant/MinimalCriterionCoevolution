#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AForge.Genetic;

#endregion

namespace MazeGenerationTester
{
    internal class Program
    {
        private const bool _useDirect = false;
        private const int _mazeSideLength = 100;

        private static void Main(string[] args)
        {
            QueuePopulation testPopulation;

            IFitnessFunction testFitnessFunction = new ExampleFitnessFunction();

            if (_useDirect)
            {
                testPopulation = new QueuePopulation(100, new MazeDirectChromosome(_mazeSideLength, 0.05),
                    testFitnessFunction,
                    new RouletteWheelSelection());

                for (int loopCnt = 0; loopCnt < 1000; loopCnt++)
                {
                    // Run the epoch
                    testPopulation.RunEpoch();

                    // Output the maze structure every 20 epochs
                    if (loopCnt%20 == 0)
                    {
                        WriteImage_DirectEncoding("Maze_DirectEncoding_Generation" + loopCnt + ".bmp",
                            ((MazeDirectChromosome) testPopulation.BestChromosome).GeneArray);
                    }
                }
            }
            testPopulation = new QueuePopulation(100, new MazePositiveIndirectChromosome(_mazeSideLength, 20, 100),
                testFitnessFunction, new RouletteWheelSelection());

            for (int loopCnt = 0; loopCnt < 1000; loopCnt++)
            {
                testPopulation.RunEpoch();

                if (loopCnt%20 == 0)
                {
                    WriteImage_IndirectPositiveEncoding("Maze_IndirectPositiveEncoding_Generation" + loopCnt + ".bmp",
                        ((MazePositiveIndirectChromosome) testPopulation.BestChromosome).GeneArray);
                }
            }
        }

        private static void WriteImage_DirectEncoding(string imageName, BitArray contents)
        {
            // Calculate the maze side lengths
            int sideLength = (int) Math.Sqrt(contents.Count);

            // Create square bitmap
            Bitmap imageBitmap = new Bitmap(sideLength, sideLength);

            // Loop through all of the cells in the maze and set the cell fill appropriately
            for (int heightIdx = 0; heightIdx < sideLength; heightIdx++)
            {
                for (int widthIdx = 0; widthIdx < sideLength; widthIdx++)
                {
                    // If the cell is filled, add black fill
                    if (contents[heightIdx*sideLength + widthIdx])
                    {
                        imageBitmap.SetPixel(widthIdx, heightIdx, Color.Black);
                    }
                    // Otherwise, fill is white
                    else
                    {
                        imageBitmap.SetPixel(widthIdx, heightIdx, Color.White);
                    }
                }
            }

            // Save the image
            imageBitmap.Save(imageName);
        }

        private static void WriteImage_IndirectPositiveEncoding(string imageName, Tuple<int, int>[] contents)
        {
            Pen blackPen = new Pen(Color.Black, 1);

            // Create square environment
            Bitmap imageBitmap = new Bitmap(_mazeSideLength, _mazeSideLength);

            // Fill with white
            using (Graphics graphics = Graphics.FromImage(imageBitmap))
            {
                Rectangle imageSize = new Rectangle(0, 0, _mazeSideLength, _mazeSideLength);
                graphics.FillRectangle(Brushes.White, imageSize);
            }

            // Decode each barrier (wall) and add to list
            List<Barrier> barrierList =
                (from pair in contents where pair != null select new Barrier(pair.Item1, pair.Item2, _mazeSideLength))
                    .ToList();

            // Modify barrier endpoints where applicable if barrier is not penetrating
            foreach (Barrier barrier in barrierList)
            {
                barrier.UpdateEndPoint(barrierList);
            }

            // Loop through all barriers a final time to draw them
            foreach (Barrier barrier in barrierList)
            {
                using (Graphics graphics = Graphics.FromImage(imageBitmap))
                {
                    graphics.DrawLine(blackPen, barrier.startPoint, barrier.endPoint);
                }
            }

            // Save the image
            imageBitmap.Save(imageName);
        }

        private static double ConvertToRadians(int angle)
        {
            return (Math.PI/180)*angle;
        }

        protected enum BarrierDirection
        {
            West,
            NorthWest,
            North,
            NorthEast,
            East,
            SouthEast,
            South,
            SouthWest
        }
    }
}