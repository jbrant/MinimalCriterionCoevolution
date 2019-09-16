using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentNavigatorTrials
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int NavigatorGenomeId { get; set; }
        public int PairedMazeGenomeId { get; set; }
        public bool IsMazeSolved { get; set; }
        public double ObjectiveDistance { get; set; }
        public int NumTimesteps { get; set; }
    }
}
