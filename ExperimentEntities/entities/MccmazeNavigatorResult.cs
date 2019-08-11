using System.ComponentModel.DataAnnotations.Schema;

namespace ExperimentEntities.entities
{
    [Table("MCCMazeNavigatorResults")]
    public partial class MccmazeNavigatorResult
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int MazeGenomeId { get; set; }
        public int NavigatorGenomeId { get; set; }
        public bool IsMazeSolved { get; set; }
        public int NumTimesteps { get; set; }
        public int RunPhaseFk { get; set; }

        public virtual RunPhase RunPhaseFkNavigation { get; set; }
    }
}
