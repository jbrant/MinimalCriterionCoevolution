#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    /// <summary>
    ///     Defines evaluation rules and process for an initialization evaluation of the minimal criteria search (MCS)
    ///     algorithm.
    /// </summary>
    internal class MazeNavigationMCSInitializationEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        #region Constructor

        /// <summary>
        ///     MCS Initialization Evaluator constructor.
        /// </summary>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="maxTimesteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="mazeVariant">The maze environment used for the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="startingEvaluations">Number of evaluations that have been executed prior to this run.</param>
        internal MazeNavigationMCSInitializationEvaluator(int maxDistanceToTarget, int maxTimesteps,
            MazeVariant mazeVariant, int minSuccessDistance,
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory, ulong startingEvaluations)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            EvaluationCount = startingEvaluations;

            // Create the maze world factory
            _mazeWorldFactory = new MazeNavigationWorldFactory<BehaviorInfo>(mazeVariant, minSuccessDistance,
                maxDistanceToTarget, maxTimesteps);
        }

        #endregion

        #region Private members

        /// <summary>
        ///     The behavior characterization factory.
        /// </summary>
        private readonly IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;

        /// <summary>
        ///     The maze navigation world factory.
        /// </summary>
        private readonly MazeNavigationWorldFactory<BehaviorInfo> _mazeWorldFactory;

        /// <summary>
        ///     Lock object for synchronizing evaluation counter increments.
        /// </summary>
        private readonly object _evaluationLock = new object();

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
        public BehaviorInfo Evaluate(IBlackBox agent, uint currentGeneration, bool isBridgingEvaluation,
            IDataLogger evaluationLogger, string genomeXml)
        {
            ulong threadLocalEvaluationCount;
            lock (_evaluationLock)
            {
                // Increment evaluation count
                threadLocalEvaluationCount = EvaluationCount++;
            }

            // Default the stop condition satisfied to false
            bool goalReached = false;

            // Generate new behavior characterization
            IBehaviorCharacterization behaviorCharacterization =
                _behaviorCharacterizationFactory.CreateBehaviorCharacterization();

            // Instantiate the maze world
            MazeNavigationWorld<BehaviorInfo> world =
                _mazeWorldFactory.CreateMazeNavigationWorld(behaviorCharacterization);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(agent, SearchType.NoveltySearch, out goalReached);

            // Set the objective distance
            trialInfo.ObjectiveDistance = world.GetDistanceToTarget();

            // Check if the current location satisfies the minimal criteria
            if (behaviorCharacterization.IsMinimalCriteriaSatisfied(trialInfo) == false)
            {
                trialInfo.DoesBehaviorSatisfyMinimalCriteria = false;
            }

            // If the navigator reached the goal or if the minimal criteria has been satisfied, then stop the experiment
            if (trialInfo.DoesBehaviorSatisfyMinimalCriteria)
                StopConditionSatisfied = true;

            // Log the trial information
            evaluationLogger?.LogRow(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization),
                new LoggableElement(EvaluationFieldElements.IsViable, trialInfo.DoesBehaviorSatisfyMinimalCriteria)
            }, world.GetLoggableElements());

            return trialInfo;
        }

        /// <summary>
        ///     Initializes the logger and writes header.
        /// </summary>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        public void Initialize(IDataLogger evaluationLogger)
        {
            // Set the run phase
            evaluationLogger?.UpdateRunPhase(RunPhase.Initialization);

            // Log the header
            evaluationLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, 0),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization),
                new LoggableElement(EvaluationFieldElements.IsViable, false)
            },
                _mazeWorldFactory.CreateMazeNavigationWorld().GetLoggableElements());
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
        ///     Updates the environment or other evaluation criteria against which the phenomes under evaluation are being
        ///     compared.  This is typically used in a coevolutionary context.
        /// </summary>
        /// <param name="evaluatorPhenomes">The new phenomes to compare against.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<IBlackBox> evaluatorPhenomes)
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
        ///     Returns MazeNavigationMCSInitializationEvaluator loggable elements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable elements for MazeNavigationMCSInitializationEvaluator.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return null;
        }

        #endregion
    }
}