using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class ExperimentDictionaryBodyBrain
    {
        public int ExperimentDictionaryId { get; set; }
        public string ExperimentName { get; set; }
        public int MaxBodyInitializationRestarts { get; set; }
        public int MaxBrainInitializationEvaluations { get; set; }
        public int BrainPopulationSize { get; set; }
        public int BodyPopulationSize { get; set; }
        public int BrainSeedGenomeCount { get; set; }
        public int BodySeedGenomeCount { get; set; }
        public int BrainOffspringBatchSize { get; set; }
        public int BodyOffspringBatchSize { get; set; }
        public int MaxBatches { get; set; }
        public int ResourceLimit { get; set; }
        public string ActivationScheme { get; set; }
        public int? ActivationIters { get; set; }
        public int MinimalCriteriaValue { get; set; }
        public int NumBodiesSolvedCriteria { get; set; }
        public int NumBrainsSolvedCriteria { get; set; }
        public int MaxBodySize { get; set; }
        public string InitializationSearchAlgorithm { get; set; }
        public string InitializationSelectionAlgorithm { get; set; }
        public int InitializationPopulationSize { get; set; }
        public int InitializationSpecieCount { get; set; }
        public int? InitializationOffspringBatchSize { get; set; }
        public int InitializationPopulationEvaluationFrequency { get; set; }
        public string InitializationComplexityRegulationStrategy { get; set; }
        public int InitializationComplexityThreshold { get; set; }
        public double InitializationSelectionProportion { get; set; }
        public double InitializationOffspringAsexualProbability { get; set; }
        public double InitializationOffspringSexualProbability { get; set; }
        public double InitializationInterspeciesMatingProbability { get; set; }
        public double InitializationGenomeConfigInitialConnectionProportion { get; set; }
        public double InitializationGenomeConfigWeightMutationProbability { get; set; }
        public double InitializationGenomeConfigAddConnnectionProbability { get; set; }
        public double InitializationGenomeConfigAddNodeProbability { get; set; }
        public double InitializationGenomeConfigDeleteConnectionProbability { get; set; }
        public double InitializationGenomeConfigConnectionWeightRange { get; set; }
        public string InitializationBehaviorCharacterization { get; set; }
        public int? InitializationNearestNeighbors { get; set; }
        public int? InitializationNoveltyConfigArchiveAdditionThreshold { get; set; }
        public double? InitializationNoveltyConfigArchiveThresholdDecreaseMultiplier { get; set; }
        public double? InitializationNoveltyConfigArchiveThresholdIncreaseMultiplier { get; set; }
        public int? InitializationNoveltyConfigMaxGenerationalArchiveAddition { get; set; }
        public int? InitializationNoveltyConfigMaxGenerationsWithoutArchiveAddition { get; set; }
        public string PrimaryBehaviorCharacterization { get; set; }
        public double PrimaryGenomeConfigInitialConnectionProportion { get; set; }
        public double PrimaryGenomeConfigWeightMutationProbability { get; set; }
        public double PrimaryGenomeConfigAddConnnectionProbability { get; set; }
        public double PrimaryGenomeConfigAddNodeProbability { get; set; }
        public double PrimaryGenomeConfigDeleteConnectionProbability { get; set; }
        public double? PrimaryGenomeConfigIncreaseResolutionProbability { get; set; }
        public double? PrimaryGenomeConfigDecreaseResolutionProbability { get; set; }
        public double PrimaryGenomeConfigConnectionWeightRange { get; set; }
        public int VoxelyzeConfigInitialXdimension { get; set; }
        public int VoxelyzeConfigInitialYdimension { get; set; }
        public int VoxelyzeConfigInitialZdimension { get; set; }
        public int VoxelyzeConfigBrainNetworkConnections { get; set; }
        public double VoxelyzeConfigMinPercentMaterial { get; set; }
        public double VoxelyzeConfigMinPercentActiveMaterial { get; set; }
        public double VoxelyzeConfigSimulatedSeconds { get; set; }
        public double VoxelyzeConfigInitializationSeconds { get; set; }
        public int VoxelyzeConfigActuationsPerSecond { get; set; }
        public double VoxelyzeConfigFloorSlope { get; set; }
    }
}
