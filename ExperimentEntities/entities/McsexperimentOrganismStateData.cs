namespace ExperimentEntities.entities
{
    public partial class McsexperimentOrganismStateData
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int Evaluation { get; set; }
        public int RunPhaseFk { get; set; }
        public bool IsViable { get; set; }
        public bool StopConditionSatisfied { get; set; }
        public double DistanceToTarget { get; set; }
        public double AgentXlocation { get; set; }
        public double AgentYlocation { get; set; }

        public virtual RunPhase RunPhaseFkNavigation { get; set; }
    }
}
