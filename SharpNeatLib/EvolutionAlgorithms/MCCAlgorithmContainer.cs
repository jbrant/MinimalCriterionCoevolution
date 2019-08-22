#region

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms.Statistics;
using SharpNeat.Loggers;

#endregion

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    ///     The MCC algorithm container encapsulates and manages the execution of two coevolving populations driven by
    ///     two entirely separate algorithms and potentially, separate encodings.  Note that this stands in contrast to more
    ///     typical MCC setups wherein two populations of agents are coevolving, but they share identical encodings and
    ///     similar evaluation criteria.  This configuration is intended two allow the MCC of radically different
    ///     constructs, such as coevolving agents with their environment.
    /// </summary>
    /// <typeparam name="TGenome1">The genome type for the agent population.</typeparam>
    /// <typeparam name="TGenome2">The genome type for the environment population.</typeparam>
    public class MCCAlgorithmContainer<TGenome1, TGenome2> : IMCCAlgorithmContainer<TGenome1, TGenome2>
        where TGenome1 : class, IGenome<TGenome1>
        where TGenome2 : class, IGenome<TGenome2>
    {
        /// <summary>
        ///     Event logger for the MCC container.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Constructors

        /// <summary>
        ///     Constructor for the MCC algorithm container, recording the two component evolutionary algorithms.
        /// </summary>
        /// <param name="agentEa">The evolutionary algorithm for the agent population.</param>
        /// <param name="environmentEa">The evolutionary algorithm for the environment population.</param>
        /// <param name="logFieldEnabledMap">Allows enabling/disabling of certain fields.</param>
        public MCCAlgorithmContainer(IEvolutionAlgorithm<TGenome1> agentEa,
            IEvolutionAlgorithm<TGenome2> environmentEa, IDictionary<FieldElement, bool> logFieldEnabledMap = null)
        {
            _agentEa = agentEa;
            _environmentEa = environmentEa;
            _logFieldEnabledMap = logFieldEnabledMap;
        }

        #endregion

        #region Instance Variables

        /// <summary>
        ///     Thread on which to run algorithm.
        /// </summary>
        private Thread _algorithmThread;

        /// <summary>
        ///     Indicates whether user has requested a pause in algorithm execution.
        /// </summary>
        private bool _pauseRequestFlag;

        /// <summary>
        ///     Notifies threads awaiting execution that a pause event has been requested.
        /// </summary>
        private readonly AutoResetEvent _awaitPauseEvent = new AutoResetEvent(false);

        /// <summary>
        ///     Notifies threads awaiting execution that a restart event has been requested.
        /// </summary>
        private readonly AutoResetEvent _awaitRestartEvent = new AutoResetEvent(false);

        /// <summary>
        ///     The evolutionary algorithm for the agent population.
        /// </summary>
        private readonly IEvolutionAlgorithm<TGenome1> _agentEa;

        /// <summary>
        ///     The evolutionary algorithm for the environment population.
        /// </summary>
        private readonly IEvolutionAlgorithm<TGenome2> _environmentEa;

        /// <summary>
        ///     The last generation during which the display/logging was updated.
        /// </summary>
        private uint _prevUpdateGeneration;

        /// <summary>
        ///     Captures the clock time of the last update.
        /// </summary>
        private long _prevUpdateTimeTick;

        /// <summary>
        ///     Defines the maximum number of generations that the algorithm can run before it is forcefully stopped (whether the
        ///     solution has been found or not).
        /// </summary>
        private int? _maxGenerations;

        /// <summary>
        ///     Defines the maximum number of network evaluations that the algorithm can run before it is forcefully stopped
        ///     (whether the solution has been found or not).
        /// </summary>
        private ulong? _maxEvaluations;

        /// <summary>
        ///     Specifies whether a given logger field is eanbled or disabled.
        /// </summary>
        private readonly IDictionary<FieldElement, bool> _logFieldEnabledMap;

        /// <summary>
        ///     The genome evaluation scheme for the agent evolution algorithm.
        /// </summary>
        protected IGenomeEvaluator<TGenome1> AgentEvaluator;

        /// <summary>
        ///     The genome evaluation scheme for the environment evolution algorithm.
        /// </summary>
        protected IGenomeEvaluator<TGenome2> EnvironmentEvaluator;

        #endregion

        #region Properties

        /// <summary>
        ///     The current generation.
        /// </summary>
        public uint CurrentGeneration { get; protected set; }

        /// <summary>
        ///     The current number of evaluations that have been executed.
        /// </summary>
        public ulong CurrentEvaluations { get; protected set; }

        /// <summary>
        ///     The update scheme for controlling when updates are passed back to the caller.
        /// </summary>
        public UpdateScheme UpdateScheme { get; set; }

        /// <summary>
        ///     The current state of execution (e.g. read, running, paused, etc.).
        /// </summary>
        public RunState RunState { get; protected set; }

        /// <summary>
        ///     Boolean flag which indicates whether a terminating condition has been reached.
        /// </summary>
        public bool StopConditionSatisfied { get; protected set; }

        /// <summary>
        ///     The current champion genome from the agent population.
        /// </summary>
        public TGenome1 AgentChampGenome => _agentEa.CurrentChampGenome;

        /// <summary>
        ///     The current champion genome from the environment population.
        /// </summary>
        public TGenome2 EnvironmentChampGenome => _environmentEa.CurrentChampGenome;

        /// <summary>
        ///     Descriptive statistics for the agent population.
        /// </summary>
        public IEvolutionAlgorithmStats AgentPopulationStats
            => ((AbstractComplexifyingEvolutionAlgorithm<TGenome1>) _agentEa).Statistics;

        /// <summary>
        ///     Descriptive statistics for the environment population.
        /// </summary>
        public IEvolutionAlgorithmStats EnvironmentPopulationStats
            => ((AbstractComplexifyingEvolutionAlgorithm<TGenome2>) _environmentEa).Statistics;

        #endregion

        #region Events

        /// <summary>
        ///     Notifies listeners that some state change has occured.
        /// </summary>
        public event EventHandler UpdateEvent;

        /// <summary>
        ///     Notifies listeners that the algorithm has paused.
        /// </summary>
        public event EventHandler PausedEvent;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Initializes the evolutionary algorithms for the agent and environment populations as well as state
        ///     variables within the container. Note that this initializer expects a preconstructed list of genomes.
        /// </summary>
        /// <param name="agentFitnessEvaluator">The genome evaluator for the agent population.</param>
        /// <param name="agentFactory">The genome factory for the agent population.</param>
        /// <param name="agentList">The initial members of the agent population.</param>
        /// <param name="environmentFitnessEvaluator">The genome evaluator for the environment population.</param>
        /// <param name="environmentFactory">The genome factory for the environment population.</param>
        /// <param name="environmentList">The initial members of the environment population.</param>
        /// <param name="maxGenerations">The maximum number of generations.</param>
        /// <param name="maxEvaluations">The maximum number of evaluations.</param>
        public void Initialize(IGenomeEvaluator<TGenome1> agentFitnessEvaluator,
            IGenomeFactory<TGenome1> agentFactory, List<TGenome1> agentList,
            IGenomeEvaluator<TGenome2> environmentFitnessEvaluator, IGenomeFactory<TGenome2> environmentFactory,
            List<TGenome2> environmentList, int? maxGenerations, ulong? maxEvaluations)
        {
            // Initialize both evolutionary algorithms
            _agentEa.Initialize(agentFitnessEvaluator, agentFactory, agentList, maxGenerations,
                maxEvaluations, null);
            _environmentEa.Initialize(environmentFitnessEvaluator, environmentFactory, environmentList, maxGenerations,
                maxEvaluations, null);

            // Set the required local references to the genome evaluators
            InitializeContainer(agentFitnessEvaluator, environmentFitnessEvaluator, maxGenerations, maxEvaluations);

            // Set update scheme and run state to ready
            UpdateScheme = new UpdateScheme(new TimeSpan(0, 0, 1));
            RunState = RunState.Ready;
        }

        /// <summary>
        ///     Initializes the evolutionary algorithms for the agent and environment populations as well as state
        ///     variables within the container. Note that this initializer expects a preconstructed list of genomes.
        ///     Additionally, a maximum population size is specified, which bounds the growth of populations whose size is
        ///     variable.
        /// </summary>
        /// <param name="agentFitnessEvaluator">The genome evaluator for the agent population.</param>
        /// <param name="agentFactory">The genome factory for the agent population.</param>
        /// <param name="agentList">The initial members of the agent population.</param>
        /// <param name="maxAgentPopulationSize">The upper bound on the agent population size.</param>
        /// <param name="environmentFitnessEvaluator">The genome evaluator for the environment population.</param>
        /// <param name="environmentFactory">The genome factory for the environment population.</param>
        /// <param name="environmentList">The initial members of the environment population.</param>
        /// <param name="maxEnvironmentPopulationSize">The upper bound on the environment population size.</param>
        /// <param name="maxGenerations">The maximum number of generations.</param>
        /// <param name="maxEvaluations">The maximum number of evaluations.</param>
        public void Initialize(IGenomeEvaluator<TGenome1> agentFitnessEvaluator,
            IGenomeFactory<TGenome1> agentFactory, List<TGenome1> agentList, int maxAgentPopulationSize,
            IGenomeEvaluator<TGenome2> environmentFitnessEvaluator, IGenomeFactory<TGenome2> environmentFactory,
            List<TGenome2> environmentList, int maxEnvironmentPopulationSize, int? maxGenerations,
            ulong? maxEvaluations)
        {
            // Initialize both evolutionary algorithms
            _agentEa.Initialize(agentFitnessEvaluator, agentFactory, agentList, maxAgentPopulationSize,
                maxGenerations, maxEvaluations, null);
            _environmentEa.Initialize(environmentFitnessEvaluator, environmentFactory, environmentList,
                maxEnvironmentPopulationSize,
                maxGenerations, maxEvaluations, null);

            // Set the required local references to the genome evaluators
            InitializeContainer(agentFitnessEvaluator, environmentFitnessEvaluator, maxGenerations, maxEvaluations);

            // Set update scheme and run state to ready
            UpdateScheme = new UpdateScheme(new TimeSpan(0, 0, 1));
            RunState = RunState.Ready;
        }

        /// <summary>
        ///     Initializes the evolutionary algorithms for the agent and environment populations as well as state
        ///     variables within the container. This initializer expects the size of both populations to be specified
        ///     (not the population itself given) so it can hand off population generation to the individual EA initializers.
        /// </summary>
        /// <param name="agentFitnessEvaluator">The genome evaluator for the agent population.</param>
        /// <param name="agentFactory">The genome factory for the agent population.</param>
        /// <param name="agentPopulationSize">The size of the agent population.</param>
        /// <param name="environmentFitnessEvaluator">The genome evaluator for the environment population.</param>
        /// <param name="environmentFactory">The genome factory for the environment population.</param>
        /// <param name="environmentPopulationSize">The size of the environment population.</param>
        /// <param name="maxGenerations">The maximum number of generations.</param>
        /// <param name="maxEvaluations">The maximum number of evaluations.</param>
        public void Initialize(IGenomeEvaluator<TGenome1> agentFitnessEvaluator,
            IGenomeFactory<TGenome1> agentFactory, int agentPopulationSize,
            IGenomeEvaluator<TGenome2> environmentFitnessEvaluator, IGenomeFactory<TGenome2> environmentFactory,
            int environmentPopulationSize, int? maxGenerations,
            ulong? maxEvaluations)
        {
            // Initialize both evolutionary algorithms
            _agentEa.Initialize(agentFitnessEvaluator, agentFactory, agentPopulationSize, maxGenerations,
                maxEvaluations, null);
            _environmentEa.Initialize(environmentFitnessEvaluator, environmentFactory, environmentPopulationSize,
                maxGenerations,
                maxEvaluations, null);

            // Set the required local references to the genome evaluators
            InitializeContainer(agentFitnessEvaluator, environmentFitnessEvaluator, maxGenerations, maxEvaluations);

            // Set update scheme and run state to ready
            UpdateScheme = new UpdateScheme(new TimeSpan(0, 0, 1));
            RunState = RunState.Ready;
        }

        /// <summary>
        ///     Starts the algorithm running. The algorithm will switch to the Running state from either
        ///     the Ready or Paused states.
        /// </summary>
        public void StartContinue()
        {
            // RunState must be Ready or Paused.
            if (RunState.Ready == RunState)
            {
                // Create a new thread and start it running.
                _algorithmThread = new Thread(AlgorithmThreadMethod);
                _algorithmThread.IsBackground = true;
                _algorithmThread.Priority = ThreadPriority.BelowNormal;
                RunState = RunState.Running;
                OnUpdateEvent();
                _algorithmThread.Start();
            }
            else if (RunState.Paused == RunState)
            {
                // Thread is paused. Resume execution.
                RunState = RunState.Running;
                OnUpdateEvent();
                _awaitRestartEvent.Set();
            }
            else if (RunState.Running == RunState)
            {
                // Already running. Log a warning.
                Log.Warn("StartContinue() called but algorithm is already running.");
            }
            else
            {
                throw new SharpNeatException(string.Format("StartContinue() call failed. Unexpected RunState [{0}]",
                    RunState));
            }
        }

        /// <summary>
        ///     Resets the algorithm by marking it as terminated, closing the logger, and cleaning up the state of the genome
        ///     evaluators.
        /// </summary>
        public void Reset()
        {
            // Reset run state to terminated
            RunState = RunState.Terminated;

            // Close the individual algorithm evolution loggers
            _agentEa.CleanupLoggers();
            _environmentEa.CleanupLoggers();

            // Cleanup agent evaluator
            AgentEvaluator.Cleanup();

            // Cleanup environment evaluator
            EnvironmentEvaluator.Cleanup();

            // Null out the internal thread
            _algorithmThread = null;
        }

        /// <summary>
        ///     Requests that the algorithm pauses but doesn't wait for the algorithm thread to stop.
        ///     The algorithm thread will pause when it is next convenient to do so, and will notify
        ///     listeners via an UpdateEvent.
        /// </summary>
        public void RequestPause()
        {
            if (RunState.Running == RunState)
            {
                _pauseRequestFlag = true;
            }
            else
            {
                Log.Warn("RequestPause() called but algorithm is not running.");
            }
        }

        /// <summary>
        ///     Request that the algorithm pause and waits for the algorithm to do so. The algorithm
        ///     thread will pause when it is next convenient to do so and notifies any UpdateEvent
        ///     listeners prior to returning control to the caller. Therefore it's generally a bad idea
        ///     to call this method from a GUI thread that also has code that may be called by the
        ///     UpdateEvent - doing so will result in deadlocked threads.
        /// </summary>
        public void RequestPauseAndWait()
        {
            if (RunState.Running == RunState)
            {
                // Set a flag that tells the algorithm thread to enter the paused state and wait 
                // for a signal that tells us the thread has paused.
                _pauseRequestFlag = true;
                _awaitPauseEvent.WaitOne();
            }
            else
            {
                Log.Warn("RequestPauseAndWait() called but algorithm is not running.");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Sets state values needed by this container during the initialization process.
        /// </summary>
        /// <param name="agentEvaluator">Reference to the genome evaluator for the agent population.</param>
        /// <param name="environmentEvaluator">Reference to the genome evaluator for the environment population.</param>
        /// <param name="maxGenerations">The maximum number of generations.</param>
        /// <param name="maxEvaluations">The maximum number of evaluations.</param>
        private void InitializeContainer(IGenomeEvaluator<TGenome1> agentEvaluator,
            IGenomeEvaluator<TGenome2> environmentEvaluator, int? maxGenerations, ulong? maxEvaluations)
        {
            AgentEvaluator = agentEvaluator;
            EnvironmentEvaluator = environmentEvaluator;
            _maxGenerations = maxGenerations;
            _maxEvaluations = maxEvaluations;

            // Update each evaluator with the phenotypes of the other population
            agentEvaluator.UpdateEvaluationBaseline(environmentEvaluator.DecodeGenomes(_environmentEa.GenomeList),
                _agentEa.CurrentGeneration);
            environmentEvaluator.UpdateEvaluationBaseline(agentEvaluator.DecodeGenomes(_agentEa.GenomeList),
                _environmentEa.CurrentGeneration);
        }

        /// <summary>
        ///     Main control loop for the algorithm.
        /// </summary>
        private void AlgorithmThreadMethod()
        {
            try
            {
                // Initialize the generation counter and wall clock time
                _prevUpdateGeneration = 0;
                _prevUpdateTimeTick = DateTime.Now.Ticks;

                // Execute continuous iterations of the algorithm until interrupt 
                // or the stop condition is satisfied
                for (;;)
                {
                    // Increment the centralized current generation as well as the individual EA ones
                    CurrentGeneration++;
                    _agentEa.CurrentGeneration++;
                    _environmentEa.CurrentGeneration++;

                    // Execute the agent algorithm for one evaluation cycle
                    _agentEa.PerformOneGeneration();

                    // Execute the environment algorithm for one evaluation cycle
                    _environmentEa.PerformOneGeneration();

                    // TODO: We probably need to update the cached phenotypes on both algorithms here
                    AgentEvaluator.UpdateEvaluationBaseline(
                        EnvironmentEvaluator.DecodeGenomes(_environmentEa.GenomeList), _agentEa.CurrentGeneration);
                    EnvironmentEvaluator.UpdateEvaluationBaseline(AgentEvaluator.DecodeGenomes(_agentEa.GenomeList),
                        _environmentEa.CurrentGeneration);

                    // Send update to the calling routine if time
                    if (UpdateTest())
                    {
                        _prevUpdateGeneration = CurrentGeneration;
                        _prevUpdateTimeTick = DateTime.Now.Ticks;
                        OnUpdateEvent();
                    }

                    // Set the current number of evaluations 
                    // (this after the algorithm has run so we have the correct number for this iteration)
                    // TODO: We may want to end up splitting this out to be a separate count per EA
                    CurrentEvaluations = AgentEvaluator.EvaluationCount + EnvironmentEvaluator.EvaluationCount;

                    // Set genome evaluator stop condition satisfied if either evaluator indicates such
                    StopConditionSatisfied = AgentEvaluator.StopConditionSatisfied ||
                                             EnvironmentEvaluator.StopConditionSatisfied;

                    // Check if a pause has been requested. 
                    // Access to the flag is not thread synchronized, but it doesn't really matter if
                    // we miss it being set and perform one other generation before pausing.
                    if (_pauseRequestFlag || StopConditionSatisfied ||
                        CurrentGeneration >= _maxGenerations || AgentEvaluator.EvaluationCount >= _maxEvaluations ||
                        EnvironmentEvaluator.EvaluationCount >= _maxEvaluations)
                    {
                        // Signal to any waiting thread that we are pausing
                        _awaitPauseEvent.Set();

                        // Reset the flag. Update RunState and notify any listeners of the state change.
                        _pauseRequestFlag = false;
                        RunState = RunState.Paused;
                        OnUpdateEvent();
                        OnPausedEvent();

                        // Wait indefinitely for a signal to wake up and continue.
                        _awaitRestartEvent.WaitOne();
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // Quietly exit thread
            }
        }

        /// <summary>
        ///     Returns true if it is time to raise an update event.
        /// </summary>
        private bool UpdateTest()
        {
            if (UpdateMode.Generational == UpdateScheme.UpdateMode)
            {
                return CurrentGeneration - _prevUpdateGeneration >= UpdateScheme.Generations;
            }

            return DateTime.Now.Ticks - _prevUpdateTimeTick >= UpdateScheme.TimeSpan.Ticks;
        }

        /// <summary>
        ///     Handles update events.
        /// </summary>
        private void OnUpdateEvent()
        {
            if (null != UpdateEvent)
            {
                // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                try
                {
                    UpdateEvent(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Log.Error("UpdateEvent listener threw exception", ex);
                }
            }
        }

        /// <summary>
        ///     Handles pause events.
        /// </summary>
        private void OnPausedEvent()
        {
            if (null != PausedEvent)
            {
                // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                try
                {
                    PausedEvent(this, EventArgs.Empty);

                    // Close the individual algorithm evolution loggers
                    _agentEa.CleanupLoggers();
                    _environmentEa.CleanupLoggers();

                    // Cleanup agent evaluator
                    AgentEvaluator.Cleanup();

                    // Cleanup environment evaluator
                    EnvironmentEvaluator.Cleanup();
                }
                catch (Exception ex)
                {
                    Log.Error("PausedEvent listener threw exception", ex);
                }
            }
        }

        #endregion
    }
}