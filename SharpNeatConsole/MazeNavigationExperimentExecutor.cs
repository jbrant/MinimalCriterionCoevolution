#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using ExperimentEntities;
using log4net.Config;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation;
using SharpNeat.Domains.MazeNavigation.FitnessExperiment;
using SharpNeat.Domains.MazeNavigation.MCNSExperiment;
using SharpNeat.Domains.MazeNavigation.MCSExperiment;
using SharpNeat.Domains.MazeNavigation.NoveltyExperiment;
using SharpNeat.Domains.MazeNavigation.RandomExperiment;
using SharpNeat.Genomes.Neat;

#endregion

namespace SharpNeatConsole
{
    public class MazeNavigationExperimentExecutor
    {
        private static IGenomeFactory<NeatGenome> _genomeFactory;
        private static List<NeatGenome> _genomeList;
        private static INeatEvolutionAlgorithm<NeatGenome> _ea;

        private static void Main(string[] args)
        {
            if (args == null || args.Length < 3)
            {
                throw new SharpNeatException(
                    "Seed population file, number of runs, and at least one experiment name are required!");
            }

            // Initialise log4net (log to console).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Read seed populuation file and number of runs
            string seedPopulationFile = args[0];
            int numRuns = Int32.Parse(args[1]);

            // Create experiment names array with the defined size
            string[] experimentNames = new string[args.Length - 2];

            // Read all experiments
            for (int cnt = 0; cnt < experimentNames.Length; cnt++)
            {
                experimentNames[cnt] = args[cnt + 2];
            }

            foreach (string curExperimentName in experimentNames)
            {
                Console.WriteLine("Executing Experiment {0}", curExperimentName);

                // Create new database context and read in configuration for the given experiment
                ExperimentDataEntities experimentContext = new ExperimentDataEntities();
                var name = curExperimentName;
                ExperimentDictionary experimentConfiguration =
                    experimentContext.ExperimentDictionaries.Single(
                        expName => expName.ExperimentName == name);

                // Initialize new steady state novelty experiment
                BaseMazeNavigationExperiment experiment =
                    determineMazeNavigationExperiment(experimentConfiguration.Primary_SearchAlgorithmName,
                        experimentConfiguration.Primary_SelectionAlgorithmName);

                // Execute the experiment for the specified number of runs
                for (int runIdx = 0; runIdx < numRuns; runIdx++)
                {
                    // Initialize the experiment
                    experiment.Initialize(experimentConfiguration);

                    // Open and load population XML file.
                    using (XmlReader xr = XmlReader.Create(seedPopulationFile))
                    {
                        _genomeList = experiment.LoadPopulation(xr);
                    }
                    _genomeFactory = _genomeList[0].GenomeFactory;
                    Console.WriteLine("Loaded [{0}] genomes as initial population.", _genomeList.Count);

                    // Create evolution algorithm and attach update event.
                    _ea = experiment.CreateEvolutionAlgorithm(_genomeFactory, _genomeList);
                    _ea.UpdateEvent += ea_UpdateEvent;

                    Console.WriteLine("Executing Experiment {0}, Run {1} of {2}", curExperimentName, runIdx + 1, numRuns);

                    // Start algorithm (it will run on a background thread).
                    _ea.StartContinue();

                    while (RunState.Terminated != _ea.RunState && RunState.Paused != _ea.RunState)
                    {
                        Thread.Sleep(2000);
                    }
                }

                // Dispose of the database context
                experimentContext.Dispose();
            }
        }

        /// <summary>
        ///     Evolutionary algorithm update delegate - logs current state of algorithm.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private static void ea_UpdateEvent(object sender, EventArgs e)
        {
            Console.WriteLine("Generation={0:N0} Evaluations={0:N0} BestFitness={1:N6}", _ea.CurrentGeneration,
                _ea.CurrentEvaluations, _ea.Statistics._maxFitness);
        }

        /// <summary>
        ///     Determine the maze navigation experiment to run based on the given search and selection algorithms.
        /// </summary>
        /// <param name="searchAlgorithmName">The search algorithm to run.</param>
        /// <param name="selectionAlgorithmName">The selection algorithm to run.</param>
        /// <returns></returns>
        private static BaseMazeNavigationExperiment determineMazeNavigationExperiment(string searchAlgorithmName,
            string selectionAlgorithmName)
        {
            // Extract the corresponding search and selection algorithm domain types
            SearchType searchType = AlgorithmTypeUtil.ConvertStringToSearchType(searchAlgorithmName);
            SelectionType selectionType = AlgorithmTypeUtil.ConvertStringToSelectionType(selectionAlgorithmName);

            // Match up with the correct experiment
            switch (searchType)
            {
                // Fitness experiment
                case SearchType.Fitness:
                    return new MazeNavigationFitnessExperiment();

                // Novelty search experiment (generational or steady state)
                case SearchType.NoveltySearch:
                    if (SelectionType.Generational.Equals(selectionType))
                    {
                        return new GenerationalMazeNavigationNoveltyExperiment();
                    }
                    return new SteadyStateMazeNavigationNoveltyExperiment();

                // Minimal criteria novelty search experiment (steady-state only)
                case SearchType.MinimalCriteriaNoveltySearch:
                    return new SteadyStateMazeNavigationMCNSExperiment();

                // Minimal criteria search experiment (steady state or queueing)
                case SearchType.MinimalCriteriaSearch:
                    if (SelectionType.SteadyState.Equals(selectionType))
                    {
                        return new SteadyStateMazeNavigationMCSExperiment();
                    }
                    // TODO: Insert queueing MCS experiment
                    break;

                // If none of the above were matched, return the steady state experiment with
                // randomly assigned fitness
                default:
                    return new SteadyStateMazeNavigationRandomExperiment();
            }

            throw new SharpNeatException("Unable to determine appropriate experiment.");
        }
    }
}