using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentEntities;

namespace EntityPersistenceTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ExperimentDataEntities dbContext = new ExperimentDataEntities();
            
            ExperimentDictionary dictionary =
                dbContext.ExperimentDictionaries.Single(expName => expName.ExperimentName == "Experiment 48");

            int maxRun =
                dictionary.NoveltyExperimentEvaluationDatas.Max(x => x.Run);

            NoveltyExperimentEvaluationData ne = new NoveltyExperimentEvaluationData();

            ne.ExperimentDictionaryID = 1;
            ne.Generation = 2;
            ne.SpecieCount = 10;
            ne.AsexualOffspringCount = 5;
            ne.SexualOffspringCount = 5;
            ne.TotalOffspringCount = 10;
            ne.InterspeciesOffspringCount = 5;
            ne.MaxFitness = 100;
            ne.MeanFitness = 50.5;
            ne.MeanSpecieChampFitness = 40.23;
            ne.MaxComplexity = 20;
            ne.MeanComplexity = 15.8;
            ne.MinSpecieSize = 3;
            ne.MaxSpecieSize = 30;

            //ne.TotalEvaluations = 300;
            ne.TotalEvaluations = null;

            //ne.EvaluationsPerSecond = 15;
            ne.EvaluationsPerSecond = null;

            ne.ChampGenomeID = 1;
            ne.ChampGenomeBirthGeneration = 5;
            ne.ChampGenomeConnectionGeneCount = 12;
            ne.ChampGenomeNeuronGeneCount = 5;
            ne.ChampGenomeTotalGeneCount = 17;

            //ne.ChampGenomeEvaluationCount = 13;
            ne.ChampGenomeEvaluationCount = null;

            ne.ChampGenomeBehavior1 = null;
            ne.ChampGenomeBehavior2 = null;

            dbContext.NoveltyExperimentEvaluationDatas.Add(ne);
            dbContext.SaveChanges();
        }
    }
}
