using System.IO;
using System.Xml;
using ExperimentEntities.entities;
using SharpNeat.Decoders.Neat;
using SharpNeat.Decoders.Substrate;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Genomes.Substrate;
using SharpNeat.Phenomes.Voxels;

namespace BodyBrainSupportLib
{
    /// <summary>
    ///     Provides methods for decoding genomes and unserializing from XML.
    /// </summary>
    public static class DecodeHandler
    {
        /// <summary>
        ///     Reads the body genome XML and decodes into its voxel body phenotype.
        /// </summary>
        /// <param name="bodyGenome">The body genome to convert into its corresponding phenotype.</param>
        /// <param name="bodyDecoder">The body genome decoder.</param>
        /// <param name="bodyGenomeFactory">The body genome factory.</param>
        /// <param name="substrateResIncrease">
        ///     The amount by which to increase the resolution from that at which the body was
        ///     evolved.
        /// </param>
        /// <returns>The decoded voxel body.</returns>
        public static VoxelBody DecodeBodyGenome(MccexperimentVoxelBodyGenome bodyGenome,
            NeatSubstrateGenomeDecoder bodyDecoder, NeatSubstrateGenomeFactory bodyGenomeFactory,
            int substrateResIncrease = 0)
        {
            VoxelBody body;

            using (var xmlReader = XmlReader.Create(new StringReader(bodyGenome.GenomeXml)))
            {
                body = new VoxelBody(bodyDecoder.Decode(
                        NeatSubstrateGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, true, bodyGenomeFactory)),
                    substrateResIncrease);
            }

            return body;
        }

        /// <summary>
        ///     Reads the brain genome XML and decodes into its voxel brain phenotype.
        /// </summary>
        /// <param name="brainGenome">The brain genome to convert into its corresponding phenotype.</param>
        /// <param name="brainDecoder">The brain genome decoder.</param>
        /// <param name="brainGenomeFactory">The brain genome factory.</param>
        /// <param name="body">The voxel body to which the brain is scaled.</param>
        /// <param name="numConnections">The number of connections in the brain controller network.</param>
        /// <returns>The decoded voxel brain.</returns>
        public static VoxelAnnBrain DecodeBrainGenome(MccexperimentVoxelBrainGenome brainGenome,
            NeatGenomeDecoder brainDecoder, CppnGenomeFactory brainGenomeFactory, VoxelBody body, int numConnections)
        {
            VoxelAnnBrain brain;

            using (var xmlReader = XmlReader.Create(new StringReader(brainGenome.GenomeXml)))
            {
                brain = new VoxelAnnBrain(
                    brainDecoder.Decode(NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, true, brainGenomeFactory)),
                    body.LengthX, body.LengthY, body.LengthZ, numConnections);
            }

            return brain;
        }

        /// <summary>
        ///     Reads the body genome XML and decodes into its voxel body phenotype.
        /// </summary>
        /// <param name="bodyGenomeXml">The XML of the body genome to convert into its corresponding phenotype.</param>
        /// <param name="bodyDecoder">The body genome decoder.</param>
        /// <param name="bodyGenomeFactory">The body genome factory.</param>
        /// <param name="substrateResIncrease">
        ///     The amount by which to increase the resolution from that at which the body was
        ///     evolved.
        /// </param>
        /// <returns>The decoded voxel body.</returns>
        public static VoxelBody DecodeBodyGenome(string bodyGenomeXml,
            NeatSubstrateGenomeDecoder bodyDecoder, NeatSubstrateGenomeFactory bodyGenomeFactory,
            int substrateResIncrease = 0)
        {
            VoxelBody body;

            using (var xmlReader = XmlReader.Create(new StringReader(bodyGenomeXml)))
            {
                body = new VoxelBody(bodyDecoder.Decode(
                        NeatSubstrateGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, true, bodyGenomeFactory)),
                    substrateResIncrease);
            }

            return body;
        }
    }
}