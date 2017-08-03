#region

using System.Collections.Generic;
using MCC_Domains.MazeNavigation.Components;
using SharpNeat.Domains;

#endregion

namespace MCC_Domains.MazeNavigation
{
    /// <summary>
    ///     The maze configuration struct encapsulates domain/experiment native representation of maze walls and the
    ///     starting/ending locations.
    /// </summary>
    public struct MazeConfiguration
    {
        /// <summary>
        ///     The location of the goal.
        /// </summary>
        public readonly DoublePoint GoalLocation;

        /// <summary>
        ///     The maximum time allotted for the simulation on this maze.
        /// </summary>
        public readonly int MaxSimulationTimesteps;

        /// <summary>
        ///     The starting location of the maze navigator.
        /// </summary>
        public readonly DoublePoint NavigatorLocation;

        /// <summary>
        ///     The list of walls in the maze.
        /// </summary>
        public readonly List<Wall> Walls;

        /// <summary>
        ///     Constructor which accepts the list of walls, navigator starting location, and the goal location.
        /// </summary>
        /// <param name="walls">The list of walls in the maze.</param>
        /// <param name="navigatorLocation">The starting location of the maze navigator.</param>
        /// <param name="goalLocation">The location of the goal.</param>
        /// <param name="maxSimulationTimesteps">The maximum time allotted for the simulation on this maze.</param>
        public MazeConfiguration(List<Wall> walls, DoublePoint navigatorLocation, DoublePoint goalLocation,
            int maxSimulationTimesteps)
        {
            Walls = walls;
            NavigatorLocation = navigatorLocation;
            GoalLocation = goalLocation;
            MaxSimulationTimesteps = maxSimulationTimesteps;
        }
    }
}