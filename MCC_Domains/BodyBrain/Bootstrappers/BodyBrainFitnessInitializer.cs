using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using MCC_Domains.BodyBrain.MCCExperiment;
using MCC_Domains.Utils;
using Microsoft.EntityFrameworkCore;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes.Voxels;
using SharpNeat.SpeciationStrategies;

namespace MCC_Domains.BodyBrain.Bootstrappers
{
    public class BodyBrainFitnessInitializer : BodyBrainInitializer
    {
        #region Instance variables

        /// <summary>
        ///     The speciation strategy used by the EA.
        /// </summary>
        private ISpeciationStrategy<NeatGenome> _speciationStrategy;

        /// <summary>
        ///     The complexity regulation strategy used by the EA.
        /// </summary>
        private IComplexityRegulationStrategy _complexityRegulationStrategy;
        
        /// <summary>
        ///     Console logger for reporting execution status.
        /// </summary>
        private static ILog _executionLogger;

        #endregion
        
        #region Method overrides

        /// <summary>
        ///     Configures and instantiates the initialization algorithm.
        /// </summary>
        /// <param name="parallelOptions">Synchronous/Asynchronous execution settings.</param>
        /// <param name="brainGenomeList">The initial population of brain genomes.</param>
        /// <param name="brainGenomeFactory">The brain genome factory initialized by the main evolution thread.</param>
        /// <param name="brainGenomeDecoder">The decoder that translates brains into neurocontrollers.</param>
        /// <param name="body">The body morphology on which the brains are evaluated.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceeded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        protected override void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> brainGenomeList, IGenomeFactory<NeatGenome> brainGenomeFactory,
            IGenomeDecoder<NeatGenome, VoxelBrain> brainGenomeDecoder, VoxelBody body, ulong startingEvaluations)
        {
            // Initialise log4net (log to console and file).
            XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetEntryAssembly()),
                new FileInfo("log4net.config"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance
            // is their weight difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            _speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, parallelOptions);

            // Create complexity regulation strategy.
            _complexityRegulationStrategy =
                ExperimentUtils.CreateComplexityRegulationStrategy(ComplexityRegulationStrategyDefinition,
                    ComplexityThreshold);
            
            // Create the initialization evolution algorithm.
            InitializationEa = new GenerationalComplexifyingEvolutionAlgorithm<NeatGenome>(_speciationStrategy,
                _complexityRegulationStrategy, RunPhase.Initialization);
            
            // Register initialization EA update event
            InitializationEa.UpdateEvent += UpdateEvent;

            // Create the brain fitness initialization evaluator
            var brainEvaluator = new BodyBrainFitnessInitializationEvaluator(body, SimulationProperties,
                MinAmbulationDistance, ExperimentName, Run, startingEvaluations);
            
            // Create the brain genome evaluator
            IGenomeEvaluator<NeatGenome> fitnessEvaluator =
                new ParallelGenomeFitnessEvaluator<NeatGenome, VoxelBrain>(brainGenomeDecoder, brainEvaluator,
                    parallelOptions);
            
            // Only pull the number of genomes from the list equivalent to the initialization algorithm population size
            brainGenomeList = brainGenomeList.Take(PopulationSize).ToList();
            
            // Replace genome factory primary NEAT parameters with initialization parameters
            ((CppnGenomeFactory) brainGenomeFactory).ResetNeatGenomeParameters(NeatGenomeParameters);
            
            // Initialize the evolution algorithm
            InitializationEa.Initialize(fitnessEvaluator, brainGenomeFactory, brainGenomeList, null, null);
        }

        /// <summary>
        ///     Executes the initialization algorithm until the specific number of viable genomes (i.e. genomes that meets the
        ///     minimal criteria) are evolved, and returns those genomes along with the total number of evaluations that were
        ///     executed to discover them.
        /// </summary>
        /// <param name="totalEvaluations">The total number of evaluations required to evolve the seed genomes.</param>
        /// <param name="maxEvaluations">
        ///     The maximum number of evaluations that can be executed before the initialization process
        ///     is restarted.  This prevents getting stuck for a long time and/or ending up with unnecessarily complex networks.
        /// </param>
        /// <param name="restartCount">
        ///     The number of times the initialization process has been restarted (this is only used for
        ///     status logging purposes).
        /// </param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public override List<NeatGenome> RunEvolution(out ulong totalEvaluations, uint? maxEvaluations, uint restartCount)
        {
            // Create list of viable genomes
            var viableGenomes = new List<NeatGenome>(MinSuccessfulBrainCount);

            do
            {
                Console.Out.WriteLine($"Starting up the algorithm on restart #{restartCount}");

                // Start the algorithm
                InitializationEa.StartContinue();

                Console.Out.WriteLine("Going into algorithm wait loop...");

                // Ping for status every few hundred milliseconds
                while (RunState.Terminated != InitializationEa.RunState &&
                       RunState.Paused != InitializationEa.RunState)
                {
                    if (InitializationEa.CurrentEvaluations >= maxEvaluations)
                    {
                        // Record the total number of evaluations
                        totalEvaluations = InitializationEa.CurrentEvaluations;

                        // Halt the EA worker thread
                        InitializationEa.RequestPauseAndWait();

                        // Null out the EA and delete the thread
                        // (it's necessary to null out these resources so that the thread will be completely garbage collected)
                        InitializationEa.Reset();
                        InitializationEa = null;

                        // Note that the calling experiment must be able to handle this null return value (not great practice)
                        return null;
                    }

                    Thread.Sleep(200);
                }

                Console.Out.WriteLine("Attempting to extract viable genome from list...");

                // Add all of the genomes that have solved the maze
                viableGenomes.AddRange(
                    InitializationEa.GenomeList.Where(
                            genome =>
                                genome.EvaluationInfo != null &&
                                genome.EvaluationInfo.TrialData[0].ObjectiveDistance >= MinAmbulationDistance));

                Console.Out.WriteLine(
                    $"Extracted [{viableGenomes.Count}] of [{MinSuccessfulBrainCount}] required viable genomes in [{InitializationEa.CurrentEvaluations}] evaluations");
            } while (viableGenomes.Count < MinSuccessfulBrainCount);

            // Set the total number of evaluations that were executed as part of the initialization process
            totalEvaluations = InitializationEa.CurrentEvaluations;

            return viableGenomes;
        }
        
        #endregion

        #region Private methods

        /// <summary>
        ///     Print update event at every generation.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void UpdateEvent(object sender, EventArgs e)
        {
            _executionLogger.Info(
                $"(Init) Generation={InitializationEa.CurrentGeneration:N0} Evaluations={InitializationEa.CurrentEvaluations:N0} BestDistance={InitializationEa.CurrentChampGenome.EvaluationInfo.Fitness}");
        }

        #endregion
    }
}