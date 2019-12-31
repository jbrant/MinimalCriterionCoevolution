using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodyBrainConfigGenerator;
using ExperimentEntities.entities;
using SharpNeat.Phenomes.Voxels;

namespace BodyBrainSupportLib
{
    /// <summary>
    ///     Provides methods for computing body/brain experiment evaluation metrics.
    /// </summary>
    public static class EvaluationHandler
    {
        /// <summary>
        ///     Generates configuration files for body/brain simulation.
        /// </summary>
        /// <param name="viableBodyBrainCombos">Successful combinations of bodies and brains.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="executionConfiguration">Encapsulates configuration parameters specified at runtime.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        public static void GenerateSimulationConfigs(
            IEnumerable<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> viableBodyBrainCombos,
            ExperimentDictionaryBodyBrain experimentConfig, int run,
            IReadOnlyDictionary<ExecutionParameter, string> executionConfiguration, VoxelFactoryDecoderPack voxelPack)
        {
            var experimentName = experimentConfig.ExperimentName;
            var numBrainConnections = experimentConfig.VoxelyzeConfigBrainNetworkConnections;
            var configTemplate = executionConfiguration[ExecutionParameter.ConfigTemplateFilePath];
            var configDirectory = executionConfiguration[ExecutionParameter.ConfigOutputDirectory];

            Parallel.ForEach(viableBodyBrainCombos,
                delegate(Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome> genomeCombo)
                {
                    // Read in voxel body XML and decode to phenotype
                    var body = DecodeHandler.DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                        voxelPack.BodyGenomeFactory);

                    // Read in voxel brain XML and decode to phenotype
                    var brain = DecodeHandler.DecodeBrainGenome(genomeCombo.Item2, voxelPack.BrainDecoder,
                        voxelPack.BrainGenomeFactory,
                        body, numBrainConnections);

                    // Construct the output directory
                    var configOutputDirectory =
                        SimulationHandler.GetConfigOutputDirectory(configDirectory, experimentName, run, body);

                    // Generate configuration file for the given body/brain combo
                    SimulationHandler.WriteConfigFile(body, brain, configOutputDirectory, experimentName, run,
                        configTemplate, experimentConfig.MinimalCriteriaValue);
                });
        }

        /// <summary>
        ///     Generates detailed, per-timestep log data for the body/brain simulation.
        /// </summary>
        /// <param name="viableBodyBrainCombos">Successful combinations of bodies and brains.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="executionConfiguration">Encapsulates configuration parameters specified at runtime.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        public static void GenerateSimulationLogData(
            IEnumerable<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> viableBodyBrainCombos,
            ExperimentDictionaryBodyBrain experimentConfig, int run,
            IReadOnlyDictionary<ExecutionParameter, string> executionConfiguration, VoxelFactoryDecoderPack voxelPack)
        {
            var bodyBrainSimulationUnits = new ConcurrentBag<BodyBrainSimulationUnit>();

            var experimentId = experimentConfig.ExperimentDictionaryId;
            var experimentName = experimentConfig.ExperimentName;
            var numBrainConnections = experimentConfig.VoxelyzeConfigBrainNetworkConnections;
            var simulationTime = double.Parse(executionConfiguration[ExecutionParameter.SimulationTimesteps]);
            var simExecutablePath = executionConfiguration[ExecutionParameter.SimExecutablePath];
            var configTemplate = executionConfiguration[ExecutionParameter.ConfigTemplateFilePath];
            var configDirectory = executionConfiguration[ExecutionParameter.ConfigOutputDirectory];
            var simLogDirectory = executionConfiguration[ExecutionParameter.SimLogOutputDirectory];

            Parallel.ForEach(viableBodyBrainCombos,
                delegate(Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome> genomeCombo)
                {
                    // Read in voxel body XML and decode to phenotype
                    var body = DecodeHandler.DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                        voxelPack.BodyGenomeFactory);

                    // Read in voxel brain XML and decode to phenotype
                    var brain = DecodeHandler.DecodeBrainGenome(genomeCombo.Item2, voxelPack.BrainDecoder,
                        voxelPack.BrainGenomeFactory,
                        body, numBrainConnections);

                    // Construct the simulation configuration file path
                    var configFilePath = SimulationHandler.GetConfigFilePath(configDirectory, experimentName, run,
                        brain.GenomeId, body.GenomeId);

                    // Construct the simulation log file path
                    var simLogFilePath = SimulationHandler.GetSimLogFilePath(simLogDirectory, experimentName, run,
                        brain.GenomeId, body.GenomeId);

                    // Run the simulation
                    SimulationHandler.ExecuteTimeboundBodyBrainSimulation(configTemplate, configFilePath,
                        simExecutablePath,
                        simLogFilePath, simulationTime, brain, body);

                    // Extract simulation log data
                    bodyBrainSimulationUnits.Add(
                        SimulationHandler.ReadSimulationLog(brain.GenomeId, body.GenomeId, simLogFilePath));
                });

            // Write simulation log data from body/brain combinations
            DataHandler.WriteSimulationLogDataToFile(experimentId, run, bodyBrainSimulationUnits);
        }

        /// <summary>
        ///     For each successful body/brain combination, upscales the body phenotype (i.e. querying the CPPN at a higher
        ///     resolution) in increments of one unit per dimension, and evaluates the brains ability to control the body and still
        ///     meet the MC. This continues until the body reaches a size where the brain fails to ambulate the body the minimum
        ///     required distance, or the maximum allowable body size is reached.
        /// </summary>
        /// <param name="viableBodyBrainCombos">Successful combinations of bodies and brains.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="executionConfiguration">Encapsulates configuration parameters specified at runtime.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        public static void GenerateUpscaleResultData(
            IEnumerable<Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome>> viableBodyBrainCombos,
            ExperimentDictionaryBodyBrain experimentConfig, int run,
            IReadOnlyDictionary<ExecutionParameter, string> executionConfiguration, VoxelFactoryDecoderPack voxelPack)
        {
            var upscaleResultUnits = new ConcurrentBag<UpscaleResultUnit>();

            var experimentId = experimentConfig.ExperimentDictionaryId;
            var experimentName = experimentConfig.ExperimentName;
            var numBrainConnections = experimentConfig.VoxelyzeConfigBrainNetworkConnections;
            var minDistance = experimentConfig.MinimalCriteriaValue;
            var maxBodySize = int.Parse(executionConfiguration[ExecutionParameter.MaxBodySize]);
            var simExecutablePath = executionConfiguration[ExecutionParameter.SimExecutablePath];
            var configTemplate = executionConfiguration[ExecutionParameter.ConfigTemplateFilePath];
            var configDirectory = executionConfiguration[ExecutionParameter.ConfigOutputDirectory];
            var simResultDirectory = executionConfiguration[ExecutionParameter.ResultsOutputDirectory];

            Parallel.ForEach(viableBodyBrainCombos,
                delegate(Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome> genomeCombo)
                {
                    var curResolutionIncrease = 0;
                    bool isSuccessful;

                    // Copy off the body/brain genome IDs
                    var bodyGenomeId = (uint) genomeCombo.Item1.GenomeId;
                    var brainGenomeId = (uint) genomeCombo.Item2.GenomeId;

                    // Get starting size
                    var evolvedSize = DecodeHandler.DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                        voxelPack.BodyGenomeFactory).LengthX;

                    // Construct the simulation configuration file path
                    var configFilePath = SimulationHandler.GetConfigFilePath(configDirectory, experimentName, run,
                        brainGenomeId, bodyGenomeId);

                    // Construct the simulation result file path
                    var simResultFilePath = SimulationHandler.GetSimResultFilePath(simResultDirectory, experimentName,
                        run,
                        brainGenomeId, bodyGenomeId);

                    do
                    {
                        // Read in voxel body XML and decode to phenotype
                        var body = DecodeHandler.DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                            voxelPack.BodyGenomeFactory, curResolutionIncrease);

                        // Read in voxel brain XML and decode to phenotype
                        var brain = DecodeHandler.DecodeBrainGenome(genomeCombo.Item2, voxelPack.BrainDecoder,
                            voxelPack.BrainGenomeFactory,
                            body, numBrainConnections);

                        // Run the simulation
                        SimulationHandler.ExecuteDistanceBoundedBodyBrainSimulation(configTemplate, configFilePath,
                            simExecutablePath, simResultFilePath, minDistance, brain, body);

                        // Extract the simulation results
                        isSuccessful = SimulationHandler.ReadSimulationDistance(simResultFilePath) >= minDistance;
                    } while (isSuccessful && maxBodySize >= evolvedSize + ++curResolutionIncrease);

                    // Record the max resolution at which the body was solvable
                    upscaleResultUnits.Add(new UpscaleResultUnit(brainGenomeId, bodyGenomeId, evolvedSize,
                        evolvedSize + Math.Max(curResolutionIncrease - 1, 0)));
                });

            // Write results of upscale analysis
            DataHandler.WriteUpscaleResultDataToFile(experimentId, run, upscaleResultUnits);
        }

        /// <summary>
        ///     Computes the diversity of a body compared to the rest of the population in terms of voxel-wise material
        ///     differences.
        /// </summary>
        /// <param name="curChunkBodies">The bodies in the current chunk undergoing diversity evaluation.</param>
        /// <param name="bodyPopulation">All of the bodies in the population.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        public static void GenerateRunBodyDiversityData(List<VoxelBody> curChunkBodies, List<VoxelBody> bodyPopulation,
            ExperimentDictionaryBodyBrain experimentConfig, int run, VoxelFactoryDecoderPack voxelPack)
        {
            // Compute diversity of each body in the current chunk vs. the population over the full run
            var bodyDiversityUnits = curChunkBodies.Select(chunkBody => ComputeBodyDiversity(chunkBody, bodyPopulation))
                .ToList();

            // Write the results of the run body diversity analysis for the current chunk
            DataHandler.WriteRunBodyDiversityDataToFile(experimentConfig.ExperimentDictionaryId, run,
                bodyDiversityUnits);
        }

        /// <summary>
        ///     Computes the diversity of a body compared to the rest of the population in terms of voxel-wise material
        ///     differences.
        /// </summary>
        /// <param name="bodies">All of the bodies in the population at the current batch.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="batch">The batch during which extant body diversity is being computed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        public static void GenerateBatchBodyDiversityData(List<VoxelBody> bodies,
            ExperimentDictionaryBodyBrain experimentConfig, int run, int batch, VoxelFactoryDecoderPack voxelPack)
        {
            // Compute diversity of each body compared to all other bodies in the current batch
            var bodyDiversityUnits = bodies.Select(body => ComputeBodyDiversity(body, bodies)).ToList();

            // Write the results of the run body diversity analysis for the current batch
            DataHandler.WriteBatchBodyDiversityDataToFile(experimentConfig.ExperimentDictionaryId, run, batch,
                bodyDiversityUnits);
        }

        /// <summary>
        ///     Computes the voxel-wise difference of the given body compared to the other bodies in the given population.
        /// </summary>
        /// <param name="body">The body for which to compute voxel-wise difference.</param>
        /// <param name="bodyPopulation">All of the bodies in the population.</param>
        /// <returns>BodyDiversityUnit encapsulating voxel-wise body difference.</returns>
        private static BodyDiversityUnit ComputeBodyDiversity(VoxelBody body, List<VoxelBody> bodyPopulation)
        {
            var numOverallDiff = 0;
            var numMaterialTypeDiff = 0;
            var numActiveDiff = 0;
            var numPassiveDiff = 0;

            // Compare the current body against all bodies in the population in terms of voxel-wise differences
            Parallel.ForEach(bodyPopulation, popBody =>
            {
                // Don't compare body to itself
                if (popBody.GenomeId == body.GenomeId) return;

                for (var x = 0; x < body.LengthX || x < popBody.LengthX; x++)
                {
                    for (var y = 0; y < body.LengthY || y < popBody.LengthY; y++)
                    {
                        for (var z = 0; z < body.LengthZ || z < popBody.LengthZ; z++)
                        {
                            // Get material for both bodies at the current location
                            var chunkBodyMaterial = body.GetMaterialAtLocation(x, y, z);
                            var popBodyMaterial = popBody.GetMaterialAtLocation(x, y, z);

                            // Increment for any kind of mismatch (active, passive or empty)
                            if (chunkBodyMaterial != popBodyMaterial) Interlocked.Increment(ref numOverallDiff);

                            // Increment if one body is empty at the location but the other has material
                            if ((chunkBodyMaterial == VoxelMaterial.None ||
                                 popBodyMaterial == VoxelMaterial.None) &&
                                chunkBodyMaterial != popBodyMaterial)
                                Interlocked.Increment(ref numMaterialTypeDiff);

                            // Increment if one body has active material at the location but the other has some
                            // other type of material or is empty
                            if ((chunkBodyMaterial == VoxelMaterial.ActiveTissue ||
                                 popBodyMaterial == VoxelMaterial.ActiveTissue) &&
                                chunkBodyMaterial != popBodyMaterial) Interlocked.Increment(ref numActiveDiff);

                            // Increment if one body has passive material at the location but the other has some
                            // other type of material or is empty
                            if ((chunkBodyMaterial == VoxelMaterial.PassiveTissue ||
                                 popBodyMaterial == VoxelMaterial.PassiveTissue) &&
                                chunkBodyMaterial != popBodyMaterial) Interlocked.Increment(ref numPassiveDiff);
                        }
                    }
                }
            });

            return new BodyDiversityUnit(body.GenomeId, (double) numOverallDiff / bodyPopulation.Count,
                (double) numMaterialTypeDiff / bodyPopulation.Count, (double) numActiveDiff / bodyPopulation.Count,
                (double) numPassiveDiff / bodyPopulation.Count);
        }
    }
}