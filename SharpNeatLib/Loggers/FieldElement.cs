#region

using System.Collections.Generic;

#endregion

namespace SharpNeat.Loggers
{
    /// <summary>
    ///     Fields capturing data related to population statistics per generation/batch.
    /// </summary>
    public class EvolutionFieldElements
    {
        /// <summary>
        ///     The number of elements in this log file/table.
        /// </summary>
        public static readonly int NumFieldElements = 32;

        /// <summary>
        ///     The generation of the observation.
        /// </summary>
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");

        /// <summary>
        ///     The run phase (initialization or primary) at the time of the observation.
        /// </summary>
        public static readonly FieldElement RunPhase = new FieldElement(1, "Run Phase");

        /// <summary>
        ///     The number of species in the population.
        /// </summary>
        public static readonly FieldElement SpecieCount = new FieldElement(2, "Specie Count");

        /// <summary>
        ///     The number of offspring that were produced asexually.
        /// </summary>
        public static readonly FieldElement AsexualOffspringCount = new FieldElement(3, "Asexual Offspring Count");

        /// <summary>
        ///     The number of offspring that were produced sexually.
        /// </summary>
        public static readonly FieldElement SexualOffspringCount = new FieldElement(4, "Sexual Offspring Count");

        /// <summary>
        ///     The number of offspring that were produced via interspecies mating.
        /// </summary>
        public static readonly FieldElement InterspeciesOffspringCount = new FieldElement(5,
            "Interspecies Offspring Count");

        /// <summary>
        ///     The total number of offspring that were produced.
        /// </summary>
        public static readonly FieldElement TotalOffspringCount = new FieldElement(6, "Total Offspring Count");

        /// <summary>
        ///     The current population size.
        /// </summary>
        public static readonly FieldElement PopulationSize = new FieldElement(7, "Population Size");

        /// <summary>
        ///     The current minimal criteria threshold (only changes if dynamic).
        /// </summary>
        public static readonly FieldElement MinimalCriteriaThreshold = new FieldElement(8, "Minimal Criteria Threshold");

        /// <summary>
        ///     The X position of the minimal criteria point in euclidean space.
        /// </summary>
        public static readonly FieldElement MinimalCriteriaPointX = new FieldElement(9,
            "Minimal Criteria MazeStructurePoint X");

        /// <summary>
        ///     The Y position of the minimal criteria point in euclidean space.
        /// </summary>
        public static readonly FieldElement MinimalCriteriaPointY = new FieldElement(10,
            "Minimal Criteria MazeStructurePoint Y");

        /// <summary>
        ///     The maximum fitness in the current population.
        /// </summary>
        public static readonly FieldElement MaxFitness = new FieldElement(11, "Max Fitness");

        /// <summary>
        ///     The mean fitness of the current population.
        /// </summary>
        public static readonly FieldElement MeanFitness = new FieldElement(12, "Mean Fitness");

        /// <summary>
        ///     The mean fitness of the best-performing genome from each species.
        /// </summary>
        public static readonly FieldElement MeanSpecieChampFitness = new FieldElement(13, "Mean Specie Champ Fitness");

        /// <summary>
        ///     The least complex organism in the current population.
        /// </summary>
        public static readonly FieldElement MinComplexity = new FieldElement(14, "Min Complexity");

        /// <summary>
        ///     The most complex organism in the current populuation.
        /// </summary>
        public static readonly FieldElement MaxComplexity = new FieldElement(15, "Max Complexity");

        /// <summary>
        ///     The mean complexity of all organisms in the current population.
        /// </summary>
        public static readonly FieldElement MeanComplexity = new FieldElement(16, "Mean Complexity");

        /// <summary>
        ///     The size of the smallest extant species.
        /// </summary>
        public static readonly FieldElement MinSpecieSize = new FieldElement(17, "Min Specie Size");

        /// <summary>
        ///     The size of the largest extant species.
        /// </summary>
        public static readonly FieldElement MaxSpecieSize = new FieldElement(18, "Max Specie Size");

        /// <summary>
        ///     The number of evaluations that were executed at the time of the observation.
        /// </summary>
        public static readonly FieldElement TotalEvaluations = new FieldElement(19, "Total Evaluations");

        /// <summary>
        ///     The average number of evaluations executed per second during the current generation/batch.
        /// </summary>
        public static readonly FieldElement EvaluationsPerSecond = new FieldElement(20, "Evaluations per Second");

        /// <summary>
        ///     The ID of the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeGenomeId = new FieldElement(21, "Champ Genome ID");

        /// <summary>
        ///     The fitness of the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeFitness = new FieldElement(22, "Champ Genome Fitness");

        /// <summary>
        ///     The birth generation of the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeBirthGeneration = new FieldElement(23,
            "Champ Genome Birth Generation");

        /// <summary>
        ///     The number of connections in the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeConnectionGeneCount = new FieldElement(24,
            "Champ Genome Connection Gene Count");

        /// <summary>
        ///     The number of neurons in the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeNeuronGeneCount = new FieldElement(25,
            "Champ Genome Neuron Gene Count");

        /// <summary>
        ///     The total number of genes in the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeTotalGeneCount = new FieldElement(26,
            "Champ Genome Total Gene Count");

        /// <summary>
        ///     The total number of evaluations undergone by the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeEvaluationCount = new FieldElement(27,
            "Champ Genome Evaluation Count");

        /// <summary>
        ///     The X position of the global best performing genome in euclidean space.
        /// </summary>
        public static readonly FieldElement ChampGenomeBehaviorX = new FieldElement(28, "Champ Genome Behavior X");

        /// <summary>
        ///     The Y position of the global best performing genome in euclidean space.
        /// </summary>
        public static readonly FieldElement ChampGenomeBehaviorY = new FieldElement(29, "Champ Genome Behavior Y");

        /// <summary>
        ///     The distance to the objective of the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeDistanceToTarget = new FieldElement(30,
            "Champ Genome Distance to Target");

        /// <summary>
        ///     The genome XML definition for the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeXml = new FieldElement(31, "Champ Genome XML");

        /// <summary>
        ///     Pre-constructs an evolution log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Evolution log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateEvolutionFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {Generation, true},
                {RunPhase, true},
                {SpecieCount, true},
                {AsexualOffspringCount, true},
                {SexualOffspringCount, true},
                {InterspeciesOffspringCount, true},
                {TotalOffspringCount, true},
                {PopulationSize, true},
                {MinimalCriteriaThreshold, true},
                {MinimalCriteriaPointX, true},
                {MinimalCriteriaPointY, true},
                {MaxFitness, true},
                {MeanFitness, true},
                {MeanSpecieChampFitness, true},
                {MinComplexity, true},
                {MaxComplexity, true},
                {MeanComplexity, true},
                {MinSpecieSize, true},
                {MaxSpecieSize, true},
                {TotalEvaluations, true},
                {EvaluationsPerSecond, true},
                {ChampGenomeGenomeId, true},
                {ChampGenomeFitness, true},
                {ChampGenomeBirthGeneration, true},
                {ChampGenomeConnectionGeneCount, true},
                {ChampGenomeNeuronGeneCount, true},
                {ChampGenomeTotalGeneCount, true},
                {ChampGenomeEvaluationCount, true},
                {ChampGenomeBehaviorX, true},
                {ChampGenomeBehaviorY, true},
                {ChampGenomeDistanceToTarget, true},
                {ChampGenomeXml, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing data related to individual organism evaluations.
    /// </summary>
    public class EvaluationFieldElements
    {
        /// <summary>
        ///     The number of elements in this log file/table.
        /// </summary>
        public static readonly int NumFieldElements = 9;

        /// <summary>
        ///     The generation of the observation.
        /// </summary>
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");

        /// <summary>
        ///     The number of evaluations that were executed at the time of the observation.
        /// </summary>
        public static readonly FieldElement EvaluationCount = new FieldElement(1, "Evaluation Count");

        /// <summary>
        ///     The run phase (initialization or primary) at the time of the observation.
        /// </summary>
        public static readonly FieldElement RunPhase = new FieldElement(3, "Run Phase");

        /// <summary>
        ///     Whether or not the organism was considered viable (i.e. satisfied some objective/non-objective criterion).
        /// </summary>
        public static readonly FieldElement IsViable = new FieldElement(4, "Is Viable");

        /// <summary>
        ///     Whether or not the experiment stop condition was satisfied.
        /// </summary>
        public static readonly FieldElement StopConditionSatisfied = new FieldElement(5, "Stop Condition Satisfied");

        /// <summary>
        ///     The distance to the objective location.
        /// </summary>
        public static readonly FieldElement DistanceToTarget = new FieldElement(6, "Distance to Target");

        /// <summary>
        ///     The X position of the organism in euclidean space.
        /// </summary>
        public static readonly FieldElement AgentXLocation = new FieldElement(7, "Agent X Location");

        /// <summary>
        ///     The Y position of the organism in euclidean space.
        /// </summary>
        public static readonly FieldElement AgentYLocation = new FieldElement(8, "Agent Y Location");

        /// <summary>
        ///     Pre-constructs an evaluation log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Evaluation log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateEvaluationFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {Generation, true},
                {EvaluationCount, true},
                {RunPhase, true},
                {IsViable, true},
                {StopConditionSatisfied, true},
                {DistanceToTarget, true},
                {AgentXLocation, true},
                {AgentYLocation, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing the XML definition of genomes in the extant population at periodic intervals.
    /// </summary>
    public class PopulationGenomesFieldElements
    {
        /// <summary>
        ///     The number of elements in this log file/table.
        /// </summary>
        public static readonly int NumFieldElements = 3;

        /// <summary>
        ///     The generation in which the given population is extant.
        /// </summary>
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");

        /// <summary>
        ///     The ID of the genome definition being logged.
        /// </summary>
        public static readonly FieldElement GenomeId = new FieldElement(1, "Genomd ID");

        /// <summary>
        ///     The XML definition of the genome.
        /// </summary>
        public static readonly FieldElement GenomeXml = new FieldElement(2, "Genome XML");

        /// <summary>
        ///     Pre-constructs an evaluation log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Evaluation log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulatePopulationGenomesFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {Generation, true},
                {GenomeId, true},
                {GenomeXml, true}
            };
        }
    }

    /// <summary>
    ///     Encapsulates the position and name of a field within an experiment data log.
    /// </summary>
    public class FieldElement
    {
        /// <summary>
        ///     FieldElement constructor.
        /// </summary>
        /// <param name="position">The absolute position of the field within the log file/table.</param>
        /// <param name="friendlyName">The name of the field.</param>
        public FieldElement(int position, string friendlyName)
        {
            Position = position;
            FriendlyName = friendlyName;
        }

        /// <summary>
        ///     The absolute position of the field within the log file/table.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        ///     The name of the field.
        /// </summary>
        public string FriendlyName { get; private set; }
    }
}