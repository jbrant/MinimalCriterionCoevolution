using System;
using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class MccbodyBrainSimLog
    {
        public int ExperimentDictionaryId { get; set; }
        public int Run { get; set; }
        public int BodyGenomeId { get; set; }
        public int BrainGenomeId { get; set; }
        public int TimeStep { get; set; }
        public double Time { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double PositionZ { get; set; }
        public double Distance { get; set; }
        public double TotalDistance { get; set; }
        public int VoxelsTouchingFloor { get; set; }
        public double MaxVoxelVelocity { get; set; }
        public double MaxVoxelDisplacement { get; set; }
        public double MaxTrialDisplacement { get; set; }
        public double DisplacementX { get; set; }
        public double DisplacementY { get; set; }
        public double DisplacementZ { get; set; }
    }
}
