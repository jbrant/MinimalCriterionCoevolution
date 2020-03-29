using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentVoxelBodyGenome
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int GenomeId { get; set; }
        public string GenomeXml { get; set; }
    }
}
