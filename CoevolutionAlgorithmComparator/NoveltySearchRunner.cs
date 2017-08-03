#region

using System;
using System.Collections.Generic;
using System.Threading;
using ExperimentEntities;
using log4net;
using MCC_Domains.MazeNavigation.CoevolutionMCSExperiment;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace CoevolutionAlgorithmComparator
{
    public class NoveltySearchRunner
    {
        /// <summary>
        ///     Execution file logger.
        /// </summary>
        private static ILog _executionLogger;

        /// <summary>
        ///     The experiment configuration against which to compare the given coevolution experiment.
        /// </summary>
        private readonly ExperimentDictionary _comparisonExperimentConfig;

        /// <summary>
        ///     The current run being analyzed.
        /// </summary>
        private readonly int _currentRun;

        /// <summary>
        ///     The maze on which the comparison will be executed.
        /// </summary>
        private readonly MazeStructure _evaluationMazeDomain;

        /// <summary>
        ///     The name of the reference (novelty search) experiment configuration.
        /// </summary>
        private readonly string _referenceExperimentName;

        /// <summary>
        ///     The total number of runs in the coevolution experiment.
        /// </summary>
        private readonly int _totalRuns;

        /// <summary>
        ///     The instantiated reference algorithm for comparison.
        /// </summary>
        private INeatEvolutionAlgorithm<NeatGenome> _comparisonAlgorithm;

        /// <summary>
        ///     Novelty search runner constructor.
        /// </summary>
        /// <param name="comparisonExperimentConfig">
        ///     The experiment configuration against which to compare the given coevolution
        ///     experiment.
        /// </param>
        /// <param name="refExperimentName">The name of the reference (novelty search) experiment configuration.</param>
        /// <param name="mazeDomain">The maze on which the comparison will be executed.</param>
        /// <param name="currentRun">The current run being analyzed.</param>
        /// <param name="totalRuns">The total number of runs in the coevolution experiment.</param>
        /// <param name="executionLogger">Execution file logger.</param>
        public NoveltySearchRunner(ExperimentDictionary comparisonExperimentConfig, string refExperimentName,
            MazeStructure mazeDomain, int currentRun, int totalRuns, ILog executionLogger)
        {
            _comparisonExperimentConfig = comparisonExperimentConfig;
            _referenceExperimentName = refExperimentName;
            _evaluationMazeDomain = mazeDomain;
            _currentRun = currentRun;
            _totalRuns = totalRuns;
            _executionLogger = executionLogger;
        }

        /// <summary>
        ///     Handles execution of the novelty search algorithm for comparison to each maze in the coevolution experiment.
        /// </summary>
        /// <returns>The end-state of the novelty search algorithm.</returns>
        public INeatEvolutionAlgorithm<NeatGenome> RunNoveltySearch()
        {
            // Instantiate a new novelty search experiment
            NoveltySearchComparisonExperiment nsExperiment = new NoveltySearchComparisonExperiment(_evaluationMazeDomain);

            // Initialize the experiment
            nsExperiment.Initialize(_comparisonExperimentConfig);

            // Create the genome factory and generate genome list
            IGenomeFactory<NeatGenome> genomeFactory = nsExperiment.CreateGenomeFactory();
            List<NeatGenome> genomeList =
                genomeFactory.CreateGenomeList(_comparisonExperimentConfig.Primary_PopulationSize, 0);

            try
            {
                // Setup the algorithm and add the update event
                _comparisonAlgorithm = nsExperiment.CreateEvolutionAlgorithm(genomeFactory, genomeList, 0);
                _comparisonAlgorithm.UpdateEvent += ea_UpdateEvent;
            }
            catch (Exception)
            {
                _executionLogger.Error(
                    string.Format(
                        "Comparison experiment [{0}], for reference experiment [{1}] Run [{2}] of [{3}], failed to initialize",
                        nsExperiment.Name, _referenceExperimentName, _currentRun + 1, _totalRuns));
                Environment.Exit(0);
            }

            _executionLogger.Info(string.Format(
                "Executing comparison experiment [{0}] for reference experiment {1}, Run {2} of {3}", nsExperiment.Name,
                _referenceExperimentName, _currentRun + 1,
                _totalRuns));

            // Start algorithm (it will run on a background thread).
            _comparisonAlgorithm.StartContinue();

            while (RunState.Terminated != _comparisonAlgorithm.RunState &&
                   RunState.Paused != _comparisonAlgorithm.RunState)
            {
                Thread.Sleep(2000);
            }

            // Return the final state of the algorithm so that statistics can be extracted
            return _comparisonAlgorithm;
        }

        /// <summary>
        ///     Evolutionary algorithm update delegate - logs current state of algorithm.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void ea_UpdateEvent(object sender, EventArgs e)
        {
            if (_comparisonAlgorithm.CurrentChampGenome != null)
            {
                double champGenomeAuxFitness =
                    _comparisonAlgorithm.CurrentChampGenome.EvaluationInfo.AuxFitnessArr.Length > 0
                        ? _comparisonAlgorithm.CurrentChampGenome.EvaluationInfo.AuxFitnessArr[0]._value
                        : 0;

                if (champGenomeAuxFitness > 0)
                {
                    _executionLogger.Info(
                        string.Format("Generation={0:N0} Evaluations={1:N0} BestFitness={2:N6} BestAuxFitness={3:N6}",
                            _comparisonAlgorithm.CurrentGeneration,
                            _comparisonAlgorithm.CurrentEvaluations,
                            _comparisonAlgorithm.CurrentChampGenome.EvaluationInfo.Fitness, champGenomeAuxFitness));
                }
                else
                {
                    _executionLogger.Info(string.Format("Generation={0:N0} Evaluations={1:N0} BestFitness={2:N6}",
                        _comparisonAlgorithm.CurrentGeneration,
                        _comparisonAlgorithm.CurrentEvaluations,
                        _comparisonAlgorithm.CurrentChampGenome.EvaluationInfo.Fitness));
                }
            }
            else
            {
                _executionLogger.Info(string.Format("Generation={0:N0} Evaluations={1:N0} BestFitness={2:N6}",
                    _comparisonAlgorithm.CurrentGeneration,
                    _comparisonAlgorithm.CurrentEvaluations, _comparisonAlgorithm.Statistics._maxFitness));
            }
        }
    }
}