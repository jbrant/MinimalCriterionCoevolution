using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentVoxelBrainEvaluationData
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int PopulationSize { get; set; }
        public int ViableOffspringCount { get; set; }
        public int MinComplexity { get; set; }
        public int MaxComplexity { get; set; }
        public double MeanComplexity { get; set; }
        public int TotalEvaluations { get; set; }
        public int? EvaluationsPerSecond { get; set; }
    }
}
