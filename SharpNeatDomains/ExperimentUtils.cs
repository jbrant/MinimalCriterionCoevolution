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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.MinimalCriterias;

#endregion

namespace SharpNeat.Domains
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
            XmlNodeList nodeList = xmlConfig.GetElementsByTagName(activationElemName, "");
            if (nodeList.Count != 1)
            {
                throw new ArgumentException("Missing or invalid activation XML config setting.");
            }

            XmlElement xmlActivation = nodeList[0] as XmlElement;
            string schemeStr = XmlUtils.TryGetValueAsString(xmlActivation, "Scheme");
            switch (schemeStr)
            {
                case "Acyclic":
                    return NetworkActivationScheme.CreateAcyclicScheme();
                case "CyclicFixedIters":
                    int iters = XmlUtils.GetValueAsInt(xmlActivation, "Iters");
                    return NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(iters);
                case "CyclicRelax":
                    double deltaThreshold = XmlUtils.GetValueAsDouble(xmlActivation, "Threshold");
                    int maxIters = XmlUtils.GetValueAsInt(xmlActivation, "MaxIters");
                    return NetworkActivationScheme.CreateCyclicRelaxingActivationScheme(deltaThreshold, maxIters);
            }
            throw new ArgumentException(string.Format("Invalid or missing ActivationScheme XML config setting [{0}]",
                schemeStr));
        }

        /// <summary>
        ///     Create a complexity regulation strategy based on the provided XML config values.
        /// </summary>
        public static IComplexityRegulationStrategy CreateComplexityRegulationStrategy(string strategyTypeStr,
            int? threshold)
        {
            ComplexityCeilingType ceilingType;
            if (!Enum.TryParse(strategyTypeStr, out ceilingType))
            {
                return new NullComplexityRegulationStrategy();
            }

            if (null == threshold)
            {
                throw new ArgumentNullException("threshold",
                    string.Format("threshold must be provided for complexity regulation strategy type [{0}]",
                        ceilingType));
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
            // Get parallel options.
            ParallelOptions parallelOptions;
            int? maxDegreeOfParallelism = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxDegreeOfParallelism");
            if (null != maxDegreeOfParallelism)
            {
                parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = maxDegreeOfParallelism.Value};
            }
            else
            {
                parallelOptions = new ParallelOptions();
            }
            return parallelOptions;
        }

        /// <summary>
        ///     Read Radial Basis Function settings from config XML.
        /// </summary>
        public static void ReadRbfAuxArgMutationConfig(XmlElement xmlConfig, out double mutationSigmaCenter,
            out double mutationSigmaRadius)
        {
            // Get root activation element.
            XmlNodeList nodeList = xmlConfig.GetElementsByTagName("RbfAuxArgMutationConfig", "");
            if (nodeList.Count != 1)
            {
                throw new ArgumentException("Missing or invalid RbfAuxArgMutationConfig XML config settings.");
            }

            XmlElement xmlRbfConfig = nodeList[0] as XmlElement;
            double? center = XmlUtils.TryGetValueAsDouble(xmlRbfConfig, "MutationSigmaCenter");
            double? radius = XmlUtils.TryGetValueAsDouble(xmlRbfConfig, "MutationSigmaRadius");
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

            if (nodeList.Count == 1)
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
                var interspeciesMatingProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "InterspeciesMatingProbability");

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
            }

            return genomeParameters;
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

        public static IBehaviorCharacterization ReadBehaviorCharacterization(XmlElement xmlConfig)
        {
            // Get root of behavior configuration section
            XmlNodeList behaviorNodeList = xmlConfig.GetElementsByTagName("BehaviorConfig", "");

            // Ensure that the behavior node list was found
            if (behaviorNodeList.Count != 1)
            {
                throw new ArgumentException("Missing or invalid BehaviorConfig XML config settings.");
            }

            XmlElement xmlBehaviorConfig = behaviorNodeList[0] as XmlElement;

            // Parse and generate the appropriate behavior characterization
            IBehaviorCharacterization behaviorCharacterization = BehaviorCharacterizationUtil
                .GenerateBehaviorCharacterization(
                    XmlUtils.TryGetValueAsString(xmlBehaviorConfig, "BehaviorCharacterization"));

            // Try to get the child minimal criteria configuration
            XmlNodeList minimalCriteriaNodeList = xmlBehaviorConfig.GetElementsByTagName("MinimalCriteriaConfig", "");

            // If a minimal criteria is specified, read in its configuration and add it to the behavior characterization
            if (minimalCriteriaNodeList.Count == 1)
            {
                XmlElement xmlMinimalCriteriaConfig = minimalCriteriaNodeList[0] as XmlElement;

                // Extract the minimal criteria constraint name
                string minimalCriteriaConstraint = XmlUtils.TryGetValueAsString(xmlMinimalCriteriaConfig,
                    "MinimalCriteriaConstraint");

                // Get the appropriate minimal criteria type
                MinimalCriteriaType mcType =
                    BehaviorCharacterizationUtil.ConvertStringToMinimalCriteria(minimalCriteriaConstraint);

                // TODO: Need to have a switch statement here when more MC types are added
                if (MinimalCriteriaType.EuclideanLocation.Equals(mcType))
                {
                    // Read in the min/max location bounds
                    double xMin = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "XMin");
                    double xMax = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "XMax");
                    double yMin = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "YMin");
                    double yMax = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "YMax");

                    // Set the minimal criteria on the behavior characterization
                    behaviorCharacterization.MinimalCriteria = new EuclideanLocationCriteria(xMin, xMax, yMin, yMax);
                }
            }

            return behaviorCharacterization;
        }
    }
}