using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.MCSExperiment;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment
{
    public class MazeNavigatorMCSEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        #region Constructors

        public MazeNavigatorMCSEvaluator(int maxTimesteps, int minSuccessDistance,
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            EvaluationCount = 0;


        }

        #endregion

        #region Private Members

        /// <summary>
        ///     The behavior characterization factory.
        /// </summary>
        private readonly IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;

        /// <summary>
        ///     Lock object for synchronizing evaluation counter increments.
        /// </summary>
        private readonly object _evaluationLock = new object();

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that the evolutionary algorithm/search
        ///     should stop.  This property's value can remain false to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied { get; private set; }

        #endregion

        #region Public Methods

        public BehaviorInfo Evaluate(IBlackBox phenome, uint currentGeneration, bool isBridgingEvaluation, IDataLogger evaluationLogger,
            string genomeXml)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Initializes the logger and writes header.
        /// </summary>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        public void Initialize(IDataLogger evaluationLogger)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Update the evaluator based on some characteristic of the given population.
        /// </summary>
        /// <typeparam name="TGenome">The genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void Update<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>
        {
            throw new NotImplementedException();
        }

        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
        }

        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
