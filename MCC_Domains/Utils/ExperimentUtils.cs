/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using ExperimentEntities.entities;
using MCC_Domains.MazeNavigation.Bootstrappers;
using Redzen.Random;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;

#endregion

namespace MCC_Domains.Utils
{
    /// <summary>
    ///     Static helper methods for experiment initialization.
    /// </summary>
    public static class ExperimentUtils
    {
        /// <summary>
        ///     Create a network activation scheme from the scheme setting in the provided config XML.
        /// </summary>
        /// <returns></returns>
        public static NetworkActivationScheme CreateActivationScheme(XmlElement xmlConfig, string activationElemName)
        {
            // Get root activation element.
            var nodeList = xmlConfig.GetElementsByTagName(activationElemName, "");
            if (nodeList.Count != 1)
            {
                throw new ArgumentException("Missing or invalid activation XML config setting.");
            }

            var xmlActivation = nodeList[0] as XmlElement;
            var schemeStr = XmlUtils.TryGetValueAsString(xmlActivation, "Scheme");
            switch (schemeStr)
            {
                case "Acyclic":
                    return NetworkActivationScheme.CreateAcyclicScheme();
                case "CyclicFixedIters":
                    var iters = XmlUtils.GetValueAsInt(xmlActivation, "Iters");
                    return NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(iters);
                case "CyclicRelax":
                    var deltaThreshold = XmlUtils.GetValueAsDouble(xmlActivation, "Threshold");
                    var maxIters = XmlUtils.GetValueAsInt(xmlActivation, "MaxIters");
                    return NetworkActivationScheme.CreateCyclicRelaxingActivationScheme(deltaThreshold, maxIters);
            }

            throw new ArgumentException($"Invalid or missing ActivationScheme XML config setting [{schemeStr}]");
        }

        /// <summary>
        ///     Create a complexity regulation strategy based on the provided XML config values.
        /// </summary>
        public static IComplexityRegulationStrategy CreateComplexityRegulationStrategy(string strategyTypeStr,
            int? threshold)
        {
            if (!Enum.TryParse(strategyTypeStr, out ComplexityCeilingType ceilingType))
            {
                return new NullComplexityRegulationStrategy();
            }

            if (null == threshold)
            {
                throw new ArgumentNullException(nameof(threshold),
                    $@"threshold must be provided for complexity regulation strategy type [{ceilingType}]");
            }

            return new DefaultComplexityRegulationStrategy(ceilingType, threshold.Value);
        }

        /// <summary>
        ///     Read Parallel Extensions options from config XML.
        /// </summary>
        /// <param name="xmlConfig"></param>
        /// <returns></returns>
        public static ParallelOptions ReadParallelOptions(XmlElement xmlConfig)
        {
            // Read in upper bound on parallelism
            var maxDegreeOfParallelism = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxDegreeOfParallelism");

            // Construct parallel options configuration with upper bound if specified, otherwise use default
            return null != maxDegreeOfParallelism
                ? new ParallelOptions {MaxDegreeOfParallelism = maxDegreeOfParallelism.Value}
                : new ParallelOptions();
        }

        /// <summary>
        ///     Read Radial Basis Function settings from config XML.
        /// </summary>
        public static void ReadRbfAuxArgMutationConfig(XmlElement xmlConfig, out double mutationSigmaCenter,
            out double mutationSigmaRadius)
        {
            // Get root activation element.
            var nodeList = xmlConfig.GetElementsByTagName("RbfAuxArgMutationConfig", "");
            if (nodeList.Count != 1)
            {
                throw new ArgumentException("Missing or invalid RbfAuxArgMutationConfig XML config settings.");
            }

            var xmlRbfConfig = nodeList[0] as XmlElement;
            var center = XmlUtils.TryGetValueAsDouble(xmlRbfConfig, "MutationSigmaCenter");
            var radius = XmlUtils.TryGetValueAsDouble(xmlRbfConfig, "MutationSigmaRadius");
            if (null == center || null == radius)
            {
                throw new ArgumentException("Missing or invalid RbfAuxArgMutationConfig XML config settings.");
            }

            mutationSigmaCenter = center.Value;
            mutationSigmaRadius = radius.Value;
        }

        /// <summary>
        ///     Read NEAT genome parameter settings from the configuration file.
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <returns>An initialized NEAT genome parameters object.</returns>
        public static NeatGenomeParameters ReadNeatGenomeParameters(XmlElement xmlConfig)
        {
            // Create new NEAT genome parameters with default values
            var genomeParameters = new NeatGenomeParameters();

            // Get root of neat genome configuration section
            var nodeList = xmlConfig.GetElementsByTagName("GenomeConfig", "");

            // Note that if there are multiple defined (such as would be the case with an experiment that uses multiple EAs), 
            // the first one is used here, which will accurately correspond to the current algorithm under consideration
            if (nodeList.Count >= 1)
            {
                // Convert to an XML element
                var xmlNeatGenomeConfig = nodeList[0] as XmlElement;

                // Read all of the applicable parameters in
                var initialConnectionProportion = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "InitialConnectionProportion");
                var weightMutationProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "WeightMutationProbability");
                var addConnectionProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "AddConnnectionProbability");
                var addNodeProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig, "AddNodeProbability");
                var deleteConnectionProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "DeleteConnectionProbability");
                var connectionWeightRange = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "ConnectionWeightRange");

                // Set each if it's specified in the configuration (otherwise, accept the default)
                if (initialConnectionProportion != null)
                {
                    genomeParameters.InitialInterconnectionsProportion = initialConnectionProportion ?? default(double);
                }

                if (weightMutationProbability != null)
                {
                    genomeParameters.ConnectionWeightMutationProbability = weightMutationProbability ?? default(double);
                }

                if (addConnectionProbability != null)
                {
                    genomeParameters.AddConnectionMutationProbability = addConnectionProbability ?? default(double);
                }

                if (addNodeProbability != null)
                {
                    genomeParameters.AddNodeMutationProbability = addNodeProbability ?? default(double);
                }

                if (deleteConnectionProbability != null)
                {
                    genomeParameters.DeleteConnectionMutationProbability = deleteConnectionProbability ??
                                                                           default(double);
                }

                if (connectionWeightRange != null)
                {
                    genomeParameters.ConnectionWeightRange = connectionWeightRange ?? default(double);
                }
            }

            return genomeParameters;
        }

        /// <summary>
        ///     Reads NEAT genome parameters from the database.
        /// </summary>
        /// <param name="experimentDictionary">Reference to experiment dictionary table.</param>
        /// <param name="isPrimary">Flag indicating whether this is the primary or an initialization algorithm.</param>
        /// <returns>Initialized NEAT genome parameters.</returns>
        public static NeatGenomeParameters ReadNeatGenomeParameters(ExperimentDictionary experimentDictionary,
            bool isPrimary)
        {
            return (isPrimary
                ? new NeatGenomeParameters
                {
                    InitialInterconnectionsProportion = experimentDictionary.Primary_ConnectionProportion,
                    ConnectionWeightMutationProbability =
                        experimentDictionary.Primary_MutateConnectionWeightsProbability,
                    AddConnectionMutationProbability = experimentDictionary.Primary_MutateAddConnectionProbability,
                    AddNodeMutationProbability = experimentDictionary.Primary_MutateAddNeuronProbability,
                    DeleteConnectionMutationProbability =
                        experimentDictionary.Primary_MutateDeleteConnectionProbability,
                    ConnectionWeightRange = experimentDictionary.Primary_ConnectionWeightRange
                }
                : new NeatGenomeParameters
                {
                    InitialInterconnectionsProportion =
                        experimentDictionary.Initialization_ConnectionProportion ?? default(double),
                    ConnectionWeightMutationProbability =
                        experimentDictionary.Initialization_MutateConnectionWeightsProbability ?? default(double),
                    AddConnectionMutationProbability =
                        experimentDictionary.Initialization_MutateAddConnectionProbability ?? default(double),
                    AddNodeMutationProbability =
                        experimentDictionary.Initialization_MutateAddNeuronProbability ?? default(double),
                    DeleteConnectionMutationProbability =
                        experimentDictionary.Initialization_MutateDeleteConnectionProbability ?? default(double),
                    ConnectionWeightRange = experimentDictionary.Initialization_ConnectionWeightRange ?? default(double)
                });
        }

        /// <summary>
        ///     Reads NEAT evolution algorithm parameters from the configuration file.
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <returns>An initialized NEAT evolution algorithm parameters object.</returns>
        public static EvolutionAlgorithmParameters ReadNeatEvolutionAlgorithmParameters(XmlElement xmlConfig)
        {
            // Create new NEAT EA parameters with default values
            return new EvolutionAlgorithmParameters
            {
                SpecieCount = XmlUtils.TryGetValueAsInt(xmlConfig, "SpecieCount") ?? default(int),
                ElitismProportion = XmlUtils.TryGetValueAsDouble(xmlConfig, "ElitismProportion") ?? default(double),
                SelectionProportion = XmlUtils.TryGetValueAsDouble(xmlConfig, "SelectionProportion") ?? default(double),
                OffspringAsexualProportion =
                    XmlUtils.TryGetValueAsDouble(xmlConfig, "OffspringAsexualProbability") ?? default(double),
                OffspringSexualProportion =
                    XmlUtils.TryGetValueAsDouble(xmlConfig, "OffspringSexualProbability") ?? default(double),
                InterspeciesMatingProportion =
                    XmlUtils.TryGetValueAsDouble(xmlConfig, "InterspeciesMatingProbability") ?? default(double)
            };
        }

        /// <summary>
        ///     Reads NEAT evolution algorithm parameters from the database.
        /// </summary>
        /// <param name="experimentDictionary">Reference to the experiment dictionary table.</param>
        /// <param name="isPrimary">Flag indicating whether this is the primary or an initialization algorithm.</param>
        /// <returns>Initialized NEAT evolution algorithm parameters.</returns>
        public static EvolutionAlgorithmParameters ReadNeatEvolutionAlgorithmParameters(
            ExperimentDictionary experimentDictionary,
            bool isPrimary)
        {
            return (isPrimary
                ? new EvolutionAlgorithmParameters
                {
                    SpecieCount = experimentDictionary.Primary_NumSpecies,
                    InterspeciesMatingProportion = experimentDictionary.Primary_InterspeciesMatingProbability,
                    ElitismProportion = experimentDictionary.Primary_ElitismProportion,
                    SelectionProportion = experimentDictionary.Primary_SelectionProportion,
                    OffspringAsexualProportion = experimentDictionary.Primary_AsexualProbability,
                    OffspringSexualProportion = experimentDictionary.Primary_CrossoverProbability
                }
                : new EvolutionAlgorithmParameters
                {
                    SpecieCount = experimentDictionary.Initialization_NumSpecies ?? default(int),
                    InterspeciesMatingProportion =
                        experimentDictionary.Initialization_InterspeciesMatingProbability ?? default(double),
                    ElitismProportion = experimentDictionary.Initialization_ElitismProportion ?? default(double),
                    SelectionProportion = experimentDictionary.Initialization_SelectionProportion ?? default(double),
                    OffspringAsexualProportion =
                        experimentDictionary.Initialization_AsexualProbability ?? default(double),
                    OffspringSexualProportion =
                        experimentDictionary.Initialization_CrossoverProbability ?? default(double)
                });
        }

        /// <summary>
        ///     Reads novelty parameter settings from the configuration file.
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="archiveAdditionThreshold">The specified archive addition threshold.</param>
        /// <param name="archiveThresholdDecreaseMultiplier">The specified archive threshold decrease multiplier.</param>
        /// <param name="archiveThresholdIncreaseMultiplier">The specified archive threshold increase multiplier.</param>
        /// <param name="maxGenerationalArchiveAddition">
        ///     The specified maximum number of genomes added to the archive within a
        ///     generation.
        /// </param>
        /// <param name="maxGenerationsWithoutArchiveAddition">
        ///     The specified maximum number of generations without an archive
        ///     addition.
        /// </param>
        public static void ReadNoveltyParameters(XmlElement xmlConfig,
            out double archiveAdditionThreshold,
            out double archiveThresholdDecreaseMultiplier, out double archiveThresholdIncreaseMultiplier,
            out int maxGenerationalArchiveAddition, out int maxGenerationsWithoutArchiveAddition)
        {
            // Get root of novelty configuration section
            var nodeList = xmlConfig.GetElementsByTagName("NoveltyConfig", "");

            Debug.Assert(nodeList.Count == 1);

            // Convert to an XML element
            var xmlNoveltyConfig = nodeList[0] as XmlElement;

            archiveAdditionThreshold = XmlUtils.GetValueAsDouble(xmlNoveltyConfig, "ArchiveAdditionThreshold");
            archiveThresholdDecreaseMultiplier = XmlUtils.GetValueAsDouble(xmlNoveltyConfig,
                "ArchiveThresholdDecreaseMultiplier");
            archiveThresholdIncreaseMultiplier = XmlUtils.GetValueAsDouble(xmlNoveltyConfig,
                "ArchiveThresholdIncreaseMultiplier");
            maxGenerationalArchiveAddition = XmlUtils.GetValueAsInt(xmlNoveltyConfig,
                "MaxGenerationalArchiveAddition");
            maxGenerationsWithoutArchiveAddition = XmlUtils.GetValueAsInt(xmlNoveltyConfig,
                "MaxGenerationsWithoutArchiveAddition");
        }

        /// <summary>
        ///     Reads the experiment logging configuration.
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="loggingType">The type of logging (evolution, evaluation, etc.).</param>
        /// <param name="parentElementName">The name of the top-level logging configuration element.</param>
        /// <returns>A constructed data logger.</returns>
        public static IDataLogger ReadDataLogger(XmlElement xmlConfig, LoggingType loggingType,
            string parentElementName = "LoggingConfig")
        {
            XmlElement xmlLoggingConfig = null;

            // Get root of novelty configuration section
            var nodeList = xmlConfig.GetElementsByTagName(parentElementName, "");

            // Iterate through the list of logging configurations, finding one that matches the specified logging type
            foreach (XmlElement curXmlLoggingConfig in nodeList)
            {
                if (loggingType ==
                    LoggingParameterUtils.ConvertStringToLoggingType(XmlUtils.TryGetValueAsString(curXmlLoggingConfig,
                        "Type")))
                {
                    xmlLoggingConfig = curXmlLoggingConfig;
                    break;
                }
            }

            // If no appropriate logger was found, just return null (meaning there won't be any logging for this type)
            if (xmlLoggingConfig == null) return null;

            // Read in the log file name
            var logFileName = XmlUtils.TryGetValueAsString(xmlLoggingConfig, "LogFile");

            // Instantiate the file data logger
            IDataLogger dataLogger = new FileDataLogger(logFileName);

            return dataLogger;
        }

        /// <summary>
        ///     Reads behavior characterization parameters from the configuration file.
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="behaviorConfigTagName"></param>
        /// <returns>The behavior characterization parameters.</returns>
        public static IBehaviorCharacterizationFactory ReadBehaviorCharacterizationFactory(XmlElement xmlConfig,
            string behaviorConfigTagName)
        {
            // Get root of behavior configuration section
            var behaviorNodeList = xmlConfig.GetElementsByTagName(behaviorConfigTagName, "");

            // Ensure that the behavior node list was found
            if (behaviorNodeList.Count != 1)
            {
                throw new ArgumentException("Missing or invalid BehaviorConfig XML config settings.");
            }

            var xmlBehaviorConfig = behaviorNodeList[0] as XmlElement;
            
            // Parse and generate the appropriate behavior characterization factory
            var behaviorCharacterizationFactory =
                BehaviorCharacterizationUtil.GenerateBehaviorCharacterizationFactory(
                    XmlUtils.TryGetValueAsString(xmlBehaviorConfig, "BehaviorCharacterization"));

            return behaviorCharacterizationFactory;
        }

        /// <summary>
        ///     Reads behavior characterization parameters from the database.
        /// </summary>
        /// <param name="experiment">The experiment dictionary entity.</param>
        /// <param name="isPrimary">
        ///     Boolean flag indicating whether this is the primary behavior characterization or the behavior
        ///     characterization used for experiment initialization.
        /// </param>
        /// <returns></returns>
        public static IBehaviorCharacterizationFactory ReadBehaviorCharacterizationFactory(
            ExperimentDictionary experiment,
            bool isPrimary)
        {
            // Read behavior characterization
            var behaviorCharacterizationName = isPrimary
                ? experiment.Primary_BehaviorCharacterizationName
                : experiment.Initialization_BehaviorCharacterizationName;

            // Ensure that the behavior was specified
            if (behaviorCharacterizationName == null)
            {
                throw new ArgumentException("Missing or invalid BehaviorConfig settings.");
            }

            // Parse and generate the appropriate behavior characterization factory
            var behaviorCharacterizationFactory =
                BehaviorCharacterizationUtil.GenerateBehaviorCharacterizationFactory(behaviorCharacterizationName);

            return behaviorCharacterizationFactory;
        }

        /// <summary>
        ///     Get all of the XML genome files in the given directory.
        /// </summary>
        /// <param name="filePath">The path to the directory containing the genome files or the genome file path itself.</param>
        /// <returns>An array of genome file paths.</returns>
        public static IEnumerable<string> GetGenomeFiles(string filePath)
        {
            string[] genomeFiles;

            // Get the attributes of the given path/file
            var fileAttributes = File.GetAttributes(filePath);

            // Determine whether this is a directory or a file
            if ((fileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                // Get all of the genome files in the directory
                genomeFiles = Directory.GetFiles(filePath, "*.xml");
            }
            else
            {
                // There's only one file, so make array length 1 and add that file
                genomeFiles = new string[1];
                genomeFiles[0] = filePath;
            }

            return genomeFiles;
        }

        /// <summary>
        ///     Reads in seed NEAT genomes used to bootstrap MCC experiments.
        /// </summary>
        /// <param name="seedNeatPath">
        ///     The path of the single NEAT genome or a directory containing multiple XML genome definitions.
        /// </param>
        /// <param name="neatGenomeFactory">The NEAT genome factory to assign to each genome.</param>
        /// <returns>The list of seed NEAT genomes.</returns>
        public static List<NeatGenome> ReadSeedNeatGenomes(string seedNeatPath, NeatGenomeFactory neatGenomeFactory)
        {
            var neatGenomes = new List<NeatGenome>();

            // Get the NEAT genome files in the given path
            var neatGenomeFiles = GetGenomeFiles(seedNeatPath);

            // Read in all NEAT genomes and add them to the list
            foreach (var neatGenomeFile in neatGenomeFiles)
            {
                using (var xr = XmlReader.Create(neatGenomeFile))
                {
                    // Read in the NEAT genomes
                    var curNeatGenomes = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, neatGenomeFactory);

                    // Add the genomes to the overall genome list
                    neatGenomes.AddRange(curNeatGenomes);
                }
            }

            return neatGenomes;
        }
    }
}