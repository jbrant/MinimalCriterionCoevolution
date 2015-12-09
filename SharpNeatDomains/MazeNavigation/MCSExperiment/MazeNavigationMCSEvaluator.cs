﻿#region

using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    public class MazeNavigationMCSEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        private readonly IBehaviorCharacterizationFactory _behaviorCharacterizationFactory;
        private readonly int _bridgingMagnitude;
        private readonly int? _maxDistanceToTarget;
        private readonly int? _maxTimesteps;
        private readonly MazeVariant _mazeVariant;
        private readonly int? _minSuccessDistance;
        private readonly object evaluationLock = new object();
        private bool _stopConditionSatisfied;

        internal MazeNavigationMCSEvaluator(int? maxDistanceToTarget, int? maxTimesteps, MazeVariant mazeVariant,
            int? minSuccessDistance, IBehaviorCharacterizationFactory behaviorCharacterizationFactory,
            ulong initializationEvaluations = 0, int bridgingMagnitude = 0)
        {
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimesteps;
            _mazeVariant = mazeVariant;
            _minSuccessDistance = minSuccessDistance;
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            EvaluationCount = initializationEvaluations;
            _bridgingMagnitude = bridgingMagnitude;
        }

        /// <summary>
        ///     Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that the evolutionary algorithm/search
        ///     should stop.  This property's value can remain false to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied { get; private set; }

        public BehaviorInfo Evaluate(IBlackBox phenome, uint currentGeneration, bool isBridgingEvaluation,
            IDataLogger evaluationLogger, string genomeXml)
        {
            ulong threadLocalEvaluationCount = default(ulong);

            // Only increment the evaluation count if this is a regular (i.e. non-bridging) evaluation
            if (isBridgingEvaluation == false)
            {
                lock (evaluationLock)
                {
                    // Increment evaluation count
                    threadLocalEvaluationCount = EvaluationCount++;
                }
            }

            // Default the stop condition satisfied to false
            bool goalReached = false;

            // Generate new behavior characterization
            IBehaviorCharacterization behaviorCharacterization =
                _behaviorCharacterizationFactory.CreateBehaviorCharacterization();

            // Instantiate the maze world
            MazeNavigationWorld<BehaviorInfo> world = new MazeNavigationWorld<BehaviorInfo>(_mazeVariant,
                _minSuccessDistance, _maxDistanceToTarget, _maxTimesteps, behaviorCharacterization,
                isBridgingEvaluation ? _bridgingMagnitude : 0);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(phenome, SearchType.MinimalCriteriaSearch,
                out goalReached);

            // Set the objective distance
            trialInfo.ObjectiveDistance = world.GetDistanceToTarget();

            // Check if the current location satisfies the minimal criteria
            if (behaviorCharacterization.IsMinimalCriteriaSatisfied(trialInfo) == false)
            {
                trialInfo.DoesBehaviorSatisfyMinimalCriteria = false;
            }

            // If the navigator reached the goal, stop the experiment
            // Also, note that if the goal has been reached, then the minimal criterion is irrelevant
            // and should also be satisfied (this is really part of a larger experiment design issue in 
            // which the goal can be reached prior to the minimal criteria being satisfied)
            if (goalReached && isBridgingEvaluation == false)
            {
                StopConditionSatisfied = true;
                trialInfo.DoesBehaviorSatisfyMinimalCriteria = true;
            }

            // Log trial information
            evaluationLogger?.LogRow(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                new LoggableElement(EvaluationFieldElements.BridgingEvaluation, isBridgingEvaluation),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Primary),
                new LoggableElement(EvaluationFieldElements.IsViable, trialInfo.DoesBehaviorSatisfyMinimalCriteria)
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
            evaluationLogger?.UpdateRunPhase(RunPhase.Primary);

            // Log the header
            evaluationLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(EvaluationFieldElements.Generation, 0),
                new LoggableElement(EvaluationFieldElements.EvaluationCount, EvaluationCount),
                new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Initialization),
                new LoggableElement(EvaluationFieldElements.IsViable, false)
            },
                new MazeNavigationWorld<FitnessInfo>(_mazeVariant, _minSuccessDistance, _maxDistanceToTarget,
                    _maxTimesteps).GetLoggableElements());
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }
    }
}