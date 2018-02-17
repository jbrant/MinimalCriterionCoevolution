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
using ExperimentEntities;
using MCC_Domains.MazeNavigation.Bootstrappers;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.MinimalCriterias;

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

            // Note that if there are multiple defined (such as would be the case with an experiment that uses multiple EAs), 
            // the first one is used here, which will accurately correspond to the current algorithm under consideration
            if (nodeList.Count >= 1)
            {
                // Convert to an XML element
                var xmlNeatGenomeConfig = nodeList[0] as XmlElement;

                // Read all of the applicable parameters in
                double? initialConnectionProportion = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "InitialConnectionProportion");
                double? weightMutationProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "WeightMutationProbability");
                double? addConnectionProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "AddConnnectionProbability");
                double? addNodeProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig, "AddNodeProbability");
                double? deleteConnectionProbability = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
                    "DeleteConnectionProbability");
                double? connectionWeightRange = XmlUtils.TryGetValueAsDouble(xmlNeatGenomeConfig,
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
        ///     Read maze genome parameter settings from the configuration file.
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <returns>An initialized maze genome parameters object.</returns>
        public static MazeGenomeParameters ReadMazeGenomeParameters(XmlElement xmlConfig)
        {
            // Create new NEAT genome parameters with default values
            MazeGenomeParameters genomeParameters = new MazeGenomeParameters();

            // Get root of neat genome configuration section
            XmlNodeList nodeList = xmlConfig.GetElementsByTagName("MazeGenomeConfig", "");

            // Note that if there are multiple defined (such as would be the case with an experiment that uses multiple EAs), 
            // the first one is used here, which will accurately correspond to the current algorithm under consideration
            if (nodeList.Count >= 1)
            {
                // Convert to an XML element
                XmlElement xmlMazeGenomeConfig = nodeList[0] as XmlElement;

                // Read all of the applicable parameters in
                double? wallStartMutationProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateWallStartLocationProbability");
                double? passageStartMutationProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutatePassageStartLocationProbability");
                double? addWallProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateAddWallProbability");
                double? deleteWallProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateDeleteWallProbability");
                double? pathWaypointLocationMutationProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutatePathWaypointLocationProbability");
                double? addPathWaypointMutationProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateAddPathWaypointProbability");
                double? expandMazeProbability = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "MutateExpandMazeProbability");
                double? perturbanceMagnitude = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "PerturbanceMagnitude");
                double? verticalWallBias = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig, "VerticalWallBias");
                double? gridCellNeighborhoodRadius = XmlUtils.TryGetValueAsInt(xmlMazeGenomeConfig,
                    "GridCellNeighborhoodRadius");
                double? sparseCellSelectionProportion = XmlUtils.TryGetValueAsDouble(xmlMazeGenomeConfig,
                    "SparseCellSelectionProportion");

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
                if (gridCellNeighborhoodRadius != null)
                {
                    genomeParameters.GridCellNeighborhoodRadius = (int) gridCellNeighborhoodRadius;
                }
                if (sparseCellSelectionProportion != null)
                {
                    genomeParameters.SparseCellSelectionProportion = (double) sparseCellSelectionProportion;
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
                    DeleteConnectionMutationProbability = experimentDictionary.Primary_MutateDeleteConnectionProbability,
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
        public static NeatEvolutionAlgorithmParameters ReadNeatEvolutionAlgorithmParameters(XmlElement xmlConfig)
        {
            // Create new NEAT EA parameters with default values
            return new NeatEvolutionAlgorithmParameters
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
        public static NeatEvolutionAlgorithmParameters ReadNeatEvolutionAlgorithmParameters(
            ExperimentDictionary experimentDictionary,
            bool isPrimary)
        {
            return (isPrimary
                ? new NeatEvolutionAlgorithmParameters
                {
                    SpecieCount = experimentDictionary.Primary_NumSpecies,
                    InterspeciesMatingProportion = experimentDictionary.Primary_InterspeciesMatingProbability,
                    ElitismProportion = experimentDictionary.Primary_ElitismProportion,
                    SelectionProportion = experimentDictionary.Primary_SelectionProportion,
                    OffspringAsexualProportion = experimentDictionary.Primary_AsexualProbability,
                    OffspringSexualProportion = experimentDictionary.Primary_CrossoverProbability
                }
                : new NeatEvolutionAlgorithmParameters
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
        ///     Reads the experiment logging configuration with the default top-level configuration element "LoggingConfig".
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="loggingType">The type of logging (evolution, evaluation, etc.).</param>
        /// <returns>A constructed data logger.</returns>
        public static IDataLogger ReadDataLogger(XmlElement xmlConfig, LoggingType loggingType)
        {
            return ReadDataLogger(xmlConfig, loggingType, "LoggingConfig");
        }

        /// <summary>
        ///     Reads the experiment logging configuration.
        /// </summary>
        /// <param name="xmlConfig">The reference to the XML configuration file.</param>
        /// <param name="loggingType">The type of logging (evolution, evaluation, etc.).</param>
        /// <param name="parentElementName">The name of the top-level logging configuration element.</param>
        /// <returns>A constructed data logger.</returns>
        public static IDataLogger ReadDataLogger(XmlElement xmlConfig, LoggingType loggingType, string parentElementName)
        {
            IDataLogger dataLogger = null;
            XmlElement xmlLoggingConfig = null;
            int cnt = 0;

            // Get root of novelty configuration section
            XmlNodeList nodeList = xmlConfig.GetElementsByTagName(parentElementName, "");

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

            // Get the logging destination
            LoggingDestination loggingDestination =
                LoggingParameterUtils.ConvertStringToLoggingDestination(
                    XmlUtils.TryGetValueAsString(xmlLoggingConfig,
                        "Destination"));

            // Configure a file-based logger
            if (LoggingDestination.File == loggingDestination)
            {
                // Read in the log file name
                string logFileName = XmlUtils.TryGetValueAsString(xmlLoggingConfig, "LogFile");

                // Instantiate the file data logger
                dataLogger = new FileDataLogger(logFileName);
            }
            else if (LoggingDestination.Database == loggingDestination)
            {
                // Read in the experiment configuration and run number
                string experimentConfigurationName = XmlUtils.TryGetValueAsString(xmlLoggingConfig,
                    "ExperimentConfigurationName");

                if (LoggingType.Evolution == loggingType)
                {
                    // Instantiate the evolution database data logger
                    dataLogger = new NoveltyExperimentEvaluationEntityDataLogger(experimentConfigurationName);
                }
                else if (LoggingType.Evaluation == loggingType)
                {
                    // Instantiate the evaluation database data logger
                    dataLogger = new NoveltyExperimentOrganismStateEntityDataLogger(experimentConfigurationName);
                }
            }

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
            XmlNodeList behaviorNodeList = xmlConfig.GetElementsByTagName(behaviorConfigTagName, "");

            // Ensure that the behavior node list was found
            if (behaviorNodeList.Count != 1)
            {
                throw new ArgumentException("Missing or invalid BehaviorConfig XML config settings.");
            }

            XmlElement xmlBehaviorConfig = behaviorNodeList[0] as XmlElement;
            IMinimalCriteria minimalCriteria = null;

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

                // Starting location used in most criterias
                double xStart, yStart;

                switch (mcType)
                {
                    case MinimalCriteriaType.EuclideanLocation:

                        // Read in the min/max location bounds
                        double xMin = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "XMin");
                        double xMax = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "XMax");
                        double yMin = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "YMin");
                        double yMax = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "YMax");

                        // Set the euclidean location minimal criteria on the behavior characterization
                        minimalCriteria = new EuclideanLocationCriteria(xMin, xMax, yMin, yMax);

                        break;

                    case MinimalCriteriaType.FixedPointEuclideanDistance:

                        // Read in the starting coordinates and the minimum required distance traveled
                        xStart = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "XStart");
                        yStart = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "YStart");
                        double minimumDistanceTraveled = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig,
                            "MinimumRequiredDistance");
                        double? maxDistanceUpdateCyclesWithoutChange =
                            XmlUtils.TryGetValueAsInt(xmlMinimalCriteriaConfig,
                                "MaxUpdateCyclesWithoutChange");

                        // Set the fixed point euclidean distance minimal criteria on the behavior characterization
                        minimalCriteria = new FixedPointEuclideanDistanceCriteria(xStart, yStart,
                            minimumDistanceTraveled, maxDistanceUpdateCyclesWithoutChange);

                        break;

                    case MinimalCriteriaType.PopulationCentroidEuclideanDistance:

                        // Read in the starting minimum required distance (if applicable)
                        double minimuCentroidDistance = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig,
                            "MinimumRequiredDistance");

                        // Set the population centroid euclidean distance criteria on the behavior characterization
                        minimalCriteria = new PopulationCentroidEuclideanDistanceCriteria(minimuCentroidDistance);

                        break;

                    case MinimalCriteriaType.Mileage:

                        // Read in the starting coordinates and minimum required total distance traveled (mileage)
                        xStart = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "XStart");
                        yStart = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "YStart");
                        double minimumMileage = XmlUtils.GetValueAsDouble(xmlMinimalCriteriaConfig, "MinimumMileage");
                        double? maxMileageUpdateCyclesWithoutChange = XmlUtils.TryGetValueAsInt(
                            xmlMinimalCriteriaConfig,
                            "MaxUpdateCyclesWithoutChange");

                        // Set the mileage minimal criteria on the behavior characterization
                        minimalCriteria = new MileageCriteria(xStart, yStart, minimumMileage,
                            maxMileageUpdateCyclesWithoutChange);

                        break;
                }
            }

            // Parse and generate the appropriate behavior characterization factory
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory =
                BehaviorCharacterizationUtil.GenerateBehaviorCharacterizationFactory(
                    XmlUtils.TryGetValueAsString(xmlBehaviorConfig, "BehaviorCharacterization"), minimalCriteria);

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
            String behaviorCharacterizationName = isPrimary
                ? experiment.Primary_BehaviorCharacterizationName
                : experiment.Initialization_BehaviorCharacterizationName;

            // Ensure that the behavior was specified
            if (behaviorCharacterizationName == null)
            {
                throw new ArgumentException("Missing or invalid BehaviorConfig settings.");
            }

            IMinimalCriteria minimalCriteria = null;

            // Get the appropriate minimal criteria type
            MinimalCriteriaType mcType = BehaviorCharacterizationUtil.ConvertStringToMinimalCriteria(isPrimary
                ? experiment.Primary_MCS_MinimalCriteriaName
                : experiment.Initialization_MCS_MinimalCriteriaName);

            // Starting location used in most criterias
            double xStart, yStart;

            switch (mcType)
            {
                case MinimalCriteriaType.EuclideanLocation:

                    // TODO: Not implemented at the database layer yet

                    break;

                case MinimalCriteriaType.FixedPointEuclideanDistance:

                    // Read in the starting coordinates and the minimum required distance traveled
                    xStart = isPrimary
                        ? experiment.Primary_MCS_MinimalCriteriaStartX ?? default(double)
                        : experiment.Initialization_MCS_MinimalCriteriaStartX ?? default(double);
                    yStart = isPrimary
                        ? experiment.Primary_MCS_MinimalCriteriaStartY ?? default(double)
                        : experiment.Initialization_MCS_MinimalCriteriaStartY ?? default(double);
                    double minimumDistanceTraveled = isPrimary
                        ? experiment.Primary_MCS_MinimalCriteriaThreshold ?? default(double)
                        : experiment.Initialization_MCS_MinimalCriteriaThreshold ?? default(double);

                    // Set the euclidean distance minimal criteria on the behavior characterization
                    minimalCriteria = new FixedPointEuclideanDistanceCriteria(xStart, yStart,
                        minimumDistanceTraveled);

                    break;

                case MinimalCriteriaType.Mileage:

                    // Read in the starting coordinates and minimum required total distance traveled (mileage)
                    xStart = isPrimary
                        ? experiment.Primary_MCS_MinimalCriteriaStartX ?? default(double)
                        : experiment.Initialization_MCS_MinimalCriteriaStartX ?? default(double);
                    yStart = isPrimary
                        ? experiment.Primary_MCS_MinimalCriteriaStartY ?? default(double)
                        : experiment.Initialization_MCS_MinimalCriteriaStartY ?? default(double);
                    double minimumMileage = isPrimary
                        ? experiment.Primary_MCS_MinimalCriteriaThreshold ?? default(double)
                        : experiment.Initialization_MCS_MinimalCriteriaThreshold ?? default(double);

                    // Set the mileage minimal criteria on the behavior characterization
                    minimalCriteria = new MileageCriteria(xStart, yStart, minimumMileage);

                    break;
            }

            // Parse and generate the appropriate behavior characterization factory
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory =
                BehaviorCharacterizationUtil.GenerateBehaviorCharacterizationFactory(behaviorCharacterizationName,
                    minimalCriteria);

            return behaviorCharacterizationFactory;
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
        public static List<MazeGenome> ReadSeedMazeGenomes(string seedMazePath, MazeGenomeFactory mazeGenomeFactory)
        {
            string[] mazeGenomeFiles;
            List<MazeGenome> mazeGenomes = new List<MazeGenome>();

            // Get the attributes of the given path/file
            FileAttributes fileAttributes = File.GetAttributes(seedMazePath);

            // Determine whether this is a directory or a file
            if ((fileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                // Get all of the genome files in the directory
                mazeGenomeFiles = Directory.GetFiles(seedMazePath, "*.xml");
            }
            else
            {
                // There's only one file, so make array length 1 and add that file
                mazeGenomeFiles = new string[1];
                mazeGenomeFiles[0] = seedMazePath;
            }

            // Read in all maze genomes and add them to the list
            foreach (string mazeGenomeFile in mazeGenomeFiles)
            {
                using (XmlReader xr = XmlReader.Create(mazeGenomeFile))
                {
                    // Read in the maze genomes
                    List<MazeGenome> curMazeGenomes = MazeGenomeXmlIO.ReadCompleteGenomeList(xr, mazeGenomeFactory);

                    // Assign the genome factory
                    foreach (MazeGenome curMazeGenome in curMazeGenomes)
                    {
                        curMazeGenome.GenomeFactory = mazeGenomeFactory;
                    }

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
            List<MazeGenome> mazeGenomes = new List<MazeGenome>(numMazeGenomes);
            Random rand = new Random();

            for (int curMazeCnt = 0; curMazeCnt < numMazeGenomes; curMazeCnt++)
            {
                // Reset innovation IDs
                mazeGenomeFactory.InnovationIdGenerator.Reset();

                // Create a new genome and pass in the requisite factory
                MazeGenome mazeGenome = new MazeGenome(mazeGenomeFactory, 0, 0);

                // Create the specified number of interior partitions (i.e. maze genes)
                for (int cnt = 0; cnt < numPartitions; cnt++)
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
        public static MCCMazeNavigationInitializer DetermineMCCInitializer(XmlElement xmlConfig)
        {
            // Make sure that the XML configuration exists
            if (xmlConfig == null)
            {
                throw new ArgumentException("Missing or invalid MCC initialization configuration.");
            }

            // Extract the corresponding search and selection algorithm domain types
            SearchType searchType =
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