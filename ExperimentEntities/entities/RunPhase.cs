using System.Collections.Generic;

namespace ExperimentEntities.entities
{
    public partial class RunPhase
    {
        public RunPhase()
        {
            MccexperimentExtantNavigatorPopulation = new HashSet<MccexperimentExtantNavigatorPopulation>();
            MccexperimentNavigatorEvaluationData = new HashSet<MccexperimentNavigatorEvaluationData>();
            MccexperimentNavigatorGenomes = new HashSet<MccexperimentNavigatorGenome>();
            MccmazeNavigatorResults = new HashSet<MccmazeNavigatorResult>();
            McsexperimentEvaluationData = new HashSet<McsexperimentEvaluationData>();
            McsexperimentOrganismStateData = new HashSet<McsexperimentOrganismStateData>();
            MccexperimentVoxelBrainGenomes = new HashSet<MccexperimentVoxelBrainGenome>();
        }

        public int RunPhaseId { get; set; }
        public string RunPhaseName { get; set; }

        public virtual ICollection<MccexperimentExtantNavigatorPopulation> MccexperimentExtantNavigatorPopulation { get; set; }
        public virtual ICollection<MccexperimentNavigatorEvaluationData> MccexperimentNavigatorEvaluationData { get; set; }
        public virtual ICollection<MccexperimentNavigatorGenome> MccexperimentNavigatorGenomes { get; set; }
        public virtual ICollection<MccmazeNavigatorResult> MccmazeNavigatorResults { get; set; }
        public virtual ICollection<McsexperimentEvaluationData> McsexperimentEvaluationData { get; set; }
        public virtual ICollection<McsexperimentOrganismStateData> McsexperimentOrganismStateData { get; set; }
        public virtual ICollection<MccexperimentVoxelBrainGenome> MccexperimentVoxelBrainGenomes { get; set; }
    }
}
