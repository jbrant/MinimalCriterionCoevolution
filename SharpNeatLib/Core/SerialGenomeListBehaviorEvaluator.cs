using System.Collections.Generic;
using System.Linq;
using SharpNeat.Behaviors;

namespace SharpNeat.Core
{
    public class SerialGenomeListBehaviorEvaluator<TGenome, TPhenome> : SerialGenomeListEvaluator<TGenome, TPhenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        private readonly IPhenomeEvaluator<TPhenome, BehaviorInfo> _phenomeBehaviorEvaluator;

        private readonly EvaluationMethod _evaluationMethod;

        /// <summary>
        /// Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and the evaluation method.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeBehaviorEvaluator">The phenome evaluator.</param>
        public SerialGenomeListBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeBehaviorEvaluator) : base(genomeDecoder, null)
        {
            _phenomeBehaviorEvaluator = phenomeBehaviorEvaluator;
            _evaluationMethod = Evaluate_Caching;
        }

        /// <summary>
        /// Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and setting the caching method.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeBehaviorEvaluator">The phenome evaluator.</param>
        /// <param name="enablePhenomeCaching">Whether or not to enable phenome caching.</param>
        public SerialGenomeListBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeBehaviorEvaluator,
            bool enablePhenomeCaching) : base(genomeDecoder, null, enablePhenomeCaching)
        {
            _phenomeBehaviorEvaluator = phenomeBehaviorEvaluator;

            if (enablePhenomeCaching)
            {
                _evaluationMethod = Evaluate_Caching;
            }
            else
            {
                _evaluationMethod = Evaluate_NonCaching;
            }
        }

        private void Evaluate_NonCaching(IList<TGenome> genomeList)
        {
            // Decode and evaluate the behavior of each genome in turn.
            foreach (var genome in genomeList)
            {
                var phenome = _genomeDecoder.Decode(genome);
                if (null == phenome)
                {
                    // Non-viable genome.
                    genome.EvaluationInfo.SetFitness(0.0);
                    genome.EvaluationInfo.AuxFitnessArr = null;
                    genome.EvaluationInfo.BehaviorCharacterization = new NullBehaviorCharacterization();
                }
                else
                {
                    // Evaluate the behavior and update the genome's behavior characterization
                    BehaviorInfo behaviorInfo = _phenomeBehaviorEvaluator.Evaluate(phenome);
                    genome.EvaluationInfo.BehaviorCharacterization.UpdateBehaviors(behaviorInfo.Behaviors.ToList());
                }
            }

            // TODO: Here is where the distance calculation to assign the final fitness should occur
        }

        private void Evaluate_Caching(IList<TGenome> genomeList)
        {
            // Decode and evaluate the behavior of each genome in turn.
            foreach (var genome in genomeList)
            {
                var phenome = _genomeDecoder.Decode(genome);
                if (null == phenome)
                {   // Decode the phenome and store a ref against the genome.
                    phenome = _genomeDecoder.Decode(genome);
                    genome.CachedPhenome = phenome;
                }

                if (null == phenome)
                {
                    // Non-viable genome.
                    genome.EvaluationInfo.SetFitness(0.0);
                    genome.EvaluationInfo.AuxFitnessArr = null;
                    genome.EvaluationInfo.BehaviorCharacterization = new NullBehaviorCharacterization();
                }
                else
                {
                    // Evaluate the behavior and update the genome's behavior characterization
                    BehaviorInfo behaviorInfo = _phenomeBehaviorEvaluator.Evaluate(phenome);
                    genome.EvaluationInfo.BehaviorCharacterization.UpdateBehaviors(behaviorInfo.Behaviors.ToList());
                }
            }

            // TODO: Here is where the distance calculation to assign the final fitness should occur
        }

        private delegate void EvaluationMethod(IList<TGenome> genomeList);
    }
}