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
    public class CoevolutionAlgorithmContainer<TGenome1, TGenome2> : ICoevolutionAlgorithmContainer<TGenome1, TGenome2>
        where TGenome1 : class, IGenome<TGenome1>
        where TGenome2 : class, IGenome<TGenome2>
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Constructors

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

        private readonly IEvolutionAlgorithm<TGenome1> _evolutionAlgorithm1;

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

        public uint CurrentGeneration { get; protected set; }

        public ulong CurrentEvaluations { get; protected set; }

        public UpdateScheme UpdateScheme { get; set; }

        public RunState RunState { get; protected set; }

        public bool StopConditionSatisfied { get; protected set; }

        #endregion

        #region Events

        public event EventHandler UpdateEvent;

        public event EventHandler PausedEvent;

        #endregion

        #region Public Methods

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
                __log.Warn("StartContinue() called but algorithm is already running.");
            }
            else
            {
                throw new SharpNeatException(string.Format("StartContinue() call failed. Unexpected RunState [{0}]",
                    RunState));
            }
        }

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

        public void RequestPause()
        {
            if (RunState.Running == RunState)
            {
                _pauseRequestFlag = true;
            }
            else
            {
                __log.Warn("RequestPause() called but algorithm is not running.");
            }
        }

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
                __log.Warn("RequestPauseAndWait() called but algorithm is not running.");
            }
        }

        #endregion

        #region Private Methods

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

                    // Send update to the calling routine if time
                    if (UpdateTest())
                    {
                        _prevUpdateGeneration = CurrentGeneration;
                        _prevUpdateTimeTick = DateTime.Now.Ticks;
                        OnUpdateEvent();
                    }

                    // Set the current number of evaluations 
                    // (this after the algorithm has run so we have the correct number for this iteration)
                    CurrentEvaluations = GenomeEvaluator.EvaluationCount;
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
                    __log.Error("UpdateEvent listener threw exception", ex);
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
                    __log.Error("PausedEvent listener threw exception", ex);
                }
            }
        }

        #endregion
    }
}