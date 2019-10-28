using System.Collections.Generic;
using System.Linq;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Phenomes.Voxels;

namespace SharpNeat.EvolutionAlgorithms.Statistics
{
    public class VoxelBodyAlgorithmStats : AbstractEvolutionaryAlgorithmStats
    {
        #region Constructor

        /// <summary>
        ///     VoxelBodyAlgorithmStats default constructor.
        /// </summary>
        /// <param name="eaParams">Evolution algorithm parameters required for initialization.</param>
        protected VoxelBodyAlgorithmStats(EvolutionAlgorithmParameters eaParams) : base(eaParams)
        {
        }

        /// <summary>
        ///     VoxelBodyAlgorithmStats overridden constructor, accepting voxel body decoder.
        /// </summary>
        /// <param name="eaParams">Evolution algorithm parameters required for initialization.</param>
        /// <param name="bodyDecoder">The voxel body decoder.</param>
        public VoxelBodyAlgorithmStats(EvolutionAlgorithmParameters eaParams,
            IGenomeDecoder<NeatGenome, VoxelBody> bodyDecoder) : this(eaParams)
        {
            _bodyDecoder = bodyDecoder;
        }

        #endregion

        #region Instance variables

        /// <summary>
        ///     The voxel body genome decoder.
        /// </summary>
        private readonly IGenomeDecoder<NeatGenome, VoxelBody> _bodyDecoder;

        /// <summary>
        ///     The minimum number of voxels in a given voxel body within the body population.
        /// </summary>
        private int _minVoxels;

        /// <summary>
        ///     The maximum number of voxels in a given voxel body within the body population.
        /// </summary>
        private int _maxVoxels;

        /// <summary>
        ///     The mean number of voxels within the body population.
        /// </summary>
        private double _meanVoxels;

        /// <summary>
        ///     The minimum number of active voxels in a given voxel body within the body population.
        /// </summary>
        private int _minActiveVoxels;

        /// <summary>
        ///     The maximum number of active voxels in a given voxel body within the body population.
        /// </summary>
        private int _maxActiveVoxels;

        /// <summary>
        ///     The mean number of active voxels within the body population.
        /// </summary>
        private double _meanActiveVoxels;

        /// <summary>
        ///     The minimum number of passive voxels in a given voxel body within the body population.
        /// </summary>
        private int _minPassiveVoxels;

        /// <summary>
        ///     The maximum number of passive voxels in a given voxel body within the body population.
        /// </summary>
        private int _maxPassiveVoxels;

        /// <summary>
        ///     The mean number of passive voxels within the body population.
        /// </summary>
        private double _meanPassiveVoxels;

        /// <summary>
        ///     The minimum active voxel proportion within the body population.
        /// </summary>
        private double _minActiveVoxelProportion;

        /// <summary>
        ///     The maximum active voxel proportion within the body population.
        /// </summary>
        private double _maxActiveVoxelProportion;

        /// <summary>
        ///     The mean active voxel proportion within the body population.
        /// </summary>
        private double _meanActiveVoxelProportion;

        /// <summary>
        ///     The minimum passive voxel proportion within the body population.
        /// </summary>
        private double _minPassiveVoxelProportion;

        /// <summary>
        ///     The maximum passive voxel proportion within the body population.
        /// </summary>
        private double _maxPassiveVoxelProportion;

        /// <summary>
        ///     The mean passive voxel proportion within the body population.
        /// </summary>
        private double _meanPassiveVoxelProportion;

        /// <summary>
        ///     The minimum voxel proportion within the body population.
        /// </summary>
        private double _minFullProportion;

        /// <summary>
        ///     The maximum voxel proportion within the body population.
        /// </summary>
        private double _maxFullProportion;

        /// <summary>
        ///     The mean voxel proportion within the body population.
        /// </summary>
        private double _meanFullProportion;

        #endregion

        #region Method overrides

        /// <summary>
        ///     Computes statistics specific to the voxel body population.
        /// </summary>
        /// <param name="population">The voxel body population from which to compute descriptive statistics.</param>
        public override void ComputeAlgorithmSpecificPopulationStats<TGenome>(IList<TGenome> population)
        {
            // Ensure that population list contains neat genomes, otherwise return
            if (population is IList<NeatGenome> == false)
                return;

            // Cast to neat genome
            var bodyPopulation = (IList<NeatGenome>) population;

            // Decode all of the bodies in the population
            var voxelBodies = bodyPopulation.Select(x => _bodyDecoder.Decode(x)).ToList();

            // Compute overall voxel statistics
            _minVoxels = voxelBodies.Min(x => x.NumVoxels);
            _maxVoxels = voxelBodies.Max(x => x.NumVoxels);
            _meanVoxels = voxelBodies.Average(x => x.NumVoxels);
            _minFullProportion = voxelBodies.Min(x => x.FullProportion);
            _maxFullProportion = voxelBodies.Max(x => x.FullProportion);
            _meanFullProportion = voxelBodies.Average(x => x.FullProportion);

            // Compute active voxel statistics
            _minActiveVoxels = voxelBodies.Min(x => x.NumActiveVoxels);
            _maxActiveVoxels = voxelBodies.Max(x => x.NumActiveVoxels);
            _meanActiveVoxels = voxelBodies.Average(x => x.NumActiveVoxels);
            _minActiveVoxelProportion = voxelBodies.Min(x => x.ActiveTissueProportion);
            _maxActiveVoxelProportion = voxelBodies.Max(x => x.ActiveTissueProportion);
            _meanActiveVoxelProportion = voxelBodies.Average(x => x.ActiveTissueProportion);

            // Compute passive voxel statistics
            _minPassiveVoxels = voxelBodies.Min(x => x.NumPassiveVoxels);
            _maxPassiveVoxels = voxelBodies.Max(x => x.NumPassiveVoxels);
            _meanPassiveVoxels = voxelBodies.Average(x => x.NumPassiveVoxels);
            _minPassiveVoxelProportion = voxelBodies.Min(x => x.PassiveTissueProportion);
            _maxPassiveVoxelProportion = voxelBodies.Max(x => x.PassiveTissueProportion);
            _meanPassiveVoxelProportion = voxelBodies.Average(x => x.PassiveTissueProportion);
        }

        /// <summary>
        ///     Returns the fields within VoxelBodyAlgorithmStats that are enabled for logging.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable fields within VoxelBodyAlgorithmStats.</returns>
        public override List<LoggableElement> GetLoggableElements(
            IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            var elements = base.GetLoggableElements(logFieldEnableMap);

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MinVoxels) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MinVoxels])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MinVoxels, _minVoxels));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MaxVoxels) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MaxVoxels])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MaxVoxels, _maxVoxels));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MinActiveVoxels) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MinActiveVoxels])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MinActiveVoxels, _minActiveVoxels));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MaxActiveVoxels) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MaxActiveVoxels])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MaxActiveVoxels, _maxActiveVoxels));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MeanActiveVoxels) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MeanActiveVoxels])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MeanActiveVoxels, _meanActiveVoxels));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MinPassiveVoxels) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MinPassiveVoxels])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MinPassiveVoxels, _minPassiveVoxels));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MaxPassiveVoxels) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MaxPassiveVoxels])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MaxPassiveVoxels, _maxPassiveVoxels));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MeanPassiveVoxels) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MeanPassiveVoxels])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MeanPassiveVoxels,
                    _meanPassiveVoxels));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MinFullProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MinFullProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MinFullProportion,
                    _minFullProportion));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MaxFullProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MaxFullProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MaxFullProportion,
                    _maxFullProportion));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MeanFullProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MeanFullProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MeanFullProportion,
                    _meanFullProportion));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MinActiveVoxelProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MinActiveVoxelProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MinActiveVoxelProportion,
                    _minActiveVoxelProportion));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MaxActiveVoxelProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MaxActiveVoxelProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MaxActiveVoxelProportion,
                    _maxActiveVoxelProportion));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MeanActiveVoxelProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MeanActiveVoxelProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MeanActiveVoxelProportion,
                    _meanActiveVoxelProportion));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MinPassiveVoxelProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MinPassiveVoxelProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MinPassiveVoxelProportion,
                    _minPassiveVoxelProportion));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MaxPassiveVoxelProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MaxPassiveVoxelProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MaxPassiveVoxelProportion,
                    _maxPassiveVoxelProportion));
            }

            if (logFieldEnableMap?.ContainsKey(BodyBrainEvolutionFieldElements.MeanPassiveVoxelProportion) == true &&
                logFieldEnableMap[BodyBrainEvolutionFieldElements.MeanPassiveVoxelProportion])
            {
                elements.Add(new LoggableElement(BodyBrainEvolutionFieldElements.MeanPassiveVoxelProportion,
                    _meanPassiveVoxelProportion));
            }

            return elements;
        }

        #endregion
    }
}