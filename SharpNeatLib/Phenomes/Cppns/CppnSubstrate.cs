namespace SharpNeat.Phenomes.CPPNs
{
    /// <summary>
    ///     CppnSubstrate is container class for compositional pattern-producing networks (CPPNs) along with the resolution of
    ///     the substrate that they query.
    /// </summary>
    public class CppnSubstrate : IBlackBoxSubstrate
    {
        #region Instance variables

        /// <summary>
        ///     The network encapsulated by the CPPN/substrate container.
        /// </summary>
        private readonly IBlackBox _network;

        #endregion

        #region Constructor

        /// <summary>
        ///     CppnSubstrate constructor.
        /// </summary>
        /// <param name="cppn">The compositional pattern-producing network (CPPN) to encapsulate.</param>
        /// <param name="substrateResolution">The resolution of the substrate queried by the CPPN.</param>
        public CppnSubstrate(IBlackBox cppn, SubstrateResolution substrateResolution)
        {
            _network = cppn;
            CppnSubstrateResolution = substrateResolution;
        }

        #endregion

        #region ICppnBlackBox Properties

        /// <summary>
        ///     The unique identifier of the genome from which the phenotype was generated.
        /// </summary>
        public uint GenomeId => _network.GenomeId;

        /// <summary>
        ///     Gets the number of inputs to the blackbox. This is assumed to be fixed for the lifetime of the IBlackBox.
        /// </summary>
        public int InputCount => _network.InputCount;

        /// <summary>
        ///     Gets the number of outputs from the blackbox. This is assumed to be fixed for the lifetime of the IBlackBox.
        /// </summary>
        public int OutputCount => _network.OutputCount;

        /// <summary>
        ///     Gets an array of input values that feed into the black box.
        /// </summary>
        public ISignalArray InputSignalArray => _network.InputSignalArray;

        /// <summary>
        ///     Gets an array of output values that feed out from the black box.
        /// </summary>
        public ISignalArray OutputSignalArray => _network.OutputSignalArray;

        /// <summary>
        ///     Gets a value indicating whether the black box's internal state is valid. It may become invalid if e.g. we ask a
        ///     recurrent
        ///     neural network to relax and it is unable to do so.
        /// </summary>
        public bool IsStateValid => _network.IsStateValid;

        /// <summary>
        ///     The resolution of a CPPN substrate along 3 dimensions.
        /// </summary>
        public SubstrateResolution CppnSubstrateResolution { get; }

        #endregion

        #region ICppnBlackBox methods

        /// <summary>
        ///     Activate the black box. This is a request for the box to accept its inputs and produce output signals
        ///     ready for reading from OutputSignalArray.
        /// </summary>
        public void Activate()
        {
            _network.Activate();
        }

        /// <summary>
        ///     Reset any internal state.
        /// </summary>
        public void ResetState()
        {
            _network.ResetState();
        }

        /// <summary>
        ///     Creates a new instance of the black box model.
        /// </summary>
        /// <returns>A new instance of the black box model.</returns>
        public IBlackBox Clone()
        {
            return _network.Clone();
        }

        #endregion
    }
}