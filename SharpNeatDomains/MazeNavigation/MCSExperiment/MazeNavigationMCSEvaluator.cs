#region

using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.Components;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.MCSExperiment
{
    /// <summary>
    ///     Defines evaluation rules and process for an evaluation of the minimal criteria search (MCS) algorithm.
    /// </summary>
    public class MazeNavigationMCSEvaluator : IPhenomeEvaluator<IBlackBox, BehaviorInfo>
    {
        #region Constructors

        /// <summary>
        ///     MCS Evaluator constructor.
        /// </summary>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="maxTimesteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="mazeVariant">The maze environment used for the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="initializationEvaluations">The number of evaluations that were expended during intialization.</param>
        internal MazeNavigationMCSEvaluator(int maxDistanceToTarget, int maxTimesteps, MazeVariant mazeVariant,
            int minSuccessDistance, IBehaviorCharacterizationFactory behaviorCharacterizationFactory,
            ulong initializationEvaluations = 0)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            EvaluationCount = initializationEvaluations;

            // Create the maze world factory
            _mazeWorldFactory = new MazeNavigationWorldFactory<BehaviorInfo>(mazeVariant, minSuccessDistance,
                maxDistanceToTarget, maxTimesteps);
        }

        /// <summary>
        ///     MCS Evaluator constructor with bridging.
        /// </summary>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="maxTimesteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="mazeVariant">The maze environment used for the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="bridgingMagnitude">The degree heading adjustment imposed by bridging.</param>
        /// <param name="numBridgingApplications">The maximum number of times bridging heading adjustment can be applied.</param>
        /// <param name="initializationEvaluations">The number of evaluations that were expended during intialization.</param>
        internal MazeNavigationMCSEvaluator(int maxDistanceToTarget, int maxTimesteps, MazeVariant mazeVariant,
            int minSuccessDistance, IBehaviorCharacterizationFactory behaviorCharacterizationFactory,
            int bridgingMagnitude, int numBridgingApplications,
            ulong initializationEvaluations = 0)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            EvaluationCount = initializationEvaluations;

            // Create the maze world factory
            _mazeWorldFactory = new MazeNavigationWorldFactory<BehaviorInfo>(mazeVariant, minSuccessDistance,
                maxDistanceToTarget, maxTimesteps, bridgingMagnitude, numBridgingApplications);
        }

        /// <summary>
        ///     MCS Evaluator constructor with grid-based niching.
        /// </summary>
        /// <param name="maxDistanceToTarget">The maximum distance possible from the target location.</param>
        /// <param name="maxTimesteps">The maximum number of time steps in a single simulation.</param>
        /// <param name="mazeVariant">The maze environment used for the simulation.</param>
        /// <param name="minSuccessDistance">The minimum distance from the target to be considered a successful run.</param>
        /// <param name="behaviorCharacterizationFactory">The initialized behavior characterization factory.</param>
        /// <param name="initializationEvaluations">The number of evaluations that were expended during intialization.</param>
        /// <param name="gridDensity">The density of the niche grid.</param>
        internal MazeNavigationMCSEvaluator(int maxDistanceToTarget, int maxTimesteps, MazeVariant mazeVariant,
            int minSuccessDistance, IBehaviorCharacterizationFactory behaviorCharacterizationFactory, int gridDensity,
            ulong initializationEvaluations = 0)
        {
            _behaviorCharacterizationFactory = behaviorCharacterizationFactory;
            EvaluationCount = initializationEvaluations;

            // Create the maze world factory
            _mazeWorldFactory = new MazeNavigationWorldFactory<BehaviorInfo>(mazeVariant, minSuccessDistance,
                maxDistanceToTarget, maxTimesteps, gridDensity);
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
            ulong threadLocalEvaluationCount = default(ulong);

            // Only increment the evaluation count if this is a regular (i.e. non-bridging) evaluation
            if (isBridgingEvaluation == false)
            {
                lock (_evaluationLock)
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

            // Generate a new maze world
            MazeNavigationWorld<BehaviorInfo> world =
                _mazeWorldFactory.CreateMazeNavigationWorld(behaviorCharacterization, isBridgingEvaluation);

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

            // Log trial information (only log for non-bridging evaluations)
            if (isBridgingEvaluation == false)
            {
                evaluationLogger?.LogRow(new List<LoggableElement>
                {
                    new LoggableElement(EvaluationFieldElements.Generation, currentGeneration),
                    new LoggableElement(EvaluationFieldElements.EvaluationCount, threadLocalEvaluationCount),
                    new LoggableElement(EvaluationFieldElements.StopConditionSatisfied, StopConditionSatisfied),
                    new LoggableElement(EvaluationFieldElements.RunPhase, RunPhase.Primary),
                    new LoggableElement(EvaluationFieldElements.IsViable, trialInfo.DoesBehaviorSatisfyMinimalCriteria)
                },
                    world.GetLoggableElements());
            }

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
                _mazeWorldFactory.CreateMazeNavigationWorld().GetLoggableElements());
        }

        /// <summary>
        ///     Update the evaluator based on some characteristic of the given population.
        /// </summary>
        /// <typeparam name="TGenome">The genome type parameter.</typeparam>
        /// <param name="population">The current population.</param>
        public void Update<TGenome>(List<TGenome> population) where TGenome : class, IGenome<TGenome>
        {
            // Calculate the new minimal criteria
            _behaviorCharacterizationFactory.UpdateBehaviorCharacterizationMinimalCriteria(population);

            // Disposition all genomes in the current population based on the new minimal criteria  
            IBehaviorCharacterization behaviorCharacterization =
                _behaviorCharacterizationFactory.CreateBehaviorCharacterization();
            foreach (TGenome genome in population)
            {
                genome.EvaluationInfo.IsViable =
                    behaviorCharacterization.IsMinimalCriteriaSatisfied(
                        new BehaviorInfo(genome.EvaluationInfo.BehaviorCharacterization));
            }
        }

        /// <summary>
        ///     Resets the internal state of the evaluation scheme.  This may not be needed for the maze navigation task.
        /// </summary>
        public void Reset()
        {
        }

        /// <summary>
        ///     Returns MazeNavigatorMCSEvaluator loggable elements.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification
        ///     of the calling routine.
        /// </param>
        /// <returns>The loggable elements for MazeNavigatorMCSEvaluator.</returns>
        public List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null)
        {
            return _behaviorCharacterizationFactory.GetLoggableElements(logFieldEnableMap);
        }

        #endregion
    }
}