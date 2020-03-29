using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentVoxelBrainGenome
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int GenomeId { get; set; }
        public string GenomeXml { get; set; }
        public int RunPhaseFk { get; set; }
        
        public virtual RunPhase RunPhaseFkNavigation { get; set; }
    }
}
