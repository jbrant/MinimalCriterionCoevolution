namespace ExperimentEntities.entities
{
    public partial class MccexperimentExtantMazePopulation
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int GenomeId { get; set; }
        public int? SpecieId { get; set; }
    }
}