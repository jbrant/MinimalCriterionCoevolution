#region

using System.Collections.Generic;
using System.IO;
using System.Xml;
using ExperimentEntities;
using MazeExperimentSuppotLib;
using SharpNeat.Decoders.Maze;
using SharpNeat.Genomes.Maze;

#endregion

namespace MazeStructureEvaluator
{
    internal class MazeStructureEvaluatorExecutor
    {
        private static void Main(string[] args)
        {
            // TODO: Refactor all of this to be more configurable - just hard-coding everything now to get results

            // Base path for maze bitmap output
            string mazeImageBase =
                @"F:\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Minimal Criteria Search\Analysis\Coevolution MCS\Images\Final Batch Mazes";
            
            // These are the experiments for which we want to get mazes in the final population
            List<string> experimentNames = new List<string>
            {
                "Coevolution MCS with Maze Initialization 9",
                "Coevolution MCS with Maze Initialization 10",
                "Coevolution MCS with Maze Initialization 11",
                "Coevolution MCS with Maze Initialization 12",
                "Coevolution MCS with Maze Initialization 13",
                "Coevolution MCS with Maze Initialization 14",
                "Coevolution MCS with Maze Initialization 15",
                "Coevolution MCS with Maze Initialization 16"
            };

            // Setup maze decoder with hard-coded height/width and scale multiplier
            MazeDecoder mazeDecoder = new MazeDecoder(10, 10, 32);

            // Create default maze genome factory
            MazeGenomeFactory mazeGenomeFactory = new MazeGenomeFactory();

            foreach (string experimentName in experimentNames)
            {
                // Get the current experiment configuration
                ExperimentDictionary curExperiment = ExperimentDataHandler.LookupExperimentConfiguration(experimentName);

                // Get the total number of runs of the experiment
                int numRuns = ExperimentDataHandler.GetNumRuns(curExperiment.ExperimentDictionaryID);

                for (int runIdx = 1; runIdx <= numRuns; runIdx++)
                {
                    // Get the total number of batches in the run
                    int numBatches = ExperimentDataHandler.GetNumBatchesForRun(curExperiment.ExperimentDictionaryID,
                        runIdx);

                    // Get the maze population extant in the last batch of the run
                    IList<string> mazePopulationGenomes =
                        ExperimentDataHandler.GetMazeGenomeXml(curExperiment.ExperimentDictionaryID, runIdx, numBatches);

                    // Build output directory
                    string imageOutputDirectory = Path.Combine(mazeImageBase,
                        string.Format("ExperimentName_{0}", curExperiment.ExperimentName),
                        string.Format("Run_{0}", runIdx));

                    if (Directory.Exists(imageOutputDirectory) == false)
                    {
                        Directory.CreateDirectory(imageOutputDirectory);
                    }

                    // Decode all genomes and render image of structure
                    foreach (string mazeGenomeStr in mazePopulationGenomes)
                    {
                        MazeGenome curMazeGenome;

                        // Unmarshall to maze genome object
                        using (XmlReader xmlReader = XmlReader.Create(new StringReader(mazeGenomeStr)))
                        {
                            curMazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, mazeGenomeFactory);
                        }

                        // Generate maze bitmap image
                        ImageGenerationHandler.GenerateMazeStructureImage(
                            Path.Combine(imageOutputDirectory,
                                string.Format("ExperimentID_{0}_Run_{1}_MazeID_{2}.bmp",
                                    curExperiment.ExperimentDictionaryID, runIdx, curMazeGenome.Id)),
                            mazeDecoder.Decode(curMazeGenome));
                    }
                }
            }
        }
    }
}