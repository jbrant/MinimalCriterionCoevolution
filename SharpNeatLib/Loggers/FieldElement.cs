namespace SharpNeat.Loggers
{
    public class NoveltyEvolutionFieldElements
    {
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");
        public static readonly FieldElement SpecieCount = new FieldElement(1, "Specie Count");
        public static readonly FieldElement AsexualOffspringCount = new FieldElement(2, "Asexual Offspring Count");
        public static readonly FieldElement SexualOffspringCount = new FieldElement(3, "Sexual Offspring Count");

        public static readonly FieldElement InterspeciesOffspringCount = new FieldElement(4,
            "Interspecies Offspring Count");

        public static readonly FieldElement TotalOffspringCount = new FieldElement(5, "Total Offspring Count");
        public static readonly FieldElement MaxFitness = new FieldElement(6, "Max Fitness");
        public static readonly FieldElement MeanFitness = new FieldElement(7, "Mean Fitness");
        public static readonly FieldElement MeanSpecieChampFitness = new FieldElement(8, "Mean Specie Champ Fitness");
        public static readonly FieldElement MaxComplexity = new FieldElement(9, "Max Complexity");
        public static readonly FieldElement MeanComplexity = new FieldElement(10, "Mean Complexity");
        public static readonly FieldElement MinSpecieSize = new FieldElement(11, "Min Specie Size");
        public static readonly FieldElement MaxSpecieSize = new FieldElement(12, "Max Specie Size");
        public static readonly FieldElement TotalEvaluations = new FieldElement(13, "Total Evaluations");
        public static readonly FieldElement EvaluationsPerSecond = new FieldElement(14, "Evaluations per Second");
        public static readonly FieldElement ChampGenomeGenomeId = new FieldElement(15, "Champ Genome ID");
        public static readonly FieldElement ChampGenomeFitness = new FieldElement(16, "Champ Genome Fitness");

        public static readonly FieldElement ChampGenomeBirthGeneration = new FieldElement(17,
            "Champ Genome Birth Generation");

        public static readonly FieldElement ChampGenomeConnectionGeneCount = new FieldElement(18,
            "Champ Genome Connection Gene Count");

        public static readonly FieldElement ChampGenomeNeuronGeneCount = new FieldElement(19,
            "Champ Genome Neuron Gene Count");

        public static readonly FieldElement ChampGenomeTotalGeneCount = new FieldElement(20,
            "Champ Genome Total Gene Count");

        public static readonly FieldElement ChampGenomeEvaluationCount = new FieldElement(21,
            "Champ Genome Evaluation Count");

        public static readonly FieldElement ChampGenomeBehaviorX = new FieldElement(22, "Champ Genome Behavior X");
        public static readonly FieldElement ChampGenomeBehaviorY = new FieldElement(23, "Champ Genome Behavior Y");
    }

    public class NoveltyEvaluationFieldElements
    {
        public static readonly FieldElement EvaluationCount = new FieldElement(0, "Evaluation Count");
        public static readonly FieldElement StopConditionSatisfied = new FieldElement(1, "Stop Condition Satisfied");
        public static readonly FieldElement DistanceToTarget = new FieldElement(2, "Distance to Target");
        public static readonly FieldElement AgentXLocation = new FieldElement(3, "Agent X Location");
        public static readonly FieldElement AgentYLocation = new FieldElement(4, "Agent Y Location");
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