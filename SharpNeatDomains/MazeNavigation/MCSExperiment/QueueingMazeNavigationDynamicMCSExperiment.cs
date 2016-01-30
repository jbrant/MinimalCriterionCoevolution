#region

using System.Collections.Generic;
using System.Xml;
using ExperimentEntities;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    public class QueueingMazeNavigationDynamicMCSExperiment : BaseMazeNavigationExperiment
    {
        private int _batchSize;
        private IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private IDataLogger _evaluationDataLogger;
        private IDataLogger _evolutionDataLogger;
        private IDictionary<FieldElement, bool> _experimentLogFieldEnableMap;

        public override void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger,
            IDataLogger evaluationDataLogger)
        {
            base.Initialize(name, xmlConfig, evolutionDataLogger, evaluationDataLogger);

            // Read in the behavior characterization
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(xmlConfig,
                "BehaviorConfig");

            // Read in number of offspring to produce in a single batch
            _batchSize = XmlUtils.GetValueAsInt(xmlConfig, "OffspringBatchSize");

            // Read in log file path/name
            _evolutionDataLogger = evolutionDataLogger ??
                                   ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evolution);
            _evaluationDataLogger = evaluationDataLogger ??
                                    ExperimentUtils.ReadDataLogger(xmlConfig, LoggingType.Evaluation);

            // Setup the specific logging options based on parameters that are enabled/disabled
            _experimentLogFieldEnableMap = new Dictionary<FieldElement, bool>();

            // Enable or disable genome XML logging
            if (SerializeGenomeToXml)
            {
                _experimentLogFieldEnableMap.Add(EvolutionFieldElements.ChampGenomeXml, true);
            }
        }

        public override void Initialize(ExperimentDictionary experimentDictionary)
        {
            base.Initialize(experimentDictionary);

            // Read in the behavior characterization
            _behaviorCharacterizationFactory = ExperimentUtils.ReadBehaviorCharacterizationFactory(
                experimentDictionary, true);

            // Read in number of offspring to produce in a single batch
            _batchSize = experimentDictionary.Primary_OffspringBatchSize ?? default(int);

            // Read in log file path/name
            _evolutionDataLogger = new McsExperimentEvaluationEntityDataLogger(experimentDictionary.ExperimentName);
            _evaluationDataLogger =
                new McsExperimentOrganismStateEntityDataLogger(experimentDictionary.ExperimentName);
        }

        /// <summary>
        ///     Create and return a SteadyStateNeatEvolutionAlgorithm object (specific to fitness-based evaluations) ready for
        ///     running the
        ///     NEAT algorithm/search based on the given genome factory and genome list.  Various sub-parts of the algorithm are
        ///     also constructed and connected up.
        /// </summary>
        /// <param name="genomeFactory">The genome factory from which to generate new genomes</param>
        /// <param name="genomeList">The current genome population</param>
        /// <param name="startingEvaluations">The number of evaluations that have been executed prior to the current run.</param>
        /// <returns>Constructed evolutionary algorithm</returns>
        public override INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(
            IGenomeFactory<NeatGenome> genomeFactory,
            List<NeatGenome> genomeList, ulong startingEvaluations)
        {            
            // Create complexity regulation strategy.
            var complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategy, Complexitythreshold);

            // Create the evolution algorithm.
            AbstractNeatEvolutionAlgorithm<NeatGenome> ea =
                new QueueingNeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters,
                    complexityRegulationStrategy, _batchSize, RunPhase.Primary, false,
                    _evolutionDataLogger, _experimentLogFieldEnableMap);

            // Create IBlackBox evaluator.
            IPhenomeEvaluator<IBlackBox, BehaviorInfo> mazeNavigationEvaluator =
                new MazeNavigationMCSEvaluator(MaxDistanceToTarget, MaxTimesteps,
                    MazeVariant, MinSuccessDistance, _behaviorCharacterizationFactory);

            // Create genome decoder.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

//            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
//                new SerialGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
//                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch, _evaluationDataLogger,
//                    SerializeGenomeToXml);

            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeBehaviorEvaluator<NeatGenome, IBlackBox>(genomeDecoder, mazeNavigationEvaluator,
                    SelectionType.Queueing, SearchType.MinimalCriteriaSearch, _evaluationDataLogger,
                    SerializeGenomeToXml);

            // Initialize the evolution algorithm.
            ea.Initialize(fitnessEvaluator, genomeFactory, genomeList, DefaultPopulationSize,
                null, MaxEvaluations + startingEvaluations);

            // Finished. Return the evolution algorithm
            return ea;
        }
    }
}