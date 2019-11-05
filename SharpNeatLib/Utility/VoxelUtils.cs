using System;
using System.Linq;

namespace SharpNeat.Utility
{
    /// <summary>
    ///     Utility class that encapsulates voxel-specific computations, transformations and manipulations.
    /// </summary>
    public static class VoxelUtils
    {
        #region Helper methods

        /// <summary>
        ///     Computes a rank 3 tensor consisting of each voxel's distance to the voxel structure center-of-mass (geometric
        ///     centroid).
        /// </summary>
        /// <param name="x">The length of the X-axis on the voxel lattice.</param>
        /// <param name="y">The length of the Y-axis on the voxel lattice.</param>
        /// <param name="z">The length of the Z-axis on the voxel lattice.</param>
        /// <returns>
        ///     The rank 3 tensor consisting of each voxel's distance to the voxel structure center-of-mass (geometric
        ///     centroid).
        /// </returns>
        public static double[,,] ComputeVoxelDistanceMatrix(int lengthX, int lengthY, int lengthZ)
        {
            var xMatrix = new double[lengthX, lengthY, lengthZ];
            var yMatrix = new double[lengthX, lengthY, lengthZ];
            var zMatrix = new double[lengthX, lengthY, lengthZ];
            var l2NormMatrix = new double[lengthX, lengthY, lengthZ];

            // Record voxel ordinals for each axis independently
            for (var x = 0; x < lengthX; x++)
            {
                for (var y = 0; y < lengthY; y++)
                {
                    for (var z = 0; z < lengthZ; z++)
                    {
                        xMatrix[x, y, z] = x;
                        yMatrix[x, y, z] = y;
                        zMatrix[x, y, z] = z;
                    }
                }
            }

            // Normalize ordinal locations for each axis
            xMatrix = NormalizeMatrix(xMatrix);
            yMatrix = NormalizeMatrix(yMatrix);
            zMatrix = NormalizeMatrix(zMatrix);

            // Compute the L2 (euclidean) norm over all axes
            for (var x = 0; x < lengthX; x++)
            {
                for (var y = 0; y < lengthY; y++)
                {
                    for (var z = 0; z < lengthZ; z++)
                    {
                        l2NormMatrix[x, y, z] = Math.Sqrt(Math.Pow(xMatrix[x, y, z], 2) +
                                                          Math.Pow(yMatrix[x, y, z], 2) +
                                                          Math.Pow(zMatrix[x, y, z], 2));
                    }
                }
            }

            // Normalize L2 norm matrix to get per-voxel distances
            var voxelDistanceMatrix = NormalizeMatrix(l2NormMatrix);

            return voxelDistanceMatrix;
        }

        #endregion

        #region Internal helper methods

        /// <summary>
        ///     Normalizes the voxel distance matrix by scaling each distance value by the min/max distance boundaries.
        /// </summary>
        /// <param name="matrix">The voxel distance matrix to normalize.</param>
        /// <returns>The normalized (i.e. scaled by min/max boundaries) voxel distance matrix.</returns>
        private static double[,,] NormalizeMatrix(double[,,] matrix)
        {
            // Instantiate new normalized matrix of equivalent dimensionality
            var normMatrix = new double[matrix.GetLength(0), matrix.GetLength(1), matrix.GetLength(2)];

            // Get the single minimum and maximum values across all dimensions and compute the diff
            var min = matrix.Cast<double>().Min();
            var max = matrix.Cast<double>().Max();
            var diff = max - min;

            // Normalize each matrix dimension, scaling by the range of values in the matrix
            for (var x = 0; x < matrix.GetLength(0); x++)
            {
                for (var y = 0; y < matrix.GetLength(1); y++)
                {
                    for (var z = 0; z < matrix.GetLength(2); z++)
                    {
                        // Only scale by range of values if all values are NOT in the same range
                        if (diff > 0)
                        {
                            normMatrix[x, y, z] = 2 * (matrix[x, y, z] - min) / diff - 1;
                        }
                        // If there is no variation in range of values, avoid division by zero
                        else
                        {
                            normMatrix[x, y, z] = 2 * (matrix[x, y, z] - min) - 1;
                        }
                    }
                }
            }

            return normMatrix;
        }

        #endregion
    }
}