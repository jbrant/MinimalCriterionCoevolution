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
    public class FitnessCoevolutionMazeNavigationInitializer : MazeNavigationInitializer
    {
        #region Public methods

        /// <summary>
        ///     Constructs and initializes the maze navigator initialization algorithm (fitness using generational selection).
        /// </summary>
        /// <param name="xmlConfig">The XML configuration for the initialization algorithm.</param>
        /// <param name="inputCount">The number of input neurons.</param>
        /// <param name="outputCount">The number of output neurons.</param>
        /// <param name="numSuccessfulAgents">The minimum number of successful maze navigators that must be produced.</param>
        /// <param name="numUnsuccessfulAgents">The minimum number of unsuccessful maze navigators that must be produced.</param>
        /// <returns>The constructed initialization algorithm.</returns>
        public void SetAlgorithmParameters(XmlElement xmlConfig, int inputCount, int outputCount,
            int numSuccessfulAgents, int numUnsuccessfulAgents)
        {
            // Set the boiler plate parameters
            base.SetAlgorithmParameters(xmlConfig, inputCount, outputCount);

            // Set the static population size
            _populationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");

            // Set the minimum number of successful and unsuccessful maze navigators
            _minSuccessfulAgentCount = numSuccessfulAgents;
            _minUnsuccessfulAgentCount = numUnsuccessfulAgents;
        }

        /// <summary>
        ///     Sets configuration variables specific to the maze navigation simulation.
        /// </summary>
        /// <param name="maxTimesteps">The maximum number of time steps for which to run the simulation.</param>
        /// <param name="mazeStructure">The initial maze environment on which to evaluate agents.</param>
        /// <param name="minSuccessDistance">The minimum distance to the target location for the maze to be considered "solved".</param>
        public void SetEnvironmentParameters(int maxTimesteps, int minSuccessDistance, MazeStructure mazeStructure)
        {
            // Set boiler plate environment parameters
            // (note that the max distance to the target is the diagonal of the maze environment)
            base.SetEnvironmentParameters(
                (int)
                    Math.Sqrt(Math.Pow(mazeStructure.ScaledMazeHeight, 2) + Math.Pow(mazeStructure.ScaledMazeWidth, 2)),
                maxTimesteps, minSuccessDistance);

            // Store off the maze structure
            _mazeStructure = mazeStructure;
        }

        /// <summary>
        ///     Configures and instantiates the initialization evolutionary algorithm.
        /// </summary>
        /// <param name="parallelOptions">Synchronous/Asynchronous execution settings.</param>
        /// <param name="genomeList">The initial population of genomes.</param>
        /// <param name="genomeDecoder">The decoder to translate genomes into phenotypes.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceeded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        public override void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> genomeList,
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ulong startingEvaluations)
        {
            // Set the boiler plate algorithm parameters
            base.InitializeAlgorithm(parallelOptions, genomeList, genomeDecoder, startingEvaluations);

            // Create the initialization evolution algorithm.
            InitializationEa = new GenerationalNeatEvolutionAlgorithm<NeatGenome>(SpeciationStrategy,
                ComplexityRegulationStrategy, RunPhase.Initialization);

            // Create IBlackBox evaluator.
            MazeNavigatorFitnessInitializationEvaluator mazeNavigatorEvaluator =
                new MazeNavigatorFitnessInitializationEvaluator(MaxTimesteps, MinSuccessDistance, MaxDistanceToTarget,
                    _mazeStructure, startingEvaluations);

            // Create the genome evaluator
            IGenomeEvaluator<NeatGenome> fitnessEvaluator = new ParallelGenomeFitnessEvaluator<NeatGenome, IBlackBox>(
                genomeDecoder, mazeNavigatorEvaluator, parallelOptions);

            // Only pull the number of genomes from the list equivalent to the initialization algorithm population size
            // (this is to handle the case where the list was created in accordance with the primary algorithm 
            // population size, which is quite likely larger)
            genomeList = genomeList.Take(_populationSize).ToList();

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
        public List<NeatGenome> EvolveViableGenomes(out ulong totalEvaluations)
        {
            // The minimum fitness for an agent who has solved the maze
            int solvedFitness = MaxDistanceToTarget - MinSuccessDistance;

            // Create list of viable genomes
            List<NeatGenome> viableGenomes = new List<NeatGenome>(_minSuccessfulAgentCount + _minUnsuccessfulAgentCount);

            do
            {
                // Start the algorithm
                InitializationEa.StartContinue();

                // Ping for status every few hundred milliseconds
                while (RunState.Terminated != InitializationEa.RunState &&
                       RunState.Paused != InitializationEa.RunState)
                {
                    Thread.Sleep(200);
                }

                // Add all of the genomes that have solved the maze
                viableGenomes.AddRange(
                    InitializationEa.GenomeList.Where(genome => genome.EvaluationInfo.Fitness > solvedFitness)
                        .Take(_minSuccessfulAgentCount));
            } while (viableGenomes.Count < _minSuccessfulAgentCount);

            // Add the remainder of genomes who have not solved the maze
            // (note that the intuition for doing this after the loop is that most will not have solved)
            viableGenomes.AddRange(
                InitializationEa.GenomeList.Where(genome => genome.EvaluationInfo.Fitness < solvedFitness)
                    .Take(_minUnsuccessfulAgentCount));

            // Ensure that the above statement was able to get the required number of unsuccessful agent genomes
            if (viableGenomes.Count(genome => genome.EvaluationInfo.Fitness < solvedFitness) <
                _minUnsuccessfulAgentCount)
            {
                throw new SharpNeatException(
                    "Fitness covolution initialization algorithm failed to produce the requisite number of unsuccessful agent genomes.");
            }

            // Set the total number of evaluations that were executed as part of the initialization process
            totalEvaluations = InitializationEa.CurrentEvaluations;

            return viableGenomes;
        }

        #endregion

        #region Constructors

        #endregion

        #region Instance variables

        /// <summary>
        ///     The population size for the initialization algorithm.
        /// </summary>
        private int _populationSize;

        /// <summary>
        ///     The minimum number of successful maze navigators.
        /// </summary>
        private int _minSuccessfulAgentCount;

        /// <summary>
        ///     The minimum number of unsuccessful maze navigators.
        /// </summary>
        private int _minUnsuccessfulAgentCount;

        /// <summary>
        ///     Starting maze structure.
        /// </summary>
        private MazeStructure _mazeStructure;

        #endregion

        #region Public properties

        #endregion

        #region Private methods

        #endregion
    }
}