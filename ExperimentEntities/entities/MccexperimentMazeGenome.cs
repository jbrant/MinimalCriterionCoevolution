using System.ComponentModel.DataAnnotations.Schema;

namespace ExperimentEntities.entities
{
    [Table("MCCExperimentMazeGenomes")]
    public partial class MccexperimentMazeGenome
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int GenomeId { get; set; }
        public string GenomeXml { get; set; }
    }
}
