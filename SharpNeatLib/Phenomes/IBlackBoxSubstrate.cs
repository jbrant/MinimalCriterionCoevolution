namespace SharpNeat.Phenomes
{
    /// <summary>
    ///     IBlackBoxSubstrate is an extension of IBlackBox that is intended to encode CPPNs along with their substrate
    ///     resolution.
    /// </summary>
    public interface IBlackBoxSubstrate : IBlackBox
    {
        /// <summary>
        ///     The resolution of a CPPN substrate along 3 dimensions.
        /// </summary>
        SubstrateResolution CppnSubstrateResolution { get; }
    }
}