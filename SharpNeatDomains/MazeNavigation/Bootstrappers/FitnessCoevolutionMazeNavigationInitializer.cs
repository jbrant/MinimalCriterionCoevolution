#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Domains.MazeNavigation.CoevolutionMCSExperiment;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.Bootstrappers
{
    /// <summary>
    ///     Initializes a specified number of "viable" genomes (i.e. genomes that satisfy the minimal criteria) in order to
    ///     bootstrap the main algorithm.  For this particular intializer, the algorithm used to perform the initialization is
    ///     fitness.
    /// </summary>
    public class FitnessCoevolutionMazeNavigationInitializer : CoevolutionMazeNavigationInitializer
    {
        #region Public methods

        /// <summary>
        ///     Configures and instantiates the initialization evolutionary algorithm.
        /// </summary>
        /// <param name="parallelOptions">Synchronous/Asynchronous execution settings.</param>
        /// <param name="genomeList">The initial population of genomes.</param>
        /// <param name="mazeEnvironment">The maze on which to evaluate the navigators.</param>
        /// <param name="genomeDecoder">The decoder to translate genomes into phenotypes.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceeded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        public override void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> genomeList,
            MazeStructure mazeEnvironment, IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ulong startingEvaluations)
        {
            // Set the boiler plate algorithm parameters
            base.InitializeAlgorithm(parallelOptions, genomeList, genomeDecoder, startingEvaluations);

            // Create the initialization evolution algorithm.
            InitializationEa = new GenerationalNeatEvolutionAlgorithm<NeatGenome>(SpeciationStrategy,
                ComplexityRegulationStrategy, RunPhase.Initialization);

            // Create IBlackBox evaluator.
            MazeNavigatorFitnessInitializationEvaluator mazeNavigatorEvaluator =
                new MazeNavigatorFitnessInitializationEvaluator(MaxTimesteps, MinSuccessDistance, MaxDistanceToTarget,
                    mazeEnvironment, startingEvaluations);

            // Create the genome evaluator
            IGenomeEvaluator<NeatGenome> fitnessEvaluator = new ParallelGenomeFitnessEvaluator<NeatGenome, IBlackBox>(
                genomeDecoder, mazeNavigatorEvaluator, parallelOptions);

            // Only pull the number of genomes from the list equivalent to the initialization algorithm population size
            // (this is to handle the case where the list was created in accordance with the primary algorithm 
            // population size, which is quite likely larger)
            genomeList = genomeList.Take(PopulationSize).ToList();

            // Initialize the evolution algorithm
            InitializationEa.Initialize(fitnessEvaluator, GenomeFactory, genomeList, null, null);
        }

        /// <summary>
        ///     Runs the initialization algorithm until the specified number of viable genomes (i.e. genomes that meets the minimal
        ///     criteria) are found and returns those genomes along with the total number of evaluations that were executed to find
        ///     them.
        /// </summary>
        /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genomes.</param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public override List<NeatGenome> EvolveViableGenomes(out ulong totalEvaluations)
        {
            // The minimum fitness for an agent who has solved the maze
            int solvedFitness = MaxDistanceToTarget - MinSuccessDistance;

            // Create list of viable genomes
            List<NeatGenome> viableGenomes = new List<NeatGenome>(MinSuccessfulAgentCount + MinUnsuccessfulAgentCount);

            do
            {
                // Start the algorithm
                InitializationEa.StartContinue();

                // Ping for status every few hundred milliseconds
                while (RunState.Terminated != InitializationEa.RunState &&
                       RunState.Paused != InitializationEa.RunState)
                {
                    Console.WriteLine(@"Current Evaluations: {0}  Mean Complexity: {1}  Closest Genome Distance: {2}",
                        InitializationEa.CurrentEvaluations, InitializationEa.Statistics._meanComplexity,
                        MaxDistanceToTarget - InitializationEa.Statistics._maxFitness);

                    Thread.Sleep(200);
                }

                // Add all of the genomes that have solved the maze
                viableGenomes.AddRange(
                    InitializationEa.GenomeList.Where(genome => genome.EvaluationInfo.Fitness > solvedFitness)
                        .Take(MinSuccessfulAgentCount));
            } while (viableGenomes.Count < MinSuccessfulAgentCount);

            // Add the remainder of genomes who have not solved the maze
            // (note that the intuition for doing this after the loop is that most will not have solved)
            viableGenomes.AddRange(
                InitializationEa.GenomeList.Where(genome => genome.EvaluationInfo.Fitness < solvedFitness)
                    .Take(MinUnsuccessfulAgentCount));

            // Ensure that the above statement was able to get the required number of unsuccessful agent genomes
            if (viableGenomes.Count(genome => genome.EvaluationInfo.Fitness < solvedFitness) <
                MinUnsuccessfulAgentCount)
            {
                throw new SharpNeatException(
                    "Fitness coevolution initialization algorithm failed to produce the requisite number of unsuccessful agent genomes.");
            }

            // Set the total number of evaluations that were executed as part of the initialization process
            totalEvaluations = InitializationEa.CurrentEvaluations;

            return viableGenomes;
        }

        #endregion
    }
}