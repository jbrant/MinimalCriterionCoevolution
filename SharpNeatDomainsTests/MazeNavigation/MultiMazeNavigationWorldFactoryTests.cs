#region

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Behaviors;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Maze;
using SharpNeat.Decoders.Neat;
using SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;
using SharpNeatDomainsTests;

#endregion

namespace SharpNeat.Domains.MazeNavigation.Tests
{
    [TestClass]
    public class MultiMazeNavigationWorldFactoryTests
    {
        [TestMethod]
        public void SetMazeConfigurationsTest()
        {
            // Setup constant parameters
            const int maxTimesteps = 300;
            const int minSuccessDistance = 5;

            // Create some dummy structures/walls
            IList<MazeStructure> mazeStructures = new List<MazeStructure>
            {
                new MazeStructure(300, 300, 1)
                {
                    Walls = {new MazeStructureWall(5, 5, 10, 10), new MazeStructureWall(6, 6, 11, 11)}
                },
                new MazeStructure(300, 300, 1)
                {
                    Walls = {new MazeStructureWall(7, 7, 12, 12), new MazeStructureWall(8, 8, 13, 13)}
                },
                new MazeStructure(300, 300, 1)
                {
                    Walls = {new MazeStructureWall(9, 9, 14, 14), new MazeStructureWall(10, 10, 15, 15)}
                },
                new MazeStructure(300, 300, 1)
                {
                    Walls = {new MazeStructureWall(11, 11, 16, 16), new MazeStructureWall(12, 12, 17, 17)}
                },
                new MazeStructure(300, 300, 1)
                {
                    Walls = {new MazeStructureWall(13, 13, 18, 18), new MazeStructureWall(14, 14, 19, 19)}
                }
            };

            // Create the factory
            MultiMazeNavigationWorldFactory<BehaviorInfo> factory =
                new MultiMazeNavigationWorldFactory<BehaviorInfo>(maxTimesteps, minSuccessDistance);

            // Attempt setting the configurations
            factory.SetMazeConfigurations(mazeStructures);

            // Ensure number of mazes is 2
            Debug.Assert(factory.NumMazes == 5);

            // Remove one of the dummy structures
            mazeStructures.RemoveAt(0);

            // Call again with one of the mazes removed
            factory.SetMazeConfigurations(mazeStructures);

            // Ensure that no longer extant maze was removed
            Debug.Assert(factory.NumMazes == 4);
        }

        [TestMethod]
        public void VerifyBootstrappedStateTest()
        {
            const string parentDirectory =
                "F:/User Data/Jonathan/Documents/school/Jonathan/Graduate/PhD/Development/C# NEAT/SharpNoveltyNeat/SharpNeatConsole/bin/Debug/";
            const string agentGenomeFile = "ViableSeedGenomes.xml";
            const string baseBitmapFilename = "AgentTrajectory";
            const int mazeHeight = 20;
            const int mazeWidth = 20;
            const int scaleMultiplier = 16;
            const int maxTimesteps = 400;
            const int minSuccessDistance = 5;

            // Setup stuff for the navigators
            List<NeatGenome> agentGenomes;
            NeatGenomeDecoder agentGenomeDecoder = new NeatGenomeDecoder(NetworkActivationScheme.CreateAcyclicScheme());
            NeatGenomeFactory agentGenomeFactory = new NeatGenomeFactory(10, 2);

            // Create new minimal maze (no barriers)
            MazeStructure mazeStructure = new MazeDecoder(mazeHeight, mazeWidth, scaleMultiplier).Decode(
                new MazeGenomeFactory(null, null, null).CreateGenome(0));

            // Create behavior characterization factory
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory =
                new TrajectoryBehaviorCharacterizationFactory(null);

            // Create evaluator
            MazeNavigatorMCSEvaluator mazeNavigatorEvaluator = new MazeNavigatorMCSEvaluator(maxTimesteps,
                minSuccessDistance, behaviorCharacterizationFactory, 1);

            // Set maze within evaluator
            mazeNavigatorEvaluator.UpdateEvaluatorPhenotypes(new List<MazeStructure> {mazeStructure});

            // Read in agents
            using (XmlReader xr = XmlReader.Create(parentDirectory + agentGenomeFile))
            {
                agentGenomes = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, agentGenomeFactory);
            }

            // Decode agent genomes to phenotype and run simulation
            for (int i = 0; i < agentGenomes.Count; i++)
            {
                // Decode navigator genome
                IBlackBox agentPhenome = agentGenomeDecoder.Decode(agentGenomes[i]);

                // Run simulation
                BehaviorInfo behaviorInfo = mazeNavigatorEvaluator.Evaluate(agentPhenome, 0, false, null, null);

                // Print the navigator trajectory through the maze
                DomainTestUtils.PrintMazeAndTrajectory(mazeStructure, behaviorInfo.Behaviors,
                    string.Format("{0}_{1}.bmp", baseBitmapFilename, i));
            }
        }
    }
}