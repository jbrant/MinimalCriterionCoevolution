using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Voxels;

namespace SharpNeat.Genomes.Voxel
{
    /// <summary>
    ///     Injected into genome class to validate whether generated genomes or mutations are valid based on the phenome that
    ///     they generate.
    /// </summary>
    public struct VoxelBodyGenomeValidator : IGenomeValidator<NeatGenome>
    {
        /// <summary>
        ///     Reference to the voxel body genome decoder.
        /// </summary>
        private readonly IGenomeDecoder<NeatGenome, VoxelBody> _bodyDecoder;

        /// <summary>
        ///     The minimum percentage of the voxel structure space that contains material (i.e. is not empty).
        /// </summary>
        private readonly double _minPercentFull;

        /// <summary>
        ///     The minimum percentage of voxels that are muscle (rather than bone/rigid or soft material).
        /// </summary>
        private readonly double _minPercentActive;

        /// <summary>
        ///     VoxelBodyMutationValidator constructor.
        /// </summary>
        /// <param name="bodyDecoder">Reference to the voxel body genome decoder.</param>
        /// <param name="minPercentFull">
        ///     The minimum percentage of the voxel structure space that contains material (i.e. is not
        ///     empty).
        /// </param>
        /// <param name="minPercentActive">
        ///     The minimum percentage of voxels that are muscle (rather than bone/rigid or soft
        ///     material).
        /// </param>
        public VoxelBodyGenomeValidator(IGenomeDecoder<NeatGenome, VoxelBody> bodyDecoder, double minPercentFull,
            double minPercentActive)
        {
            _bodyDecoder = bodyDecoder;
            _minPercentFull = minPercentFull;
            _minPercentActive = minPercentActive;
        }

        /// <summary>
        ///     Uses the genome decoder to convert the given genome to to its phenotypic representation and performs a validity
        ///     check on the phenome.
        /// </summary>
        /// <param name="genome">The genome to validate.</param>
        /// <returns>Boolean indicator of whether the given genome is structurally valid.</returns>
        public bool IsGenomeValid(NeatGenome genome)
        {
            // Decode to voxel body phenotype
            var body = _bodyDecoder.Decode(genome);

            // Check whether the minimum number of voxels are present and that the requisite percentage of them
            // are active voxels 
            return body.FullProportion >= _minPercentFull && body.ActiveTissueProportion >= _minPercentActive;
        }
    }
}