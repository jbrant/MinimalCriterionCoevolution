// This is a test of setting the file header.

#region

using System.Collections.Generic;
using System.Linq;
using SharpNeat.Genomes.Maze;
using SharpNeat.Loggers;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.EvolutionAlgorithms.Statistics
{
    /// <summary>
    ///     Encapsulates descriptive statistics about the extant population of maze genomes.
    /// </summary>
    public class MazeAlgorithmStats : AbstractEvolutionaryAlgorithmStats
    {
        #region Constructor

        /// <summary>
        ///     MazeAlgorithmStats constructor.
        /// </summary>
        /// <param name="eaParams">Evolution algorithm parameters required for initialization.</param>
        public MazeAlgorithmStats(EvolutionAlgorithmParameters eaParams) : base(eaParams)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The minimum number of walls in a maze within the maze population.
        /// </summary>
        public int MinWalls { get; private set; }

        /// <summary>
        ///     The maximum number of walls in a maze within the maze population.
        /// </summary>
        public int MaxWalls { get; private set; }

        /// <summary>
        ///     The mean number of walls among mazes within the maze population.
        /// </summary>
        public double MeanWalls { get; private set; }

        /// <summary>
        ///     The minimum number of waypoints in a maze within the maze population.
        /// </summary>
        public int MinWaypoints { get; private set; }

        /// <summary>
        ///     The maximum number of waypoints in a maze within the maze population.
        /// </summary>
        public int MaxWaypoints { get; private set; }

        /// <summary>
        ///     The mean number of waypoints among mazes within the maze population.
        /// </summary>
        public double MeanWaypoints { get; private set; }

        /// <summary>
        ///     The minimum number of junctures in a maze within the maze population.
        /// </summary>
        public int MinJunctures { get; private set; }

        /// <summary>
        ///     The maximum number of junctures in a maze within the maze population.
        /// </summary>
        public int MaxJunctures { get; private set; }

        /// <summary>
        ///     The mean number of junctures among mazes within the maze population.
        /// </summary>
        public double MeanJunctures { get; private set; }

        /// <summary>
        ///     The minimum number of openings facing the trajectory in a maze within the maze population.
        /// </summary>
        public int MinTrajectoryFacingOpenings { get; private set; }

        /// <summary>
        ///     The maximum number of openings facing the trajectory in a maze within the maze population.
        /// </summary>
        public int MaxTrajectoryFacingOpenings { get; private set; }

        /// <summary>
        ///     The mean number of openings facing the trajectory among mazes within the maze population.
        /// </summary>
        public double MeanTrajectoryFacingOpenings { get; private set; }

        /// <summary>
        ///     The minimum height of a maze within the maze population.
        /// </summary>
        public int MinHeight { get; private set; }

        /// <summary>
        ///     The maximum height of a maze within the maze population.
        /// </summary>
        public int MaxHeight { get; private set; }

        /// <summary>
        ///     The mean height among mazes within the maze population.
        /// </summary>
        public double MeanHeight { get; private set; }

        /// <summary>
        ///     The minimum width of a maze within the maze population.
        /// </summary>
        public int MinWidth { get; private set; }

        /// <summary>
        ///     The maximum width of a maze within the maze population.
        /// </summary>
        public int MaxWidth { get; private set; }

        /// <summary>
        ///     The mean width among mazes within the maze population.
        /// </summary>
        public double MeanWidth { get; private set; }

        #endregion
        
        #region Method overrides
        
        /// <summary>
        ///     Computes maze genome implementation-specific details about the population.
        /// </summary>
        /// <param name="population">The maze population from which to compute more specific, descriptive statistics.</param>
        public override void ComputeAlgorithmSpecificPopulationStats<TGenome>(IList<TGenome> population)
        {
            // Ensure that population list contains maze genomes, otherwise return
            if ((population is IList<MazeGenome>) == false)
                return;

            // Cast to maze genomes
            var mazePopulation = (IList<MazeGenome>) population;

            // Compute wall statistics
            MinWalls = mazePopulation.Min(g => g.WallGeneList.Count);
            MaxWalls = mazePopulation.Max(g => g.WallGeneList.Count);
            MeanWalls = mazePopulation.Average(g => g.WallGeneList.Count);

            // Compute waypoint statistics
            MinWaypoints = mazePopulation.Min(g => g.PathGeneList.Count);
            MaxWaypoints = mazePopulation.Max(g => g.PathGeneList.Count);
            MeanWaypoints = mazePopulation.Average(g => g.PathGeneList.Count);

            // Compute junctures
            MinJunctures = mazePopulation.Min(MazeUtils.GetNumJunctures);
            MaxJunctures = mazePopulation.Max(MazeUtils.GetNumJunctures);
            MeanJunctures = mazePopulation.Average(MazeUtils.GetNumJunctures);

            // Compute trajectories facing opening
            MinTrajectoryFacingOpenings = mazePopulation.Min(MazeUtils.GetNumPathFacingRoomOpenings);
            MaxTrajectoryFacingOpenings = mazePopulation.Max(MazeUtils.GetNumPathFacingRoomOpenings);
            MeanTrajectoryFacingOpenings = mazePopulation.Average(MazeUtils.GetNumPathFacingRoomOpenings);

            // Compute maze dimension statistics
            MinHeight = mazePopulation.Min(g => g.MazeBoundaryHeight);
            MaxHeight = mazePopulation.Max(g => g.MazeBoundaryHeight);
            MeanHeight = mazePopulation.Average(g => g.MazeBoundaryHeight);
            MinWidth = mazePopulation.Min(g => g.MazeBoundaryWidth);
            MaxWidth = mazePopulation.Max(g => g.MazeBoundaryWidth);
            MeanWidth = mazePopulation.Average(g => g.MazeBoundaryWidth);
        }

        /// <summary>
        ///     Returns the fields within MazeAlgorithmStats that are enabled for logging.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable fields within MazeAlgorithmStats.</returns>
        public override List<LoggableElement> GetLoggableElements(
            IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            var elements = base.GetLoggableElements(logFieldEnableMap);

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinWalls) == true &&
                logFieldEnableMap[EvolutionFieldElements.MinWalls])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MinWalls, MinWalls));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxWalls) == true &&
                logFieldEnableMap[EvolutionFieldElements.MaxWalls])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MaxWalls, MaxWalls));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanWalls) == true &&
                logFieldEnableMap[EvolutionFieldElements.MeanWalls])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MeanWalls, MeanWalls));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinWaypoints) == true &&
                logFieldEnableMap[EvolutionFieldElements.MinWaypoints])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MinWaypoints, MinWaypoints));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxWaypoints) == true &&
                logFieldEnableMap[EvolutionFieldElements.MaxWaypoints])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MaxWaypoints, MaxWaypoints));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanWaypoints) == true &&
                logFieldEnableMap[EvolutionFieldElements.MeanWaypoints])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MeanWaypoints, MeanWaypoints));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinJunctures) == true &&
                logFieldEnableMap[EvolutionFieldElements.MinJunctures])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MinJunctures, MinJunctures));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxJunctures) == true &&
                logFieldEnableMap[EvolutionFieldElements.MaxJunctures])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MaxJunctures, MaxJunctures));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanJunctures) == true &&
                logFieldEnableMap[EvolutionFieldElements.MeanJunctures])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MeanJunctures, MeanJunctures));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinTrajectoryFacingOpenings) == true &&
                logFieldEnableMap[EvolutionFieldElements.MinTrajectoryFacingOpenings])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MinTrajectoryFacingOpenings,
                    MinTrajectoryFacingOpenings));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxTrajectoryFacingOpenings) == true &&
                logFieldEnableMap[EvolutionFieldElements.MaxTrajectoryFacingOpenings])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MaxTrajectoryFacingOpenings,
                    MaxTrajectoryFacingOpenings));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanTrajectoryFacingOpenings) == true &&
                logFieldEnableMap[EvolutionFieldElements.MeanTrajectoryFacingOpenings])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MeanTrajectoryFacingOpenings,
                    MeanTrajectoryFacingOpenings));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinHeight) == true &&
                logFieldEnableMap[EvolutionFieldElements.MinHeight])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MinHeight, MinHeight));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxHeight) == true &&
                logFieldEnableMap[EvolutionFieldElements.MaxHeight])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MaxHeight, MaxHeight));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanHeight) == true &&
                logFieldEnableMap[EvolutionFieldElements.MeanHeight])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MeanHeight, MeanHeight));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MinWidth) == true &&
                logFieldEnableMap[EvolutionFieldElements.MinWidth])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MinWidth, MinWidth));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MaxWidth) == true &&
                logFieldEnableMap[EvolutionFieldElements.MaxWidth])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MaxWidth, MaxWidth));
            }

            if (logFieldEnableMap?.ContainsKey(EvolutionFieldElements.MeanWidth) == true &&
                logFieldEnableMap[EvolutionFieldElements.MeanWidth])
            {
                elements.Add(new LoggableElement(EvolutionFieldElements.MeanWidth, MeanWidth));
            }

            return elements;
        }
        
        #endregion
    }
}