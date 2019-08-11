namespace ExperimentEntities.entities
{
    public partial class MccexperimentExtantNavigatorPopulation
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int GenomeId { get; set; }
        public int RunPhaseFk { get; set; }
        public int? SpecieId { get; set; }

        public virtual RunPhase RunPhaseFkNavigation { get; set; }
    }
}
