using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Utility;

namespace SharpNeat.Genomes.Substrate
{
    /// <summary>
    ///     Static class for reading and writing NeatSubstrateGenome(s) to and from XML.
    /// </summary>
    public static class NeatSubstrateGenomeXmlIO
    {
        #region Public Static Methods [Read from XML]

        /// <summary>
        ///     Reads a list of NeatGenome(s) from XML that has a containing 'Root' element. The root
        ///     element also contains the activation function library that the genomes are associated with.
        /// </summary>
        /// <param name="xr">The XmlReader to read from.</param>
        /// <param name="nodeFnIds">
        ///     Indicates if node activation function IDs should be read. If false then
        ///     all node activation function IDs default to 0.
        /// </param>
        /// <param name="substrateGenomeFactory">A NeatSubstrateGenomeFactory object to construct genomes against.</param>
        public static List<NeatSubstrateGenome> ReadCompleteGenomeList(XmlReader xr, bool nodeFnIds,
            NeatSubstrateGenomeFactory substrateGenomeFactory)
        {
            // Find <Root>.
            XmlIoUtils.MoveToElement(xr, false, ElemRoot);

            // Read IActivationFunctionLibrary. This library is not used, it is compared against the one already present in the 
            // genome factory to confirm that the loaded genomes are compatible with the genome factory.
            XmlIoUtils.MoveToElement(xr, true, ElemActivationFunctions);
            var activationFnLib = NetworkXmlIO.ReadActivationFunctionLibrary(xr);
            XmlIoUtils.MoveToElement(xr, false, ElemNetworks);

            // Read genomes.
            var genomeList = new List<NeatSubstrateGenome>();
            using (var xrSubtree = xr.ReadSubtree())
            {
                // Re-scan for the root <Networks> element.
                XmlIoUtils.MoveToElement(xrSubtree, false);

                // Move to first Network elem.
                XmlIoUtils.MoveToElement(xrSubtree, true, ElemNetwork);

                // Read Network elements.
                do
                {
                    genomeList.Add(ReadGenome(xrSubtree, nodeFnIds));
                } while (xrSubtree.ReadToNextSibling(ElemNetwork));
            }

            // Check for empty list.
            if (genomeList.Count == 0)
            {
                return genomeList;
            }

            // Get the number of inputs and outputs expected by the genome factory.
            var inputCount = substrateGenomeFactory.NeatGenomeFactory.InputNeuronCount;
            var outputCount = substrateGenomeFactory.NeatGenomeFactory.OutputNeuronCount;

            // Check all genomes have the same number of inputs & outputs.
            // Also track the highest genomeID and innovation ID values; we need these to construct a new genome factory.
            uint maxGenomeId = 0;
            uint maxInnovationId = 0;

            foreach (var genome in genomeList)
            {
                // Check number of inputs/outputs.
                if (genome.InputNeuronCount != inputCount ||
                    genome.OutputNeuronCount != outputCount)
                {
                    throw new SharpNeatException(
                        string.Format(
                            "Genome with wrong number of inputs and/or outputs, expected [{0}][{1}] got [{2}][{3}]",
                            inputCount, outputCount, genome.InputNeuronCount,
                            genome.OutputNeuronCount));
                }

                // Track max IDs.
                maxGenomeId = Math.Max(maxGenomeId, genome.Id);

                // Node and connection innovation IDs are in the same ID space.
                foreach (var nGene in genome.NeuronGeneList)
                {
                    maxInnovationId = Math.Max(maxInnovationId, nGene.InnovationId);
                }

                // Register connection IDs.
                foreach (var cGene in genome.ConnectionGeneList)
                {
                    maxInnovationId = Math.Max(maxInnovationId, cGene.InnovationId);
                }
            }

            // Check that activation functions in XML match that in the genome factory.
            var loadedActivationFnList = activationFnLib.GetFunctionList();
            var factoryActivationFnList =
                substrateGenomeFactory.NeatGenomeFactory.ActivationFnLibrary.GetFunctionList();
            if (loadedActivationFnList.Count != factoryActivationFnList.Count)
            {
                throw new SharpNeatException(
                    "The activation function library loaded from XML does not match the genome factory's activation function library.");
            }

            for (var i = 0; i < factoryActivationFnList.Count; i++)
            {
                if (loadedActivationFnList[i].Id != factoryActivationFnList[i].Id
                    ||
                    loadedActivationFnList[i].ActivationFunction.FunctionId !=
                    factoryActivationFnList[i].ActivationFunction.FunctionId)
                {
                    throw new SharpNeatException(
                        "The activation function library loaded from XML does not match the genome factory's activation function library.");
                }
            }

            // Initialise the genome factory's genome and innovation ID generators.
            substrateGenomeFactory.GenomeIdGenerator.Reset(Math.Max(substrateGenomeFactory.GenomeIdGenerator.Peek,
                maxGenomeId + 1));
            substrateGenomeFactory.NeatGenomeFactory.InnovationIdGenerator.Reset(Math.Max(
                substrateGenomeFactory.NeatGenomeFactory.InnovationIdGenerator.Peek,
                maxInnovationId + 1));

            // Assign the genome factory to the genomes. This is how we overcome the genome/genomeFactory
            // chicken and egg problem.
            foreach (var genome in genomeList)
            {
                genome.NeatSubstrateGenomeFactory = substrateGenomeFactory;
            }

            return genomeList;
        }

        /// <summary>
        ///     Reads a single genome from a population from the given XML file.  This is typically used in cases where a
        ///     population file is being read in, but it only contains one genome.
        /// </summary>
        /// <param name="xr">The XmlReader to read from.</param>
        /// <param name="nodeFnIds">
        ///     Indicates if node activation function IDs should be read. If false then
        ///     all node activation function IDs default to 0.
        /// </param>
        /// <param name="substrateGenomeFactory">A NeatSubstrateGenomeFactory object to construct genomes against.</param>
        public static NeatSubstrateGenome ReadSingleGenomeFromRoot(XmlReader xr, bool nodeFnIds,
            NeatSubstrateGenomeFactory substrateGenomeFactory)
        {
            return ReadCompleteGenomeList(xr, nodeFnIds, substrateGenomeFactory)[0];
        }

        /// <summary>
        ///     Reads a NeatGenome from XML.
        /// </summary>
        /// <param name="xr">The XmlReader to read from.</param>
        /// <param name="nodeFnIds">
        ///     Indicates if node activation function IDs should be read. They are required
        ///     for HyperNEAT genomes but not for NEAT
        /// </param>
        public static NeatSubstrateGenome ReadGenome(XmlReader xr, bool nodeFnIds)
        {
            // Find <Network>.
            XmlIoUtils.MoveToElement(xr, false, ElemNetwork);
            var initialDepth = xr.Depth;

            // Read genome ID attribute if present. Otherwise default to zero; it's the caller's responsibility to 
            // check IDs are unique and in-line with the genome factory's ID generators.
            var genomeIdStr = xr.GetAttribute(AttrId);
            uint genomeId;
            uint.TryParse(genomeIdStr, out genomeId);

            // Read birthGeneration attribute if present. Otherwise default to zero.
            var birthGenStr = xr.GetAttribute(AttrBirthGeneration);
            uint birthGen;
            uint.TryParse(birthGenStr, out birthGen);

            // Read substrateX attribute
            var substrateXstr = xr.GetAttribute(AttrSubstrateX);
            int substrateX;
            int.TryParse(substrateXstr, out substrateX);

            // Read substrateY attribute
            var substrateYstr = xr.GetAttribute(AttrSubstrateY);
            int substrateY;
            int.TryParse(substrateYstr, out substrateY);

            // Read substrateZ attribute
            var substrateZstr = xr.GetAttribute(AttrSubstrateZ);
            int substrateZ;
            int.TryParse(substrateZstr, out substrateZ);

            // Find <Nodes>.
            XmlIoUtils.MoveToElement(xr, true, ElemNodes);

            // Create a reader over the <Nodes> sub-tree.
            var inputNodeCount = 0;
            var outputNodeCount = 0;
            var nGeneList = new NeuronGeneList();
            using (var xrSubtree = xr.ReadSubtree())
            {
                // Re-scan for the root <Nodes> element.
                XmlIoUtils.MoveToElement(xrSubtree, false);

                // Move to first node elem.
                XmlIoUtils.MoveToElement(xrSubtree, true, ElemNode);

                // Read node elements.
                do
                {
                    var neuronType = NetworkXmlIO.ReadAttributeAsNodeType(xrSubtree, AttrType);
                    var id = XmlIoUtils.ReadAttributeAsUInt(xrSubtree, AttrId);
                    var functionId = 0;
                    double[] auxState = null;
                    if (nodeFnIds)
                    {
                        // Read activation fn ID.
                        functionId = XmlIoUtils.ReadAttributeAsInt(xrSubtree, AttrActivationFunctionId);

                        // Read aux state as comma seperated list of real values.
                        auxState = XmlIoUtils.ReadAttributeAsDoubleArray(xrSubtree, AttrAuxState);
                    }

                    var nGene = new NeuronGene(id, neuronType, functionId, auxState);
                    nGeneList.Add(nGene);

                    // Track the number of input and output nodes.
                    switch (neuronType)
                    {
                        case NodeType.Input:
                            inputNodeCount++;
                            break;
                        case NodeType.Output:
                            outputNodeCount++;
                            break;
                    }
                } while (xrSubtree.ReadToNextSibling(ElemNode));
            }

            // Find <Connections>.
            XmlIoUtils.MoveToElement(xr, false, ElemConnections);

            // Create a reader over the <Connections> sub-tree.
            var cGeneList = new ConnectionGeneList();
            using (var xrSubtree = xr.ReadSubtree())
            {
                // Re-scan for the root <Connections> element.
                XmlIoUtils.MoveToElement(xrSubtree, false);

                // Move to first connection elem.
                var localName = XmlIoUtils.MoveToElement(xrSubtree, true);
                if (localName == ElemConnection)
                {
                    // We have at least one connection.
                    // Read connection elements.
                    do
                    {
                        var id = XmlIoUtils.ReadAttributeAsUInt(xrSubtree, AttrId);
                        var srcId = XmlIoUtils.ReadAttributeAsUInt(xrSubtree, AttrSourceId);
                        var tgtId = XmlIoUtils.ReadAttributeAsUInt(xrSubtree, AttrTargetId);
                        var weight = XmlIoUtils.ReadAttributeAsDouble(xrSubtree, AttrWeight);
                        var cGene = new ConnectionGene(id, srcId, tgtId, weight);
                        cGeneList.Add(cGene);
                    } while (xrSubtree.ReadToNextSibling(ElemConnection));
                }
            }

            // Move the reader beyond the closing tags </Connections> and </Network>.
            do
            {
                if (xr.Depth <= initialDepth)
                {
                    break;
                }
            } while (xr.Read());

            // Construct and return loaded NeatSubstrateGenome.
            return new NeatSubstrateGenome(null,
                new NeatGenome(null, genomeId, birthGen, nGeneList, cGeneList, inputNodeCount, outputNodeCount, true),
                substrateX, substrateY, substrateZ);
        }

        #endregion

        #region Constants [XML Strings]

        private const string ElemRoot = "Root";
        private const string ElemNetworks = "Networks";
        private const string ElemNetwork = "Network";
        private const string ElemNodes = "Nodes";
        private const string ElemNode = "Node";
        private const string ElemConnections = "Connections";
        private const string ElemConnection = "Con";
        private const string ElemActivationFunctions = "ActivationFunctions";

        private const string AttrId = "id";
        private const string AttrBirthGeneration = "birthGen";
        private const string AttrSubstrateX = "substrateX";
        private const string AttrSubstrateY = "substrateY";
        private const string AttrSubstrateZ = "substrateZ";
        private const string AttrFitness = "fitness";
        private const string AttrType = "type";
        private const string AttrSourceId = "src";
        private const string AttrTargetId = "tgt";
        private const string AttrWeight = "wght";
        private const string AttrActivationFunctionId = "fnId";
        private const string AttrAuxState = "aux";

        #endregion

        #region Public Static Methods [Write to XML]

        /// <summary>
        ///     Writes a list of NeatSubstrateGenome(s) to XML within a containing 'Root' element and the activation
        ///     function library that the genomes are associated with.
        /// </summary>
        /// <param name="xw">XmlWriter to write XML to.</param>
        /// <param name="genomeList">List of genomes to write as XML.</param>
        /// <param name="nodeFnIds">
        ///     Indicates if node activation function IDs should be emitted. They are required
        ///     for HyperNEAT genomes but not for NEAT.
        /// </param>
        public static void WriteComplete(XmlWriter xw, IList<NeatSubstrateGenome> genomeList, bool nodeFnIds)
        {
            if (genomeList.Count == 0)
            {
                // Nothing to do.
                return;
            }

            // <Root>
            xw.WriteStartElement(ElemRoot);

            // Write activation function library from the first genome.
            // (we expect all genomes to use the same library).
            var activationFnLib = genomeList[0].ActivationFnLibrary;
            NetworkXmlIO.Write(xw, activationFnLib);

            // <Networks>
            xw.WriteStartElement(ElemNetworks);

            // Write genomes.
            foreach (var genome in genomeList)
            {
                Debug.Assert(genome.ActivationFnLibrary == activationFnLib);
                Write(xw, genome, nodeFnIds);
            }

            // </Networks>
            xw.WriteEndElement();

            // </Root>
            xw.WriteEndElement();
        }

        /// <summary>
        ///     Writes a single NeatSubstrateGenome to XML within a containing 'Root' element and the activation
        ///     function library that the genome is associated with.
        /// </summary>
        /// <param name="xw">XmlWriter to write XML to.</param>
        /// <param name="genome">Genome to write as XML.</param>
        /// <param name="nodeFnIds">
        ///     Indicates if node activation function IDs should be emitted. They are required
        ///     for HyperNEAT genomes but not for NEAT.
        /// </param>
        public static void WriteComplete(XmlWriter xw, NeatSubstrateGenome genome, bool nodeFnIds)
        {
            // <Root>
            xw.WriteStartElement(ElemRoot);

            // Write activation function library.
            NetworkXmlIO.Write(xw, genome.ActivationFnLibrary);

            // <Networks>
            xw.WriteStartElement(ElemNetworks);

            // Write single genome.
            Write(xw, genome, nodeFnIds);

            // </Networks>
            xw.WriteEndElement();

            // </Root>
            xw.WriteEndElement();
        }

        /// <summary>
        ///     Writes a NeatGenome to XML.
        /// </summary>
        /// <param name="xw">XmlWriter to write XML to.</param>
        /// <param name="genome">Genome to write as XML.</param>
        /// <param name="nodeFnIds">
        ///     Indicates if node activation function IDs should be emitted. They are required
        ///     for HyperNEAT genomes but not for NEAT.
        /// </param>
        public static void Write(XmlWriter xw, NeatSubstrateGenome genome, bool nodeFnIds)
        {
            xw.WriteStartElement(ElemNetwork);
            xw.WriteAttributeString(AttrId, genome.Id.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(AttrBirthGeneration,
                genome.BirthGeneration.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(AttrSubstrateX, genome.SubstrateX.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(AttrSubstrateY, genome.SubstrateY.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(AttrSubstrateZ, genome.SubstrateZ.ToString(NumberFormatInfo.InvariantInfo));
            xw.WriteAttributeString(AttrFitness,
                genome.EvaluationInfo.Fitness.ToString("R", NumberFormatInfo.InvariantInfo));

            // Emit nodes.
            var sb = new StringBuilder();
            xw.WriteStartElement(ElemNodes);
            foreach (var nGene in genome.NeuronGeneList)
            {
                xw.WriteStartElement(ElemNode);
                xw.WriteAttributeString(AttrType, NetworkXmlIO.GetNodeTypeString(nGene.NodeType));
                xw.WriteAttributeString(AttrId, nGene.Id.ToString(NumberFormatInfo.InvariantInfo));
                if (nodeFnIds)
                {
                    // Write activation fn ID.
                    xw.WriteAttributeString(AttrActivationFunctionId,
                        nGene.ActivationFnId.ToString(NumberFormatInfo.InvariantInfo));

                    // Write aux state as comma separated list of real values.
                    XmlIoUtils.WriteAttributeString(xw, AttrAuxState, nGene.AuxState);
                }

                xw.WriteEndElement();
            }

            xw.WriteEndElement();

            // Emit connections.
            xw.WriteStartElement(ElemConnections);
            foreach (ConnectionGene cGene in genome.ConnectionList)
            {
                xw.WriteStartElement(ElemConnection);
                xw.WriteAttributeString(AttrId, cGene.InnovationId.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrSourceId, cGene.SourceNodeId.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrTargetId, cGene.TargetNodeId.ToString(NumberFormatInfo.InvariantInfo));
                xw.WriteAttributeString(AttrWeight, cGene.Weight.ToString("R", NumberFormatInfo.InvariantInfo));
                xw.WriteEndElement();
            }

            xw.WriteEndElement();

            // </Network>
            xw.WriteEndElement();
        }

        #endregion
    }
}