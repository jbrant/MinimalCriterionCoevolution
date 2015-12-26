#region

using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;

#endregion

namespace SharpNeat.Domains.MazeNavigation
{
    /// <summary>
    ///     The purpose of this class is to capture properties of the maze domain that are common throughout evolution and
    ///     generate separate worlds per evaluation without going through the initialization process.
    /// </summary>
    public class MazeNavigationWorldFactory<TTrialInfo>
    {
        #region Constructors

        /// <summary>
        ///     Constructor which sets all simulation properties as well as the maze walls/boundaries and bridging parameters.
        /// </summary>
        /// <param name="mazeVariant">The maze environment used for the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="maxTimeSteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="bridgingMagnitude">The degree heading adjustment imposed by bridging.</param>
        /// <param name="numBridgingApplications">The maximum number of times bridging heading adjustment can be applied.</param>
        public MazeNavigationWorldFactory(MazeVariant mazeVariant, int minSuccessDistance,
            int maxDistanceToTarget, int maxTimeSteps, int bridgingMagnitude = 0,
            int numBridgingApplications = 0)
        {
            _mazeVariant = mazeVariant;
            _minSuccessDistance = minSuccessDistance;
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimeSteps;
            _numBridgingApplications = numBridgingApplications;

            switch (mazeVariant)
            {
                // Setup appropriate parameters for medium maze
                case MazeVariant.MediumMaze:
                    // Initialize the navigator starting location
                    _navigatorLocation = new DoublePoint(30, 22);

                    // Initialize the goal location
                    _goalLocation = new DoublePoint(270, 100);

                    // Define all of the maze walls
                    _walls = new List<Wall>(11)
                    {
                        // TODO: Set appropriate adjustment coefficients for the walls
                        new Wall(new DoubleLine(293, 7, 289, 130), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(289, 130, 6, 134), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(6, 134, 8, 5), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(8, 5, 292, 7), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(241, 130, 58, 65), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(114, 7, 73, 42), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(130, 91, 107, 46), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(196, 8, 139, 51), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(219, 122, 182, 63), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(267, 9, 214, 63), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(271, 129, 237, 88), 1, 1, bridgingMagnitude)
                    };
                    break;

                // Setup appropriate parameters for hard maze
                case MazeVariant.HardMaze:
                    // Initialize the navigator starting location
                    _navigatorLocation = new DoublePoint(36, 184);

                    // Initialize the goal location
                    _goalLocation = new DoublePoint(31, 20);

                    // Define all of the maze walls
                    _walls = new List<Wall>(13)
                    {
                        // Boundary walls
                        new Wall(new DoubleLine(7, 202, 195, 198), -1, -1, bridgingMagnitude),
                        new Wall(new DoubleLine(41, 5, 3, 8), -1, -1, bridgingMagnitude),
                        new Wall(new DoubleLine(3, 8, 4, 49), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(4, 49, 7, 202), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(195, 198, 186, 8), -1, -1, bridgingMagnitude),
                        new Wall(new DoubleLine(186, 8, 39, 5), -1, -1, bridgingMagnitude),

                        // Obstructing walls
                        new Wall(new DoubleLine(4, 49, 57, 53), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(56, 54, 56, 157), -1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(57, 106, 158, 162), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(77, 201, 108, 164), -1, -1, bridgingMagnitude),
                        new Wall(new DoubleLine(6, 80, 33, 121), -1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(192, 146, 87, 91), -1, -1, bridgingMagnitude),
                        new Wall(new DoubleLine(56, 55, 133, 30), 1, 1, bridgingMagnitude)
                    };
                    break;

                // Setup appropriate parameters for open-ended medium maze
                case MazeVariant.OpenEndedMediumMaze:
                    // Initialize the navigator starting location
                    _navigatorLocation = new DoublePoint(30, 22);

                    // Initialize the goal location
                    _goalLocation = new DoublePoint(270, 100);

                    // Define all of the maze walls
                    _walls = new List<Wall>(10)
                    {
                        // TODO: Set appropriate adjustment coefficients for the walls
                        new Wall(new DoubleLine(293, 7, 289, 130), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(289, 130, 6, 134), 1, 1, bridgingMagnitude),

                        // Left wall is missing here

                        new Wall(new DoubleLine(8, 5, 292, 7), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(241, 130, 58, 65), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(114, 7, 73, 42), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(130, 91, 107, 46), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(196, 8, 139, 51), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(219, 122, 182, 63), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(267, 9, 214, 63), 1, 1, bridgingMagnitude),
                        new Wall(new DoubleLine(271, 129, 237, 88), 1, 1, bridgingMagnitude)
                    };
                    break;

                // Setup appropriate parameters for open-ended hard maze
                case MazeVariant.OpenEndedHardMaze:
                    // TODO: Need to implement this
                    break;
            }
        }

        /// <summary>
        ///     Constructor which sets all simulation properties as well as the maze walls/boundaries and bridging parameters.
        /// </summary>
        /// <param name="mazeVariant">The maze environment used for the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="maxTimeSteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="gridDensity">The density of the niche grid.</param>
        public MazeNavigationWorldFactory(MazeVariant mazeVariant, int minSuccessDistance,
            int maxDistanceToTarget, int maxTimeSteps, uint gridDensity)
            : this(mazeVariant, minSuccessDistance, maxDistanceToTarget, maxTimeSteps, 0, 0)
        {
            // Instantiate the grid of behavioral niches
            _mazeNicheGrid = new MazeNicheGrid(gridDensity, _walls);
        }

        #endregion

        #region Public methods

        /// <summary>
        ///     Instantiates a new maze navigation world.
        /// </summary>
        /// <returns>A constructed maze navigation world ready for evaluation.</returns>
        public MazeNavigationWorld<TTrialInfo> CreateMazeNavigationWorld()
        {
            return new MazeNavigationWorld<TTrialInfo>(_walls, _mazeNicheGrid, _navigatorLocation, _goalLocation,
                _minSuccessDistance, _maxDistanceToTarget, _maxTimesteps, _numBridgingApplications);
        }

        /// <summary>
        ///     Instantiates a new maze navigation world.
        /// </summary>
        /// <param name="behaviorCharacterization">
        ///     The way in which an agents behavior is characterized (i.e. end point,
        ///     trajectory, etc.).
        /// </param>
        /// <returns>A constructed maze navigation world ready for evaluation.</returns>
        public MazeNavigationWorld<TTrialInfo> CreateMazeNavigationWorld(
            IBehaviorCharacterization behaviorCharacterization)
        {
            return new MazeNavigationWorld<TTrialInfo>(_walls, _mazeNicheGrid, _navigatorLocation, _goalLocation,
                _minSuccessDistance, _maxDistanceToTarget, _maxTimesteps, _numBridgingApplications,
                behaviorCharacterization);
        }

        /// <summary>
        ///     Instantiates a new maze navigation world.
        /// </summary>
        /// <param name="behaviorCharacterization">
        ///     The way in which an agents behavior is characterized (i.e. end point,
        ///     trajectory, etc.).
        /// </param>
        /// <param name="isBridgingEvaluation">Indicates whether the current evaluation should use bridging or not.</param>
        /// <returns>A constructed maze navigation world ready for evaluation.</returns>
        public MazeNavigationWorld<TTrialInfo> CreateMazeNavigationWorld(
            IBehaviorCharacterization behaviorCharacterization, bool isBridgingEvaluation)
        {
            return new MazeNavigationWorld<TTrialInfo>(_walls, _mazeNicheGrid, _navigatorLocation, _goalLocation,
                _minSuccessDistance, _maxDistanceToTarget, _maxTimesteps,
                isBridgingEvaluation ? _numBridgingApplications : 0,
                behaviorCharacterization);
        }

        #endregion

        #region Private members

        /// <summary>
        ///     Maximum distance to the target (i.e. goal location).
        /// </summary>
        private readonly int _maxDistanceToTarget;

        /// <summary>
        ///     Maximum timesteps for the trial to run.
        /// </summary>
        private readonly int _maxTimesteps;

        /// <summary>
        ///     Minimum distance from the target for the evaluation to be considered a success.
        /// </summary>
        private readonly int _minSuccessDistance;

        /// <summary>
        ///     The number of times within a given trial to apply bridging "help".
        /// </summary>
        private readonly int _numBridgingApplications;

        /// <summary>
        ///     List of walls in the environment.
        /// </summary>
        private readonly List<Wall> _walls;

        /// <summary>
        ///     The maze environment to be used in the simulation.
        /// </summary>
        private readonly MazeVariant _mazeVariant;

        /// <summary>
        ///     Encapsulates the maze boundaries and supporting methods for mapping ending locations into the niche grid.
        /// </summary>
        private readonly MazeNicheGrid _mazeNicheGrid;

        /// <summary>
        ///     The starting location of the maze navigator.
        /// </summary>
        private readonly DoublePoint _navigatorLocation;

        /// <summary>
        ///     The location of the goal (target).
        /// </summary>
        private readonly DoublePoint _goalLocation;

        #endregion
    }
}