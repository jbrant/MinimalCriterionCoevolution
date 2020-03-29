using System.Collections.Generic;
using Redzen.Random;
using Redzen.Sorting;
using SharpNeat.Core;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Utility;

namespace SharpNeat.Genomes.Substrate
{
    /// <summary>
    ///     The NeatSubstrateGenomeFactory generates NEAT substrate genomes along with variable size substrates whose
    ///     resolution can be modified throughout evolution.
    /// </summary>
    public class NeatSubstrateGenomeFactory : IGenomeFactory<NeatSubstrateGenome>
    {
        #region Instance variables

        /// <summary>
        ///     Reference to the genome validator, which is used to determine whether the generated phenotype is valid.
        /// </summary>
        private readonly IGenomeValidator<NeatSubstrateGenome> _genomeGenomeValidator;

        #endregion

        /// <summary>
        ///     Create a copy of an existing NeatSubstrateGenomeFactory, substituting in the specified ID and birth generation.
        ///     Overridable method to allow alternative NeatGenome sub-classes to be used.
        /// </summary>
        public NeatSubstrateGenome CreateGenomeCopy(NeatSubstrateGenome copyFrom, uint id, uint birthGeneration)
        {
            return new NeatSubstrateGenome(copyFrom, id, birthGeneration);
        }

        #region Public methods

        /// <summary>
        ///     Checks whether a mutation or crossover operation produces a valid phenotype.
        /// </summary>
        /// <param name="genome"></param>
        /// <returns></returns>
        public bool IsGeneratedPhenomeValid(NeatSubstrateGenome genome)
        {
            return _genomeGenomeValidator == null || _genomeGenomeValidator.IsGenomeValid(genome);
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Creates a single randomly initialised genome.
        ///     A random set of connections are made form the input to the output neurons, the number of
        ///     connections made is based on the NeatGenomeParameters.InitialInterconnectionsProportion
        ///     which specifies the proportion of all posssible input-output connections to be made in
        ///     initial genomes.
        ///     The connections that are made are allocated innovation IDs in a consistent manner across
        ///     the initial population of genomes. To do this we allocate IDs sequentially to all possible
        ///     interconnections and then randomly select some proportion of connections for inclusion in the
        ///     genome. In addition, for this scheme to work the innovation ID generator must be reset to zero
        ///     prior to each call to CreateGenome(), and a test is made to ensure this is the case.
        ///     The consistent allocation of innovation IDs ensure that equivalent connections in different
        ///     genomes have the same innovation ID, and although this isn't strictly necessary it is
        ///     required for sexual reproduction to work effectively - like structures are detected by comparing
        ///     innovation IDs only.
        /// </summary>
        /// <param name="birthGeneration">
        ///     The current evolution algorithm generation. Assigned to the new genome as its birth
        ///     generation.
        /// </param>
        /// <returns>The newly-created genome.</returns>
        private NeatSubstrateGenome CreateGenome(uint birthGeneration)
        {
            return new NeatSubstrateGenome(this, NeatGenomeFactory.CreateGenome(birthGeneration), DefaultSubstrateX,
                DefaultSubstrateY, DefaultSubstrateZ);
        }

        #endregion

        #region Public variables

        /// <summary>
        ///     Reference to the NEAT genome factory which is leveraged for generating NEAT substrate genomes.
        /// </summary>
        public NeatGenomeFactory NeatGenomeFactory { get; }

        /// <summary>
        ///     Gets a random number generator associated with the factory.
        ///     Note. The provided RNG is not thread safe, if concurrent use is required then sync locks
        ///     are necessary or some other RNG mechanism.
        /// </summary>
        public IRandomSource Rng => NeatGenomeFactory.Rng;

        /// <summary>
        ///     Encapsulates NEAT genome mutation parameters (assuming that the affected network codes for a CPPN) along with
        ///     mutation parameters specific to the modification of a substrate queried by that CPPN.
        /// </summary>
        public NeatSubstrateGenomeParameters NeatSubstrateGenomeParameters { get; }

        /// <summary>
        ///     The default resolution of the substrate along the X dimension.
        /// </summary>
        public int DefaultSubstrateX { get; }

        /// <summary>
        ///     The default resolution of the substrate along the Y dimension.
        /// </summary>
        public int DefaultSubstrateY { get; }

        /// <summary>
        ///     The default resolution of the substrate along the Z dimension.
        /// </summary>
        public int DefaultSubstrateZ { get; }

        /// <summary>
        ///     The maximum substrate resolution along any dimension.
        /// </summary>
        public int MaxSubstrateResolution { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     NeatSubstrateGenomeFactory constructor.
        /// </summary>
        /// <param name="inputNeuronCount">The number of inputs.</param>
        /// <param name="outputNeuronCount">The number of outputs.</param>
        /// <param name="activationFnLibrary">The activation functions allowed along with their selection probability.</param>
        /// <param name="neatSubstrateGenomeParameters">The substrate-specific mutation parameters.</param>
        /// <param name="defaultSubstrateX">The default resolution of the substrate along the X dimension.</param>
        /// <param name="defaultSubstrateY">The default resolution of the substrate along the Y dimension.</param>
        /// <param name="defaultSubstrateZ">The default resolution of the substrate along the Z dimension.</param>
        /// <param name="maxSubstrateResolution">The maximum resolution of the CPPN substrate.</param>
        /// <param name="genomeValidator">
        ///     Reference to the genome validator, which is used to determine whether the generated phenotype is valid.
        /// </param>
        public NeatSubstrateGenomeFactory(int inputNeuronCount, int outputNeuronCount,
            IActivationFunctionLibrary activationFnLibrary, NeatSubstrateGenomeParameters neatSubstrateGenomeParameters,
            int defaultSubstrateX, int defaultSubstrateY, int defaultSubstrateZ, int maxSubstrateResolution,
            IGenomeValidator<NeatSubstrateGenome> genomeValidator = null) : this(
            new CppnGenomeFactory(inputNeuronCount, outputNeuronCount, activationFnLibrary),
            neatSubstrateGenomeParameters, defaultSubstrateX, defaultSubstrateY, defaultSubstrateZ,
            maxSubstrateResolution, genomeValidator)
        {
        }

        /// <summary>
        ///     NeatSubstrateGenomeFactory constructor.
        /// </summary>
        /// <param name="inputNeuronCount">The number of inputs.</param>
        /// <param name="outputNeuronCount">The number of outputs.</param>
        /// <param name="activationFnLibrary">The activation functions allowed along with their selection probability.</param>
        /// <param name="neatGenomeParams">
        ///     NEAT hyperparameters that control mutation/crossover rates and other properties of
        ///     evolution.
        /// </param>
        /// <param name="neatSubstrateGenomeParameters">The substrate-specific mutation parameters.</param>
        /// <param name="defaultSubstrateX">The default resolution of the substrate along the X dimension.</param>
        /// <param name="defaultSubstrateY">The default resolution of the substrate along the Y dimension.</param>
        /// <param name="defaultSubstrateZ">The default resolution of the substrate along the Z dimension.</param>
        /// <param name="maxSubstrateResolution">The maximum resolution of the CPPN substrate.</param>
        /// <param name="genomeValidator">
        ///     Reference to the genome validator, which is used to determine whether the generated phenotype
        ///     is valid.
        /// </param>
        public NeatSubstrateGenomeFactory(int inputNeuronCount, int outputNeuronCount,
            IActivationFunctionLibrary activationFnLibrary, NeatGenomeParameters neatGenomeParams,
            NeatSubstrateGenomeParameters neatSubstrateGenomeParameters, int defaultSubstrateX, int defaultSubstrateY,
            int defaultSubstrateZ, int maxSubstrateResolution,
            IGenomeValidator<NeatSubstrateGenome> genomeValidator = null) : this(
            new CppnGenomeFactory(inputNeuronCount, outputNeuronCount, activationFnLibrary, neatGenomeParams),
            neatSubstrateGenomeParameters, defaultSubstrateX, defaultSubstrateY, defaultSubstrateZ,
            maxSubstrateResolution, genomeValidator)
        {
        }

        /// <summary>
        ///     NeatSubstrateGenomeFactory constructor.
        /// </summary>
        /// <param name="inputNeuronCount">The number of inputs.</param>
        /// <param name="outputNeuronCount">The number of outputs.</param>
        /// <param name="activationFnLibrary">The activation functions allowed along with their selection probability.</param>
        /// <param name="neatGenomeParams">
        ///     NEAT hyperparameters that control mutation/crossover rates and other properties of
        ///     evolution.
        /// </param>
        /// <param name="genomeIdGenerator">Auto-incrementing integer generator for genome IDs.</param>
        /// <param name="innovationIdGenerator">Auto-incrementing integer generator for node/connection innovation IDs.</param>
        /// <param name="neatSubstrateGenomeParameters">The substrate-specific mutation parameters.</param>
        /// <param name="defaultSubstrateX">The default resolution of the substrate along the X dimension.</param>
        /// <param name="defaultSubstrateY">The default resolution of the substrate along the Y dimension.</param>
        /// <param name="defaultSubstrateZ">The default resolution of the substrate along the Z dimension.</param>
        /// <param name="maxSubstrateResolution">The maximum resolution of the CPPN substrate.</param>
        /// <param name="genomeValidator">
        ///     Reference to the genome validator, which is used to determine whether the generated phenotype
        ///     is valid.
        /// </param>
        public NeatSubstrateGenomeFactory(int inputNeuronCount, int outputNeuronCount,
            IActivationFunctionLibrary activationFnLibrary, NeatGenomeParameters neatGenomeParams,
            UInt32IdGenerator genomeIdGenerator, UInt32IdGenerator innovationIdGenerator,
            NeatSubstrateGenomeParameters neatSubstrateGenomeParameters, int defaultSubstrateX, int defaultSubstrateY,
            int defaultSubstrateZ, int maxSubstrateResolution,
            IGenomeValidator<NeatSubstrateGenome> genomeValidator = null) : this(
            new CppnGenomeFactory(inputNeuronCount, outputNeuronCount, activationFnLibrary, neatGenomeParams,
                genomeIdGenerator, innovationIdGenerator), neatSubstrateGenomeParameters, defaultSubstrateX,
            defaultSubstrateY, defaultSubstrateZ, maxSubstrateResolution, genomeValidator)
        {
        }

        /// <summary>
        ///     NeatSubstrateGenomeFactory constructor.
        /// </summary>
        /// <param name="neatGenomeFactory">The CPPN factory.</param>
        /// <param name="neatSubstrateGenomeParameters">The substrate-specific mutation parameters.</param>
        /// <param name="defaultSubstrateX">The default resolution of the substrate along the X dimension.</param>
        /// <param name="defaultSubstrateY">The default resolution of the substrate along the Y dimension.</param>
        /// <param name="defaultSubstrateZ">The default resolution of the substrate along the Z dimension.</param>
        /// <param name="maxSubstrateResolution">The maximum resolution of the CPPN substrate.</param>
        /// <param name="genomeValidator">
        ///     Reference to the genome validator, which is used to determine whether the generated phenotype
        ///     is valid.
        /// </param>
        public NeatSubstrateGenomeFactory(CppnGenomeFactory neatGenomeFactory,
            NeatSubstrateGenomeParameters neatSubstrateGenomeParameters, int defaultSubstrateX, int defaultSubstrateY,
            int defaultSubstrateZ, int maxSubstrateResolution, IGenomeValidator<NeatSubstrateGenome> genomeValidator)
        {
            NeatGenomeFactory = neatGenomeFactory;
            NeatSubstrateGenomeParameters = neatSubstrateGenomeParameters;
            DefaultSubstrateX = defaultSubstrateX;
            DefaultSubstrateY = defaultSubstrateY;
            DefaultSubstrateZ = defaultSubstrateZ;
            MaxSubstrateResolution = maxSubstrateResolution;
            _genomeGenomeValidator = genomeValidator;
        }

        #endregion

        #region IGenomeFactory properties

        /// <summary>
        ///     Gets the factory's genome ID generator.
        /// </summary>
        public UInt32IdGenerator GenomeIdGenerator => NeatGenomeFactory.GenomeIdGenerator;

        /// <summary>
        ///     Gets or sets a mode value. This is intended as a means for an evolution algorithm to convey changes
        ///     in search mode to genomes, and because the set of modes is specific to each concrete implementation
        ///     of an IEvolutionAlgorithm the mode is defined as an integer (rather than an enum[eration]).
        ///     E.g. SharpNEAT's implementation of NEAT uses an evolutionary algorithm that alternates between
        ///     a complexifying and simplifying mode, in order to do this the algorithm class needs to notify the genomes
        ///     of the current mode so that the CreateOffspring() methods are able to generate offspring appropriately -
        ///     e.g. we avoid adding new nodes and connections and increase the rate of deletion mutations when in
        ///     simplifying mode.
        /// </summary>
        public int SearchMode
        {
            get => NeatGenomeFactory.SearchMode;
            set => NeatGenomeFactory.SearchMode = value;
        }

        #endregion

        #region IGenomeFactory methods

        /// <summary>
        ///     Creates a list of randomly initialized genomes.
        /// </summary>
        /// <param name="length">The number of genomes to create.</param>
        /// <param name="birthGeneration">
        ///     The current evolution algorithm generation.
        ///     Assigned to the new genomes as their birth generation.
        /// </param>
        public List<NeatSubstrateGenome> CreateGenomeList(int length, uint birthGeneration)
        {
            var genomeList = new List<NeatSubstrateGenome>(length);

            for (var i = 0; i < length; i++)
            {
                NeatSubstrateGenome genome;

                // Create new random random genome until the generated phenome is valid
                do
                {
                    // We reset the innovation ID to zero so that all created genomes use the same 
                    // innovation IDs for matching neurons and connections. This isn't a strict requirement but
                    // throughout the SharpNeat code we attempt to use the same innovation ID for like structures
                    // to improve the effectiveness of sexual reproduction.
                    NeatGenomeFactory.InnovationIdGenerator.Reset();

                    // Create new NEAT substrate genome
                    genome = CreateGenome(birthGeneration);
                } while (!IsGeneratedPhenomeValid(genome));

                genomeList.Add(genome);
            }

            return genomeList;
        }

        /// <summary>
        ///     Creates a list of genomes spawned from a seed genome. Spawning uses asexual reproduction.
        /// </summary>
        /// <param name="length">The number of genomes to create.</param>
        /// <param name="birthGeneration">
        ///     The current evolution algorithm generation.
        ///     Assigned to the new genomes as their birth generation.
        /// </param>
        /// <param name="seedGenome">The seed genome to spawn new genomes from.</param>
        public List<NeatSubstrateGenome> CreateGenomeList(int length, uint birthGeneration,
            NeatSubstrateGenome seedGenome)
        {
            var genomeList = new List<NeatSubstrateGenome>(length);

            // Add an exact copy of the seed to the list
            var newGenome = CreateGenomeCopy(seedGenome, NeatGenomeFactory.NextGenomeId(), birthGeneration);
            genomeList.Add(newGenome);

            // For the remainder we create mutated offspring from the seed
            for (var i = 0; i < length; i++)
            {
                genomeList.Add(seedGenome.CreateOffspring(birthGeneration));
            }

            return genomeList;
        }

        /// <summary>
        ///     Creates a list of genomes spawned from a list of seed genomes. Spawning uses asexual reproduction and
        ///     typically we would simply repeatedly loop over (and spawn from) the seed genomes until we have the
        ///     required number of spawned genomes.
        /// </summary>
        /// <param name="length">The number of genomes to create.</param>
        /// <param name="birthGeneration">
        ///     The current evolution algorithm generation.
        ///     Assigned to the new genomes as their birth generation.
        /// </param>
        /// <param name="seedGenomeList">A list of seed genomes from which to spawn new genomes from.</param>
        public List<NeatSubstrateGenome> CreateGenomeList(int length, uint birthGeneration,
            List<NeatSubstrateGenome> seedGenomeList)
        {
            if (seedGenomeList.Count == 0)
            {
                throw new SharpNeatException("CreateGenomeList() requires at least on seed genome in seedGenomeList.");
            }

            // Create a copy of the list so that we can shuffle the items without modifying the original list
            seedGenomeList = new List<NeatSubstrateGenome>(seedGenomeList);
            SortUtils.Shuffle(seedGenomeList, Rng);

            // Make exact copies of seed genomes and insert them into our new genome list
            var genomeList = new List<NeatSubstrateGenome>(length);
            var idx = 0;
            var seedCount = seedGenomeList.Count;

            for (var seedIdx = 0; idx < length && seedIdx < seedCount; idx++, seedIdx++)
            {
                // Add an exact copy of the seed to the list
                var newGenome = CreateGenomeCopy(seedGenomeList[seedIdx], NeatGenomeFactory.NextGenomeId(),
                    birthGeneration);
                genomeList.Add(newGenome);
            }

            // Keep spawning offspring from seed genomes until we have the required number of genomes
            for (; idx < length;)
            {
                for (var seedIdx = 0; idx < length && seedIdx < seedCount; idx++, seedIdx++)
                {
                    genomeList.Add(seedGenomeList[seedIdx].CreateOffspring(birthGeneration));
                }
            }

            return genomeList;
        }

        /// <summary>
        ///     Supports debug/integrity checks. Checks that a given genome object's type is consistent with the genome factory.
        ///     Typically the wrong type of object may occur where factories are subtyped and not all of the relevant virtual
        ///     methods are overriden.
        /// </summary>
        /// <param name="genome">The genome whose type to check.</param>
        /// <returns>Flag indicating whether the genome is of the correct/expected type.</returns>
        public bool CheckGenomeType(NeatSubstrateGenome genome)
        {
            return genome.GetType() == typeof(NeatSubstrateGenome);
        }

        #endregion
    }
}