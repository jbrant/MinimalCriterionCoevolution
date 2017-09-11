using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Phenomes.Mazes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var seedMazePath = @"F:\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Development\MCC_Projects\MCC_Executor\MazeNavigation\SeedMazes\TestMaze_Genome_10_Height_10_Width_0_Walls.xml";
            var outputPath = @"F:\User Data\Jonathan\Documents\school\Jonathan\Graduate\PhD\Minimal Criteria Search\ExperimentData\DebugOutput";

            var genomeParameters = new MazeGenomeParameters();

            genomeParameters.MutateExpandMazeProbability = 1;
            genomeParameters.MutateDeleteWallProbability = 0.01;
            genomeParameters.MutatePassageStartLocationProbability = 0.1;
            genomeParameters.MutateWallStartLocationProbability = 0.2;

            var mazeGenomeFactory = new MazeGenomeFactory(genomeParameters, 10, 10);

            var seedMazeGenome = ExperimentUtils.ReadSeedMazeGenomes(seedMazePath, mazeGenomeFactory).First();

            MazeUtils.BuildMazeSolutionPath(seedMazeGenome);

            //var structure = MazeUtils.ConvertMazeGenomeToUnscaledStructure(seedMazeGenome);
            
        }
    }
}