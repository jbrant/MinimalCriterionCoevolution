using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentMazeTrials
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int MazeGenomeId { get; set; }
        public int PairedNavigatorGenomeId { get; set; }
        public bool IsMazeSolved { get; set; }
        public double ObjectiveDistance { get; set; }
        public int NumTimesteps { get; set; }
    }
}
