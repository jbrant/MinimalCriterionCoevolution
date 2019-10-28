using System;
using System.Collections.Generic;
using System.Xml;
using MCC_Domains.MazeNavigation.Bootstrappers;
using MCC_Domains.Utils;
using Redzen.Random;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;

namespace MCC_Domains.MazeNavigation
{
    public static class MazeNavigationExperimentUtils
    {
        /// <summary>
        ///     Read maze genome parameter settings from the configuration file.
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <returns>An initialized maze genome parameters object.</returns>
        public static MazeGenomeParameters ReadMazeGenomeParameters(XmlElement xmlConfig)
        {
            // Create new NEAT genome parameters with default values
            var genomeParameters = new MazeGenomeParameters();

            // Get root of neat genome configuration section
            var nodeList = xmlConfig.GetElementsByTagName("MazeGenomeConfig", "");

            // Note that if there are multiple defined (such as would be the case with an experiment that uses multiple EAs), 
            // the first one is used here, which will accurately correspond to the current algorithm under consideration
            if (nodeList.Count >= 1)
            {
                // Convert to an XML element
                var xmlMazeGenomeConfig = nodeList[0] as XmlElement;

                // Read all of the applicable parameters in
                var wallStartMutationProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateWallStartLocationProbability");
                var passageStartMutationProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutatePassageStartLocationProbability");
                var addWallProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateAddWallProbability");
                var deleteWallProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateDeleteWallProbability");
                var pathWaypointLocationMutationProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutatePathWaypointLocationProbability");
                var addPathWaypointMutationProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateAddPathWaypointProbability");
                var expandMazeProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateExpandMazeProbability");
                var perturbanceMagnitude = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "PerturbanceMagnitude");
                var verticalWallBias = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig, "VerticalWallBias");

                // Set each if it's specified in the configuration (otherwise, accept the default)
                if (wallStartMutationProbability != null)
                {
                    genomeParameters.MutateWallStartLocationProbability = (double) wallStartMutationProbability;
                }

                if (passageStartMutationProbability != null)
                {
                    genomeParameters.MutatePassageStartLocationProbability = (double) passageStartMutationProbability;
                }

                if (addWallProbability != null)
                {
                    genomeParameters.MutateAddWallProbability = (double) addWallProbability;
                }

                if (deleteWallProbability != null)
                {
                    genomeParameters.MutateDeleteWallProbability = (double) deleteWallProbability;
                }

                if (pathWaypointLocationMutationProbability != null)
                {
                    genomeParameters.MutatePathWaypointLocationProbability =
                        (double) pathWaypointLocationMutationProbability;
                }

                if (addPathWaypointMutationProbability != null)
                {
                    genomeParameters.MutateAddPathWaypointProbability = (double) addPathWaypointMutationProbability;
                }

                if (expandMazeProbability != null)
                {
                    genomeParameters.MutateExpandMazeProbability = (double) expandMazeProbability;
                }

                if (perturbanceMagnitude != null)
                {
                    genomeParameters.PerturbanceMagnitude = (double) perturbanceMagnitude;
                }

                if (verticalWallBias != null)
                {
                    genomeParameters.VerticalWallBias = (double) verticalWallBias;
                }
            }

            return genomeParameters;
        }
        
        /// <summary>
        ///     Reads in seed maze genomes used to bootstrap MCC experiments.
        /// </summary>
        /// <param name="seedMazePath">
        ///     The path of the single maze genome or a directory containing multiple XML genome
        ///     definitions.
        /// </param>
        /// <param name="mazeGenomeFactory">The maze genome factory to assign to each genome.</param>
        /// <returns>The list of seed maze genomes.</returns>
        public static IEnumerable<MazeGenome> ReadSeedMazeGenomes(string seedMazePath,
            MazeGenomeFactory mazeGenomeFactory)
        {
            var mazeGenomes = new List<MazeGenome>();

            // Get the maze genome files in the given path
            var mazeGenomeFiles = ExperimentUtils.GetGenomeFiles(seedMazePath);

            // Read in all maze genomes and add them to the list
            foreach (var mazeGenomeFile in mazeGenomeFiles)
            {
                using (var xr = XmlReader.Create(mazeGenomeFile))
                {
                    // Read in the maze genomes
                    var curMazeGenomes = MazeGenomeXmlIO.ReadCompleteGenomeList(xr, mazeGenomeFactory);

                    // Add the genomes to the overall genome list
                    mazeGenomes.AddRange(curMazeGenomes);
                }
            }

            return mazeGenomes;
        }

        /// <summary>
        ///     Generates the specified number of maze genomes with the specified complexity (i.e. number of interior partitions).
        /// </summary>
        /// <param name="numMazeGenomes">The number of maze genomes to generate.</param>
        /// <param name="numPartitions">The number of initial partitions (the starting complexity of the genome).</param>
        /// <param name="mazeGenomeFactory">Reference to the maze genome factory.</param>
        /// <returns></returns>
        public static List<MazeGenome> GenerateMazeGenomes(int numMazeGenomes, int numPartitions,
            MazeGenomeFactory mazeGenomeFactory)
        {
            var mazeGenomes = new List<MazeGenome>(numMazeGenomes);
            var rand = RandomDefaults.CreateRandomSource();

            for (var curMazeCnt = 0; curMazeCnt < numMazeGenomes; curMazeCnt++)
            {
                // Reset innovation IDs
                mazeGenomeFactory.InnovationIdGenerator.Reset();

                // Create a new genome and pass in the requisite factory
                var mazeGenome = new MazeGenome(mazeGenomeFactory, 0, 0);

                // Create the specified number of interior partitions (i.e. maze genes)
                for (var cnt = 0; cnt < numPartitions; cnt++)
                {
                    // Create new maze gene and add to genome
                    mazeGenome.WallGeneList.Add(new WallGene(mazeGenomeFactory.InnovationIdGenerator.NextId,
                        rand.NextDouble(), rand.NextDouble(), rand.NextDouble() < 0.5));
                }

                mazeGenomes.Add(mazeGenome);
            }

            return mazeGenomes;
        }
        
        /// <summary>
        ///     Determines which MCC initializer to instantiate and return based on the initialization algorithm search
        ///     type.
        /// </summary>
        /// <param name="xmlConfig">XML initialization configuration.</param>
        /// <returns>The instantiated initializer.</returns>
        public static MCCMazeNavigationInitializer DetermineMazeNavigationMCCInitializer(XmlElement xmlConfig)
        {
            // Make sure that the XML configuration exists
            if (xmlConfig == null)
            {
                throw new ArgumentException("Missing or invalid MCC initialization configuration.");
            }

            // Extract the corresponding search and selection algorithm domain types
            var searchType =
                AlgorithmTypeUtil.ConvertStringToSearchType(XmlUtils.TryGetValueAsString(xmlConfig, "SearchAlgorithm"));

            // There's currently just two MCC initializers: fitness and novelty search
            switch (searchType)
            {
                case SearchType.Fitness:
                    return new FitnessMCCMazeNavigationInitializer();
                default:
                    return new NoveltySearchMCCMazeNavigationInitializer();
            }
        }
    }
}