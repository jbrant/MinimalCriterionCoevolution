#region

using System;

#endregion

namespace NavigatorMazeMapEvaluator
{
    public struct ExperimentParameters
    {
        public ExperimentParameters(int maxTimesteps, int minSuccessDistance, int? mazeHeight, int? mazeWidth,
            int? mazeScaleMultiplier)
        {
            if (null == mazeHeight)
            {
                throw new ArgumentNullException("mazeHeight");
            }

            if (null == mazeWidth)
            {
                throw new ArgumentNullException("mazeWidth");
            }

            if (null == mazeScaleMultiplier)
            {
                throw new ArgumentNullException("mazeScaleMultiplier");
            }

            MaxTimesteps = maxTimesteps;
            MinSuccessDistance = minSuccessDistance;
            MazeHeight = mazeHeight.Value;
            MazeWidth = mazeWidth.Value;
            MazeScaleMultiplier = mazeScaleMultiplier.Value;
        }

        public int MaxTimesteps { get; }
        public int MinSuccessDistance { get; }
        public int MazeHeight { get; }
        public int MazeWidth { get; }
        public int MazeScaleMultiplier { get; }
    }
}