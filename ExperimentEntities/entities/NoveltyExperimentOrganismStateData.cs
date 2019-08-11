namespace ExperimentEntities.entities
{
    public partial class NoveltyExperimentOrganismStateData
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int Evaluation { get; set; }
        public bool StopConditionSatisfied { get; set; }
        public double DistanceToTarget { get; set; }
        public double AgentXlocation { get; set; }
        public double AgentYlocation { get; set; }
    }
}
