#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ImageMagick;
using Redzen.Random;
using SharpNeat.Decoders.Maze;
using SharpNeat.Genomes.Maze;
using SharpNeat.Utility;

#endregion

namespace MazeGenomeGenerator
{
    internal class MazeGenomeGeneratorExecutor
    {
        private static readonly Dictionary<ExecutionParameter, string> _executionConfiguration =
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
            var mazeGenomeFile = _executionConfiguration[ExecutionParameter.MazeGenomeFile];
            var imageOutputPath = _executionConfiguration[ExecutionParameter.BitmapOutputBaseDirectory];

            // Create a new (mostly dummy) maze genome factory
            var mazeGenomeFactory = new MazeGenomeFactory();

            // Read in the genome
            using (var xr = XmlReader.Create(mazeGenomeFile))
            {
                mazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xr, mazeGenomeFactory);
            }

            // Get the maze genome ID
            var mazeGenomeId = mazeGenome.Id;

            // Render the maze phenotype (i.e. the graphical structure0 and print to a bitmap file
            PrintMazeToFile(mazeGenome,
                Path.Combine(imageOutputPath, string.Format("EvolvedMaze_ID_{0}.png", mazeGenomeId)));
        }

        /// <summary>
        ///     Handles the process of generating maze genomes and rendering bitmap files of their structure.
        /// </summary>
        private static void HandleMazeGeneration()
        {
            var rand = RandomDefaults.CreateRandomSource();

            // Get the evolved maze height and width
            var mazeHeight = int.Parse(_executionConfiguration[ExecutionParameter.MazeHeight]);
            var mazeWidth = int.Parse(_executionConfiguration[ExecutionParameter.MazeWidth]);
            var mazeQuadrantHeight = int.Parse(_executionConfiguration[ExecutionParameter.MazeQuadrantHeight]);
            var mazeQuadrantWidth = int.Parse(_executionConfiguration[ExecutionParameter.MazeQuadrantWidth]);

            // Get the number of interior walls and maze genome output directory
            var numWaypoints = int.Parse(_executionConfiguration[ExecutionParameter.NumWaypoints]);
            var numInteriorWalls = int.Parse(_executionConfiguration[ExecutionParameter.NumWalls]);
            var mazeGenomeOutputDirectory = _executionConfiguration[ExecutionParameter.MazeGenomeOutputBaseDirectory];

            // Get the number of sample mazes to generate (or just 1 maze if not specified)
            var numMazes = _executionConfiguration.ContainsKey(ExecutionParameter.NumMazes)
                ? int.Parse(_executionConfiguration[ExecutionParameter.NumMazes])
                : 1;

            // Get whether images are being generated for the sample mazes
            var generateMazeImages = _executionConfiguration.ContainsKey(ExecutionParameter.OutputMazeBitmap) &&
                                     bool.Parse(_executionConfiguration[ExecutionParameter.OutputMazeBitmap]);

            // Get whether maze genomes are being serialized to the same file (defaults to false)
            var isSingleOutputFile = _executionConfiguration.ContainsKey(ExecutionParameter.SingleGenomeOutputFile) &&
                                     bool.Parse(_executionConfiguration[ExecutionParameter.SingleGenomeOutputFile]);

            // Create a new maze genome factory
            var mazeGenomeFactory =
                new MazeGenomeFactory(mazeHeight, mazeWidth, mazeQuadrantHeight, mazeQuadrantWidth);

            // Instantiate list to hold generated maze genomes 
            // (only really used when we're writing everything out to one file)
            var mazeGenomeList = new List<MazeGenome>(numMazes);

            for (var curMazeCnt = 0; curMazeCnt < numMazes; curMazeCnt++)
            {
                MazeGenome mazeGenome;

                // Lay out the base file name
                var fileBaseName =
                    string.Format("GeneratedMazeGenome_{0}_Height_{1}_Width_{2}_Waypoints_{3}_Walls_{4}", mazeHeight,
                        mazeWidth, numWaypoints, numInteriorWalls, curMazeCnt);

                // With a single output file, the genomes are likely being used for separate experiments, so we
                // reset the innovation IDs and assign the maze a constant identifier
                if (isSingleOutputFile == false)
                {
                    // Reset innovation IDs
                    mazeGenomeFactory.InnovationIdGenerator.Reset();

                    // Create a new genome and pass in the requisite factory
                    mazeGenome = new MazeGenome(mazeGenomeFactory, 0, 0);
                }
                // Otherwise, we leave the innovation ID generator alone and create a new maze genome with
                // an identifier that's incremented by one
                else
                {
                    mazeGenome = new MazeGenome(mazeGenomeFactory, (uint) curMazeCnt, 0);
                }

                // Add the specified number of waypoints (less one because center waypoint is created on maze initialization)
                for (var cnt = 0; cnt < numWaypoints - 1; cnt++)
                {
                    Point2DInt waypoint;

                    // Randomly select an orientation
                    var newPointOrientation = mazeGenomeFactory.Rng.NextBool()
                        ? IntersectionOrientation.Horizontal
                        : IntersectionOrientation.Vertical;

                    // Iterate until we get a waypoint that's valid
                    do
                    {
                        waypoint = new Point2DInt(mazeGenomeFactory.Rng.Next(mazeGenome.MazeBoundaryWidth - 1),
                            mazeGenomeFactory.Rng.Next(mazeGenome.MazeBoundaryHeight - 1));
                    } while (
                        MazeUtils.IsValidWaypointLocation(mazeGenome, waypoint, uint.MaxValue, newPointOrientation) ==
                        false);

                    mazeGenome.PathGeneList.Add(new PathGene(mazeGenomeFactory.InnovationIdGenerator.NextId, waypoint,
                        newPointOrientation));
                }

                // Create the specified number of interior walls (i.e. maze genes)
                for (var cnt = 0; cnt < numInteriorWalls; cnt++)
                {
                    // Create new maze gene and add to genome
                    mazeGenome.WallGeneList.Add(new WallGene(mazeGenomeFactory.InnovationIdGenerator.NextId,
                        rand.NextDouble(), rand.NextDouble(), rand.NextDouble() < 0.5));
                }

                // Only serialize genomes to separate files if single output file option is turned off
                if (isSingleOutputFile == false)
                {
                    // Serialize the genome to XML            
                    using (
                        var xmlWriter =
                            XmlWriter.Create(
                                Path.Combine(mazeGenomeOutputDirectory, string.Format("{0}.xml", fileBaseName)),
                                new XmlWriterSettings {Indent = true}))
                    {
                        // Get genome XML
                        MazeGenomeXmlIO.WriteComplete(xmlWriter, mazeGenome);
                    }
                }
                // Otherwise, just add genome to list to be serialized to single file in bulk
                else
                {
                    mazeGenomeList.Add(mazeGenome);
                }

                // Print the maze to a bitmap file if that option has been specified
                if (generateMazeImages)
                {
                    PrintMazeToFile(mazeGenome, string.Format("{0}_Structure.png", fileBaseName));
                }
            }

            // If serialize to single output file is turned off, go ahead and write everything out to the file
            if (isSingleOutputFile)
            {
                // Serialize all genomes to XML
                using (
                    var xmlWriter =
                        XmlWriter.Create(Path.Combine(mazeGenomeOutputDirectory,
                            string.Format("GeneratedMaze_{0}_Genomes_{1}_Height_{2}_Width_{3}_Waypoints_{4}_Walls.xml",
                                numMazes,
                                mazeHeight, mazeWidth, numWaypoints, numInteriorWalls))))
                {
                    MazeGenomeXmlIO.WriteComplete(xmlWriter, mazeGenomeList);
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
            var mazeScaleFactor = int.Parse(_executionConfiguration[ExecutionParameter.MazeScaleFactor]);
            var mazeBitmapOutputDirectory = _executionConfiguration[ExecutionParameter.BitmapOutputBaseDirectory];

            // Instantiate the maze genome decoder
            var mazeDecoder = new MazeDecoder(mazeScaleFactor);

            // Decode the maze to get a maze structure
            var mazeStructure = mazeDecoder.Decode(mazeGenome);

            using (var image = new MagickImage(MagickColors.White,
                mazeStructure.ScaledMazeWidth + 1, mazeStructure.ScaledMazeHeight + 1))
            {
                var strokeColor = new DrawableStrokeColor(MagickColors.Black);
                var strokeWidth = new DrawableStrokeWidth(0.0001f);

                // Draw border around maze
                image.Draw(strokeColor, strokeWidth, new DrawableFillColor(MagickColors.Transparent),
                    new DrawableRectangle(0, 0, mazeStructure.ScaledMazeWidth + 1, mazeStructure.ScaledMazeHeight + 1));

                // Draw start and end points
                image.Draw(strokeColor, strokeWidth, new DrawableFillColor(MagickColors.Green),
                    new DrawableEllipse(mazeStructure.ScaledStartLocation.X, mazeStructure.ScaledStartLocation.Y, 2.5, 2.5, 0, 360));
                image.Draw(strokeColor, strokeWidth, new DrawableFillColor(MagickColors.Red),
                    new DrawableEllipse(mazeStructure.ScaledTargetLocation.X, mazeStructure.ScaledTargetLocation.Y, 2.5, 2.5, 0, 360));

                // Draw walls
                foreach (var wall in mazeStructure.Walls)
                {
                    image.Draw(strokeColor, strokeWidth,
                        new DrawableLine(wall.StartMazeStructurePoint.X, wall.StartMazeStructurePoint.Y,
                            wall.EndMazeStructurePoint.X, wall.EndMazeStructurePoint.Y));
                }

                // Save the PNG image
                image.Write(Path.Combine(mazeBitmapOutputDirectory, mazeImageName));
            }
        }

        /// <summary>
        ///     Populates the execution configuration and checks for any errors in said configuration.
        /// </summary>
        /// <param name="executionArguments">The arguments with which the experiment executor is being invoked.</param>
        /// <returns>Boolean status indicating whether parsing the configuration suceeded.</returns>
        private static bool ParseAndValidateConfiguration(string[] executionArguments)
        {
            _generateMazes = true;
            var isConfigurationValid = executionArguments != null;

            // Only continue if there are execution arguments
            if (executionArguments != null && executionArguments.Length > 0)
            {
                foreach (var executionArgument in executionArguments)
                {
                    ExecutionParameter curParameter;

                    // Get the key/value pair
                    var parameterValuePair = executionArgument.Split('=');

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
                            if (int.TryParse(parameterValuePair[1], out testInt) == false)
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
                        case ExecutionParameter.SingleGenomeOutputFile:
                            bool testBool;
                            if (bool.TryParse(parameterValuePair[1], out testBool) == false)
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
                _generateMazes = bool.Parse(_executionConfiguration[ExecutionParameter.GenerateMazes]);
            }


            // If the per-parameter configuration is valid but not a full list of parameters were specified, makes sure the necessary ones are present
            if (isConfigurationValid && _executionConfiguration.Count ==
                Enum.GetNames(typeof(ExecutionParameter)).Length == false)
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
                     _executionConfiguration.ContainsKey(ExecutionParameter.MazeQuadrantHeight) == false ||
                     _executionConfiguration.ContainsKey(ExecutionParameter.MazeQuadrantWidth) == false ||
                     _executionConfiguration.ContainsKey(ExecutionParameter.MazeScaleFactor) == false ||
                     _executionConfiguration.ContainsKey(ExecutionParameter.BitmapOutputBaseDirectory) == false))
                {
                    Console.Error.WriteLine(
                        "The maze height/width, quadrant width/height, scale factor, and output directory must be specified when outputting bitmap images of the generated maze.");
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            Console.Error.WriteLine("The experiment executor invocation must take the following form:");
            Console.Error.WriteLine(
                "MazeGenomeGenerator.exe \n\t" +
                "Required: {0}=[{11}] \n\t" +
                "Required: {1}=[{10}] \n\t" +
                "Required: {2}=[{13}] \n\t" +
                "Optional: {3}=[{12}] \n\t" +
                "Optional: {4}=[{11}] \n\t" +
                "Optional: {5}=[{12}] \n\t" +
                "Optional: {6}=[{12}] \n\t" +
                "Optional: {7}=[{12}] \n\t" +
                "Optional: {8}=[{13}] \n\t" +
                "Optional: {9}=[{13}]",
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