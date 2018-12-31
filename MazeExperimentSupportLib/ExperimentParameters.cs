#region

using System;

#endregion

namespace MazeExperimentSupportLib
{
    public struct ExperimentParameters
    {
        public ExperimentParameters(int maxTimesteps, int minSuccessDistance, int? mazeHeight, int? mazeWidth, int? mazeQuadrantHeight, int? mazeQuadrantWidth,
            int? mazeScaleMultiplier)
        {
            if (null == mazeHeight)
            {
                throw new ArgumentNullException(nameof(mazeHeight));
            }

            if (null == mazeWidth)
            {
                throw new ArgumentNullException(nameof(mazeWidth));
            }

            if (null == mazeQuadrantHeight)
            {
                throw new ArgumentNullException(nameof(mazeQuadrantHeight));
            }

            if (null == mazeQuadrantWidth)
            {
                throw new ArgumentNullException(nameof(mazeQuadrantWidth));
            }

            if (null == mazeScaleMultiplier)
            {
                throw new ArgumentNullException(nameof(mazeScaleMultiplier));
            }

            MaxTimesteps = maxTimesteps;
            MinSuccessDistance = minSuccessDistance;
            MazeHeight = mazeHeight.Value;
            MazeWidth = mazeWidth.Value;
            MazeQuadrantHeight = mazeQuadrantHeight.Value;
            MazeQuadrantWidth = mazeQuadrantWidth.Value;
            MazeScaleMultiplier = mazeScaleMultiplier.Value;
        }

        public int MaxTimesteps { get; }
        public int MinSuccessDistance { get; }
        public int MazeHeight { get; }
        public int MazeWidth { get; }
        public int MazeQuadrantHeight { get; }
        public int MazeQuadrantWidth { get; }
        public int MazeScaleMultiplier { get; }
    }
}