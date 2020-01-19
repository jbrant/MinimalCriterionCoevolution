using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain
{
    /// <summary>
    ///     Encapsulates the voxel brain phenotype and caches its voxel controller representation scaled to various resolutions
    ///     (given the voxel body sizes on which brains have been evaluated).
    /// </summary>
    internal class VoxelBrainContainer
    {
        /// <summary>
        ///     VoxelBrainContainer constructor.
        /// </summary>
        /// <param name="brainCppn">The voxel brain phenotype, which encodes per-voxel neurocontrollers.</param>
        public VoxelBrainContainer(IBlackBox brainCppn)
        {
            BrainCppn = brainCppn;
            ScaledBrains = new Dictionary<SubstrateResolution, IVoxelBrain>();
        }

        /// <summary>
        ///     The voxel brain phenotype, which encodes per-voxel neurocontrollers.
        /// </summary>
        public IBlackBox BrainCppn { get; }

        /// <summary>
        ///     The map of brains scaled to a given substrate resolution.
        /// </summary>
        public IDictionary<SubstrateResolution, IVoxelBrain> ScaledBrains { get; }
    }

    /// <summary>
    ///     The voxel brain factory produces per-voxel neurocontrollers to ambulate voxel bodies.
    /// </summary>
    public class VoxelBrainFactory
    {
        #region Properties

        /// <summary>
        ///     The number of voxel brains that the factory can produce.
        /// </summary>
        public int NumBrains { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        ///     VoxelBrainFactory constructor, which set the number of network connections in voxel controllers and initializes a
        ///     collection of neural network voxel brains.
        /// </summary>
        /// <param name="numConnections">The number of connections in a given voxel-specific controller.</param>
        public VoxelBrainFactory(int numConnections)
        {
            _numConnections = numConnections;

            // Initialize the internal collection of voxel bodies and voxel brain CPPNs
            _voxelBrainCppns = new ConcurrentDictionary<uint, VoxelBrainContainer>();
        }

        /// <summary>
        ///     VoxelBrainFactory constructor, which initializes a collection of phase offset voxel brains.
        /// </summary>
        public VoxelBrainFactory()
        {
            // Initialize the internal collection of voxel bodies and voxel brain CPPNs
            _voxelBrainCppns = new ConcurrentDictionary<uint, VoxelBrainContainer>();
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The number of connections in a given voxel-specific controller.
        /// </summary>
        private readonly int _numConnections;

        /// <summary>
        ///     List of keys for the voxel brain dictionary. This permits an ordering on the keys without having to build a sorted
        ///     dictionary.
        /// </summary>
        private IList<uint> _currentKeys;

        /// <summary>
        ///     The internal collection of voxel brain CPPNs.
        /// </summary>
        private readonly IDictionary<uint, VoxelBrainContainer> _voxelBrainCppns;

        /// <summary>
        ///     Lock object for synchronizing voxel brain map updates.
        /// </summary>
        private readonly object _lock = new object();

        #endregion

        #region Public methods

        /// <summary>
        ///     Adds new voxel brain CPPNs and removes brains that have aged out of the population.
        /// </summary>
        /// <param name="brainCppns">The new voxel brain CPPNs.</param>
        public void SetVoxelBrains(IList<IBlackBox> brainCppns)
        {
            // Add new voxel brains
            foreach (var brainCppn in brainCppns.Where(x => _voxelBrainCppns.ContainsKey(x.GenomeId) == false)
                .AsParallel())
            {
                _voxelBrainCppns.Add(brainCppn.GenomeId, new VoxelBrainContainer(brainCppn));
            }

            // Extract current voxel brain genome IDs
            var curIds = brainCppns.Select(brain => brain.GenomeId);

            // Remove brains that are no longer in the population
            foreach (var key in _voxelBrainCppns.Keys.Where(key => curIds.Contains(key) == false))
            {
                _voxelBrainCppns.Remove(key);
            }

            // Set the number of voxel brains in the factory
            NumBrains = _voxelBrainCppns.Count;

            // Store off keys so we have a static mapping between list indexes and dictionary keys
            _currentKeys = _voxelBrainCppns.Keys.ToList();
        }

        /// <summary>
        ///     Retrieves the voxel brain phenotype whose genome ID is at the given index and whose scaled representation matches
        ///     the given substrate dimensions.
        /// </summary>
        /// <param name="genomeIdx">The index of the genome ID.</param>
        /// <param name="x">The size of the substrate along the X dimension.</param>
        /// <param name="y">The size of the substrate along the Y dimension.</param>
        /// <param name="z">The size of the substrate along the Z dimension.</param>
        /// <param name="brainType">The type of brain controller (e.g. neural network or phase offset controller).</param>
        /// <returns>The scaled voxel brain.</returns>
        public IVoxelBrain GetVoxelBrain(int genomeIdx, int x, int y, int z, BrainType brainType)
        {
            IVoxelBrain voxelBrain;

            // Get voxel brain container entry at the given index
            var brainEntry = _voxelBrainCppns[_currentKeys[genomeIdx]];

            // Create substrate resolution property
            var resolution = new SubstrateResolution(x, y, z);

            lock (_lock)
            {
                if (brainEntry.ScaledBrains.ContainsKey(resolution))
                {
                    return brainEntry.ScaledBrains[resolution];
                }

                // Create new voxel brain with a resolution matching that of the body
                if (brainType == BrainType.NeuralNet)
                    voxelBrain = new VoxelAnnBrain(brainEntry.BrainCppn, resolution.X, resolution.Y, resolution.Z,
                        _numConnections);
                else
                    voxelBrain =
                        new VoxelPhaseOffsetBrain(brainEntry.BrainCppn, resolution.X, resolution.Y, resolution.Z);

                // Cache voxel brain
                brainEntry.ScaledBrains.Add(resolution, voxelBrain);
            }

            return voxelBrain;
        }

        #endregion
    }
}