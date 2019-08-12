namespace ExperimentEntities.entities
{
    public partial class McsexperimentEvaluationData
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int RunPhaseFk { get; set; }
        public int PopulationSize { get; set; }
        public int OffspringCount { get; set; }
        public int MaxComplexity { get; set; }
        public double MeanComplexity { get; set; }
        public int TotalEvaluations { get; set; }
        public int? EvaluationsPerSecond { get; set; }
        public int ClosestGenomeId { get; set; }
        public int ClosestGenomeConnectionGeneCount { get; set; }
        public int ClosestGenomeNeuronGeneCount { get; set; }
        public int ClosestGenomeTotalGeneCount { get; set; }
        public double? ClosestGenomeDistanceToTarget { get; set; }
        public double? ClosestGenomeEndPositionX { get; set; }
        public double? ClosestGenomeEndPositionY { get; set; }
        public string ClosestGenomeXml { get; set; }
        public int? ViableOffspringCount { get; set; }

        public virtual ExperimentDictionary ExperimentDictionary { get; set; }
        public virtual RunPhase RunPhaseFkNavigation { get; set; }
    }
}
