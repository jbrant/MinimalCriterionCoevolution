using System.Collections.Generic;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EliteArchives;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

namespace SharpNeat.Domains.MazeNavigation.NoveltyExperiment
{
    internal class MazeNavigationNoveltyExperiment : BaseMazeNavigationExperiment
    {
        private double _archiveAdditionThreshold;
        private double _archiveThresholdDecreaseMultiplier;
        private double _archiveThresholdIncreaseMultiplier;
        private IBehaviorCharacterization _behaviorCharacterization;
        private int _maxGenerationalArchiveAddition;
        private int _minGenerationalArchiveAddition;

        public override void Initialize(string name, XmlElement xmlConfig)
        {
            base.Initialize(name, xmlConfig);

            // Read in the behavior characterization
            _behaviorCharacterization =
                BehaviorCharacterizationUtil.GenerateBehaviorCharacterization(
                    BehaviorCharacterizationUtil.ConvertStringToBehavioralCharacterization(
                        XmlUtils.TryGetValueAsString(xmlConfig, "BehaviorCharacterization")));

            // Read in the novelty archive parameters
            _archiveAdditionThreshold = XmlUtils.GetValueAsDouble(xmlConfig, "ArchiveAdditionThreshold");
            _archiveThresholdDecreaseMultiplier = XmlUtils.GetValueAsDouble(xmlConfig,
                "ArchiveThresholdDecreaseMultiplier");
            _archiveThresholdIncreaseMultiplier = XmlUtils.GetValueAsDouble(xmlConfig,
                "ArchiveThresholdIncreaseMultiplier");
            _maxGenerationalArchiveAddition = XmlUtils.GetValueAsInt(xmlConfig, "MaxGenerationalArchiveAddition");
            _minGenerationalArchiveAddition = XmlUtils.GetValueAsInt(xmlConfig, "MinGenerationalArchiveAddition");
        }

        /// <summary>
        ///     Create and return a NeatEvolutionAlgorithm object (specific to fitness-based evaluations) ready for running the
        ///     NEAT algorithm/search based on the given genome factory and genome list.  Various sub-parts of the algorithm are
        ///     also constructed and connected up.
        /// </summary>
        /// <param name="genomeFactory">The genome factory from which to generate new genomes</param>
        /// <param name="genomeList">The current genome population</param>
        /// <returns>Constructed evolutionary algorithm</returns>
        public override NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList)
        {
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy =
                new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, ParallelOptions);

            // Create complexity regulation strategy.
            var complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategy, Complexitythreshold);

            // Create the evolution algorithm.
            var ea = new NeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters, speciationStrategy,
                complexityRegulationStrategy);

            // Create IBlackBox evaluator.
            var mazeNavigationEvaluator = new MazeNavigationNoveltyEvaluator(MaxDistanceToTarget, MaxTimesteps,
                MazeVariant,
                MinSuccessDistance, _behaviorCharacterization);

            // Create genome decoder.
            var genomeDecoder = CreateGenomeDecoder();

            // Create a genome list evaluator. This packages up the genome decoder with the genome evaluator.
            IGenomeListEvaluator<NeatGenome> listEvaluator =
                new SerialGenomeListBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator);
//            IGenomeListEvaluator<NeatGenome> listEvaluator =
//                new ParallelGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
//                    ParallelOptions);

            // Create a novelty archive.
            EliteArchive<NeatGenome> archive = new NoveltyArchive<NeatGenome>(_archiveAdditionThreshold,
                _archiveThresholdDecreaseMultiplier, _archiveThresholdIncreaseMultiplier,
                _maxGenerationalArchiveAddition, _minGenerationalArchiveAddition);

            // Initialize the evolution algorithm.
            ea.Initialize(listEvaluator, genomeFactory, genomeList, archive);

            // Finished. Return the evolution algorithm
            return ea;
        }
    }
}