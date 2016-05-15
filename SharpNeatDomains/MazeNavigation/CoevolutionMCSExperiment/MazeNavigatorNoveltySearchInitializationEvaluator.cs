﻿#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment
{
    /// <summary>
    ///     Defines evaluation rules and process for an initialization evaluation of the novelty search algorithm.
    /// </summary>
    public class MazeNavigatorNoveltySearchInitializationEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        #region Constructors

        /// <summary>
        ///     Maze Navigator fitness initialization evaluator constructor.
        /// </summary>
        /// <param name="maxTimesteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="maxDistanceToTarget">The maximum distance from the target possible.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="startingEvaluations">The number of evaluations from which the evaluator is starting (defaults to 0).</param>
        public MazeNavigatorNoveltySearchInitializationEvaluator(int maxTimesteps, int minSuccessDistance,
            int maxDistanceToTarget, IBehaviorCharacterizationFactory behaviorCharacterizationFactory,
            ulong startingEvaluations = 0)
        {
            EvaluationCount = startingEvaluations;
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;

            // Create factory for generating mazes
            _multiMazeWorldFactory = new MultiMazeNavigationWorldFactory<BehaviorInfo>(maxTimesteps, minSuccessDistance,
                maxDistanceToTarget);
        }

        /// <summary>
        ///     Maze Navigator fitness initialization evaluator constructor.
        /// </summary>
        /// <param name="maxTimesteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="maxDistanceToTarget">The maximum distance from the target possible.</param>
        /// <param name="initialMazeStructure">
        ///     The maze structure with which to seed the list of maze structures in the maze
        ///     factory.
        /// </param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="startingEvaluations">The number of evaluations from which the evaluator is starting (defaults to 0).</param>
        public MazeNavigatorNoveltySearchInitializationEvaluator(int maxTimesteps, int minSuccessDistance,
            int maxDistanceToTarget, MazeStructure initialMazeStructure,
            IBehaviorCharacterizationFactory behaviorCharacterizationFactory, ulong startingEvaluations = 0)
            : this(
                maxTimesteps, minSuccessDistance, maxDistanceToTarget, behaviorCharacterizationFactory,
                startingEvaluations)
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
        /// <param name="isBridgingEvaluation">Not used by this evaluator configuration.</param>
        /// <param name="evaluationLogger">Reference to the evaluation logger.</param>
        /// <param name="genomeXml">The string-representation of the genome (for logging purposes).</param>
        /// <returns>A fitness info (which is a function of the euclidean distance to the target).</returns>
        public BehaviorInfo Evaluate(IBlackBox agent, uint currentGeneration, bool isBridgingEvaluation,
            IDataLogger evaluationLogger,
            string genomeXml)
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
            MazeNavigationWorld<BehaviorInfo> world = _multiMazeWorldFactory.CreateMazeNavigationWorld(behaviorCharacterization);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(agent, SearchType.NoveltySearch, out goalReached);

            // Set the objective distance
            trialInfo.ObjectiveDistance = world.GetDistanceToTarget();

            // Set the stop condition to the outcome
            if (goalReached)
                StopConditionSatisfied = true;

            // Log trial information (only log for non-bridging evaluations)
            evaluationLogger?.LogRow(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Primary)
            },
                world.GetLoggableElements());

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
            }, _multiMazeWorldFactory.CreateMazeNavigationWorld(new MazeStructure(0, 0, 1), null).GetLoggableElements());
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