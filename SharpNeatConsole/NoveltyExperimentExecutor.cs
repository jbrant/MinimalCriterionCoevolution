/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
using log4net.Config;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.NoveltyExperiment;
using SharpNeat.Genomes.Neat;

#endregion

namespace SharpNeatConsole
{
    /// <summary>
    ///     Minimal console application that hardwaires the setting up on a evolution algorithm and start it running.
    /// </summary>
    internal class NoveltyExperimentExecutor
    {
        private static IGenomeFactory<NeatGenome> _genomeFactory;
        private static List<NeatGenome> _genomeList;
        private static INeatEvolutionAlgorithm<NeatGenome> _ea;

        private static void Main(string[] args)
        {
            Debug.Assert(args != null && args.Length == 2,
                "Experiment configuration file amd max generations are required!");

            // Read in experiment configuration file
            string exprimentConfigurationFile = args[0];
            int maxGenerations = Int32.Parse(args[1]);

            // Initialise log4net (log to console).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Experiment classes encapsulate much of the nuts and bolts of setting up a NEAT search.
            //XorExperiment experiment = new XorExperiment();
            SteadyStateMazeNavigationNoveltyExperiment experiment = new SteadyStateMazeNavigationNoveltyExperiment();

            // Load config XML.
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load("./ExperimentConfigurations/" + exprimentConfigurationFile);
            experiment.Initialize("Novelty", xmlConfig.DocumentElement);

            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            _genomeFactory = experiment.CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            _genomeList = _genomeFactory.CreateGenomeList(experiment.DefaultPopulationSize, 0);

            // Create evolution algorithm and attach update event.
            _ea = experiment.CreateEvolutionAlgorithm(_genomeFactory, _genomeList);
            _ea.UpdateEvent += ea_UpdateEvent;

            // Start algorithm (it will run on a background thread).
            _ea.StartContinue();

            while (RunState.Terminated != _ea.RunState && RunState.Paused != _ea.RunState &&
                   _ea.CurrentGeneration < maxGenerations)
            {
                Thread.Sleep(2000);
            }

            // Hit return to quit.
            //Console.ReadLine();
        }

        private static void ea_UpdateEvent(object sender, EventArgs e)
        {
            Console.WriteLine("gen={0:N0} bestFitness={1:N6}", _ea.CurrentGeneration, _ea.Statistics._maxFitness);
        }
    }
}