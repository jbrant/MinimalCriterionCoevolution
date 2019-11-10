using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using ExperimentEntities;
using ExperimentEntities.entities;
using MCC_Domains.BodyBrain;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Voxel;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Voxels;
using RunPhase = SharpNeat.Core.RunPhase;

namespace BodyBrainConfigGenerator
{
    public class ConfigGenerator
    {
        /// <summary>
        ///     Number of times query should be retried before finally throwing exception.  This is to deal with connection
        ///     resiliency issues and issues with connections in shared pool being disposed while another connection is using it.
        /// </summary>
        private const int MaxQueryRetryCnt = 100;

        /// <summary>
        ///     The number of brain CPPN inputs (x/y/z location, distance and bias).
        /// </summary>
        private const int BrainCppnInputCount = 5;

        /// <summary>
        ///     The number of brain CPPN outputs (presence/weights of controller connections).
        /// </summary>
        private const int BrainCppnOutputCount = 32;

        /// <summary>
        ///     The number of body CPPN inputs (x/y/z location, distance and bias).
        /// </summary>
        private const int BodyCppnInputCount = 5;

        /// <summary>
        ///     The number of body CPPN outputs (material presence and type).
        /// </summary>
        private const int BodyCppnOutputCount = 2;

        /// <summary>
        ///     The body genome decoder.
        /// </summary>
        private readonly VoxelBodyDecoder _bodyDecoder;

        /// <summary>
        ///     The body genome factory.
        /// </summary>
        private readonly CppnGenomeFactory _bodyGenomeFactory;

        /// <summary>
        ///     The brain genome decoder.
        /// </summary>
        private readonly VoxelBrainDecoder _brainDecoder;

        /// <summary>
        ///     The brain genome factory.
        /// </summary>
        private readonly CppnGenomeFactory _brainGenomeFactory;

        /// <summary>
        ///     The simulation configuration template including simulation parameter defaults.
        /// </summary>
        private readonly string _configTemplate;

        /// <summary>
        ///     The unique ID of the experiment for which simulation configuration files are being generated.
        /// </summary>
        private readonly int _experimentId;

        /// <summary>
        ///     The name of the experiment for which simulation configuration files are being generated.
        /// </summary>
        private readonly string _experimentName;

        /// <summary>
        ///     The minimal criteria.
        /// </summary>
        private readonly double _mcValue;

        /// <summary>
        ///     The output directory into which to write generated configuration files.
        /// </summary>
        private readonly string _outputDirectory;

        /// <summary>
        ///     The run number of the given experiment.
        /// </summary>
        private readonly int _run;

        /// <summary>
        ///     ConfigGenerator constructor.
        /// </summary>
        /// <param name="experimentConfig">Object containing experiment configuration parameters.</param>
        /// <param name="run">The current experiment run number.</param>
        /// <param name="configTemplate">The simulation configuration template including simulation parameter defaults.</param>
        /// <param name="outputDirectory">The output directory into which to write generated configuration files.</param>
        private ConfigGenerator(ExperimentDictionaryBodyBrain experimentConfig, int run, string configTemplate,
            string outputDirectory)
        {
            _experimentId = experimentConfig.ExperimentDictionaryId;
            _experimentName = experimentConfig.ExperimentName;
            _run = run;
            _configTemplate = configTemplate;
            _mcValue = experimentConfig.MinimalCriteriaValue;

            // Construct output directory
            _outputDirectory = Path.Combine(outputDirectory, experimentConfig.ExperimentName, $"Run{run}");

            // Delete directory if it already exists
            if (Directory.Exists(_outputDirectory))
            {
                Directory.Delete(_outputDirectory, true);
            }

            // Create the target directory
            Directory.CreateDirectory(_outputDirectory);

            // Set the appropriate activation scheme
            var activationScheme = experimentConfig.ActivationIters != null
                ? NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(experimentConfig.ActivationIters ?? 0)
                : NetworkActivationScheme.CreateAcyclicScheme();

            // Create the body and brain genome factories
            _brainGenomeFactory = new CppnGenomeFactory(BrainCppnInputCount, BrainCppnOutputCount);
            _bodyGenomeFactory = new CppnGenomeFactory(BodyCppnInputCount, BodyCppnOutputCount);

            // Create the body and brain genome decoders
            _brainDecoder = new VoxelBrainDecoder(activationScheme, experimentConfig.VoxelyzeConfigInitialXdimension,
                experimentConfig.VoxelyzeConfigInitialYdimension, experimentConfig.VoxelyzeConfigInitialZdimension,
                experimentConfig.VoxelyzeConfigBrainNetworkConnections);
            _bodyDecoder = new VoxelBodyDecoder(activationScheme, experimentConfig.VoxelyzeConfigInitialXdimension,
                experimentConfig.VoxelyzeConfigInitialYdimension, experimentConfig.VoxelyzeConfigInitialZdimension);
        }

        #region Private file I/O methods

        /// <summary>
        ///     Writes a simulation configuration file for a given body and brain.
        /// </summary>
        /// <param name="bodyGenome">The body to serialize to the configuration file.</param>
        /// <param name="brainGenome">The brain to serialize to the configuration file.</param>
        private void WriteConfigFile(MccexperimentVoxelBodyGenome bodyGenome, MccexperimentVoxelBrainGenome brainGenome)
        {
            VoxelBody body;
            VoxelBrain brain;

            // Read in voxel body XML and decode to phenotype
            using (var xmlReader = XmlReader.Create(new StringReader(bodyGenome.GenomeXml)))
            {
                body = _bodyDecoder.Decode(
                    NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false, _bodyGenomeFactory));
            }

            // Read in voxel brain XML and decode to phenotype
            using (var xmlReader = XmlReader.Create(new StringReader(brainGenome.GenomeXml)))
            {
                brain = _brainDecoder.Decode(
                    NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false, _brainGenomeFactory));
            }

            // Construct the output file path and name
            var outputFile = BodyBrainExperimentUtils.ConstructVoxelyzeFilePath("config", "vxa",
                _outputDirectory, _experimentName, _run, body.GenomeId, brain.GenomeId);

            // Write the configuration file
            BodyBrainExperimentUtils.WriteVoxelyzeSimulationFile(_configTemplate, outputFile, ".", brain, body,
                _mcValue);
        }

        #endregion

        #region Public static methods

        /// <summary>
        ///     Static method that constructs ConfigGenerator instance and runs simulation configuration file generation.
        /// </summary>
        /// <param name="experimentConfig">Object containing experiment configuration parameters.</param>
        /// <param name="run">The current experiment run number.</param>
        /// <param name="configTemplate">The simulation configuration template including simulation parameter defaults.</param>
        /// <param name="outputDirectory">The output directory into which to write generated configuration files.</param>
        /// <param name="chunkSize">The number of configuration files to generate at a time.</param>
        public static void GenerateSimulationConfigs(ExperimentDictionaryBodyBrain experimentConfig, int run,
            string configTemplate, string outputDirectory, int chunkSize = 10)
        {
            var instance = new ConfigGenerator(experimentConfig, run, configTemplate, outputDirectory);

            // Get all viable bodies
            var bodyGenomeIds = instance.GetBodyGenomeIds();

            for (var curChunk = 0; curChunk < bodyGenomeIds.Count; curChunk += chunkSize)
            {
                List<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> successfulGenomeCombos;

                // Get body genome IDs for the current chunk
                var curBodyGenomeIds = bodyGenomeIds.Skip(curChunk).Take(chunkSize).ToList();

                // Get combinations of brains that successfully ambulate a given body
                successfulGenomeCombos = instance.GetSuccessfulGenomeCombosFromBodyTrials(curBodyGenomeIds);

                foreach (var genomeCombo in successfulGenomeCombos)
                {
                    // Generate configuration file for the given body/brain combo
                    instance.WriteConfigFile(genomeCombo.Item1, genomeCombo.Item2);
                }
            }
        }

        /// <summary>
        ///     Looks up an experiment configuration given the unique experiment name.
        /// </summary>
        /// <param name="experimentName">The experiment name whose configuration to lookup.</param>
        /// <returns>The corresponding experiment configuration (i.e. experiment dictionary).</returns>
        public static ExperimentDictionaryBodyBrain LookupExperimentConfiguration(string experimentName)
        {
            ExperimentDictionaryBodyBrain experimentConfiguration = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the experiment configuration given the name (which is guaranteed to be unique)
                    using (var context = new ExperimentDataContext())
                    {
                        experimentConfiguration =
                            context.ExperimentDictionaryBodyBrain.Single(expName =>
                                expName.ExperimentName == experimentName);
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return experimentConfiguration;
        }

        #endregion

        #region Private static methods

        /// <summary>
        ///     Handles logging of query success state after one or more failed attempts.
        /// </summary>
        /// <param name="methodName">The name of the method executing the query.</param>
        /// <param name="retryCnt">The number of times the query has been retried.</param>
        private static void LogFailedQuerySuccess(string methodName, int retryCnt)
        {
            Console.Error.WriteLine("Successfully executed {0}.{1} query on batch retry {2}",
                typeof(ConfigGenerator).FullName, methodName, retryCnt);
        }

        /// <summary>
        ///     Handles logging and retry boundary checking for exceptions that are thrown during query execution.
        /// </summary>
        /// <param name="methodName">The name of the method executing the query.</param>
        /// <param name="retryCnt">The number of times the query has been retried.</param>
        /// <param name="e">The exception object that was thrown.</param>
        private static void HandleQueryException(string methodName, int retryCnt, Exception e)
        {
            Console.Error.WriteLine("{0}.{1} failed to execute query on retry {2}",
                typeof(ConfigGenerator).FullName, methodName, retryCnt);

            // Throw exception if we've no exceeded the retry count
            if (retryCnt + 1 > MaxQueryRetryCnt)
            {
                throw e;
            }
        }

        #endregion

        #region Private database methods

        /// <summary>
        ///     Retrieves the body genome IDs for a particular run/experiment.
        /// </summary>
        /// <returns>The body genome IDs.</returns>
        private IList<int> GetBodyGenomeIds()
        {
            IList<int> bodyGenomeIds = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the distinct body genome IDs logged during the run
                    using (var context = new ExperimentDataContext())
                    {
                        bodyGenomeIds =
                            context.MccexperimentVoxelBodyGenomes.Where(
                                    expData => expData.ExperimentDictionaryId == _experimentId && expData.Run == _run)
                                .Select(m => m.GenomeId)
                                .ToList();
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return bodyGenomeIds;
        }

        /// <summary>
        ///     Extracts successful body and brain genome pairs from experiment body trials.
        /// </summary>
        /// <param name="bodyGenomeIds">The list of body genome IDs for which to find successful brains.</param>
        /// <returns>Successful body and brain genome pairs.</returns>
        private List<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>>
            GetSuccessfulGenomeCombosFromBodyTrials(IList<int> bodyGenomeIds)
        {
            // Get successful ambulation trials during experiments (if produced)
            var perBodySuccessfulTrials = GetSuccessfulBrainTrialPerBody(_experimentId, _run, bodyGenomeIds);

            var successfulGenomeCombos =
                new List<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>>(perBodySuccessfulTrials
                    .Count());

            // Get distinct body and brain genomes
            var bodyGenomeData = GetBodyGenomeData(_experimentId, _run,
                perBodySuccessfulTrials.Select(trial => trial.BodyGenomeId).Distinct().ToList());
            var brainGenomeData = GetBrainGenomeData(_experimentId, _run, RunPhase.Primary,
                perBodySuccessfulTrials.Select(trial => trial.PairedBrainGenomeId).Distinct().ToList());

            // Build list of successful body/brain combinations
            successfulGenomeCombos.AddRange(
                perBodySuccessfulTrials.Select(
                    successfulTrial =>
                        new Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>(
                            bodyGenomeData.First(gd => successfulTrial.BodyGenomeId == gd.GenomeId),
                            brainGenomeData.First(gd => successfulTrial.PairedBrainGenomeId == gd.GenomeId))));

            return successfulGenomeCombos;
        }

        /// <summary>
        ///     For each body, retrieves the first brain that solved within the given experiment and run.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="bodyGenomeIds">The list of body genome IDs by which to filter the brain results.</param>
        /// <returns>The list of distinct body ambulation trials that were successful for the given experiment and run.</returns>
        private IList<MccexperimentVoxelBodyTrials> GetSuccessfulBrainTrialPerBody(int experimentId, int run,
            IList<int> bodyGenomeIds)
        {
            IList<MccexperimentVoxelBodyTrials> bodyTrials = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    using (var context = new ExperimentDataContext())
                    {
                        // Get single maze trial for each of the specified maze IDs over the entirety of the run
                        bodyTrials = context.MccexperimentVoxelBodyTrials.Where(
                                nav =>
                                    experimentId == nav.ExperimentDictionaryId && run == nav.Run &&
                                    nav.IsBodySolved && bodyGenomeIds.Contains(nav.BodyGenomeId))
                            .GroupBy(nav => nav.BodyGenomeId)
                            .Select(m => m.OrderBy(x => x.BodyGenomeId).FirstOrDefault())
                            .ToList();
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return bodyTrials;
        }

        /// <summary>
        ///     Retrieves the body genome data (i.e. evaluation statistics and XML) for the entirety of a given run/experiment,
        ///     constrained by the specified genome IDs.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="bodyGenomeIds">The body genome IDs by which to constrain.</param>
        /// <returns>The body genome data.</returns>
        private IList<MccexperimentVoxelBodyGenome> GetBodyGenomeData(int experimentId, int run,
            IList<int> bodyGenomeIds)
        {
            IList<MccexperimentVoxelBodyGenome> bodyGenomes = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the body genomes corresponding to the specified genome IDs
                    using (var context = new ExperimentDataContext())
                    {
                        bodyGenomes =
                            context.MccexperimentVoxelBodyGenomes.Where(
                                    expData =>
                                        expData.ExperimentDictionaryId == experimentId && expData.Run == run &&
                                        bodyGenomeIds.Contains(expData.GenomeId))
                                .ToList();
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return bodyGenomes;
        }

        /// <summary>
        ///     Retrieves the brain genome data (i.e. evaluation statistics and XML) for the entirety of a given
        ///     run/experiment, constrained by the specified genome IDs.
        /// </summary>
        /// <param name="experimentId">The experiment that was executed.</param>
        /// <param name="run">The run number of the given experiment.</param>
        /// <param name="runPhase">
        ///     Indicates whether this is part of the initialization or primary experiment phase.
        /// </param>
        /// <param name="brainGenomeIds">The brain genome IDs by which to constrain.</param>
        /// <returns>The navigator genome data.</returns>
        private IList<MccexperimentVoxelBrainGenome> GetBrainGenomeData(int experimentId, int run,
            RunPhase runPhase, IList<int> brainGenomeIds)
        {
            IList<MccexperimentVoxelBrainGenome> brainGenomes = null;
            var querySuccess = false;
            var retryCnt = 0;

            while (querySuccess == false && retryCnt <= MaxQueryRetryCnt)
            {
                try
                {
                    // Query for the brain genomes corresponding to the specified genome IDs
                    using (var context = new ExperimentDataContext())
                    {
                        brainGenomes =
                            context.MccexperimentVoxelBrainGenomes.Where(
                                expData =>
                                    expData.ExperimentDictionaryId == experimentId && expData.Run == run &&
                                    expData.RunPhaseFkNavigation.RunPhaseName == runPhase.ToString() &&
                                    brainGenomeIds.Contains(expData.GenomeId)).ToList();
                    }

                    if (retryCnt > 0)
                    {
                        LogFailedQuerySuccess(MethodBase.GetCurrentMethod().ToString(), retryCnt);
                    }

                    querySuccess = true;
                }
                catch (Exception e)
                {
                    HandleQueryException(MethodBase.GetCurrentMethod().ToString(), retryCnt++, e);
                }
            }

            return brainGenomes;
        }

        #endregion
    }
}