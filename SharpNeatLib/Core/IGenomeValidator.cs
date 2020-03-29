namespace SharpNeat.Core
{
    /// <summary>
    ///     Injected into genome class to to validate whether genomes that are created are valid per the implementing
    ///     phenotype.
    /// </summary>
    /// <typeparam name="TGenome">The genome type.</typeparam>
    public interface IGenomeValidator<in TGenome>
    {
        /// <summary>
        ///     Uses the genome decoder to convert the given genome to to its phenotypic representation and performs a validity on
        ///     the phenome.
        /// </summary>
        /// <param name="genome">The genome to validate.</param>
        /// <returns>Boolean indicator of whether the given genome is valid.</returns>
        bool IsGenomeValid(TGenome genome);
    }
}