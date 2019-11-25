using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Voxels;

namespace MCC_Domains.BodyBrain
{
    /// <summary>
    ///     Encapsulates the voxel body phenotype along with usage count.
    /// </summary>
    internal class VoxelBodyContainer
    {
        /// <summary>
        ///     VoxelBodyContainer constructor.
        /// </summary>
        /// <param name="body">The voxel body phenotype, which encodes voxel locations and materials.</param>
        public VoxelBodyContainer(IBlackBoxSubstrate body)
        {
            Body = new VoxelBody(body);
            UsageCount = 0;
        }

        /// <summary>
        ///     The voxel body phenotype, which encodes voxel locations and materials.
        /// </summary>
        public VoxelBody Body { get; }

        /// <summary>
        ///     The number of times the voxel body has been used to satisfy a brain MC.
        /// </summary>
        public int UsageCount { get; set; }
    }

    /// <summary>
    ///     The voxel body factory produces voxel body structures to be ambulated by per-voxel controllers.
    /// </summary>
    public class VoxelBodyFactory
    {
        #region Constructor

        /// <summary>
        ///     VoxelBodyFactory constructor, which records the resource limit and initializes a collection of voxel bodies.
        /// </summary>
        /// <param name="resourceLimit">The number of times a given body can be used to satisfy a brain's MC.</param>
        public VoxelBodyFactory(int resourceLimit)
        {
            _resourceLimit = resourceLimit;

            // Initialize the internal collection of voxel bodies and voxel brain CPPNs
            _voxelBodies = new ConcurrentDictionary<uint, VoxelBodyContainer>();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The number of voxel bodies that the factory can produce.
        /// </summary>
        public int NumBodies { get; private set; }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The number of times a given body can be used to satisfy a brain's MC.
        /// </summary>
        private readonly int _resourceLimit;

        /// <summary>
        ///     List of keys for the voxel body dictionary. This permits an ordering on the keys without having to build a sorted
        ///     dictionary.
        /// </summary>
        private IList<uint> _currentKeys;

        /// <summary>
        ///     The internal collection of decoded voxel bodies.
        /// </summary>
        private readonly IDictionary<uint, VoxelBodyContainer> _voxelBodies;

        #endregion

        #region Public methods

        /// <summary>
        ///     Adds new voxel body CPPNs and removes bodies that have either aged out of the population or who have reached their
        ///     resource limit.
        /// </summary>
        /// <param name="bodyCppns">The new voxel body CPPNs.</param>
        public void SetVoxelBodies(IList<IBlackBoxSubstrate> bodyCppns)
        {
            // Add new voxel bodies
            foreach (var bodyCppn in bodyCppns.Where(x => _voxelBodies.ContainsKey(x.GenomeId) == false).AsParallel())
            {
                _voxelBodies.Add(bodyCppn.GenomeId, new VoxelBodyContainer(bodyCppn));
            }

            // Extract current voxel body genome IDs
            var curIds = bodyCppns.Select(body => body.GenomeId);

            // Remove bodies that are no longer in the population
            foreach (var key in _voxelBodies.Keys.Where(key => curIds.Contains(key) == false))
            {
                _voxelBodies.Remove(key);
            }

            // Remove bodies that have reached their resource limit
            foreach (var bodyEntry in _voxelBodies.Where(x => x.Value.UsageCount >= _resourceLimit))
            {
                _voxelBodies.Remove(bodyEntry.Key);
            }

            // Set the number of voxel bodies in the factory
            NumBodies = _voxelBodies.Count;

            // Store off keys so we have a static mapping between list indexes and dictionary keys
            _currentKeys = _voxelBodies.Keys.ToList();
        }

        /// <summary>
        ///     Retrieves the voxel body phenotype whose genome ID is at the given index.
        /// </summary>
        /// <param name="genomeIdx">The index of the genome ID.</param>
        /// <returns>The voxel body phenotype.</returns>
        public VoxelBody GetVoxelBody(int genomeIdx)
        {
            return _voxelBodies[_currentKeys[genomeIdx]].Body;
        }

        /// <summary>
        ///     Determines whether a voxel body is under its resource limit.
        /// </summary>
        /// <param name="genomeIdx">The index of the genome ID.</param>
        /// <returns>Flag indicating whether the voxel body whose genome ID is at the given index is under its resource limit.</returns>
        public bool IsBodyUnderResourceLimit(int genomeIdx)
        {
            return _voxelBodies[_currentKeys[genomeIdx]].UsageCount < _resourceLimit;
        }

        /// <summary>
        ///     Increments the usage count for the voxel body phenotype whose genome ID is at the given index.
        /// </summary>
        /// <param name="genomeIdx">The index of the genome ID.</param>
        public void IncrementBodyUsageCount(int genomeIdx)
        {
            _voxelBodies[_currentKeys[genomeIdx]].UsageCount++;
        }

        /// <summary>
        ///     Retrieves the voxel body genome ID at the given index.
        /// </summary>
        /// <param name="genomeIdx">The index of the genome ID.</param>
        /// <returns>The voxel body genome ID.</returns>
        public uint GetBodyGenomeId(int genomeIdx)
        {
            return _currentKeys[genomeIdx];
        }

        /// <summary>
        ///     Retrieves the MC usage count for the voxel body phenotype whose genome ID is at the given index.
        /// </summary>
        /// <param name="genomeIdx">The index of the genome ID.</param>
        /// <returns>The voxel body MC usage count.</returns>
        public int GetBodyUsageCount(int genomeIdx)
        {
            return _voxelBodies[_currentKeys[genomeIdx]].UsageCount;
        }

        #endregion
    }
}