using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Loggers
{
    public static class FieldPosition
    {
        public enum NoveltyEvaluationFieldPosition
        {
            SpecieCount = 0,
            AsexualOffspringCount = 1,
            CurrentGeneration = 2,
            EvaluationsPerSecond = 3,
            InterspeciesOffspringCount = 4,
            MaxComplexity = 5,
            MeanFitness = 6,
            MeanSpecieChampFitness = 7,
            MinSpecieSize = 8,
            SexualOffspringCount = 9,
            TotalEvaluations = 10,
            TotalOffspringCount = 11,
            ChampGenomeBehavior1 = 12,
            ChampGenomeBehavior2 = 13,
            ChampGenomeBirthGeneration = 14,
            ChampGenomeConnectionGeneCount = 15,
            ChampGenomeEvaluationCount = 16,
            ChampGenomeFitness = 17,
            ChampGenomeGenomeId = 18,
            ChampGenomeNeuronGeneCount = 19,
            ChampGenomeTotalGeneCount = 20
        }
    }
}
