#region

using System;

#endregion

namespace MazeExperimentSupportLib
{
    public struct ExperimentParameters
    {
        public ExperimentParameters(int maxTimesteps, int minSuccessDistance, int? mazeHeight, int? mazeWidth, int? mazeQuadrantHeight, int? mazeQuadrantWidth,
            int? mazeScaleMultiplier, string activationScheme, int? activationIters, double? activationDeltaThreshold)
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
            ActivationScheme = activationScheme;
            ActivationIters = activationIters ?? 0;
            ActivationDeltaThreshold = activationDeltaThreshold ?? 0;
        }

        public int MaxTimesteps { get; }
        public int MinSuccessDistance { get; }
        public int MazeHeight { get; }
        public int MazeWidth { get; }
        public int MazeQuadrantHeight { get; }
        public int MazeQuadrantWidth { get; }
        public int MazeScaleMultiplier { get; }
        public string ActivationScheme { get; }
        public int ActivationIters { get; }
        public double ActivationDeltaThreshold { get; }
    }
}