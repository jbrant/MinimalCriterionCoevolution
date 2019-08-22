#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MCC_Domains.MazeNavigation.MCCExperiment
{
    /// <inheritdoc />
    /// <summary>
    ///     Defines evaluation rules and process for an initialization evaluation of the novelty search algorithm.
    /// </summary>
    public class MazeNavigatorNoveltySearchInitializationEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        #region Constructors

        /// <summary>
        ///     Maze Navigator fitness initialization evaluator constructor.
        /// </summary>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="maxDistanceToTarget">The maximum distance from the target possible.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="startingEvaluations">The number of evaluations from which the evaluator is starting (defaults to 0).</param>
        /// <param name="evaluationLogger">Per-evaluation data logger (optional).</param>
        public MazeNavigatorNoveltySearchInitializationEvaluator(int minSuccessDistance,
            int maxDistanceToTarget, IBehaviorCharacterizationFactory behaviorCharacterizationFactory,
            ulong startingEvaluations = 0, IDataLogger evaluationLogger = null)
        {
            EvaluationCount = startingEvaluations;
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            _evaluationLogger = evaluationLogger;

            // Create factory for generating mazes
            _multiMazeWorldFactory = new MultiMazeNavigationWorldFactory<BehaviorInfo>(minSuccessDistance,
                maxDistanceToTarget);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Maze Navigator fitness initialization evaluator constructor.
        /// </summary>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="maxDistanceToTarget">The maximum distance from the target possible.</param>
        /// <param name="initialMazeStructure">
        ///     The maze structure with which to seed the list of maze structures in the maze
        ///     factory.
        /// </param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="startingEvaluations">The number of evaluations from which the evaluator is starting (defaults to 0).</param>
        /// <param name="evaluationLogger">Per-evaluation data logger (optional).</param>
        public MazeNavigatorNoveltySearchInitializationEvaluator(int minSuccessDistance,
            int maxDistanceToTarget, MazeStructure initialMazeStructure,
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory, ulong startingEvaluations = 0,
            IDataLogger evaluationLogger = null)
            : this(
                minSuccessDistance, maxDistanceToTarget, behaviorCharacterizationFactory, startingEvaluations,
                evaluationLogger)
        {
            // Add initial maze structure
            _multiMazeWorldFactory.SetMazeConfigurations(new List<MazeStructure>(1) {initialMazeStructure});
        }

        #endregion

        #region Private members

        /// <summary>
        ///     The multi maze navigation world factory.
        /// </summary>
        private readonly MultiMazeNavigationWorldFactory<BehaviorInfo> _multiMazeWorldFactory;

        /// <summary>
        ///     The behavior characterization factory.
        /// </summary>
        private readonly IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;

        /// <summary>
        ///     Per-evaluation data logger (generates one row per maze trial).
        /// </summary>
        private readonly IDataLogger _evaluationLogger;

        /// <summary>
        ///     Lock object for synchronizing evaluation counter increments.
        /// </summary>
        private readonly object _evaluationLock = new object();

        #endregion

        #region Public properties

        /// <inheritdoc />
        /// <summary>
        ///     Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <inheritdoc />
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
        /// <returns>A fitness info (which is a function of the euclidean distance to the target).</returns>
        public BehaviorInfo Evaluate(IBlackBox agent, uint currentGeneration)
        {
            lock (_evaluationLock)
            {
                // Increment evaluation count
                EvaluationCount++;
            }

            // Generate new behavior characterization
            var behaviorCharacterization = _behaviorCharacterizationFactory.CreateBehaviorCharacterization();

            // Instantiate the maze world
            var world = _multiMazeWorldFactory.CreateMazeNavigationWorld(behaviorCharacterization);

            // Run a single trial
            var trialInfo = world.RunTrial(agent, SearchType.NoveltySearch, out var goalReached);

            // Set the objective distance
            trialInfo.ObjectiveDistance = world.GetDistanceToTarget();

            // Set the stop condition to the outcome
            if (goalReached)
                StopConditionSatisfied = true;

            // Log trial information (only log for non-bridging evaluations)
            _evaluationLogger?.LogRow(new List<LoggableElement>
                {
                    new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                    new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                    new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                    new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization)
                },
                world.GetLoggableElements());

            return trialInfo;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes the logger and writes header.
        /// </summary>
        public void Initialize()
        {
            // Set the run phase
            _evaluationLogger?.UpdateRunPhase(RunPhase.Initialization);

            // Log the header
            _evaluationLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, 0),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization),
                new LoggableElement(EvaluationFieldElements.IsViable, false)
            }, _multiMazeWorldFactory.CreateMazeNavigationWorld(new MazeStructure(0, 0, 1), null).GetLoggableElements());
        }

        /// <inheritdoc />
        /// <summary>
        ///     Updates the environment or other evaluation criteria against which the phenomes under evaluation are being
        ///     compared.  This is typically used in a MCC context.
        /// </summary>
        /// <param name="evaluatorPhenomes">The new phenomes to compare against.</param>
        /// <param name="lastGeneration">The generation that was just executed.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes, uint lastGeneration)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Cleans up evaluator state after end of execution or upon execution interruption.  In particular, this
        ///     closes out any existing evaluation logger instance.
        /// </summary>
        public void Cleanup()
        {
            _evaluationLogger?.Close();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }

        /// <inheritdoc />
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