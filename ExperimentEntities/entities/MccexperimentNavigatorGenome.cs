using System.ComponentModel.DataAnnotations.Schema;

namespace ExperimentEntities.entities
{
    [Table("MCCExperimentNavigatorGenomes")]
    public partial class MccexperimentNavigatorGenome
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int GenomeId { get; set; }
        public string GenomeXml { get; set; }
        public int RunPhaseFk { get; set; }

        public virtual RunPhase RunPhaseFkNavigation { get; set; }
    }
}
