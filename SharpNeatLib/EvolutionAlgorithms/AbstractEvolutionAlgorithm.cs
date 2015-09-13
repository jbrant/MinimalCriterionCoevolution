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
    ///     Abstract class providing some common/baseline data and methods for implementions of IEvolutionAlgorithm.
    /// </summary>
    /// <typeparam name="TGenome">The genome type that the algorithm will operate on.</typeparam>
    public abstract class AbstractEvolutionAlgorithm<TGenome> : IEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Instance fields

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
        ///     Logs evolution data from classes that implement ILoggable.
        /// </summary>
        protected IDataLogger EvolutionLogger;

        /// <summary>
        ///     The last generation during which the display/logging was updated.
        /// </summary>
        private uint _prevUpdateGeneration;

        /// <summary>
        ///     Captures the clock time of the last update.
        /// </summary>
        private long _prevUpdateTimeTick;

        /// <summary>
        ///     The genome evaluation scheme for the evolution algorithm.
        /// </summary>
        protected IGenomeEvaluator<TGenome> GenomeEvaluator;

        /// <summary>
        ///     The factory that will be used to create the genome list.
        /// </summary>
        protected IGenomeFactory<TGenome> GenomeFactory;

        /// <summary>
        ///     The current population of genomes.
        /// </summary>
        public IList<TGenome> GenomeList { get; protected set; }

        /// <summary>
        ///     The total number of genomes in the population (typically, this should remain fixed).
        /// </summary>
        protected int PopulationSize;

        /// <summary>
        ///     An archive of genomes that are particularly performant or "unique" with respect to some characterization.
        ///     Typically, this will persistent through multiple generations or evaluations.
        /// </summary>
        protected AbstractNoveltyArchive<TGenome> AbstractNoveltyArchive;

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

        #region IEvolutionAlgorithm<TGenome> Members

        /// <summary>
        ///     Gets the current generation.
        /// </summary>
        public uint CurrentGeneration { get; protected set; }

        /// <summary>
        ///     Gets or sets the algorithm's update scheme.
        /// </summary>
        public UpdateScheme UpdateScheme { get; set; }

        /// <summary>
        ///     Gets the current execution/run state of the IEvolutionAlgorithm.
        /// </summary>
        public RunState RunState { get; protected set; }

        /// <summary>
        ///     Gets the population's current champion genome.
        /// </summary>
        public TGenome CurrentChampGenome { get; protected set; }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that the algorithm has therefore stopped.
        /// </summary>
        public bool StopConditionSatisfied { get; protected set; }

        /// <summary>
        ///     Initializes the evolution algorithm with the provided IGenomeFitnessEvaluator, IGenomeFactory
        ///     and an initial population of genomes.
        /// </summary>
        /// <param name="genomeFitnessEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">
        ///     The factory that was used to create the genomeList and which is therefore referenced by the
        ///     genomes.
        /// </param>
        /// <param name="genomeList">An initial genome population.</param>
        /// <param name="abstractNoveltyArchive">
        ///     The persistent archive of genomes posessing a unique trait with respect to a behavior
        ///     characterization (optional).
        /// </param>
        public virtual void Initialize(IGenomeEvaluator<TGenome> genomeFitnessEvaluator,
            IGenomeFactory<TGenome> genomeFactory,
            List<TGenome> genomeList, AbstractNoveltyArchive<TGenome> abstractNoveltyArchive)
        {
            CurrentGeneration = 0;
            GenomeEvaluator = genomeFitnessEvaluator;
            GenomeFactory = genomeFactory;
            GenomeList = genomeList;
            AbstractNoveltyArchive = abstractNoveltyArchive;
            PopulationSize = GenomeList.Count;
            RunState = RunState.Ready;
            UpdateScheme = new UpdateScheme(new TimeSpan(0, 0, 1));
        }

        /// <summary>
        ///     Initializes the evolution algorithm with the provided IGenomeFitnessEvaluator
        ///     and an IGenomeFactory that can be used to create an initial population of genomes.
        /// </summary>
        /// <param name="genomeFitnessEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">
        ///     The factory that was used to create the genomeList and which is therefore referenced by the
        ///     genomes.
        /// </param>
        /// <param name="populationSize">The number of genomes to create for the initial population.</param>
        /// <param name="abstractNoveltyArchive">
        ///     The persistent archive of genomes posessing a unique trait with respect to a behavior
        ///     characterization (optional).
        /// </param>
        public virtual void Initialize(IGenomeEvaluator<TGenome> genomeFitnessEvaluator,
            IGenomeFactory<TGenome> genomeFactory,
            int populationSize, AbstractNoveltyArchive<TGenome> abstractNoveltyArchive)
        {
            CurrentGeneration = 0;
            GenomeEvaluator = genomeFitnessEvaluator;
            GenomeFactory = genomeFactory;
            GenomeList = genomeFactory.CreateGenomeList(populationSize, CurrentGeneration);
            PopulationSize = populationSize;
            AbstractNoveltyArchive = abstractNoveltyArchive;
            RunState = RunState.Ready;
            UpdateScheme = new UpdateScheme(new TimeSpan(0, 0, 1));
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
                __log.Warn("RequestPause() called but algorithm is not running.");
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
                __log.Warn("RequestPauseAndWait() called but algorithm is not running.");
            }
        }

        #endregion

        #region Private/Protected Methods

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
                    CurrentGeneration++;
                    PerformOneGeneration();

                    if (UpdateTest())
                    {
                        _prevUpdateGeneration = CurrentGeneration;
                        _prevUpdateTimeTick = DateTime.Now.Ticks;
                        OnUpdateEvent();
                    }

                    // Check if a pause has been requested. 
                    // Access to the flag is not thread synchronized, but it doesn't really matter if
                    // we miss it being set and perform one other generation before pausing.
                    if (_pauseRequestFlag || GenomeEvaluator.StopConditionSatisfied)
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
                // Quietly exit thread.
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
                }
                catch (Exception ex)
                {
                    __log.Error("PausedEvent listener threw exception", ex);
                }
            }
        }

        /// <summary>
        ///     Progress forward by one generation. Perform one generation/cycle of the evolution algorithm.
        /// </summary>
        protected abstract void PerformOneGeneration();

        #endregion
    }
}