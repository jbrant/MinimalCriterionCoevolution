using System.Collections.Generic;
using SharpNeat.Utility;

namespace SharpNeat.Core
{
    public class SerialGenomeListBehaviorEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        private readonly EliteArchive<TGenome> _eliteArchive;
        private readonly bool _enablePhenomeCaching;
        private readonly EvaluationMethod _evaluationMethod;
        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly int _nearestNeighbors;
        private readonly IPhenomeEvaluator<TPhenome, BehaviorInfo> _phenomeEvaluator;

        /// <summary>
        ///     Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and the
        ///     evaluation method.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        private SerialGenomeListBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _enablePhenomeCaching = true;
            _evaluationMethod = Evaluate_Caching;
        }

        /// <summary>
        ///     Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and setting the
        ///     caching method.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="enablePhenomeCaching">Whether or not to enable phenome caching.</param>
        private SerialGenomeListBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator,
            bool enablePhenomeCaching)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _enablePhenomeCaching = enablePhenomeCaching;

            if (_enablePhenomeCaching)
            {
                _evaluationMethod = Evaluate_Caching;
            }
            else
            {
                _evaluationMethod = Evaluate_NonCaching;
            }
        }

        /// <summary>
        ///     Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and the
        ///     evaluation method.  Also sets the number of nearest neighbors to utilize in behavior distance calculations and
        ///     accepts an optional elite archive.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="nearestNeighbors">The number of nearest neighbors to use in behavior distance calculations.</param>
        /// <param name="archive">A reference to the elite archive (optional).</param>
        public SerialGenomeListBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator, int nearestNeighbors,
            EliteArchive<TGenome> archive = null) : this(genomeDecoder, phenomeEvaluator)
        {
            _nearestNeighbors = nearestNeighbors;
            _eliteArchive = archive;
        }

        /// <summary>
        ///     Constructs serial genome list behavior evaluator, customizing only the phenome behavior evaluator and setting the
        ///     caching method.  Also sets the number of nearest neighbors to utilize in behavior distance calculations and accepts
        ///     an optional elite archive.
        /// </summary>
        /// <param name="genomeDecoder">The genome decoder to use.</param>
        /// <param name="phenomeEvaluator">The phenome evaluator.</param>
        /// <param name="enablePhenomeCaching">Whether or not to enable phenome caching.</param>
        /// <param name="nearestNeighbors">The number of nearest neighbors to use in behavior distance calculations.</param>
        /// <param name="archive">A reference to the elite archive (optional).</param>
        public SerialGenomeListBehaviorEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
            IPhenomeEvaluator<TPhenome, BehaviorInfo> phenomeEvaluator,
            bool enablePhenomeCaching, int nearestNeighbors, EliteArchive<TGenome> archive = null)
        {
            _nearestNeighbors = nearestNeighbors;
            _eliteArchive = archive;
        }

        /// <summary>
        ///     Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        public ulong EvaluationCount
        {
            get { return _phenomeEvaluator.EvaluationCount; }
        }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that
        ///     the the evolutionary algorithm/search should stop. This property's value can remain false
        ///     to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied
        {
            get { return _phenomeEvaluator.StopConditionSatisfied; }
        }

        /// <summary>
        ///     Evaluates a list of genomes. Here we decode each genome in series using the contained
        ///     IGenomeDecoder and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
        /// </summary>
        public void Evaluate(IList<TGenome> genomeList)
        {
            _evaluationMethod(genomeList);
        }

        /// <summary>
        ///     Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
            _phenomeEvaluator.Reset();
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
                    genome.EvaluationInfo.BehaviorCharacterization = new double[0];
                }
                else
                {
                    // Evaluate the behavior and update the genome's behavior characterization
                    var behaviorInfo = _phenomeEvaluator.Evaluate(phenome);
                    genome.EvaluationInfo.BehaviorCharacterization = behaviorInfo.Behaviors;
                }
            }

            // TODO: Here is where the distance calculation to assign the final fitness should occur
            foreach (var genome in genomeList)
            {
            }
        }

        private void Evaluate_Caching(IList<TGenome> genomeList)
        {
            // Decode and evaluate the behavior of each genome in turn.
            foreach (var genome in genomeList)
            {
                var phenome = _genomeDecoder.Decode(genome);
                if (null == phenome)
                {
                    // Decode the phenome and store a ref against the genome.
                    phenome = _genomeDecoder.Decode(genome);
                    genome.CachedPhenome = phenome;
                }

                if (null == phenome)
                {
                    // Non-viable genome.
                    genome.EvaluationInfo.SetFitness(0.0);
                    genome.EvaluationInfo.AuxFitnessArr = null;
                    genome.EvaluationInfo.BehaviorCharacterization = new double[0];
                }
                else
                {
                    // Evaluate the behavior and update the genome's behavior characterization
                    var behaviorInfo = _phenomeEvaluator.Evaluate(phenome);
                    genome.EvaluationInfo.BehaviorCharacterization = behaviorInfo.Behaviors;
                }
            }

            // TODO: Here is where the distance calculation to assign the final fitness should occur
            foreach (var genome in genomeList)
            {
                var fitness =
                    BehaviorUtils<TGenome>.CalculateBehavioralDistance(genome.EvaluationInfo.BehaviorCharacterization,
                        genomeList, _nearestNeighbors, _eliteArchive);

                var fitnessInfo = new FitnessInfo(fitness, fitness);
                genome.EvaluationInfo.SetFitness(fitnessInfo._fitness);
                genome.EvaluationInfo.AuxFitnessArr = fitnessInfo._auxFitnessArr;

                // Add the genome to the archive if it qualifies
                _eliteArchive?.TestAndAddCandidateToArchive(genome);
            }
        }

        private delegate void EvaluationMethod(IList<TGenome> genomeList);
    }
}