namespace ExperimentEntities.entities
{
    public partial class MccexperimentMazeEvaluationData
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int PopulationSize { get; set; }
        public int ViableOffspringCount { get; set; }
        public int MinWalls { get; set; }
        public int MaxWalls { get; set; }
        public double MeanWalls { get; set; }
        public int? MinWaypoints { get; set; }
        public int? MaxWaypoints { get; set; }
        public double? MeanWaypoints { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }
        public double? MeanHeight { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public double? MeanWidth { get; set; }
        public int? MinJunctures { get; set; }
        public int? MaxJunctures { get; set; }
        public double? MeanJunctures { get; set; }
        public int? MinTrajectoryFacingOpenings { get; set; }
        public int? MaxTrajectoryFacingOpenings { get; set; }
        public double? MeanTrajectoryFacingOpenings { get; set; }
        public int TotalEvaluations { get; set; }
        public int? EvaluationsPerSecond { get; set; }
    }
}
