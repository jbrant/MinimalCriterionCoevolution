using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentExtantVoxelBrainPopulation
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int GenomeId { get; set; }
        public int RunPhaseFk { get; set; }
    }
}
