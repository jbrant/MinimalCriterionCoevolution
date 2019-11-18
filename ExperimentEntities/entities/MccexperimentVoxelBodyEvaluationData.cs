using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccexperimentVoxelBodyEvaluationData
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int Generation { get; set; }
        public int PopulationSize { get; set; }
        public int ViableOffspringCount { get; set; }
        public int MinComplexity { get; set; }
        public int MaxComplexity { get; set; }
        public double MeanComplexity { get; set; }
        public int MinVoxels { get; set; }
        public int MaxVoxels { get; set; }
        public double MeanVoxels { get; set; }
        public double MinFullProportion { get; set; }
        public double MaxFullProportion { get; set; }
        public double MeanFullProportion { get; set; }
        public int MinActiveVoxels { get; set; }
        public int MaxActiveVoxels { get; set; }
        public double MeanActiveVoxels { get; set; }
        public int MinPassiveVoxels { get; set; }
        public int MaxPassiveVoxels { get; set; }
        public double MeanPassiveVoxels { get; set; }
        public double MinActiveVoxelProportion { get; set; }
        public double MaxActiveVoxelProportion { get; set; }
        public double MeanActiveVoxelProportion { get; set; }
        public double MinPassiveVoxelProportion { get; set; }
        public double MaxPassiveVoxelProportion { get; set; }
        public double MeanPassiveVoxelProportion { get; set; }
        public int TotalEvaluations { get; set; }
        public int? EvaluationsPerSecond { get; set; }
    }
}
