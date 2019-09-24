#region

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MCC_Domains.Common;
using MCC_Domains.MazeNavigation.Components;
using SharpNeat.Core;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MCC_Domains.MazeNavigation
{
    /// <summary>
    ///     The multi maze navigation world factory facilitates generating maze navigation worlds based on evolved maze
    ///     structures.
    /// </summary>
    public class MultiMazeNavigationWorldFactory
    {
        #region Properties

        /// <summary>
        ///     The total number of distinct mazes that the factory can currently produce.
        /// </summary>
        public int NumMazes { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructor which sets the given minimum distance for success and maximum distance to target.
        /// </summary>
        /// <param name="minSuccessDistance">The minimum distance from the target location to be considered a success.</param>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        public MultiMazeNavigationWorldFactory(int minSuccessDistance, int maxDistanceToTarget)
        {
            _minSuccessDistance = minSuccessDistance;
            _maxDistanceToTarget = maxDistanceToTarget;

            // Initialize the internal collection of maze configurations
            _mazeConfigurations = new ConcurrentDictionary<uint, MazeConfiguration>();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor which sets the given minimum distance for success and omits the maximum distance to target.
        /// </summary>
        /// <param name="minSuccessDistance">The minimum distance from the target location to be considered a success.</param>
        public MultiMazeNavigationWorldFactory(int minSuccessDistance)
            : this(minSuccessDistance, 0)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Converts the given maze structure into domain-specific maze configurations.
        /// </summary>
        /// <param name="mazeStructures">The evolved maze structures.</param>
        public void SetMazeConfigurations(IList<MazeStructure> mazeStructures)
        {
            // Convert maze structure into experiment domain-specific maze, but only run conversion 
            // if maze hasn't been cached from a previous conversion
            foreach (
                var mazeStructure in
                mazeStructures.Where(
                        mazeStructure => _mazeConfigurations.ContainsKey(mazeStructure.GenomeId) == false)
                    .AsParallel())
            {
                _mazeConfigurations.Add(mazeStructure.GenomeId,
                    new MazeConfiguration(ExtractMazeWalls(mazeStructure.Walls),
                        ExtractStartEndPoint(mazeStructure.ScaledStartLocation),
                        ExtractStartEndPoint(mazeStructure.ScaledTargetLocation), mazeStructure.MaxTimesteps));
            }

            // Build the list of current maze genome IDs
            var curIds = mazeStructures.Select(mazeStructure => mazeStructure.GenomeId);

            // Remove mazes that are no longer in the current population of mazes
            foreach (var key in _mazeConfigurations.Keys.Where(key => curIds.Contains(key) == false).AsParallel())
            {
                _mazeConfigurations.Remove(key);
            }

            // Set the number of mazes in the factory
            NumMazes = _mazeConfigurations.Count;

            // Store off keys so we have a static mapping between list indexes and dictionary keys
            _currentKeys = _mazeConfigurations.Keys.ToList();
        }

        /// <summary>
        ///     Sets the viability usage count on maze structure phenomes based on the corresponding successful navigation count of
        ///     corresponding maze configurations.
        /// </summary>
        /// <param name="mazeStructures">The maze structure phenomes.</param>
        public void UpdateMazePhenomeUsage(IList<MazeStructure> mazeStructures)
        {
            foreach (var mazeStructure in mazeStructures)
            {
                mazeStructure.ViabilityUsageCount =
                    _mazeConfigurations[mazeStructure.GenomeId].SuccessfulNavigationCount;
            }
        }

        /// <summary>
        ///     Indicates whether the maze corresponding to the given genome index is under its resource limit (an upper bound on
        ///     the
        ///     number of times successful navigations of a maze can be attributable toward satisfying an agent MC).
        /// </summary>
        /// <param name="genomeIdx">The index of the genome corresponding to the maze configuration navigated.</param>
        /// <param name="resourceLimit">
        ///     The number of times a maze can be used for successful navigations that contribute to
        ///     meeting an agent's MC.
        /// </param>
        /// <returns>Boolean flag indicating whether the maze has reached its resource limit.</returns>
        public bool IsMazeUnderResourceLimit(int genomeIdx, int resourceLimit)
        {
            return _mazeConfigurations[_currentKeys[genomeIdx]].SuccessfulNavigationCount < resourceLimit;
        }

        /// <summary>
        ///     Increments the number of times the given maze configuration has been successfully evaluated by an agent.
        /// </summary>
        /// <param name="genomeIdx">The index of the genome corresponding to the maze configuration navigated.</param>
        public void IncrementSuccessfulMazeNavigationCount(int genomeIdx)
        {
            _mazeConfigurations[_currentKeys[genomeIdx]].IncrementSuccessfulNavigation();
        }

        /// <summary>
        ///     Looks up and returns the maze genome ID at the given index.
        /// </summary>
        /// <param name="genomeIdx">The index of the genome ID.</param>
        /// <returns>The genome ID at the given index.</returns>
        public uint GetMazeGenomeId(int genomeIdx)
        {
            return _currentKeys[genomeIdx];
        }

        /// <summary>
        ///     Constructs a new maze navigation world using the maze at the specified index and a given behavior characterization.
        /// </summary>
        /// <param name="genomeIdx">The genome index of the maze configuration to use in the maze navigation world.</param>
        /// <param name="behaviorCharacterization">
        ///     The way in which an agents behavior is characterized (i.e. end point,
        ///     trajectory, etc.).
        /// </param>
        /// <returns>A constructed maze navigation world ready for evaluation.</returns>
        public MazeNavigationWorld CreateMazeNavigationWorld(int genomeIdx,
            IBehaviorCharacterization behaviorCharacterization)
        {
            // Get the maze configuration corresponding to the genome ID at the given index
            var mazeConfig = _mazeConfigurations[_currentKeys[genomeIdx]];

            // Create maze navigation world and return
            return new MazeNavigationWorld(mazeConfig.Walls, mazeConfig.NavigatorLocation,
                mazeConfig.GoalLocation, _minSuccessDistance, _maxDistanceToTarget, mazeConfig.MaxSimulationTimesteps,
                behaviorCharacterization);
        }

        /// <summary>
        ///     Constructs a new maze navigation world using the given maze structure and behavior characterization.
        /// </summary>
        /// <param name="mazeStructure">The maze structure to convert into a maze configuration.</param>
        /// <param name="behaviorCharacterization">
        ///     The way in which an agents behavior is characterized (i.e. end point,
        ///     trajectory, etc.).
        /// </param>
        /// <returns>A constructed maze navigation world ready for evaluation.</returns>
        public MazeNavigationWorld CreateMazeNavigationWorld(MazeStructure mazeStructure,
            IBehaviorCharacterization behaviorCharacterization)
        {
            // Build the single maze configuration
            var mazeConfig = new MazeConfiguration(ExtractMazeWalls(mazeStructure.Walls),
                ExtractStartEndPoint(mazeStructure.ScaledStartLocation), ExtractStartEndPoint(mazeStructure.ScaledTargetLocation),
                mazeStructure.MaxTimesteps);

            // Create maze navigation world and return
            return new MazeNavigationWorld(mazeConfig.Walls, mazeConfig.NavigatorLocation,
                mazeConfig.GoalLocation, _minSuccessDistance, _maxDistanceToTarget, mazeConfig.MaxSimulationTimesteps,
                behaviorCharacterization);
        }

        /// <summary>
        ///     Constructs a new maze navigation world with a null behavior characterization reference and using the first maze
        ///     configuration in the collection of maze configurations.
        /// </summary>
        /// <param name="behaviorCharacterization">
        ///     The way in which an agents behavior is characterized (i.e. end point,
        ///     trajectory, etc.).
        /// </param>
        /// <returns>A constructed maze navigation world ready for evaluation.</returns>
        public MazeNavigationWorld CreateMazeNavigationWorld(
            IBehaviorCharacterization behaviorCharacterization = null)
        {
            return new MazeNavigationWorld(_mazeConfigurations[_currentKeys[0]].Walls,
                _mazeConfigurations[_currentKeys[0]].NavigatorLocation,
                _mazeConfigurations[_currentKeys[0]].GoalLocation, _minSuccessDistance, _maxDistanceToTarget,
                _mazeConfigurations[_currentKeys[0]].MaxSimulationTimesteps, behaviorCharacterization);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Converts the evolved walls into experiment domain walls so that experiment-specific calculations can be applied on
        ///     them.
        /// </summary>
        /// <param name="mazeStructureWalls">The evolved walls.</param>
        /// <returns>List of the experiment-specific walls.</returns>
        private static List<Wall> ExtractMazeWalls(List<MazeStructureWall> mazeStructureWalls)
        {
            var mazeWalls = new List<Wall>(mazeStructureWalls.Count);

            // Convert each of the maze structure walls to the experiment domain wall
            mazeWalls.AddRange(
                mazeStructureWalls.Select(
                    mazeStructureWall =>
                        new Wall(new DoubleLine(mazeStructureWall.StartMazeStructurePoint.X,
                            mazeStructureWall.StartMazeStructurePoint.Y,
                            mazeStructureWall.EndMazeStructurePoint.X, mazeStructureWall.EndMazeStructurePoint.Y))));

            return mazeWalls;
        }

        /// <summary>
        ///     Converts evolved point (start or finish) to experiment domain point for the navigator start location and the target
        ///     (goal).
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>The domain-specific point object.</returns>
        private static DoublePoint ExtractStartEndPoint(MazeStructurePoint point)
        {
            return new DoublePoint(point.X, point.Y);
        }

        #endregion

        #region Instance Variables

        /// <summary>
        ///     Minimum distance from the target for the evaluation to be considered a success.
        /// </summary>
        private readonly int _minSuccessDistance;

        /// <summary>
        ///     Maximum distance to the target (i.e. goal location).
        /// </summary>
        private readonly int _maxDistanceToTarget;

        /// <summary>
        ///     Stores collection of maze configurations, indexed by the originating phenotype's corresponding genotype ID.  This
        ///     allows quick lookup to determine whether a maze needs to be converted or if that operation has already been
        ///     performed.
        /// </summary>
        private readonly IDictionary<uint, MazeConfiguration> _mazeConfigurations;

        /// <summary>
        ///     List of keys for the maze configuration dictionary.  This permits an ordering on the keys without having to
        ///     build a concurrent, sorted dictionary.
        /// </summary>
        private IList<uint> _currentKeys;

        #endregion
    }
}