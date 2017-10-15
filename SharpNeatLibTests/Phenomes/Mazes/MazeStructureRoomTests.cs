using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Phenomes.Mazes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MazeExperimentSupportLib;
using MCC_Domains.Utils;
using SharpNeat.Genomes.Maze;
using SharpNeat.Utility;

namespace SharpNeat.Phenomes.Mazes.Tests
{
    [TestClass()]
    public class MazeStructureRoomTests
    {
        [TestMethod()]
        public void DivideRoomTest()
        {
            int mazeHeight = 10;
            int mazeWidth = 10;
            //var seedMazePath = @"F:\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Development\MCC_Projects\MCC_Executor\MazeNavigation\SeedMazes\PathEvo_Test9.xml";
            var seedMazePath = @"F:\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Minimal Criteria Search\ExperimentData\DebugOutput\MazePathEvo_TestBattery\Maze_Genome_Iter_5.xml";
            var outputPath = @"F:\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Minimal Criteria Search\ExperimentData\DebugOutput";

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

            var structure = MazeUtils.BuildMazeStructureAroundPath(seedMazeGenome, grid);
            
            var mazePhenotype = new MazeStructure(mazeWidth, mazeHeight, 32);
            mazePhenotype.ConvertGridArrayToWalls(structure);

            ImageGenerationHandler.GenerateMazeStructureImage(outputPath + "\\Test_Maze_Structure.bmp", mazePhenotype);

            
            var childMaze = seedMazeGenome.CreateOffspring(1);
            
            grid = MazeUtils.BuildMazeSolutionPath(childMaze);

            structure = MazeUtils.BuildMazeStructureAroundPath(childMaze, grid);

            mazePhenotype = new MazeStructure(mazeWidth, mazeHeight, 32);
            mazePhenotype.ConvertGridArrayToWalls(structure);

            ImageGenerationHandler.GenerateMazeStructureImage(outputPath + "\\Test_Maze_Child_Structure.bmp", mazePhenotype);
            
        }
    }
}