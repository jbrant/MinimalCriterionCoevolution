using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Domains.MazeNavigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Phenomes.Mazes;

namespace SharpNeat.Domains.MazeNavigation.Tests
{
    [TestClass()]
    public class MultiMazeNavigationWorldFactoryTests
    {
        [TestMethod()]
        public void SetMazeConfigurationsTest()
        {
            // Setup constant parameters
            const int maxTimesteps = 300;
            const int minSuccessDistance = 5;

            // Create some dummy structures/walls
            IList<MazeStructure> mazeStructures = new List<MazeStructure>()
            {
                new MazeStructure(300, 300, 1)
                {
                    Walls = { new MazeStructureWall(5, 5, 10, 10), new MazeStructureWall(6, 6, 11, 11)}
                },
                new MazeStructure(300, 300, 1)
                {
                    Walls = { new MazeStructureWall(7, 7, 12, 12), new MazeStructureWall(8, 8, 13, 13)}
                },
                new MazeStructure(300, 300, 1)
                {
                    Walls = { new MazeStructureWall(9, 9, 14, 14), new MazeStructureWall(10, 10, 15, 15)}
                },
                new MazeStructure(300, 300, 1)
                {
                    Walls = { new MazeStructureWall(11, 11, 16, 16), new MazeStructureWall(12, 12, 17, 17)}
                },
                new MazeStructure(300, 300, 1)
                {
                    Walls = { new MazeStructureWall(13, 13, 18, 18), new MazeStructureWall(14, 14, 19, 19)}
                }
            };

            // Create the factory
            MultiMazeNavigationWorldFactory<BehaviorInfo> factory = new MultiMazeNavigationWorldFactory<BehaviorInfo>(maxTimesteps, minSuccessDistance);

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

        [TestMethod()]
        public void VerifyBootstrappedStateTest()
        {
            // Read in maze genome
            
        }
    }
}