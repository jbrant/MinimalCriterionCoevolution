#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using ExperimentEntities;
using log4net.Config;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.NoveltyExperiment;
using SharpNeat.Genomes.Neat;

#endregion

namespace SharpNeatConsole
{
    public class SteadyStateNoveltyExperimentExecutor
    {
        private static IGenomeFactory<NeatGenome> _genomeFactory;
        private static List<NeatGenome> _genomeList;
        private static INeatEvolutionAlgorithm<NeatGenome> _ea;

        private static void Main(string[] args)
        {
            Debug.Assert(args != null && args.Length >= 3,
                "Seed population file, number of runs, and at least one experiment name are required!");

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
                // Execute the experiment for the specified number of runs
                for (int runIdx = 0; runIdx < numRuns; runIdx++)
                {
                    // Create new database context and read in configuration for the given experiment
                    ExperimentDataEntities experimentContext = new ExperimentDataEntities();
                    var name = curExperimentName;
                    ExperimentDictionary experimentConfiguration =
                        experimentContext.ExperimentDictionaries.Single(
                            expName => expName.ExperimentName == name);

                    // Initialize new steady state novelty experiment
                    SteadyStateMazeNavigationNoveltyExperiment experiment =
                        new SteadyStateMazeNavigationNoveltyExperiment();

                    // Initialize the experiment
                    experiment.Initialize(experimentConfiguration);

                    // Open and load population XML file.
                    using (XmlReader xr = XmlReader.Create(seedPopulationFile))
                    {
                        _genomeList = experiment.LoadPopulation(xr);
                    }
                    _genomeFactory = _genomeList[0].GenomeFactory;
                    Console.WriteLine("Loaded [{0}] genomes.", _genomeList.Count);

                    // Create evolution algorithm and attach update event.
                    _ea = experiment.CreateEvolutionAlgorithm(_genomeFactory, _genomeList);
                    _ea.UpdateEvent += ea_UpdateEvent;

                    // Start algorithm (it will run on a background thread).
                    _ea.StartContinue();

                    while (RunState.Terminated != _ea.RunState && RunState.Paused != _ea.RunState)
                    {
                        Thread.Sleep(2000);
                    }

                    // Dispose of the database context
                    experimentContext.Dispose();
                }
            }
        }

        private static void ea_UpdateEvent(object sender, EventArgs e)
        {
            // TODO: Need to expose number of evaluations from EA so that it can be logged here
            Console.WriteLine("gen={0:N0} bestFitness={1:N6}", _ea.CurrentGeneration, _ea.Statistics._maxFitness);
        }
    }
}