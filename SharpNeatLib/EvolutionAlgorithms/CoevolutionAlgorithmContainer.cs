#region

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;
using SharpNeat.Core;

#endregion

namespace SharpNeat.EvolutionAlgorithms
{
    /// <summary>
    ///     The coevolution algorithm container encapsulates and manages the execution of two coevolving populations driven by
    ///     two entirely separate algorithms and potentially, separate encodings.  Note that this stands in contrast to more
    ///     typical coevolution setups wherein two populations of agents are coevolving, but they share identical encodings and
    ///     similar evaluation criteria.  This configuration is intended two allow the coevolution of radically different
    ///     constructs, such as coevolving agents with their environment.
    /// </summary>
    /// <typeparam name="TGenome1">The genome type for the first population.</typeparam>
    /// <typeparam name="TGenome2">The genome type for the second population.</typeparam>
    public class CoevolutionAlgorithmContainer<TGenome1, TGenome2> : ICoevolutionAlgorithmContainer<TGenome1, TGenome2>
        where TGenome1 : class, IGenome<TGenome1>
        where TGenome2 : class, IGenome<TGenome2>
    {
        /// <summary>
        ///     Event logger for the coevolution container.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Constructors

        /// <summary>
        ///     Constructor for the coevolution algorithm container, recording the two component evolutionary algorithms.
        /// </summary>
        /// <param name="algorithm1">The evolutionary algorithm for the first population.</param>
        /// <param name="algorithm2">The evolutionary algorithm for the second population.</param>
        public CoevolutionAlgorithmContainer(IEvolutionAlgorithm<TGenome1> algorithm1,
            IEvolutionAlgorithm<TGenome2> algorithm2)
        {
            _evolutionAlgorithm1 = algorithm1;
            _evolutionAlgorithm2 = algorithm2;
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
        ///     The evolutionary algorithm for the first population.
        /// </summary>
        private readonly IEvolutionAlgorithm<TGenome1> _evolutionAlgorithm1;

        /// <summary>
        ///     The evolutionary algorithm for the second population.
        /// </summary>
        private readonly IEvolutionAlgorithm<TGenome2> _evolutionAlgorithm2;

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
        ///     Defines the maximum number of network evaluations that the algorithm can run before it is forefully stopped
        ///     (whether the solution has been found or not).
        /// </summary>
        private ulong? _maxEvaluations;

        /// <summary>
        ///     Logs evolution data from classes that implement ILoggable.
        /// </summary>
        protected IDataLogger EvolutionLogger;

        /// <summary>
        ///     The genome evaluation scheme for the first evolution algorithm.
        /// </summary>
        protected IGenomeEvaluator<TGenome1> GenomeEvaluator1;

        /// <summary>
        ///     The genome evaluation scheme for the second evolution algorithm.
        /// </summary>
        protected IGenomeEvaluator<TGenome2> GenomeEvaluator2;

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
        ///     Initializes the evolutionary algorithms for the two populations as well as state variables within the container.
        ///     Note that this initializer expects a preconstructed list of genomes.
        /// </summary>
        /// <param name="genomeFitnessEvaluator1">The genome evaluator for the first population.</param>
        /// <param name="genomeFactory1">The genome factory for the first population.</param>
        /// <param name="genomeList1">The initial members of the first population.</param>
        /// <param name="genomeFitnessEvaluator2">The genome evaluator for the second population.</param>
        /// <param name="genomeFactory2">The genome factory for the second population.</param>
        /// <param name="genomeList2">The initial members of the second population.</param>
        /// <param name="maxGenerations">The maximum number of generations.</param>
        /// <param name="maxEvaluations">The maximum number of evaluations.</param>
        public void Initialize(IGenomeEvaluator<TGenome1> genomeFitnessEvaluator1,
            IGenomeFactory<TGenome1> genomeFactory1, List<TGenome1> genomeList1,
            IGenomeEvaluator<TGenome2> genomeFitnessEvaluator2, IGenomeFactory<TGenome2> genomeFactory2,
            List<TGenome2> genomeList2, int? maxGenerations,
            ulong? maxEvaluations)
        {
            // Initialize both evolutionary algorithms
            _evolutionAlgorithm1.Initialize(genomeFitnessEvaluator1, genomeFactory1, genomeList1, maxGenerations,
                maxEvaluations, null);
            _evolutionAlgorithm2.Initialize(genomeFitnessEvaluator2, genomeFactory2, genomeList2, maxGenerations,
                maxEvaluations, null);

            // Set the required local references to the genome evaluators
            InitializeContainer(genomeFitnessEvaluator1, genomeFitnessEvaluator2, maxGenerations, maxEvaluations);
        }

        /// <summary>
        ///     Initializes the evolutionary algorithms for the two populations as well as state variables within the container.
        ///     Note that this initializer expects a preconstructed list of genomes.  Additionally, a maximum population size is
        ///     specified, which bounds the growth of populations whose size is variable.
        /// </summary>
        /// <param name="genomeFitnessEvaluator1">The genome evaluator for the first population.</param>
        /// <param name="genomeFactory1">The genome factory for the first population.</param>
        /// <param name="genomeList1">The initial members of the first population.</param>
        /// <param name="maxPopulationsize1">The upper bound on the first population size.</param>
        /// <param name="genomeFitnessEvaluator2">The genome evaluator for the second population.</param>
        /// <param name="genomeFactory2">The genome factory for the second population.</param>
        /// <param name="genomeList2">The initial members of the second population.</param>
        /// <param name="maxPopulationSize2">The upper bound on the second population size.</param>
        /// <param name="maxGenerations">The maximum number of generations.</param>
        /// <param name="maxEvaluations">The maximum number of evaluations.</param>
        public void Initialize(IGenomeEvaluator<TGenome1> genomeFitnessEvaluator1,
            IGenomeFactory<TGenome1> genomeFactory1, List<TGenome1> genomeList1, int maxPopulationsize1,
            IGenomeEvaluator<TGenome2> genomeFitnessEvaluator2, IGenomeFactory<TGenome2> genomeFactory2,
            List<TGenome2> genomeList2, int maxPopulationSize2, int? maxGenerations,
            ulong? maxEvaluations)
        {
            // Initialize both evolutionary algorithms
            _evolutionAlgorithm1.Initialize(genomeFitnessEvaluator1, genomeFactory1, genomeList1, maxPopulationsize1,
                maxGenerations, maxEvaluations, null);
            _evolutionAlgorithm2.Initialize(genomeFitnessEvaluator2, genomeFactory2, genomeList2, maxPopulationSize2,
                maxGenerations, maxEvaluations, null);

            // Set the required local references to the genome evaluators
            InitializeContainer(genomeFitnessEvaluator1, genomeFitnessEvaluator2, maxGenerations, maxEvaluations);
        }

        /// <summary>
        ///     Initializes the evolutionary algorithms for the two populations as well as state variables within the container.
        ///     This initializer expects the size of both populations to be specified (not the population itself given) so it can
        ///     hand off population generation to the individual EA initializers.
        /// </summary>
        /// <param name="genomeFitnessEvaluator1">The genome evaluator for the first population.</param>
        /// <param name="genomeFactory1">The genome factory for the first population.</param>
        /// <param name="populationSize1">The size of the first population.</param>
        /// <param name="genomeFitnessEvaluator2">The genome evaluator for the second population.</param>
        /// <param name="genomeFactory2">The genome factory for the second population.</param>
        /// <param name="populationSize2">The size of the second population.</param>
        /// <param name="maxGenerations">The maximum number of generations.</param>
        /// <param name="maxEvaluations">The maximum number of evaluations.</param>
        public void Initialize(IGenomeEvaluator<TGenome1> genomeFitnessEvaluator1,
            IGenomeFactory<TGenome1> genomeFactory1, int populationSize1,
            IGenomeEvaluator<TGenome2> genomeFitnessEvaluator2, IGenomeFactory<TGenome2> genomeFactory2,
            int populationSize2, int? maxGenerations,
            ulong? maxEvaluations)
        {
            // Initialize both evolutionary algorithms
            _evolutionAlgorithm1.Initialize(genomeFitnessEvaluator1, genomeFactory1, populationSize1, maxGenerations,
                maxEvaluations, null);
            _evolutionAlgorithm2.Initialize(genomeFitnessEvaluator2, genomeFactory2, populationSize2, maxGenerations,
                maxEvaluations, null);

            // Set the required local references to the genome evaluators
            InitializeContainer(genomeFitnessEvaluator1, genomeFitnessEvaluator2, maxGenerations, maxEvaluations);
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

            // Close the evolution logger
            EvolutionLogger?.Close();

            // Cleanup genome evaluator 1
            GenomeEvaluator1.Cleanup();

            // Cleanup genome evaluator 2
            GenomeEvaluator2.Cleanup();

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
        /// <param name="genomeEvaluator1">Reference to the genome evaluator for the first population.</param>
        /// <param name="genomeEvaluator2">Reference to the genome evaluator for the second population.</param>
        /// <param name="maxGenerations">The maximum number of generations.</param>
        /// <param name="maxEvaluations">The maximum number of evaluations.</param>
        private void InitializeContainer(IGenomeEvaluator<TGenome1> genomeEvaluator1,
            IGenomeEvaluator<TGenome2> genomeEvaluator2, int? maxGenerations, ulong? maxEvaluations)
        {
            GenomeEvaluator1 = genomeEvaluator1;
            GenomeEvaluator2 = genomeEvaluator2;
            _maxGenerations = maxGenerations;
            _maxEvaluations = maxEvaluations;

            // Update each evaluator with the phenotypes of the other population
            genomeEvaluator1.UpdateEvaluationBaseline(_evolutionAlgorithm2.GenomeList);
            genomeEvaluator2.UpdateEvaluationBaseline(_evolutionAlgorithm1.GenomeList);
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
                    // Increment the current generation
                    CurrentGeneration++;

                    // Execute the first algorithm for one evaluation cycle
                    _evolutionAlgorithm1.PerformOneGeneration();

                    // Execute the second algorithm for one evaluation cycle
                    _evolutionAlgorithm2.PerformOneGeneration();

                    // TODO: We probably need to udpate the cached phenotypes on both algorithms here
                    GenomeEvaluator1.UpdateEvaluationBaseline(_evolutionAlgorithm2.GenomeList);
                    GenomeEvaluator2.UpdateEvaluationBaseline(_evolutionAlgorithm1.GenomeList);

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
                    CurrentEvaluations = GenomeEvaluator1.EvaluationCount + GenomeEvaluator2.EvaluationCount;

                    // Set genome evaluator stop condition satisfied if either evaluator indicates such
                    StopConditionSatisfied = GenomeEvaluator1.StopConditionSatisfied ||
                                             GenomeEvaluator2.StopConditionSatisfied;

                    // Check if a pause has been requested. 
                    // Access to the flag is not thread synchronized, but it doesn't really matter if
                    // we miss it being set and perform one other generation before pausing.
                    if (_pauseRequestFlag || StopConditionSatisfied ||
                        CurrentGeneration >= _maxGenerations || GenomeEvaluator1.EvaluationCount >= _maxEvaluations ||
                        GenomeEvaluator1.EvaluationCount >= _maxEvaluations)
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
                return (CurrentGeneration - _prevUpdateGeneration) >= UpdateScheme.Generations;
            }

            return (DateTime.Now.Ticks - _prevUpdateTimeTick) >= UpdateScheme.TimeSpan.Ticks;
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

                    // Close the evolution logger
                    EvolutionLogger?.Close();

                    // Cleanup genome evaluator 1
                    GenomeEvaluator1.Cleanup();

                    // Cleanup genome evaluator 2
                    GenomeEvaluator2.Cleanup();
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