using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SharpNeat.Utility;

namespace SharpNeat.Phenomes.Voxels
{
    /// <summary>
    ///     Encapsulates frequency and phase offsets for voxel brains corresponding to each voxel in a body.
    /// </summary>
    public class VoxelPhaseOffsetBrain : IVoxelBrain
    {
        #region Instance variables

        /// <summary>
        ///     The layer-wise list of phase-offset values for each voxel in the voxel body.
        /// </summary>
        private readonly IList<IList<double>> _voxelCellPhaseOffsets;

        #endregion

        #region Constructor

        public VoxelPhaseOffsetBrain(IBlackBox cppn, int substrateX, int substrateY, int substrateZ)
        {
            // Activate CPPN for all positions on the substrate to get the oscillation frequency and
            // per-voxel phase offset values
            ExtractVoxelFrequencyPhaseOffsets(cppn, substrateX, substrateY, substrateZ, out var frequency,
                out var phaseOffsets);

            Frequency = frequency;
            _voxelCellPhaseOffsets = phaseOffsets;

            // Carry through the genome ID from the generate genome
            GenomeId = cppn.GenomeId;
        }

        #endregion

        #region Public methods

        /// <summary>
        ///     Returns the phase offset values for the specified layer.
        /// </summary>
        /// <param name="layer">The layer index for which to retrieve the phase offset values.</param>
        /// <returns>
        ///     The comma-delimited string of phase offset values for all voxel-specific controllers in the given layer.
        /// </returns>
        public string GetFlattenedLayerData(int layer)
        {
            return string.Join(",", _voxelCellPhaseOffsets[layer].Select(x => x));
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Activates the CPPN for every position on the substrate to output the phase offset value for every voxel.
        /// </summary>
        /// <param name="cppn">The CPPN coding for the voxel brain.</param>
        /// <param name="substrateX">The substrate resolution along the X dimension.</param>
        /// <param name="substrateY">The substrate resolution along the Y dimension.</param>
        /// <param name="substrateZ">The substrate resolution along the Z dimension.</param>
        /// <returns>The layer-wise phase offset values for each voxel.</returns>
        private void ExtractVoxelFrequencyPhaseOffsets(IBlackBox cppn,
            int substrateX,
            int substrateY,
            int substrateZ, out double frequency, out IList<IList<double>> phaseOffsets)
        {
            IList<IList<double>> layerwisePhaseOffsets = new List<IList<double>>(substrateZ);
            var frequencies = new List<double>(substrateX * substrateY * substrateZ);

            // Compute distance to centroid for each voxel in the body
            var distanceMatrix = VoxelUtils.ComputeVoxelDistanceMatrix(substrateX, substrateY, substrateZ);

            // Normalize each position along each of three axes
            var xAxisNorm = VoxelUtils.NormalizeAxis(substrateX);
            var yAxisNorm = VoxelUtils.NormalizeAxis(substrateY);
            var zAxisNorm = VoxelUtils.NormalizeAxis(substrateZ);

            // Activate the CPPN for each voxel in the substrate
            // (the z dimension is first because this defines the layers of the voxel structure -
            // x/y are reversed for consistency, though order doesn't matter here)
            for (var z = 0; z < substrateZ; z++)
            {
                IList<double> layerPhaseOffsets = new List<double>(substrateX * substrateY);

                for (var y = 0; y < substrateY; y++)
                {
                    for (var x = 0; x < substrateX; x++)
                    {
                        // Get references to CPPN input and output
                        var inputSignalArr = cppn.InputSignalArray;
                        var outputSignalArr = cppn.OutputSignalArray;

                        // Set the input values at the current voxel
                        inputSignalArr[0] = xAxisNorm[x]; // X coordinate
                        inputSignalArr[1] = yAxisNorm[y]; // Y coordinate
                        inputSignalArr[2] = zAxisNorm[z]; // Z coordinate
                        inputSignalArr[3] = distanceMatrix[x, y, z]; // distance

                        // Reset from prior network activations
                        cppn.ResetState();

                        // Activate the network with the current inputs
                        cppn.Activate();

                        // Set phase offset for the layer
                        layerPhaseOffsets.Add(outputSignalArr[0]);

                        // Add frequency
                        frequencies.Add(outputSignalArr[1]);
                    }
                }

                // Add layer weights to the overall list of weights for the brain
                layerwisePhaseOffsets.Add(layerPhaseOffsets);
            }

            // Compute overall oscillation frequency
            frequency = 7.5 + 5.0 * Math.Max(-0.5, Math.Min(0.5, frequencies.Average()));

            // Set the phase offsets output parameter
            phaseOffsets = layerwisePhaseOffsets;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId { get; }

        /// <summary>
        ///     The oscillation frequency.
        /// </summary>
        public double Frequency { get; }

        #endregion
    }
}