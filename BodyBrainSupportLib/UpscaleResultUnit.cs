namespace BodyBrainSupportLib
{
    /// <summary>
    ///     Encapsulates the maximum size to which the body substrate can be scaled and still be solvable by the paired brain.
    /// </summary>
    public struct UpscaleResultUnit
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
        ///     The size at which the body was solved during evolution.
        /// </summary>
        public int BaseSize { get; }

        /// <summary>
        ///     The maximum size to which the body can be scaled and the brain still able to ambulate it a sufficient distance to
        ///     meet the MC.
        /// </summary>
        public int MaxSize { get; }

        /// <summary>
        ///     UpscaleResultUnit constructor.
        /// </summary>
        /// <param name="brainId">The voxel brain genome ID.</param>
        /// <param name="bodyId">The voxel body genome ID.</param>
        /// <param name="baseSize">The size at which the body was solved during evolution.</param>
        /// <param name="maxSize">
        ///     The maximum size to which the body can be scaled and the brain still able to ambulate it a
        ///     sufficient distance to meet the MC.
        /// </param>
        public UpscaleResultUnit(uint brainId, uint bodyId, int baseSize, int maxSize)
        {
            BrainId = brainId;
            BodyId = bodyId;
            BaseSize = baseSize;
            MaxSize = maxSize;
        }
    }
}