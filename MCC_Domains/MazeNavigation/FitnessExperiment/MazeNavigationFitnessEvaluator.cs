#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation
{
    /// <summary>
    ///     Defines evaluation rules and process for an evaluation of a fitness-based algorithm.
    /// </summary>
    public class MazeNavigationFitnessEvaluator : IPhenomeEvaluator<IBlackBox, FitnessInfo>
    {
        #region Private members

        /// <summary>
        ///     The maze navigation world factory.
        /// </summary>
        private readonly MazeNavigationWorldFactory<FitnessInfo> _mazeWorldFactory;

        #endregion

        #region Constructor

        /// <summary>
        ///     Fitness Evaluator constructor.
        /// </summary>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="mazeVariant">The maze environment used for the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        internal MazeNavigationFitnessEvaluator(int maxDistanceToTarget, MazeVariant mazeVariant,
            int minSuccessDistance)
        {
            // Create the maze world factory
            _mazeWorldFactory = new MazeNavigationWorldFactory<FitnessInfo>(mazeVariant, minSuccessDistance,
                maxDistanceToTarget);
        }

        #endregion

        #region Public properties

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

        #region Public methods

        /// <summary>
        ///     Runs a agent (i.e. maze navigator brain) through a single maze trial.
        /// </summary>
        /// <param name="agent">The maze navigator brain (ANN).</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <param name="isBridgingEvaluation">Indicates whether bridging is enabled for this evaluation.</param>
        /// <param name="evaluationLogger">Reference to the evaluation logger.</param>
        /// <param name="genomeXml">The string-representation of the genome (for logging purposes).</param>
        /// <returns>A behavior info (which is a type of behavior-based trial information).</returns>
        public FitnessInfo Evaluate(IBlackBox agent, uint currentGeneration, bool isBridgingEvaluation,
            IDataLogger evaluationLogger, string genomeXml)
        {
            // Increment eval count
            EvaluationCount++;

            // Default the stop condition satisfied to false
            bool goalReached = false;

            // Instantiate the maze world
            MazeNavigationWorld<FitnessInfo> world = _mazeWorldFactory.CreateMazeNavigationWorld();

            // Run a single trial
            FitnessInfo triaInfo = world.RunTrial(agent, SearchType.Fitness, out goalReached);

            // Set the stop condition to the outcome
            StopConditionSatisfied = goalReached;

            // Log trial information (only log for non-bridging evaluations)
            evaluationLogger?.LogRow(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Primary)
            },
                world.GetLoggableElements());

            return triaInfo;
        }

        /// <summary>
        ///     Initializes the logger and writes header.
        /// </summary>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        public void Initialize(IDataLogger evaluationLogger)
        {
            evaluationLogger?.LogHeader(_mazeWorldFactory.CreateMazeNavigationWorld().GetLoggableElements());
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

        /// <summary>
        ///     Updates the environment or other evaluation criteria against which the phenomes under evaluation are being
        ///     compared.  This is typically used in a coevolutionary context.
        /// </summary>
        /// <param name="evaluatorPhenomes">The new phenomes to compare against.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }

        /// <summary>
        ///     Returns MazeNavigationFitnessEvaluator loggable elements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable elements for MazeNavigationFitnessEvaluator.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return null;
        }

        #endregion
    }
}