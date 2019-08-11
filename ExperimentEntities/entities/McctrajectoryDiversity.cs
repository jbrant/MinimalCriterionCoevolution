namespace ExperimentEntities.entities
{
    public partial class McctrajectoryDiversity
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int MazeGenomeId { get; set; }
        public int NavigatorGenomeId { get; set; }
        public double IntraMazeDiversityScore { get; set; }
        public double InterMazeDiversityScore { get; set; }
        public double GlobalDiversityScore { get; set; }

        public virtual ExperimentDictionary ExperimentDictionary { get; set; }
    }
}
