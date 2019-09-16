using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentMazeResourceUsage
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int GenomeId { get; set; }
        public int UsageCount { get; set; }
    }
}
