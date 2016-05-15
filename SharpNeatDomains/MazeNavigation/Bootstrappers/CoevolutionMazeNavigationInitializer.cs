#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace SharpNeat.Domains.MazeNavigation.Bootstrappers
{
    /// <summary>
    ///     Base class for coevolution experiment initializers.
    /// </summary>
    public abstract class CoevolutionMazeNavigationInitializer : MazeNavigationInitializer
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
        public virtual void SetAlgorithmParameters(XmlElement xmlConfig, int inputCount, int outputCount,
            int numSuccessfulAgents, int numUnsuccessfulAgents)
        {
            // Set the boiler plate parameters
            base.SetAlgorithmParameters(xmlConfig, inputCount, outputCount);

            // Set the static population size
            PopulationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");

            // Set the minimum number of successful and unsuccessful maze navigators
            MinSuccessfulAgentCount = numSuccessfulAgents;
            MinUnsuccessfulAgentCount = numUnsuccessfulAgents;
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
        }

        #endregion

        #region Abstract methods

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
        public abstract void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> genomeList,
            MazeStructure mazeEnvironment, IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder,
            ulong startingEvaluations);

        /// <summary>
        ///     Runs the initialization algorithm until the specified number of viable genomes (i.e. genomes that meets the minimal
        ///     criteria) are found and returns those genomes along with the total number of evaluations that were executed to find
        ///     them.
        /// </summary>
        /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genomes.</param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public abstract List<NeatGenome> EvolveViableGenomes(out ulong totalEvaluations);

        #endregion

        #region Protected members

        /// <summary>
        ///     The population size for the initialization algorithm.
        /// </summary>
        protected int PopulationSize;

        /// <summary>
        ///     The minimum number of successful maze navigators.
        /// </summary>
        protected int MinSuccessfulAgentCount;

        /// <summary>
        ///     The minimum number of unsuccessful maze navigators.
        /// </summary>
        protected int MinUnsuccessfulAgentCount;

        #endregion
    }
}