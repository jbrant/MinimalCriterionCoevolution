#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;
using SharpNeat.Decoders.Maze;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeGenomeGenerator
{
    internal class MazeGenomeGeneratorExecutor
    {
        private static readonly Dictionary<ExecutionParameter, String> _executionConfiguration =
            new Dictionary<ExecutionParameter, string>();

        private static bool _generateMazes;

        private static void Main(string[] args)
        {
            // Extract the execution parameters and check for any errors (exit application if any found)
            if (ParseAndValidateConfiguration(args) == false)
                Environment.Exit(0);

            // Mazes genomes along with their graphical rendering is produced by default
            if (_generateMazes)
            {
                HandleMazeGeneration();
            }
            // Alternatively, an existing genome file can be read in and the graphical depiction of that genome produced
            else
            {
                HandleMazeImageReproduction();
            }
        }

        /// <summary>
        ///     Handles the process of rendering a bitmap file of an existing maze genome.
        /// </summary>
        private static void HandleMazeImageReproduction()
        {
            MazeGenome mazeGenome = null;

            // Get the maze genome file path and image output path
            string mazeGenomeFile = _executionConfiguration[ExecutionParameter.MazeGenomeFile];
            string imageOutputPath = _executionConfiguration[ExecutionParameter.BitmapOutputBaseDirectory];

            // Create a new (mostly dummy) maze genome factory
            MazeGenomeFactory mazeGenomeFactory = new MazeGenomeFactory();

            // Read in the genome
            using (XmlReader xr = XmlReader.Create(mazeGenomeFile))
            {
                mazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xr, mazeGenomeFactory);
            }

            // Get the maze genome ID
            uint mazeGenomeId = mazeGenome.Id;

            // Render the maze phenotype (i.e. the graphical structure0 and print to a bitmap file
            PrintMazeToFile(mazeGenome,
                Path.Combine(imageOutputPath, string.Format("EvolvedMaze_ID_{0}.bmp", mazeGenomeId)));
        }

        /// <summary>
        ///     Handles the process of generating maze genomes and rendering bitmap files of their structure.
        /// </summary>
        private static void HandleMazeGeneration()
        {
            Random rand = new Random();

            // Get the number of interior walls and maze genome output directory
            int numInteriorWalls = Int32.Parse(_executionConfiguration[ExecutionParameter.NumWalls]);
            string mazeGenomeOutputDirectory = _executionConfiguration[ExecutionParameter.MazeGenomeOutputBaseDirectory];

            // Get the number of sample mazes to generate (or just 1 maze if not specified)
            int numMazes = _executionConfiguration.ContainsKey(ExecutionParameter.NumMazes)
                ? Int32.Parse(_executionConfiguration[ExecutionParameter.NumMazes])
                : 1;

            // Get whether images are being generated for the sample mazes
            bool generateMazeImages = _executionConfiguration.ContainsKey(ExecutionParameter.OutputMazeBitmap) &&
                                      Boolean.Parse(_executionConfiguration[ExecutionParameter.OutputMazeBitmap]);

            // Create a new maze genome factory
            MazeGenomeFactory mazeGenomeFactory = new MazeGenomeFactory();

            for (int curMazeCnt = 0; curMazeCnt < numMazes; curMazeCnt++)
            {
                string fileBaseName = string.Format("GeneratedMaze_{0}", curMazeCnt);

                // Create a new genome and pass in the requisite factory
                MazeGenome mazeGenome = new MazeGenome(mazeGenomeFactory, 0, 0);

                // Create the specified number of interior walls (i.e. maze genes)
                for (int cnt = 0; cnt < numInteriorWalls; cnt++)
                {
                    // Create new maze gene and add to genome
                    mazeGenome.GeneList.Add(new MazeGene(rand.NextDouble(), rand.NextDouble(), rand.NextDouble() < 0.5));
                }

                // Serialize the genome to XML            
                using (
                    XmlWriter xmlWriter =
                        XmlWriter.Create(
                            Path.Combine(mazeGenomeOutputDirectory, string.Format("{0}_Genome.xml", fileBaseName)),
                            new XmlWriterSettings {Indent = true}))
                {
                    // Get genome XML
                    MazeGenomeXmlIO.WriteComplete(xmlWriter, mazeGenome);
                }

                // Print the maze to a bitmap file if that option has been specified
                if (generateMazeImages)
                {
                    PrintMazeToFile(mazeGenome, string.Format("{0}_Structure.bmp", fileBaseName));
                }
            }
        }

        /// <summary>
        ///     Converts the maze genome into a maze structure and prints it to a bitmap file.
        /// </summary>
        /// <param name="mazeGenome">The maze genome to convert and print.</param>
        /// <param name="mazeImageName">The name of the maze output file.</param>
        private static void PrintMazeToFile(MazeGenome mazeGenome, string mazeImageName)
        {
            // Read in the maze decode parameters
            int mazeHeight = Int32.Parse(_executionConfiguration[ExecutionParameter.MazeHeight]);
            int mazeWidth = Int32.Parse(_executionConfiguration[ExecutionParameter.MazeWidth]);
            int mazeScaleFactor = Int32.Parse(_executionConfiguration[ExecutionParameter.MazeScaleFactor]);
            string mazeBitmapOutputDirectory = _executionConfiguration[ExecutionParameter.BitmapOutputBaseDirectory];

            // Instantiate the maze genome decoder
            MazeDecoder mazeDecoder = new MazeDecoder(mazeHeight, mazeWidth, mazeScaleFactor);

            // Decode the maze to get a maze structure
            MazeStructure mazeStructure = mazeDecoder.Decode(mazeGenome);

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
            mazeBitmap.Save(Path.Combine(mazeBitmapOutputDirectory, mazeImageName));
        }

        /// <summary>
        ///     Populates the execution configuration and checks for any errors in said configuration.
        /// </summary>
        /// <param name="executionArguments">The arguments with which the experiment executor is being invoked.</param>
        /// <returns>Boolean status indicating whether parsing the configuration suceeded.</returns>
        private static bool ParseAndValidateConfiguration(string[] executionArguments)
        {
            _generateMazes = true;
            bool isConfigurationValid = executionArguments != null;

            // Only continue if there are execution arguments
            if (executionArguments != null && executionArguments.Length > 0)
            {
                foreach (string executionArgument in executionArguments)
                {
                    ExecutionParameter curParameter;

                    // Get the key/value pair
                    string[] parameterValuePair = executionArgument.Split('=');

                    // Attempt to parse the current parameter
                    isConfigurationValid = Enum.TryParse(parameterValuePair[0], true, out curParameter);

                    // If the current parameter is not valid, break out of the loop and return
                    if (isConfigurationValid == false)
                    {
                        Console.Error.WriteLine("[{0}] is not a valid configuration parameter.",
                            parameterValuePair[0]);
                        break;
                    }

                    // If the parameter is valid but it already exists in the map, break out of the loop and return
                    if (_executionConfiguration.ContainsKey(curParameter))
                    {
                        Console.Error.WriteLine(
                            "Ambiguous configuration - parameter [{0}] has been specified more than once.",
                            curParameter);
                        break;
                    }

                    switch (curParameter)
                    {
                        // Ensure valid integer values were given
                        case ExecutionParameter.NumWalls:
                        case ExecutionParameter.NumMazes:
                        case ExecutionParameter.MazeHeight:
                        case ExecutionParameter.MazeWidth:
                        case ExecutionParameter.MazeScaleFactor:
                            int testInt;
                            if (Int32.TryParse(parameterValuePair[1], out testInt) == false)
                            {
                                Console.Error.WriteLine(
                                    "The value for parameter [{0}] must be an integer.",
                                    curParameter);
                                isConfigurationValid = false;
                            }
                            break;

                        // Ensure that valid boolean values were given
                        case ExecutionParameter.OutputMazeBitmap:
                        case ExecutionParameter.GenerateMazes:
                            bool testBool;
                            if (Boolean.TryParse(parameterValuePair[1], out testBool) == false)
                            {
                                Console.Error.WriteLine("The value for parameter [{0}] must be a boolean.",
                                    curParameter);
                                isConfigurationValid = false;
                            }
                            break;
                    }

                    // If all else checks out, add the parameter to the map
                    _executionConfiguration.Add(curParameter, parameterValuePair[1]);
                }
            }
            // If there are no execution arguments, the configuration is invalid
            else
            {
                isConfigurationValid = false;
            }

            // If the generate mazes option was specified, then set it - otherwise defualt to true
            if (_executionConfiguration.ContainsKey(ExecutionParameter.GenerateMazes))
            {
                _generateMazes = Boolean.Parse(_executionConfiguration[ExecutionParameter.GenerateMazes]);
            }


            // If the per-parameter configuration is valid but not a full list of parameters were specified, makes sure the necessary ones are present
            if (isConfigurationValid && (_executionConfiguration.Count ==
                                         Enum.GetNames(typeof (ExecutionParameter)).Length) == false)
            {
                // Check for existence of desired number of interior walls to generate
                if (_generateMazes && _executionConfiguration.ContainsKey(ExecutionParameter.NumWalls) == false)
                {
                    Console.Error.WriteLine("Parameter [{0}] must be specified.", ExecutionParameter.NumWalls);
                    isConfigurationValid = false;
                }

                // Check for existence of the maze genome XML output directory
                if (_generateMazes &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.MazeGenomeOutputBaseDirectory) == false)
                {
                    Console.Error.WriteLine("Parameter [{0}] must be specified.",
                        ExecutionParameter.MazeGenomeOutputBaseDirectory);
                    isConfigurationValid = false;
                }

                // Check for the existence of the existing maze genome file if we're just rendering an image of the maze from that genome
                if (_generateMazes == false &&
                    _executionConfiguration.ContainsKey(ExecutionParameter.MazeGenomeFile) == false)
                {
                    Console.Error.WriteLine("Parameter [{0}] must be specified.",
                        ExecutionParameter.MazeGenomeFile);
                    isConfigurationValid = false;
                }

                // If the maze is being output to a bitmap file, the dimensions and scaling multiplier must be specified
                if (_executionConfiguration.ContainsKey(ExecutionParameter.OutputMazeBitmap) &&
                    Convert.ToBoolean(_executionConfiguration[ExecutionParameter.OutputMazeBitmap]) &&
                    (_executionConfiguration.ContainsKey(ExecutionParameter.MazeHeight) == false ||
                     _executionConfiguration.ContainsKey(ExecutionParameter.MazeWidth) == false ||
                     _executionConfiguration.ContainsKey(ExecutionParameter.MazeScaleFactor) == false ||
                     _executionConfiguration.ContainsKey(ExecutionParameter.BitmapOutputBaseDirectory) == false))
                {
                    Console.Error.WriteLine(
                        "The maze height, width, scale factor, and output directory must be specified when outputting bitmap images of the generated maze.");
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            Console.Error.WriteLine("The experiment executor invocation must take the following form:");
            Console.Error.WriteLine(
                "MazeGenomeGenerator.exe {0}=[{11}] {1}=[{10}] {2}=[{13}] (Optional: {3}=[{12}]) (Optional: {4}=[{11}] {5}=[{12}] {6}=[{12}] {7}=[{12}] {8}=[{13}] {9}=[{13}])",
                ExecutionParameter.GenerateMazes, ExecutionParameter.NumWalls,
                ExecutionParameter.MazeGenomeOutputBaseDirectory,
                ExecutionParameter.NumMazes, ExecutionParameter.OutputMazeBitmap, ExecutionParameter.MazeHeight,
                ExecutionParameter.MazeWidth, ExecutionParameter.MazeScaleFactor,
                ExecutionParameter.BitmapOutputBaseDirectory, ExecutionParameter.MazeGenomeFile, "# of interior walls",
                "true|false", "integer value", "directory");

            return false;
        }
    }
}