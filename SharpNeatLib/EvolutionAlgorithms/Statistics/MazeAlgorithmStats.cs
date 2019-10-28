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

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MinWalls) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MinWalls])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MinWalls, MinWalls));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MaxWalls) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MaxWalls])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MaxWalls, MaxWalls));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MeanWalls) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MeanWalls])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MeanWalls, MeanWalls));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MinWaypoints) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MinWaypoints])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MinWaypoints, MinWaypoints));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MaxWaypoints) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MaxWaypoints])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MaxWaypoints, MaxWaypoints));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MeanWaypoints) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MeanWaypoints])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MeanWaypoints, MeanWaypoints));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MinJunctures) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MinJunctures])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MinJunctures, MinJunctures));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MaxJunctures) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MaxJunctures])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MaxJunctures, MaxJunctures));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MeanJunctures) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MeanJunctures])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MeanJunctures, MeanJunctures));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MinTrajectoryFacingOpenings) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MinTrajectoryFacingOpenings])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MinTrajectoryFacingOpenings,
                    MinTrajectoryFacingOpenings));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MaxTrajectoryFacingOpenings) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MaxTrajectoryFacingOpenings])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MaxTrajectoryFacingOpenings,
                    MaxTrajectoryFacingOpenings));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MeanTrajectoryFacingOpenings) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MeanTrajectoryFacingOpenings])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MeanTrajectoryFacingOpenings,
                    MeanTrajectoryFacingOpenings));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MinHeight) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MinHeight])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MinHeight, MinHeight));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MaxHeight) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MaxHeight])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MaxHeight, MaxHeight));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MeanHeight) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MeanHeight])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MeanHeight, MeanHeight));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MinWidth) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MinWidth])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MinWidth, MinWidth));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MaxWidth) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MaxWidth])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MaxWidth, MaxWidth));
            }

            if (logFieldEnableMap?.ContainsKey(MazeNavEvolutionFieldElements.MeanWidth) == true &&
                logFieldEnableMap[MazeNavEvolutionFieldElements.MeanWidth])
            {
                elements.Add(new LoggableElement(MazeNavEvolutionFieldElements.MeanWidth, MeanWidth));
            }

            return elements;
        }
        
        #endregion
    }
}