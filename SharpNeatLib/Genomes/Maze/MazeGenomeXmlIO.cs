﻿#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    /// <summary>
    ///     Static class for reading and writing MazeGenome(s) to and from XML.
    /// </summary>
    public static class MazeGenomeXmlIO
    {
        #region Constants [XML Strings]

        private const string __ElemRoot = "Root";
        private const string __ElemMazes = "Mazes";
        private const string __ElemMaze = "Maze";
        private const string __ElemWalls = "Walls";
        private const string __ElemWall = "Wall";

        private const string __AttrId = "id";
        private const string __AttrBirthGeneration = "birthGen";
        private const string __AttrRelativeWallLocation = "RelativeWallLocation";
        private const string __AttrRelativePassageLocation = "RelativePassageLocation";
        private const string __AttrOrientationSeed = "OrientationSeed";

        #endregion

        #region Public Static Methods [Write to XML]

        /// <summary>
        ///     Writes a list of maze genomes to XML.
        /// </summary>
        /// <param name="xw">Reference to the XmlWriter.</param>
        /// <param name="genomeList">List of genomes to write.</param>
        public static void WriteComplete(XmlWriter xw, IList<MazeGenome> genomeList)
        {
            if (genomeList.Count == 0)
            {
                // Nothing to do.
                return;
            }

            // <Root>
            xw.WriteStartElement(__ElemRoot);

            // <Mazes>
            xw.WriteStartElement(__ElemMazes);

            // Write genomes
            foreach (MazeGenome genome in genomeList)
            {
                Write(xw, genome);
            }

            // </Mazes>
            xw.WriteEndElement();

            // </Root>
            xw.WriteEndElement();
        }

        /// <summary>
        ///     Writes a single genome to XML.
        /// </summary>
        /// <param name="xw">Reference to the XmlWriter.</param>
        /// <param name="genome">The genome to write.</param>
        public static void WriteComplete(XmlWriter xw, MazeGenome genome)
        {
            WriteComplete(xw, new List<MazeGenome>(1) {genome});
        }

        /// <summary>
        ///     Writes all of the components of a single maze genome to XML.
        /// </summary>
        /// <param name="xw">Reference to the XmlWriter.</param>
        /// <param name="genome">The genome to write.</param>
        public static void Write(XmlWriter xw, MazeGenome genome)
        {
            // Write maze element and accompanying attributes
            xw.WriteStartElement(__ElemMaze);
            xw.WriteAttributeString(__AttrId, genome.Id.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(__AttrBirthGeneration,
                genome.BirthGeneration.ToString(NumberFormatInfo.InvariantInfo));

            // Emit walls
            xw.WriteStartElement(__ElemWalls);
            foreach (MazeGene mazeGene in genome.GeneList)
            {
                xw.WriteStartElement(__ElemWall);
                xw.WriteAttributeString(__AttrId, mazeGene.InnovationId.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(__AttrRelativeWallLocation,
                    mazeGene.WallLocation.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(__AttrRelativePassageLocation,
                    mazeGene.PassageLocation.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(__AttrOrientationSeed, mazeGene.OrientationSeed.ToString());

                // </Wall>
                xw.WriteEndElement();
            }
            // </Walls>
            xw.WriteEndElement();

            // </Maze>
            xw.WriteEndElement();
        }

        /// <summary>
        ///     Serializes a maze genome into a string.
        /// </summary>
        /// <param name="mazeGenome">The maze genome object to serialize.</param>
        /// <returns>The serialized maze genome string.</returns>
        public static string GetGenomeXml(MazeGenome mazeGenome)
        {
            // Create a new string writer into which to write the genome XML
            StringWriter genomeStringWriter = new StringWriter();

            // Serialize the genome to XML, but write it into a string writer instead of outputting to a file
            if (mazeGenome != null)
            {
                using (XmlTextWriter genomeTextWriter = new XmlTextWriter(genomeStringWriter))
                {
                    WriteComplete(genomeTextWriter, mazeGenome);
                }
            }

            // Convert to a string representation and return
            return genomeStringWriter.ToString();
        }

        #endregion

        #region Public Static Methods [Read from XML]

        /// <summary>
        ///     Reads a list of maze genomes from an XML file.
        /// </summary>
        /// <param name="xr">Reference to the XmlReader.</param>
        /// <param name="genomeFactory">A MazeGenomeFactory to construct genomes against.</param>
        /// <returns>Instantiated list of maze genomes.</returns>
        public static List<MazeGenome> ReadCompleteGenomeList(XmlReader xr, MazeGenomeFactory genomeFactory)
        {
            // Find <Root>
            XmlIoUtils.MoveToElement(xr, false, __ElemRoot);

            // Find <Mazes>
            XmlIoUtils.MoveToElement(xr, true, __ElemMazes);

            // Read mazes
            List<MazeGenome> genomeList = new List<MazeGenome>();
            using (XmlReader xrSubtree = xr.ReadSubtree())
            {
                // Re-scan for the root <Mazes> element
                XmlIoUtils.MoveToElement(xrSubtree, false);

                // Move to first Maze element
                XmlIoUtils.MoveToElement(xrSubtree, true, __ElemMaze);

                // Read maze elements
                do
                {
                    MazeGenome genome = ReadGenome(xrSubtree);
                    genomeList.Add(genome);
                } while (xrSubtree.ReadToNextSibling(__ElemMaze));
            }

            // Check for empty list
            if (genomeList.Count == 0)
            {
                return genomeList;
            }

            // Determine the max genome ID
            uint maxGenomeId = genomeList.Aggregate<MazeGenome, uint>(0,
                (current, genome) => Math.Max(current, genome.Id));

            // Determine the max gene innovation ID
            uint maxInnovationId = genomeList.Aggregate<MazeGenome, uint>(0,
                (curMaxPopulationInnovationId, genome) =>
                    genome.GeneList.Aggregate(curMaxPopulationInnovationId,
                        (curMaxGenomeInnovationId, mazeGene) =>
                            Math.Max(curMaxGenomeInnovationId, mazeGene.InnovationId)));

            // Set the genome factory ID generator and innovation ID generator to one more than the max
            genomeFactory.GenomeIdGenerator.Reset(Math.Max(genomeFactory.GenomeIdGenerator.Peek, maxGenomeId + 1));
            genomeFactory.InnovationIdGenerator.Reset(Math.Max(genomeFactory.InnovationIdGenerator.Peek,
                maxInnovationId + 1));

            // Retrospecitively assign the genome factory to the genomes
            foreach (MazeGenome genome in genomeList)
            {
                genome.GenomeFactory = genomeFactory;
            }

            return genomeList;
        }

        /// <summary>
        ///     Reads a single genome from a population from the given XML file.  This is typically used in cases where a
        ///     population file is being read in, but it only contains one genome.
        /// </summary>
        /// <param name="xr">Reference to the XmlReader.</param>
        /// <param name="genomeFactory">A MazeGenomeFactory to construct genomes against.</param>
        /// <returns>Instantiated maze genome.</returns>
        public static MazeGenome ReadSingleGenomeFromRoot(XmlReader xr, MazeGenomeFactory genomeFactory)
        {
            return ReadCompleteGenomeList(xr, genomeFactory)[0];
        }

        /// <summary>
        ///     Reads all of the components of a single maze genome from the given XML file.
        /// </summary>
        /// <param name="xr">Reference to the XmlReader.</param>
        /// <returns>Instantiated maze genome.</returns>
        public static MazeGenome ReadGenome(XmlReader xr)
        {
            // Find <Maze>
            XmlIoUtils.MoveToElement(xr, false, __ElemMaze);
            int initialDepth = xr.Depth;

            string genomeIdStr = xr.GetAttribute(__AttrId);
            uint genomeId;
            uint.TryParse(genomeIdStr, out genomeId);

            // Read birthGeneration attribute if present. Otherwise default to zero.
            string birthGenStr = xr.GetAttribute(__AttrBirthGeneration);
            uint birthGen;
            uint.TryParse(birthGenStr, out birthGen);

            // Find <Walls>
            XmlIoUtils.MoveToElement(xr, true, __ElemWalls);

            // Instantiate list to hold maze genes that are read from the file
            IList<MazeGene> genes = new List<MazeGene>();

            // Create a reader over the <Nodes> sub-tree.
            using (XmlReader xrSubtree = xr.ReadSubtree())
            {
                // Re-scan for the root <Walls> element
                XmlIoUtils.MoveToElement(xrSubtree, false);

                // Only continue if there are genes (i.e. walls) in the maze genome
                if (xrSubtree.IsEmptyElement == false)
                {
                    // Move to first wall gene
                    XmlIoUtils.MoveToElement(xrSubtree, true, __ElemWall);

                    do
                    {
                        // Read the wall, passage, and orientation information for the gene
                        uint geneId = XmlIoUtils.ReadAttributeAsUInt(xrSubtree, __AttrId);
                        double relativeWallLocation = XmlIoUtils.ReadAttributeAsDouble(xrSubtree,
                            __AttrRelativeWallLocation);
                        double relativePassageLocation = XmlIoUtils.ReadAttributeAsDouble(xrSubtree,
                            __AttrRelativePassageLocation);
                        bool orientationSeed = XmlIoUtils.ReadAttributeAsBool(xrSubtree, __AttrOrientationSeed);

                        // Create a new maze gene and add it to the list
                        genes.Add(new MazeGene(geneId, relativeWallLocation, relativePassageLocation, orientationSeed));
                    } while (xrSubtree.ReadToNextSibling(__ElemWall));
                }
            }

            // Move the reader beyond the closing tags </Walls> and </Maze>.
            do
            {
                if (xr.Depth <= initialDepth)
                {
                    break;
                }
            } while (xr.Read());

            // Construct and return loaded MazeGenome
            return new MazeGenome(null, genomeId, birthGen, genes);
        }

        #endregion
    }
}