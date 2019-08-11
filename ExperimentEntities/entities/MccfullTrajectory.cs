using System.ComponentModel.DataAnnotations.Schema;

namespace ExperimentEntities.entities
{
    [Table("MCCFullTrajectories")]
    public partial class MccfullTrajectory
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int Timestep { get; set; }
        public int MazeGenomeId { get; set; }
        public int NavigatorGenomeId { get; set; }
        public decimal Xposition { get; set; }
        public decimal Yposition { get; set; }

        public virtual ExperimentDictionary ExperimentDictionary { get; set; }
    }
}
