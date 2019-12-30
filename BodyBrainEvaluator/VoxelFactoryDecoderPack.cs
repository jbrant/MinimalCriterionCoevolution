using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.Decoders.Substrate;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Substrate;
using SharpNeat.Network;

namespace BodyBrainConfigGenerator
{
    /// <summary>
    ///     Encapsulates body/brain genome factories and decoders.
    /// </summary>
    public struct VoxelFactoryDecoderPack
    {
        /// <summary>
        ///     The voxel brain genome factory.
        /// </summary>
        public CppnGenomeFactory BrainGenomeFactory { get; }

        /// <summary>
        ///     The voxel brain decoder.
        /// </summary>
        public NeatGenomeDecoder BrainDecoder { get; }

        /// <summary>
        ///     The voxel body genome factory.
        /// </summary>
        public NeatSubstrateGenomeFactory BodyGenomeFactory { get; }

        /// <summary>
        ///     The voxel body decoder.
        /// </summary>
        public NeatSubstrateGenomeDecoder BodyDecoder { get; }

        /// <summary>
        ///     VoxelFactoryDecoderPack constructor.
        /// </summary>
        /// <param name="bodyX">The body size along the X dimension.</param>
        /// <param name="bodyY">The body size along the Y dimension.</param>
        /// <param name="bodyZ">The body size along the Z dimension.</param>
        /// <param name="maxBodySize">The maximum permissible body size.</param>
        /// <param name="activationIters">The number of times a recurrent connection is activated (only applicable for RNNs).</param>
        public VoxelFactoryDecoderPack(int bodyX, int bodyY, int bodyZ, int maxBodySize, int? activationIters)
        {
            // Get the activation schemes for the body and brain CPPNs
            var activationScheme = GetNetworkActivationScheme(activationIters);

            // Create the body and brain genome factories
            BrainGenomeFactory = CreateBrainGenomeFactory();
            BodyGenomeFactory = CreateBodyGenomeFactory(bodyX, bodyY, bodyZ, maxBodySize);

            // Create the body and brain genome decoders
            BrainDecoder = new NeatGenomeDecoder(activationScheme);
            BodyDecoder = new NeatSubstrateGenomeDecoder(activationScheme);
        }

        /// <summary>
        ///     Creates the appropriate network activation scheme based on whether recurrent properties are specified.
        /// </summary>
        /// <param name="activationIters">
        ///     The number of times a recurrent connection should be activated (only applicable for
        ///     RNNs).
        /// </param>
        /// <returns>The instantiated NetworkActivationScheme.</returns>
        private static NetworkActivationScheme GetNetworkActivationScheme(int? activationIters)
        {
            return activationIters != null
                ? NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(activationIters ?? 0)
                : NetworkActivationScheme.CreateAcyclicScheme();
        }

        /// <summary>
        ///     Creates a new voxel brain genome factory given the input/output neuron counts.
        /// </summary>
        /// <param name="brainCppnInputCount">The number of input neurons.</param>
        /// <param name="brainCppnOutputCount">The number of output neurons.</param>
        /// <returns>The instantiated voxel brain genome factory.</returns>
        private static CppnGenomeFactory CreateBrainGenomeFactory(int brainCppnInputCount = 5,
            int brainCppnOutputCount = 32)
        {
            return new CppnGenomeFactory(brainCppnInputCount, brainCppnOutputCount);
        }

        /// <summary>
        ///     Creates a new voxel body genome factory based on the body dimensions, size and CPPN input/output neuron counts.
        /// </summary>
        /// <param name="xDim">The body size along the x-dimension.</param>
        /// <param name="yDim">The body size along the y-dimension.</param>
        /// <param name="zDim">The body size along the z-dimension.</param>
        /// <param name="maxSize">The maximum possible body size.</param>
        /// <param name="bodyCppnInputCount">The number of input neurons.</param>
        /// <param name="bodyCppnOutputCount">The number of output neurons.</param>
        /// <returns>The instantiated voxel body genome factory.</returns>
        private static NeatSubstrateGenomeFactory CreateBodyGenomeFactory(int xDim, int yDim, int zDim, int maxSize,
            int bodyCppnInputCount = 5, int bodyCppnOutputCount = 2)
        {
            return new NeatSubstrateGenomeFactory(bodyCppnInputCount, bodyCppnOutputCount,
                DefaultActivationFunctionLibrary.CreateLibraryCppn(), new NeatSubstrateGenomeParameters(), xDim, yDim,
                zDim, maxSize);
        }
    }
}