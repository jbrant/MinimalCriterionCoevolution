#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Loggers;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;
using SharpNeat.Utility;

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
        ///     Sets the data loggers for the initialization process.
        /// </summary>
        /// <param name="navigatorEvolutionDataLogger">The logger for evolution statistics.</param>
        /// <param name="navigatorPopulationDataLogger">The logger for serializing navigator genomes.</param>
        /// <param name="navigatorEvolutionLogFieldEnableMap">Indicates the enabled/disabled status of the evolution logger fields.</param>
        /// <param name="populationLoggingInterval">The batch interval at which the current population is serialized to a file.</param>
        public void SetDataLoggers(IDataLogger navigatorEvolutionDataLogger,
            IDataLogger navigatorPopulationDataLogger,
            IDictionary<FieldElement, bool> navigatorEvolutionLogFieldEnableMap, int? populationLoggingInterval)
        {
            NavigatorEvolutionDataLogger = navigatorEvolutionDataLogger;
            NavigatorPopulationDataLogger = navigatorPopulationDataLogger;
            NavigatorEvolutionLogFieldEnableMap = navigatorEvolutionLogFieldEnableMap;
            PopulationLoggingBatchInterval = populationLoggingInterval;
        }

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
        /// <param name="genomeFactory">The genome factory initialized by the main evolution thread.</param>
        /// <param name="mazeEnvironment">The maze on which to evaluate the navigators.</param>
        /// <param name="genomeDecoder">The decoder to translate genomes into phenotypes.</param>
        /// <param name="startingEvaluations">
        ///     The number of evaluations that preceeded this from which this process will pick up
        ///     (this is used in the case where we're restarting a run because it failed to find a solution in the allotted time).
        /// </param>
        public abstract void InitializeAlgorithm(ParallelOptions parallelOptions, List<NeatGenome> genomeList,
            IGenomeFactory<NeatGenome> genomeFactory, MazeStructure mazeEnvironment,
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ulong startingEvaluations);

        /// <summary>
        ///     Runs the initialization algorithm until the specified number of viable genomes (i.e. genomes that meets the minimal
        ///     criteria) are found and returns those genomes along with the total number of evaluations that were executed to find
        ///     them.
        /// </summary>
        /// <param name="totalEvaluations">The resulting number of evaluations to find the viable seed genomes.</param>
        /// <param name="maxEvaluations">
        ///     The maximum number of evaluations that can be executed before the initialization process
        ///     is restarted.  This prevents getting stuck for a long time and/or ending up with unecessarily complex networks.
        /// </param>
        /// <param name="restartCount">
        ///     The number of times the initialization process has been restarted (this is only used for
        ///     status logging purposes).
        /// </param>
        /// <returns>The list of seed genomes that meet the minimal criteria.</returns>
        public abstract List<NeatGenome> EvolveViableGenomes(out ulong totalEvaluations, uint? maxEvaluations,
            uint restartCount);

        /// <summary>
        ///     Logs the static maze genomes on which all initialization evaluations will be conducted.
        /// </summary>
        /// <param name="initializationMazeGenomes">The initialization maze genomes to log.</param>
        /// <param name="mazeGenomeDataLogger">The maze data logger.</param>
        public void LogStartingMazeGenomes(List<MazeGenome> initializationMazeGenomes, IDataLogger mazeGenomeDataLogger)
        {
            // Open the logger
            mazeGenomeDataLogger?.Open();

            // Write the header
            mazeGenomeDataLogger?.LogHeader(new List<LoggableElement>
            {
                new LoggableElement(PopulationGenomesFieldElements.Generation, null),
                new LoggableElement(PopulationGenomesFieldElements.GenomeId, null),
                new LoggableElement(PopulationGenomesFieldElements.GenomeXml, null),
                new LoggableElement(PopulationGenomesFieldElements.SpecieId, null)
            });

            // Write the genome XML for all initialization genomes
            foreach (MazeGenome mazeGenome in initializationMazeGenomes)
            {
                // Write the genome XML
                mazeGenomeDataLogger?.LogRow(new List<LoggableElement>
                {
                    new LoggableElement(PopulationGenomesFieldElements.Generation, mazeGenome.BirthGeneration),
                    new LoggableElement(PopulationGenomesFieldElements.GenomeId, mazeGenome.Id),
                    new LoggableElement(PopulationGenomesFieldElements.GenomeXml,
                        XmlIoUtils.GetGenomeXml(mazeGenome)),
                    new LoggableElement(PopulationGenomesFieldElements.SpecieId, mazeGenome.SpecieIdx)
                });
            }
        }

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

        /// <summary>
        ///     The logger for navigator evolution statistics.
        /// </summary>
        protected IDataLogger NavigatorEvolutionDataLogger;

        /// <summary>
        ///     The logger for serializing navigator genomes.
        /// </summary>
        protected IDataLogger NavigatorPopulationDataLogger;

        /// <summary>
        ///     Map indicating which evolution logger fields are enabled/disabled.
        /// </summary>
        protected IDictionary<FieldElement, bool> NavigatorEvolutionLogFieldEnableMap;

        /// <summary>
        ///     The number of batches to execute between logging the population genomic contents.
        /// </summary>
        protected int? PopulationLoggingBatchInterval;

        #endregion
    }
}