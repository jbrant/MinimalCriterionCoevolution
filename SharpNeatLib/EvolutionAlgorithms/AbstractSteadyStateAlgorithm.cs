using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;
using SharpNeat.Core;

namespace SharpNeat.EvolutionAlgorithms
{
    public abstract class AbstractSteadyStateAlgorithm<TGenome> : IEvolutionAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly AutoResetEvent _awaitPauseEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _awaitRestartEvent = new AutoResetEvent(false);
        // Misc working variables.
        private Thread _algorithmThread;
        private bool _pauseRequestFlag;
        // Update event scheme / data.
        private uint _prevUpdateEvaluation;
        private long _prevUpdateTimeTick;
        protected EliteArchive<TGenome> EliteArchive;
        protected IGenomeFactory<TGenome> GenomeFactory;
        protected List<TGenome> GenomeList;
        protected IGenomeListEvaluator<TGenome> GenomeListEvaluator;
        protected int PopulationSize;

        /// <summary>
        ///     Gets the current number of evaluations.
        /// </summary>
        public uint CurrentEvaluation { get; private set; }

        /// <summary>
        ///     Notifies listeners that some state change has occured.
        /// </summary>
        public event EventHandler UpdateEvent;

        /// <summary>
        ///     Gets or sets the algorithm's update scheme.
        /// </summary>
        public UpdateScheme UpdateScheme { get; set; }

        /// <summary>
        ///     Gets the current execution/run state of the IEvolutionAlgorithm.
        /// </summary>
        public RunState RunState { get; private set; } = RunState.NotReady;

        /// <summary>
        ///     Gets the population's current champion genome.
        /// </summary>
        public TGenome CurrentChampGenome { get; protected set; }

        /// <summary>
        ///     Gets a value indicating whether some goal fitness has been achieved and that the algorithm has therefore stopped.
        /// </summary>
        public bool StopConditionSatisfied => GenomeListEvaluator.StopConditionSatisfied;

        /// <summary>
        ///     Initializes the evolution algorithm with the provided IGenomeListEvaluator, IGenomeFactory
        ///     and an initial population of genomes.
        /// </summary>
        /// <param name="genomeListEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">
        ///     The factory that was used to create the genomeList and which is therefore referenced by the
        ///     genomes.
        /// </param>
        /// <param name="genomeList">An initial genome population.</param>
        /// <param name="eliteArchive">The cross-evaluation archive of high-performing genomes (optional).</param>
        public virtual void Initialize(IGenomeListEvaluator<TGenome> genomeListEvaluator,
            IGenomeFactory<TGenome> genomeFactory,
            List<TGenome> genomeList,
            EliteArchive<TGenome> eliteArchive)
        {
            CurrentEvaluation = 0;
            GenomeListEvaluator = genomeListEvaluator;
            GenomeFactory = genomeFactory;
            GenomeList = genomeList;
            EliteArchive = eliteArchive;
            PopulationSize = GenomeList.Count;
            RunState = RunState.Ready;
            UpdateScheme = new UpdateScheme(new TimeSpan(0, 0, 1));
        }

        /// <summary>
        ///     Initializes the evolution algorithm with the provided IGenomeListEvaluator
        ///     and an IGenomeFactory that can be used to create an initial population of genomes.
        /// </summary>
        /// <param name="genomeListEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">
        ///     The factory that was used to create the genomeList and which is therefore referenced by the
        ///     genomes.
        /// </param>
        /// <param name="populationSize">The number of genomes to create for the initial population.</param>
        /// <param name="eliteArchive">The cross-evaluation archive of high-performing genomes (optional).</param>
        public virtual void Initialize(IGenomeListEvaluator<TGenome> genomeListEvaluator,
            IGenomeFactory<TGenome> genomeFactory,
            int populationSize,
            EliteArchive<TGenome> eliteArchive)
        {
            CurrentEvaluation = 0;
            GenomeListEvaluator = genomeListEvaluator;
            GenomeFactory = genomeFactory;
            GenomeList = genomeFactory.CreateGenomeList(populationSize, CurrentEvaluation);
            PopulationSize = populationSize;
            EliteArchive = eliteArchive;
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

        public void RequestPause()
        {
            throw new NotImplementedException();
        }

        public void RequestPauseAndWait()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Notifies listeners that the algorithm has paused.
        /// </summary>
        public event EventHandler PausedEvent;

        private void AlgorithmThreadMethod()
        {
            try
            {
                _prevUpdateEvaluation = 0;
                _prevUpdateTimeTick = DateTime.Now.Ticks;

                for (;;)
                {
                    CurrentEvaluation++;
                    PerformOneEvaluation();

                    if (UpdateTest())
                    {
                        _prevUpdateEvaluation = CurrentEvaluation;
                        _prevUpdateTimeTick = DateTime.Now.Ticks;
                        OnUpdateEvent();
                    }

                    // Check if a pause has been requested. 
                    // Access to the flag is not thread synchronized, but it doesn't really matter if
                    // we miss it being set and perform one other evaluation before pausing.
                    if (_pauseRequestFlag || GenomeListEvaluator.StopConditionSatisfied)
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
            if (UpdateMode.SteadyState == UpdateScheme.UpdateMode)
            {
                return (CurrentEvaluation - _prevUpdateEvaluation) >= UpdateScheme.Evaluations;
            }

            return (DateTime.Now.Ticks - _prevUpdateTimeTick) >= UpdateScheme.TimeSpan.Ticks;
        }

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

        private void OnPausedEvent()
        {
            if (null != PausedEvent)
            {
                // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                try
                {
                    PausedEvent(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    __log.Error("PausedEvent listener threw exception", ex);
                }
            }
        }

        /// <summary>
        ///     Progress forward by one evaluation. Perform one evaluation/cycle of the evolution algorithm.
        /// </summary>
        protected abstract void PerformOneEvaluation();
    }
}