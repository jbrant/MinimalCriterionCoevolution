﻿namespace SharpNeat.Core
{
    /// <summary>
    ///     Interface for unifying fitness and behavior information.
    /// </summary>
    public interface ITrialInfo
    {
        /// <summary>
        ///     The genotypic, phenotypic, or behavioral niche into which the organism under evaluation maps based on the
        ///     evaluation.
        /// </summary>
        int NicheId { get; set; }

        /// <summary>
        ///     Indicates the distance (in a euclidean since or otherwise) to the objective (i.e. target).
        /// </summary>
        double ObjectiveDistance { get; set; }
    }
}