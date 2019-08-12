namespace ExperimentEntities.entities
{
    public partial class NoveltyExperimentEvaluationData
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int SpecieCount { get; set; }
        public int AsexualOffspringCount { get; set; }
        public int SexualOffspringCount { get; set; }
        public int TotalOffspringCount { get; set; }
        public int InterspeciesOffspringCount { get; set; }
        public double MaxFitness { get; set; }
        public double MeanFitness { get; set; }
        public double MeanSpecieChampFitness { get; set; }
        public int MaxComplexity { get; set; }
        public double MeanComplexity { get; set; }
        public int MinSpecieSize { get; set; }
        public int MaxSpecieSize { get; set; }
        public int? TotalEvaluations { get; set; }
        public int? EvaluationsPerSecond { get; set; }
        public int ChampGenomeId { get; set; }
        public double ChampGenomeFitness { get; set; }
        public int ChampGenomeBirthGeneration { get; set; }
        public int ChampGenomeConnectionGeneCount { get; set; }
        public int ChampGenomeNeuronGeneCount { get; set; }
        public int ChampGenomeTotalGeneCount { get; set; }
        public int? ChampGenomeEvaluationCount { get; set; }
        public double? ChampGenomeBehavior1 { get; set; }
        public double? ChampGenomeBehavior2 { get; set; }
    }
}
