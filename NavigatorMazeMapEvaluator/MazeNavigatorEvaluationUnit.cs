#region

using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace NavigatorMazeMapEvaluator
{
    public class MazeNavigatorEvaluationUnit
    {
        public MazeNavigatorEvaluationUnit(MazeStructure mazeStructure, IBlackBox agentPhenome, int mazeId, int agentId)
        {
            MazePhenome = mazeStructure;
            AgentPhenome = agentPhenome;
            MazeId = mazeId;
            AgentId = agentId;
        }

        public int AgentId { get; }
        public int MazeId { get; }
        public IBlackBox AgentPhenome { get; }
        public MazeStructure MazePhenome { get; }
        public bool IsMazeSolved { get; set; }
        public int NumTimesteps { get; set; }
        public double[] AgentTrajectory { get; set; }
    }
}