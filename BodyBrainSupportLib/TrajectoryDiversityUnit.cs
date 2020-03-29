namespace BodyBrainSupportLib
{
    /// <summary>
    ///     Encapsulates the distance between the current body/brain trajectory and end point compared to the same in other
    ///     members of the population.
    /// </summary>
    public struct TrajectoryDiversityUnit
    {
        /// <summary>
        ///     The voxel brain genome ID.
        /// </summary>
        public uint BrainId { get; }

        /// <summary>
        ///     The voxel body genome ID.
        /// </summary>
        public uint BodyId { get; }

        /// <summary>
        ///     The size of the voxel body.
        /// </summary>
        public int BodySize { get; }

        /// <summary>
        ///     The average distance of the full trajectory compared to that of other body/brains in the population.
        /// </summary>
        public double TrajectoryDiversity { get; }

        /// <summary>
        ///     The average distance between the end point and that of other body/brains in the population.
        /// </summary>
        public double EndPointDiversity { get; }

        /// <summary>
        ///     TrajectoryDiversityUnit constructor.
        /// </summary>
        /// <param name="brainId">The voxel brain genome ID.</param>
        /// <param name="bodyId">The voxel body genome ID.</param>
        /// <param name="bodySize">The size of the voxel body grid.</param>
        /// <param name="trajectoryDiversity">
        ///     The average distance of the full trajectory compared to that of other body/brains in
        ///     the population.
        /// </param>
        /// <param name="endPointDiversity">
        ///     The average distance between the end point and that of other body/brains in the
        ///     population.
        /// </param>
        public TrajectoryDiversityUnit(uint brainId, uint bodyId, int bodySize, double trajectoryDiversity,
            double endPointDiversity)
        {
            BrainId = brainId;
            BodyId = bodyId;
            BodySize = bodySize;
            TrajectoryDiversity = trajectoryDiversity;
            EndPointDiversity = endPointDiversity;
        }
    }
}