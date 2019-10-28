using System;
using System.Diagnostics;
using System.Xml;
using MCC_Domains.BodyBrain.Bootstrappers;
using MCC_Domains.BodyBrain.MCCExperiment;
using MCC_Domains.Utils;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain
{
    public static class BodyBrainExperimentUtils
    {
        /// <summary>
        ///     Determines which MCC body/brain initializer to instantiate and return based on the initialization algorithm search
        ///     type.
        /// </summary>
        /// <param name="xmlConfig">XML initialization configuration.</param>
        /// <returns>The instantiated initializer.</returns>
        public static BodyBrainInitializer DetermineMCCBodyBrainInitializer(XmlElement xmlConfig)
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
                // TODO: Implement fitness initializer
                case SearchType.Fitness:
                    return null;
                default:
                    return new BodyBrainNoveltySearchInitializer();
            }
        }

        /// <summary>
        ///     Reads voxelyze-specific simulation parameters from the given XML configuration file, and instantiates a simulation
        ///     properties class with the XML-defined parameters as well as more dynamic, invocation-parameters that were passed
        ///     from the command line.
        /// </summary>
        /// <param name="xmlElem">The top-level XML element containing the simulation configuration parameters.</param>
        /// <param name="simConfigDirectory">
        ///     The directory into which to persist the generated simulation configuration that is fed
        ///     to voxelyze.
        /// </param>
        /// <param name="simResultsDirectory">The directory into which simulation results should be written.</param>
        /// <param name="simExecutableFile">The path to the voxelyze simulation executable.</param>
        /// <returns>A simulation properties object containing all of the extracted simulation parameters.</returns>
        public static SimulationProperties ReadSimulationProperties(XmlElement xmlElem, string simConfigDirectory,
            string simResultsDirectory, string simExecutableFile)
        {
            // Get root of the voxelyze configuration section
            var nodeList = xmlElem.GetElementsByTagName("VoxelyzeConfig", "");

            // Convert to an XML element
            var xmlSimProps = nodeList[0] as XmlElement;

            // Read all of the applicable parameters in and create the simulation properties object
            return new SimulationProperties(simConfigDirectory, simResultsDirectory,
                XmlUtils.GetValueAsString(xmlSimProps, "VxaTemplateFile"), simExecutableFile,
                XmlUtils.GetValueAsDouble(xmlSimProps, "MinPercentMaterial"),
                XmlUtils.GetValueAsDouble(xmlSimProps, "MinPercentActiveMaterial"),
                XmlUtils.GetValueAsInt(xmlSimProps, "InitialXDimension"),
                XmlUtils.GetValueAsInt(xmlSimProps, "InitialYDimension"),
                XmlUtils.GetValueAsInt(xmlSimProps, "InitialZDimension"),
                XmlUtils.GetValueAsInt(xmlSimProps, "BrainNetworkConnections"),
                XmlUtils.GetValueAsDouble(xmlSimProps, "SimulatedSeconds"),
                XmlUtils.GetValueAsDouble(xmlSimProps, "InitializationSeconds"),
                XmlUtils.GetValueAsInt(xmlSimProps, "ActuationsPerSecond"),
                XmlUtils.GetValueAsDouble(xmlSimProps, "FloorSlope"),
                XmlUtils.GetValueAsString(xmlSimProps, "VxaSimOutputXPath"),
                XmlUtils.GetValueAsString(xmlSimProps, "VxaSimStopConditionXPath"),
                XmlUtils.GetValueAsString(xmlSimProps, "VxaEnvThermalXPath"),
                XmlUtils.GetValueAsString(xmlSimProps, "VxaEnvGravityXPath"),
                XmlUtils.GetValueAsString(xmlSimProps, "VxaStructureXPath"));
        }

        /// <summary>
        ///     Reads simulation results from the given simulation results file.
        /// </summary>
        /// <param name="resultsFilePath">The path to the simulation results file.</param>
        /// <returns>A simulation results object containing extracted simulation results.</returns>
        public static SimulationResults ReadSimulationResults(string resultsFilePath)
        {
            // Load the results file
            var resultsDoc = new XmlDocument();
            resultsDoc.Load(resultsFilePath);

            // Attempt to get the root of the fitness section
            var nodeList = resultsDoc.GetElementsByTagName("Fitness", "");

            if (nodeList.Count <= 0)
            {
                throw new SharpNeatException($"Failed to read simulation results file: [{resultsFilePath}]");
            }

            // Convert to an XML element
            var fitnessXml = nodeList[0] as XmlElement;

            // Extract distance and location
            var xPos = XmlUtils.GetValueAsDouble(fitnessXml, "xPos");
            var yPos = XmlUtils.GetValueAsDouble(fitnessXml, "yPos");
            var distance = XmlUtils.GetValueAsDouble(fitnessXml, "Distance");
            var simTime = XmlUtils.GetValueAsDouble(fitnessXml, "simTime");

            // Construct and return simulation results object
            return new SimulationResults(xPos, yPos, distance, simTime);
        }

        /// <summary>
        ///     Writes the simulation configuration file that dynamically configures the voxelyze simulator.
        /// </summary>
        /// <param name="vxaTemplatePath">
        ///     The path to the simulation configuration template file, containing simulation parameter
        ///     defaults.
        /// </param>
        /// <param name="outputPath">The directory into which the generated Voxelyze simulation configuration file is written.</param>
        /// <param name="vxaSimGaXPath">The XPath location containing GA simulation parameters.</param>
        /// <param name="vxaStructureXPath">The XPath location containing voxel structure configuration properties.</param>
        /// <param name="simResultsFilePath">The directory into which to write simulation results.</param>
        /// <param name="brain">The voxel brain object containing per-voxel network weights.</param>
        /// <param name="body">The voxel body object containing voxel material specifications.</param>
        public static void WriteVoxelyzeSimulationFile(string vxaTemplatePath, string outputPath,
            string vxaSimGaXPath, string vxaStructureXPath, string simResultsFilePath, VoxelBrain brain, VoxelBody body)
        {
            // Instantiate XML reader for VXA template file
            var simDoc = new XmlDocument();
            simDoc.Load(vxaTemplatePath);

            // Set the results output file name and path
            simDoc.SelectSingleNode(string.Join("/", vxaSimGaXPath, "FitnessFileName")).InnerText = simResultsFilePath;

            // Get reference to structure definition section
            var structureElem = simDoc.SelectSingleNode(vxaStructureXPath);

            // Set voxel structure dimensions
            structureElem.SelectSingleNode("X_Voxels").InnerText = body.Xlength.ToString();
            structureElem.SelectSingleNode("Y_Voxels").InnerText = body.Ylength.ToString();
            structureElem.SelectSingleNode("Z_Voxels").InnerText = body.Zlength.ToString();

            // Set number of brain connections
            structureElem.SelectSingleNode("numSynapses").InnerText = brain.NumConnections.ToString();

            // Set layer-wise material and connection weights
            for (var layerIdx = 0; layerIdx < body.Zlength; layerIdx++)
            {
                // Create a new layer XML element for body materials and connections
                var bodyLayerElem = simDoc.CreateElement("Layer");
                var connLayerElem = simDoc.CreateElement("Layer");

                // Wrap layer material codes and connection weights in a CDATA and add to each layer XML
                bodyLayerElem.AppendChild(simDoc.CreateCDataSection(body.GetLayerMaterialCodes(layerIdx)));
                connLayerElem.AppendChild(simDoc.CreateCDataSection(brain.GetLayerSynapseWeights(layerIdx)));

                // Append layers to XML document
                structureElem.SelectSingleNode("Data").AppendChild(bodyLayerElem);
                structureElem.SelectSingleNode("SynapseWeights").AppendChild(connLayerElem);
            }

            simDoc.Save(outputPath);
        }

        /// <summary>
        ///     Builds the file path for a particular Voxelyze configuration or output file.
        /// </summary>
        /// <param name="fileType">The type of file being written (e.g. configuration, results).</param>
        /// <param name="outputDirectory">The directory into which the file will be written or is located.</param>
        /// <param name="experimentName">The name of the experiment to which the file corresponds.</param>
        /// <param name="run">The run number of the experiment to which the file corresponds.</param>
        /// <param name="bodyGenomeId">The unique ID of the body genome being simulated.</param>
        /// <param name="brainGenomeId">The unique ID of the brain genome being simulated.</param>
        /// <returns></returns>
        public static string ConstructVoxelyzeFilePath(string fileType, string outputDirectory, string experimentName,
            int run, uint bodyGenomeId, uint brainGenomeId)
        {
            return string.Join("/", outputDirectory,
                $"voxelyze_sim_{fileType}_exp_{experimentName.Replace(" ", "_")}_run_{run}_body_{bodyGenomeId}_brain_{brainGenomeId}.xml");
        }

        /// <summary>
        ///     Configures the command for executing a voxelyze simulation.
        /// </summary>
        /// <param name="simExecutableFile">The path to the Voxelyze executable.</param>
        /// <param name="simConfigFilePath">The path to the simulation configuration file.</param>
        /// <returns>A ProcessStartInfo configured to execute a Voxelyze simulation.</returns>
        public static ProcessStartInfo ConfigureSimulationExecution(string simExecutableFile, string simConfigFilePath)
        {
            return new ProcessStartInfo
            {
                FileName = simExecutableFile,
                Arguments = $"-f \"{simConfigFilePath}\"",
                CreateNoWindow = true
            };
        }
    }
}