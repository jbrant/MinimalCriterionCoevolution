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
        #region Public evaluation methods

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

            Parallel.ForEach(viableBodyBrainCombos, new ParallelOptions {MaxDegreeOfParallelism = 1},
                delegate(Tuple<MccexperimentVoxelBodyGenome, MccexperimentVoxelBrainGenome> genomeCombo)
                {
                    VoxelBody body;
                    VoxelAnnBrain brain;

                    try
                    {
                        // Read in voxel body XML and decode to phenotype
                        body = DecodeHandler.DecodeBodyGenome(genomeCombo.Item1, voxelPack.BodyDecoder,
                            voxelPack.BodyGenomeFactory);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Error on body ID {genomeCombo.Item1.GenomeId}");
                        return;
                    }

                    try
                    {
                        // Read in voxel brain XML and decode to phenotype
                        brain = DecodeHandler.DecodeBrainGenome(genomeCombo.Item2, voxelPack.BrainDecoder,
                            voxelPack.BrainGenomeFactory,
                            body, numBrainConnections);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Error on brain ID {genomeCombo.Item2.GenomeId}");
                        return;
                    }

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
        /// <param name="constrainToSize">
        ///     Indicates whether bodies in run should be compared only to other bodies that are of
        ///     equivalent dimensionality or to the entire population regardless of size.
        /// </param>
        public static void GenerateRunBodyDiversityData(IList<VoxelBody> curChunkBodies,
            IList<VoxelBody> bodyPopulation,
            ExperimentDictionaryBodyBrain experimentConfig, int run, VoxelFactoryDecoderPack voxelPack,
            bool constrainToSize)
        {
            // Compute diversity of each body in the current chunk vs. the population over the full run
            var bodyDiversityUnits = curChunkBodies.Select(chunkBody => ComputeBodyDiversity(chunkBody,
                    constrainToSize
                        ? bodyPopulation.Where(x => x.LengthX == chunkBody.LengthX).ToList()
                        : bodyPopulation))
                .ToList();

            // Write the results of the run body diversity analysis for the current chunk
            if (constrainToSize)
                DataHandler.WriteRunSizeBodyDiversityDataToFile(experimentConfig.ExperimentDictionaryId, run,
                    bodyDiversityUnits);
            else
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
        public static void GenerateBatchBodyDiversityData(IList<VoxelBody> bodies,
            ExperimentDictionaryBodyBrain experimentConfig, int run, int batch, VoxelFactoryDecoderPack voxelPack)
        {
            // Compute diversity of each body compared to all other bodies in the current batch
            var bodyDiversityUnits = bodies.Select(body => ComputeBodyDiversity(body, bodies)).ToList();

            // Write the results of the run body diversity analysis for the current batch
            DataHandler.WriteBatchBodyDiversityDataToFile(experimentConfig.ExperimentDictionaryId, run, batch,
                bodyDiversityUnits);
        }

        /// <summary>
        ///     Computes the diversity between population ambulation trajectories.
        /// </summary>
        /// <param name="curChunkSimulationUnits">The simulation units in the current chunk undergoing diversity evaluation.</param>
        /// <param name="populationSimulationUnits">
        ///     The simulation units resulting from trials produced by all members of the
        ///     population.
        /// </param>
        /// <param name="bodySizeMap">Association between body genome ID and its size.</param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        /// <param name="constrainToSize">
        ///     Indicates whether bodies in run should be compared only to other bodies that are of
        ///     equivalent dimensionality or to the entire population regardless of size.
        /// </param>
        public static void GenerateRunTrajectoryDiversityData(IList<BodyBrainSimulationUnit> curChunkSimulationUnits,
            IList<BodyBrainSimulationUnit> populationSimulationUnits, IDictionary<uint, int> bodySizeMap,
            ExperimentDictionaryBodyBrain experimentConfig,
            int run, VoxelFactoryDecoderPack voxelPack, bool constrainToSize)
        {
            // Default lattice dimensionality used in Voxelyze simulations
            var latticeDim = 0.01;

            // Compute trajectory diversity between the current trajectory and all others in the current chunk
            var trajectoryDiversityUnits = curChunkSimulationUnits.Select(chunkSimUnit =>
                ComputeTrajectoryDiversity(chunkSimUnit,
                    constrainToSize
                        ? populationSimulationUnits
                            .Where(x => bodySizeMap[x.BodyId] == bodySizeMap[chunkSimUnit.BodyId]).ToList()
                        : populationSimulationUnits, bodySizeMap, latticeDim));

            // Write the results of trajectory diversity analysis for the current chunk
            if (constrainToSize)
                DataHandler.WriteRunSizeTrajectoryDiversityDataToFile(experimentConfig.ExperimentDictionaryId, run,
                    trajectoryDiversityUnits);
            else
                DataHandler.WriteRunTrajectoryDiversityDataToFile(experimentConfig.ExperimentDictionaryId, run,
                    trajectoryDiversityUnits);
        }

        /// <summary>
        ///     Computes the diversity between population ambulation trajectories.
        /// </summary>
        /// <param name="simulationUnits">
        ///     The simulation units resulting from trials produced by all members of the population
        ///     during the current batch.
        /// </param>
        /// <param name="experimentConfig">The parameters of the experiment being executed.</param>
        /// <param name="run">The run being executed.</param>
        /// <param name="batch">The batch during which trajectory diversity is being computed.</param>
        /// <param name="voxelPack">The voxel factory/decoder instances.</param>
        public static void GenerateBatchTrajectoryDiversityData(IList<BodyBrainSimulationUnit> simulationUnits,
            IDictionary<uint, int> bodySizeMap, ExperimentDictionaryBodyBrain experimentConfig, int run, int batch,
            VoxelFactoryDecoderPack voxelPack)
        {
            // Default lattice dimensionality used in Voxelyze simulations
            var latticeDim = 0.01;

            // Compute trajectory diversity between all in the current batch
            var trajectoryDiversityUnits = simulationUnits.Select(simUnit =>
                ComputeTrajectoryDiversity(simUnit, simulationUnits, bodySizeMap, latticeDim));

            // Write the results of trajectory diversity analysis for the current batch
            DataHandler.WriteBatchTrajectoryDiversityDataToFile(experimentConfig.ExperimentDictionaryId, run, batch,
                trajectoryDiversityUnits);
        }

        #endregion

        #region Private evaluation helper methods

        /// <summary>
        ///     Computes the euclidean distance between each trajectory point and averages over the number and over the number of
        ///     trajectories for which distance is being assessed. Also computes the average euclidean distance between trajectory
        ///     end points.
        /// </summary>
        /// <param name="simulationUnit">Simulation unit that is the subject of evaluation.</param>
        /// <param name="popSimulationUnits">Simulation units for the rest of the population, including trajectory positions.</param>
        /// <param name="bodySizeMap">Association between body genome ID and its size.</param>
        /// <param name="latticeDim">
        ///     The dimensionality of a single voxel within the voxel lattice - used to scale distance
        ///     measurements.
        /// </param>
        /// <returns></returns>
        private static TrajectoryDiversityUnit ComputeTrajectoryDiversity(BodyBrainSimulationUnit simulationUnit,
            IList<BodyBrainSimulationUnit> popSimulationUnits, IDictionary<uint, int> bodySizeMap, double latticeDim)
        {
            var incrementLock = new object();
            var totalTrajectoryDistance = 0.0;
            var totalEndPointDistance = 0.0;
            var posMeasurementCount = simulationUnit.BodyBrainSimulationTimestepUnits.Count;
            var timestepUnits = simulationUnit.BodyBrainSimulationTimestepUnits;

            Parallel.ForEach(popSimulationUnits, popSimulationUnit =>
            {
                var totalTrajectoryPointDistance = 0.0;
                var curTimestepUnits = popSimulationUnit.BodyBrainSimulationTimestepUnits;

                // Sum distance between each corresponding trajectory point
                for (var i = 0; i < posMeasurementCount; i++)
                {
                    totalTrajectoryPointDistance +=
                        Math.Sqrt(Math.Pow(timestepUnits[i].Position.X - curTimestepUnits[i].Position.X, 2) +
                                  Math.Pow(timestepUnits[i].Position.Y - curTimestepUnits[i].Position.Y, 2)) /
                        latticeDim;
                }

                // Compute current trajectory distance
                var curTrajectoryDistance = totalTrajectoryPointDistance / posMeasurementCount;

                // Compute end-point distance
                var curEndPointDistance =
                    Math.Sqrt(
                        Math.Pow(
                            timestepUnits[posMeasurementCount - 1].Position.X -
                            curTimestepUnits[posMeasurementCount - 1].Position.X, 2) +
                        Math.Pow(
                            timestepUnits[posMeasurementCount - 1].Position.Y -
                            curTimestepUnits[posMeasurementCount - 1].Position.Y, 2)) / latticeDim;

                // Lock for thread-safe increment
                lock (incrementLock)
                {
                    totalTrajectoryDistance += curTrajectoryDistance;
                    totalEndPointDistance += curEndPointDistance;
                }
            });

            return new TrajectoryDiversityUnit(simulationUnit.BrainId, simulationUnit.BodyId,
                bodySizeMap[simulationUnit.BodyId], totalTrajectoryDistance / popSimulationUnits.Count,
                totalEndPointDistance / popSimulationUnits.Count);
        }

        /// <summary>
        ///     Computes the voxel-wise difference of the given body compared to the other bodies in the given population.
        /// </summary>
        /// <param name="body">The body for which to compute voxel-wise difference.</param>
        /// <param name="bodyPopulation">All of the bodies in the population.</param>
        /// <returns>BodyDiversityUnit encapsulating voxel-wise body difference.</returns>
        private static BodyDiversityUnit ComputeBodyDiversity(VoxelBody body, IList<VoxelBody> bodyPopulation)
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

            return new BodyDiversityUnit(body.GenomeId, body.LengthX, (double) numOverallDiff / bodyPopulation.Count,
                (double) numMaterialTypeDiff / bodyPopulation.Count, (double) numActiveDiff / bodyPopulation.Count,
                (double) numPassiveDiff / bodyPopulation.Count);
        }

        #endregion
    }
}