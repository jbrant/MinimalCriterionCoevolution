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
    ///     Defines evaluation rules and process for an initialization evaluation of the fitness algorithm.
    /// </summary>
    public class MazeNavigatorFitnessInitializationEvaluator : IPhenomeEvaluator<IBlackBox, FitnessInfo>
    {
        #region Constructors

        /// <summary>
        ///     Maze Navigator fitness initialization evaluator constructor.
        /// </summary>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="maxDistanceToTarget">The maximum distance from the target possible.</param>
        /// <param name="startingEvaluations">The number of evaluations from which the evaluator is starting (defaults to 0).</param>
        public MazeNavigatorFitnessInitializationEvaluator(int minSuccessDistance,
            int maxDistanceToTarget, ulong startingEvaluations = 0)
        {
            EvaluationCount = startingEvaluations;

            // Create factory for generating mazes
            _multiMazeWorldFactory = new MultiMazeNavigationWorldFactory<FitnessInfo>(minSuccessDistance,
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
        /// <param name="startingEvaluations">The number of evaluations from which the evaluator is starting (defaults to 0).</param>
        public MazeNavigatorFitnessInitializationEvaluator(int minSuccessDistance,
            int maxDistanceToTarget, MazeStructure initialMazeStructure, ulong startingEvaluations = 0)
            : this(minSuccessDistance, maxDistanceToTarget, startingEvaluations)
        {
            // Add initial maze structure
            _multiMazeWorldFactory.SetMazeConfigurations(new List<MazeStructure>(1) {initialMazeStructure});
        }

        #endregion

        #region Private members

        /// <summary>
        ///     The multi maze navigation world factory.
        /// </summary>
        private readonly MultiMazeNavigationWorldFactory<FitnessInfo> _multiMazeWorldFactory;

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

        /// <inheritdoc />
        /// <summary>
        ///     Runs a agent (i.e. maze navigator brain) through a single maze trial.
        /// </summary>
        /// <param name="agent">The maze navigator brain (ANN).</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <param name="evaluationLogger">Reference to the evaluation logger.</param>
        /// <returns>A fitness info (which is a function of the euclidean distance to the target).</returns>
        public FitnessInfo Evaluate(IBlackBox agent, uint currentGeneration,
            IDataLogger evaluationLogger)
        {
            lock (_evaluationLock)
            {
                // Increment evaluation count
                EvaluationCount++;
            }

            // Instantiate the maze world
            var world = _multiMazeWorldFactory.CreateMazeNavigationWorld();

            // Run a single trial
            var trialInfo = world.RunTrial(agent, SearchType.Fitness, out var goalReached);

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

        /// <inheritdoc />
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

        /// <inheritdoc />
        /// <summary>
        ///     Update the evaluator based on some characteristic of the given population.
        /// </summary>
        /// <typeparam name="TGenome">The genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void Update<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Updates the environment or other evaluation criteria against which the phenomes under evaluation are being
        ///     compared.  This is typically used in a MCC context.
        /// </summary>
        /// <param name="evaluatorPhenomes">The new phenomes to compare against.</param>
        public void UpdateEvaluatorPhenotypes(IEnumerable<object> evaluatorPhenomes)
        {
            throw new NotImplementedException();
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