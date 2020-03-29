using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentVoxelBodyTrials
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int BodyGenomeId { get; set; }
        public int PairedBrainGenomeId { get; set; }
        public bool IsBodySolved { get; set; }
        public double Distance { get; set; }
        public int NumTimesteps { get; set; }
    }
}
