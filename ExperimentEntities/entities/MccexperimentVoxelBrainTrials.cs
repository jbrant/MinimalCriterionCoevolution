using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentVoxelBrainTrials
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int BrainGenomeId { get; set; }
        public int PairedBodyGenomeId { get; set; }
        public bool IsBodySolved { get; set; }
        public double Distance { get; set; }
        public int NumTimesteps { get; set; }
    }
}
