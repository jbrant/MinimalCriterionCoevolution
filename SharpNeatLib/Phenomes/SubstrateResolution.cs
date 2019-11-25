namespace SharpNeat.Phenomes
{
    /// <summary>
    ///     Encapsulates the resolution of a CPPN substrate along 3 dimensions.
    /// </summary>
    public struct SubstrateResolution
    {
        /// <summary>
        ///     Substrate resolution along the X dimension.
        /// </summary>
        public int X { get; }

        /// <summary>
        ///     Substrate resolution along the Y dimension.
        /// </summary>
        public int Y { get; }

        /// <summary>
        ///     Substrate resolution along the Z dimension.
        /// </summary>
        public int Z { get; }

        /// <summary>
        ///     SubstrateResolution constructor.
        /// </summary>
        /// <param name="x">Substrate resolution along the X dimension.</param>
        /// <param name="y">Substrate resolution along the Y dimension.</param>
        /// <param name="z">Substrate resolution along the Z dimension.</param>
        public SubstrateResolution(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}