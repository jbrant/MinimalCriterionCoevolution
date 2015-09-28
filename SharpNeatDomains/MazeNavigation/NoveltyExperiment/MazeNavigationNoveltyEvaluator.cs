#region

using System;
using System.Collections.Generic;
using System.Globalization;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.NoveltyExperiment
{
    internal class MazeNavigationNoveltyEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        private readonly IBehaviorCharacterization _behaviorCharacterization;
        private readonly int? _maxDistanceToTarget;
        private readonly int? _maxTimesteps;
        private readonly MazeVariant _mazeVariant;
        private readonly int? _minSuccessDistance;
        private bool _stopConditionSatisfied;

        internal MazeNavigationNoveltyEvaluator(int? maxDistanceToTarget, int? maxTimesteps, MazeVariant mazeVariant,
            int? minSuccessDistance, IBehaviorCharacterization behaviorCharacterization)
        {
            _maxDistanceToTarget = maxDistanceToTarget;
            _maxTimesteps = maxTimesteps;
            _mazeVariant = mazeVariant;
            _minSuccessDistance = minSuccessDistance;
            _behaviorCharacterization = behaviorCharacterization;
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

        public BehaviorInfo Evaluate(IBlackBox phenome, IDataLogger evaluationLogger)
        {
            // Increment evaluation count
            EvaluationCount++;

            // Default the stop condition satisfied to false
            bool stopConditionSatisfied = false;

            // Instantiate the maze world
            MazeNavigationWorld<BehaviorInfo> world = new MazeNavigationWorld<BehaviorInfo>(_mazeVariant,
                _minSuccessDistance, _maxDistanceToTarget,
                _maxTimesteps, _behaviorCharacterization);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(phenome, EvaluationType.NoveltySearch, out stopConditionSatisfied);

            // If the navigator reached the goal, stop the experiment
            if (stopConditionSatisfied)
                StopConditionSatisfied = true;

            // Log trial information
            evaluationLogger?.LogRow(new List<LoggableElement>
            {
                new LoggableElement("MazeNavigationNoveltyExperiment - Evaluation Count",
                    Convert.ToString(EvaluationCount, CultureInfo.InvariantCulture)),
                new LoggableElement("MazeNavigationNoveltyExperiment - Stop Condition Satisfied",
                    Convert.ToString(StopConditionSatisfied, CultureInfo.InvariantCulture))
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
            evaluationLogger?.LogHeader(
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