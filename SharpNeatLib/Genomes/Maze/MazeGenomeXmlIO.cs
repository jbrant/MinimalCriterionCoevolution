#region

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
        #region Private helper methods

        /// <summary>
        ///     Extracts the corresponding intersection orientation enumeration value for the given string representation.
        /// </summary>
        /// <param name="strIntersectionOrientation">String representation of the intersection orientation.</param>
        /// <returns>Enumeration value for the given intersection orientation.</returns>
        private static IntersectionOrientation ParseIntersectionOrientation(string strIntersectionOrientation)
        {
            return "HORIZONTAL".Equals(strIntersectionOrientation.ToUpperInvariant())
                ? IntersectionOrientation.Horizontal
                : IntersectionOrientation.Vertical;
        }

        #endregion

        #region Constants [XML Strings]

        private const string ElemRoot = "Root";
        private const string ElemMazes = "Mazes";
        private const string ElemMaze = "Maze";
        private const string ElemPaths = "Paths";
        private const string ElemPath = "Path";
        private const string ElemWalls = "Walls";
        private const string ElemWall = "Wall";

        private const string AttrId = "id";
        private const string AttrBirthGeneration = "birthGen";
        private const string AttrHeight = "height";
        private const string AttrWidth = "width";
        private const string AttrWaypointCoordinateX = "WaypointCoordinateX";
        private const string AttrWaypointCoordinateY = "WaypointCoordinateY";
        private const string AttrOrientation = "DefaultOrientation";
        private const string AttrRelativeWallLocation = "RelativeWallLocation";
        private const string AttrRelativePassageLocation = "RelativePassageLocation";
        private const string AttrOrientationSeed = "OrientationSeed";

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
            xw.WriteStartElement(ElemRoot);

            // <Mazes>
            xw.WriteStartElement(ElemMazes);

            // Write genomes
            foreach (var genome in genomeList)
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
            xw.WriteStartElement(ElemMaze);
            xw.WriteAttributeString(AttrId, genome.Id.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(AttrBirthGeneration,
                genome.BirthGeneration.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(AttrHeight, genome.MazeBoundaryHeight.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(AttrWidth, genome.MazeBoundaryWidth.ToString(NumberFormatInfo.InvariantInfo));

            // Emit paths
            xw.WriteStartElement(ElemPaths);
            foreach (var pathGene in genome.PathGeneList)
            {
                // <Path>
                xw.WriteStartElement(ElemPath);

                xw.WriteAttributeString(AttrId, pathGene.InnovationId.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrWaypointCoordinateX,
                    pathGene.Waypoint.X.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrWaypointCoordinateY,
                    pathGene.Waypoint.Y.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrOrientation, pathGene.DefaultOrientation.ToString());

                // </Path>
                xw.WriteEndElement();
            }

            // </Paths>
            xw.WriteEndElement();

            // Emit walls
            xw.WriteStartElement(ElemWalls);
            foreach (var wallGene in genome.WallGeneList)
            {
                // <Wall>
                xw.WriteStartElement(ElemWall);

                xw.WriteAttributeString(AttrId, wallGene.InnovationId.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrRelativeWallLocation,
                    wallGene.WallLocation.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrRelativePassageLocation,
                    wallGene.PassageLocation.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrOrientationSeed, wallGene.OrientationSeed.ToString());

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
            var genomeStringWriter = new StringWriter();

            // Serialize the genome to XML, but write it into a string writer instead of outputting to a file
            if (mazeGenome != null)
            {
                using (var genomeTextWriter = new XmlTextWriter(genomeStringWriter))
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
            XmlIoUtils.MoveToElement(xr, false, ElemRoot);

            // Find <Mazes>
            XmlIoUtils.MoveToElement(xr, true, ElemMazes);

            // Read mazes
            var genomeList = new List<MazeGenome>();
            using (var xrSubtree = xr.ReadSubtree())
            {
                // Re-scan for the root <Mazes> element
                XmlIoUtils.MoveToElement(xrSubtree, false);

                // Move to first Maze element
                XmlIoUtils.MoveToElement(xrSubtree, true, ElemMaze);

                // Read maze elements
                do
                {
                    var genome = ReadGenome(xrSubtree, genomeFactory);
                    genomeList.Add(genome);
                } while (xrSubtree.ReadToNextSibling(ElemMaze));
            }

            // Check for empty list
            if (genomeList.Count == 0)
            {
                return genomeList;
            }

            // Determine the max genome ID
            var maxGenomeId = genomeList.Aggregate<MazeGenome, uint>(0,
                (current, genome) => Math.Max(current, genome.Id));

            // Determine the max gene innovation ID
            var maxInnovationId = genomeList.Aggregate<MazeGenome, uint>(0,
                (curMaxPopulationInnovationId, genome) =>
                    genome.WallGeneList.Aggregate(curMaxPopulationInnovationId,
                        (curMaxGenomeInnovationId, mazeGene) =>
                            Math.Max(curMaxGenomeInnovationId, mazeGene.InnovationId)));

            // Set the genome factory ID generator and innovation ID generator to one more than the max
            genomeFactory.GenomeIdGenerator.Reset(Math.Max(genomeFactory.GenomeIdGenerator.Peek, maxGenomeId + 1));
            genomeFactory.InnovationIdGenerator.Reset(Math.Max(genomeFactory.InnovationIdGenerator.Peek,
                maxInnovationId + 1));

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
        public static MazeGenome ReadGenome(XmlReader xr, MazeGenomeFactory genomeFactory)
        {
            // Find <Maze>
            XmlIoUtils.MoveToElement(xr, false, ElemMaze);
            var initialDepth = xr.Depth;

            // Read genome ID attribute
            var genomeIdStr = xr.GetAttribute(AttrId);
            uint.TryParse(genomeIdStr, out var genomeId);

            // Read birthGeneration attribute if present. Otherwise default to zero.
            var birthGenStr = xr.GetAttribute(AttrBirthGeneration);
            uint.TryParse(birthGenStr, out var birthGen);

            // Read maze height attribute
            var heightStr = xr.GetAttribute(AttrHeight);
            int.TryParse(heightStr, out var height);

            // Read maze width attribute
            var widthStr = xr.GetAttribute(AttrWidth);
            int.TryParse(widthStr, out var width);

            // Find <Paths>
            XmlIoUtils.MoveToElement(xr, true, ElemPaths);

            // Instantiate list to hold path genes that are read from the file
            IList<PathGene> pathGenes = new List<PathGene>();

            // Create a reader over the <Paths> sub-tree.
            using (var xrSubtree = xr.ReadSubtree())
            {
                // Re-scan for the root <Paths> element
                XmlIoUtils.MoveToElement(xrSubtree, false);

                // Only continue if there are genes (i.e. paths) in the maze genome
                if (xrSubtree.IsEmptyElement == false)
                {
                    // Move to first path gene
                    XmlIoUtils.MoveToElement(xrSubtree, true, ElemPath);

                    do
                    {
                        // Read the path, waypoint coordinates, and orientation information for the gene
                        var geneId = XmlIoUtils.ReadAttributeAsUInt(xrSubtree, AttrId);
                        var waypointCoordinateX = XmlIoUtils.ReadAttributeAsInt(xrSubtree, AttrWaypointCoordinateX);
                        var waypointCoordinateY = XmlIoUtils.ReadAttributeAsInt(xrSubtree, AttrWaypointCoordinateY);
                        var orientation =
                            ParseIntersectionOrientation(XmlIoUtils.ReadAttributeAsString(xrSubtree, AttrOrientation));

                        // Create a new path gene and add it to the list
                        pathGenes.Add(new PathGene(geneId, new Point2DInt(waypointCoordinateX, waypointCoordinateY),
                            orientation));
                    } while (xrSubtree.ReadToNextSibling(ElemPath));
                }
            }

            // Find <Walls>
            XmlIoUtils.MoveToElement(xr, true, ElemWalls);

            // Instantiate list to hold wall genes that are read from the file
            IList<WallGene> wallGenes = new List<WallGene>();

            // Create a reader over the <Walls> sub-tree.
            using (var xrSubtree = xr.ReadSubtree())
            {
                // Re-scan for the root <Walls> element
                XmlIoUtils.MoveToElement(xrSubtree, false);

                // Only continue if there are genes (i.e. walls) in the maze genome
                if (xrSubtree.IsEmptyElement == false)
                {
                    // Move to first wall gene
                    XmlIoUtils.MoveToElement(xrSubtree, true, ElemWall);

                    do
                    {
                        // Read the wall, passage, and orientation information for the gene
                        var geneId = XmlIoUtils.ReadAttributeAsUInt(xrSubtree, AttrId);
                        var relativeWallLocation = XmlIoUtils.ReadAttributeAsDouble(xrSubtree,
                            AttrRelativeWallLocation);
                        var relativePassageLocation = XmlIoUtils.ReadAttributeAsDouble(xrSubtree,
                            AttrRelativePassageLocation);
                        var orientationSeed = XmlIoUtils.ReadAttributeAsBool(xrSubtree, AttrOrientationSeed);

                        // Create a new maze gene and add it to the list
                        wallGenes.Add(new WallGene(geneId, relativeWallLocation, relativePassageLocation,
                            orientationSeed));
                    } while (xrSubtree.ReadToNextSibling(ElemWall));
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
            return new MazeGenome(genomeFactory, genomeId, birthGen, height, width, wallGenes, pathGenes);
        }

        public static IList<MazeGenome> ReadMazeGenomesFromXml(IList<string> mazeXmlCollection, MazeGenomeFactory genomeFactory)
        {
            var mazeGenomes = new List<MazeGenome>(mazeXmlCollection.Count);
            
            foreach (var mazeXml in mazeXmlCollection)
            {
                using (var xr = XmlReader.Create(new StringReader(mazeXml)))
                {
                    mazeGenomes.Add(ReadCompleteGenomeList(xr, genomeFactory)[0]);
                }
            }
            
            return mazeGenomes;
        }
        
        #endregion
    }
}