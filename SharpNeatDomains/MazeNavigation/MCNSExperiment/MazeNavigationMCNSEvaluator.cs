#region

using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCNSExperiment
{
    /// <summary>
    ///     Defines evaluation rules and process for an evaluation of the minimal criteria search (MCNS) algorithm.
    /// </summary>
    public class MazeNavigationMCNSEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        #region Constructor

        /// <summary>
        ///     MCNS Evaluator constructor.
        /// </summary>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="maxTimesteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="mazeVariant">The maze environment used for the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        internal MazeNavigationMCNSEvaluator(int maxDistanceToTarget, int maxTimesteps, MazeVariant mazeVariant,
            int minSuccessDistance, IBehaviorCharacterizationFactory behaviorCharacterizationFactory)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;

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
        ///     Runs a phenome (i.e. maze navigator brain) through a single maze trial.
        /// </summary>
        /// <param name="phenome">The maze navigator brain (ANN).</param>
        /// <param name="currentGeneration">The current generation or evaluation batch.</param>
        /// <param name="isBridgingEvaluation">Indicates whether bridging is enabled for this evaluation.</param>
        /// <param name="evaluationLogger">Reference to the evaluation logger.</param>
        /// <param name="genomeXml">The string-representation of the genome (for logging purposes).</param>
        /// <returns>A behavior info (which is a type of behavior-based trial information).</returns>
        public BehaviorInfo Evaluate(IBlackBox phenome, uint currentGeneration, bool isBridgingEvaluation,
            IDataLogger evaluationLogger, string genomeXml)
        {
            // Increment evaluation count
            EvaluationCount++;

            // Default the stop condition satisfied to false
            bool stopConditionSatisfied = false;

            // Generate new behavior characterization
            IBehaviorCharacterization behaviorCharacterization =
                _behaviorCharacterizationFactory.CreateBehaviorCharacterization();

            // Instantiate the maze world
            MazeNavigationWorld<BehaviorInfo> world =
                _mazeWorldFactory.CreateMazeNavigationWorld(behaviorCharacterization);

            // Run a single trial
            BehaviorInfo trialInfo = world.RunTrial(phenome, SearchType.MinimalCriteriaNoveltySearch,
                out stopConditionSatisfied);

            // Check if the current location satisfies the minimal criteria
            if (behaviorCharacterization.IsMinimalCriteriaSatisfied(trialInfo) == false)
            {
                trialInfo.DoesBehaviorSatisfyMinimalCriteria = false;
            }

            // If the navigator reached the goal, stop the experiment
            if (stopConditionSatisfied)
                StopConditionSatisfied = true;

            return trialInfo;
        }

        /// <summary>
        ///     Initializes the logger and writes header.
        /// </summary>
        /// <param name="evaluationLogger">The evaluation logger.</param>
        public void Initialize(IDataLogger evaluationLogger)
        {
            evaluationLogger?.LogHeader(
                _mazeWorldFactory.CreateMazeNavigationWorld().GetLoggableElements());
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }

        #endregion
    }
}