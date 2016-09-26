#region

using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeExperimentSuppotLib
{
    /// <summary>
    ///     Encapsulates one unit of evaluation - a single agent through a single maze.
    /// </summary>
    public class MazeNavigatorEvaluationUnit
    {
        /// <summary>
        ///     Evaluation unit constructor.
        /// </summary>
        /// <param name="mazeStructure">The maze structure on which the agent is evaluated.</param>
        /// <param name="agentPhenome">The agent ANN phenome.</param>
        /// <param name="mazeId">The unique maze identifier.</param>
        /// <param name="agentId">The unique agent identifier.</param>
        public MazeNavigatorEvaluationUnit(MazeStructure mazeStructure, IBlackBox agentPhenome, int mazeId, int agentId)
        {
            MazePhenome = mazeStructure;
            AgentPhenome = agentPhenome;
            MazeId = mazeId;
            AgentId = agentId;
        }

        /// <summary>
        ///     The unique agent identifier.
        /// </summary>
        public int AgentId { get; }

        /// <summary>
        ///     The unique maze identifier
        /// </summary>
        public int MazeId { get; }

        /// <summary>
        ///     The agent ANN phenome.
        /// </summary>
        public IBlackBox AgentPhenome { get; }

        /// <summary>
        ///     The maze structure on which the agent is evaluated.
        /// </summary>
        public MazeStructure MazePhenome { get; }

        /// <summary>
        ///     Flag indicating whether the maze was solved by the agent.
        /// </summary>
        public bool IsMazeSolved { get; set; }

        /// <summary>
        ///     The number of simulation timesteps.
        /// </summary>
        public int NumTimesteps { get; set; }

        /// <summary>
        ///     The full, two-dimensional trajectory of the agent through the maze.
        /// </summary>
        public double[] AgentTrajectory { get; set; }
    }
}