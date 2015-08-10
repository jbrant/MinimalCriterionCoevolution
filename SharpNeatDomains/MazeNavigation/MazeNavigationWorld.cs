using System.Collections.Generic;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    internal class MazeNavigationWorld
    {
        #region Static fields

        private static readonly int MIN_SUCCESS_DISTANCE = 5;
        private static readonly int MAX_DISTANCE_TO_TARGET = 300;

        #endregion

        #region Instance fields

        /// <summary>
        ///     Location of the goal.
        /// </summary>
        private Point2D _goalLocation;

        #endregion

        #region Constructor

        public MazeNavigationWorld(MazeVariant mazeVariant)
        {
            if (mazeVariant == MazeVariant.MEDIUM_MAZE)
            {
                //TODO: Implement navigator with initial location and heading in the medium maze

                //TODO: Implement medium maze walls
            }
            else if (mazeVariant == MazeVariant.HARD_MAZE)
            {
                //TODO: Implement navigator with initial location and heading in the hard maze

                //TODO: Implement hard maze walls
            }
        }

        #endregion

        #region Instance properties

        public List<Line2D> Walls { get; private set; }

        #endregion

        public bool RunTrial(IBlackBox agent)
        {
            //TODO: Needs to actually be implemented.
            return false;
        }
    }
}