namespace SharpNeat.Loggers
{
    public class EvolutionFieldElements
    {
        public static readonly int NumFieldElements = 27;
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");
        public static readonly FieldElement RunPhase = new FieldElement(1, "Run Phase");
        public static readonly FieldElement SpecieCount = new FieldElement(2, "Specie Count");
        public static readonly FieldElement AsexualOffspringCount = new FieldElement(3, "Asexual Offspring Count");
        public static readonly FieldElement SexualOffspringCount = new FieldElement(4, "Sexual Offspring Count");

        public static readonly FieldElement InterspeciesOffspringCount = new FieldElement(5,
            "Interspecies Offspring Count");

        public static readonly FieldElement TotalOffspringCount = new FieldElement(6, "Total Offspring Count");
        public static readonly FieldElement MaxFitness = new FieldElement(7, "Max Fitness");
        public static readonly FieldElement MeanFitness = new FieldElement(8, "Mean Fitness");
        public static readonly FieldElement MeanSpecieChampFitness = new FieldElement(9, "Mean Specie Champ Fitness");
        public static readonly FieldElement MaxComplexity = new FieldElement(10, "Max Complexity");
        public static readonly FieldElement MeanComplexity = new FieldElement(11, "Mean Complexity");
        public static readonly FieldElement MinSpecieSize = new FieldElement(12, "Min Specie Size");
        public static readonly FieldElement MaxSpecieSize = new FieldElement(13, "Max Specie Size");
        public static readonly FieldElement TotalEvaluations = new FieldElement(14, "Total Evaluations");
        public static readonly FieldElement EvaluationsPerSecond = new FieldElement(15, "Evaluations per Second");
        public static readonly FieldElement ChampGenomeGenomeId = new FieldElement(16, "Champ Genome ID");
        public static readonly FieldElement ChampGenomeFitness = new FieldElement(17, "Champ Genome Fitness");

        public static readonly FieldElement ChampGenomeBirthGeneration = new FieldElement(18,
            "Champ Genome Birth Generation");

        public static readonly FieldElement ChampGenomeConnectionGeneCount = new FieldElement(19,
            "Champ Genome Connection Gene Count");

        public static readonly FieldElement ChampGenomeNeuronGeneCount = new FieldElement(20,
            "Champ Genome Neuron Gene Count");

        public static readonly FieldElement ChampGenomeTotalGeneCount = new FieldElement(21,
            "Champ Genome Total Gene Count");

        public static readonly FieldElement ChampGenomeEvaluationCount = new FieldElement(22,
            "Champ Genome Evaluation Count");

        public static readonly FieldElement ChampGenomeBehaviorX = new FieldElement(24, "Champ Genome Behavior X");
        public static readonly FieldElement ChampGenomeBehaviorY = new FieldElement(25, "Champ Genome Behavior Y");
        public static readonly FieldElement ChampGenomeXml = new FieldElement(26, "Champ Genome XML");
    }

    public class EvaluationFieldElements
    {
        public static readonly int NumFieldElements = 8;
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");
        public static readonly FieldElement EvaluationCount = new FieldElement(1, "Evaluation Count");
        public static readonly FieldElement RunPhase = new FieldElement(2, "Run Phase");
        public static readonly FieldElement IsViable = new FieldElement(3, "Is Viable");
        public static readonly FieldElement StopConditionSatisfied = new FieldElement(4, "Stop Condition Satisfied");
        public static readonly FieldElement DistanceToTarget = new FieldElement(5, "Distance to Target");
        public static readonly FieldElement AgentXLocation = new FieldElement(6, "Agent X Location");
        public static readonly FieldElement AgentYLocation = new FieldElement(7, "Agent Y Location");
        public static readonly FieldElement AgentXml = new FieldElement(8, "Agent XML");
    }

    public class FieldElement
    {
        public FieldElement(int position, string friendlyName)
        {
            Position = position;
            FriendlyName = friendlyName;
        }

        public int Position { get; private set; }
        public string FriendlyName { get; private set; }
    }
}