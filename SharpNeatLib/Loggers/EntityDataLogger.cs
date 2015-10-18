#region

using System;
using System.Collections.Generic;
using ExperimentEntities;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Loggers
{
    public class EntityDataLogger : IDataLogger
    {
        #region Constructors

        public EntityDataLogger(int experimentConfiguration)
        {
            _experimentConfiguration = experimentConfiguration;
        }

        #endregion

        #region Logger Properties

        /// <summary>
        ///     The name of the log file.
        /// </summary>
        public string LogFileName { get; }

        #endregion

        private List<LoggableElement> extractSortedCombinedList(params List<LoggableElement>[] loggableElements)
        {
            List<LoggableElement> combinedElements = new List<LoggableElement>();

            // Combine everything into a single list
            foreach (var loggableElementList in loggableElements)
            {
                combinedElements.AddRange(loggableElementList);
            }

            // Sort the elements so that everything logged is kept in the same order
            combinedElements.Sort();

            return combinedElements;
        }

        #region Logger Instance Fields

        private ExperimentDataEntities _dbContext;

        private readonly int _experimentConfiguration;

        #endregion

        #region Logging Control Methods

        public void Open()
        {
            _dbContext = new ExperimentDataEntities();
        }

        public void LogHeader(params List<LoggableElement>[] loggableElements)
        {
        }

        public void LogRow(params List<LoggableElement>[] loggableElements)
        {
            // Combine and sort the loggable elements
            List<LoggableElement> combinedElements = extractSortedCombinedList(loggableElements);

            NoveltyExperimentEvaluationData noveltyData = new NoveltyExperimentEvaluationData();

            noveltyData.ExperimentID_FK = _experimentConfiguration;
            noveltyData.Generation =
                (int)
                    Convert.ChangeType(combinedElements[NoveltyEvolutionFieldElements.Generation.Position].Value,
                        noveltyData.Generation.GetType());
            noveltyData.SpecieCount =
                (int)
                    Convert.ChangeType(combinedElements[NoveltyEvolutionFieldElements.SpecieCount.Position].Value,
                        noveltyData.SpecieCount.GetType());
            noveltyData.AsexualOffspringCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.AsexualOffspringCount.Position].Value,
                        noveltyData.AsexualOffspringCount.GetType());
            noveltyData.SexualOffspringCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.SexualOffspringCount.Position].Value,
                        noveltyData.SexualOffspringCount.GetType());
            noveltyData.TotalOffspringCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.TotalOffspringCount.Position].Value,
                        noveltyData.TotalOffspringCount.GetType());
            noveltyData.InterspeciesOffspringCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.InterspeciesOffspringCount.Position].Value,
                        noveltyData.InterspeciesOffspringCount.GetType());
            noveltyData.MaxFitness =
                (double)
                    Convert.ChangeType(combinedElements[NoveltyEvolutionFieldElements.MaxFitness.Position].Value,
                        noveltyData.MaxFitness.GetType());
            noveltyData.MeanFitness =
                (double)
                    Convert.ChangeType(combinedElements[NoveltyEvolutionFieldElements.MeanFitness.Position].Value,
                        noveltyData.MeanFitness.GetType());
            noveltyData.MeanSpecieChampFitness =
                (double)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.MeanSpecieChampFitness.Position].Value,
                        noveltyData.MeanSpecieChampFitness.GetType());
            noveltyData.MaxComplexity =
                (int)
                    Convert.ChangeType(combinedElements[NoveltyEvolutionFieldElements.MaxComplexity.Position].Value,
                        noveltyData.MaxComplexity.GetType());
            noveltyData.MeanComplexity =
                (double)
                    Convert.ChangeType(combinedElements[NoveltyEvolutionFieldElements.MeanComplexity.Position].Value,
                        noveltyData.MeanComplexity.GetType());
            noveltyData.MinSpecieSize =
                (int)
                    Convert.ChangeType(combinedElements[NoveltyEvolutionFieldElements.MinSpecieSize.Position].Value,
                        noveltyData.MinSpecieSize.GetType());
            noveltyData.MaxSpecieSize =
                (int)
                    Convert.ChangeType(combinedElements[NoveltyEvolutionFieldElements.MaxSpecieSize.Position].Value,
                        noveltyData.MaxSpecieSize.GetType());
            if (noveltyData.TotalEvaluations != null)
                noveltyData.TotalEvaluations =
                    (int)
                        Convert.ChangeType(
                            combinedElements[NoveltyEvolutionFieldElements.TotalEvaluations.Position].Value,
                            noveltyData.TotalEvaluations.GetType());
            if (noveltyData.EvaluationsPerSecond != null)
                noveltyData.EvaluationsPerSecond =
                    (int)
                        Convert.ChangeType(
                            combinedElements[NoveltyEvolutionFieldElements.EvaluationsPerSecond.Position].Value,
                            noveltyData.EvaluationsPerSecond.GetType());
            noveltyData.ChampGenomeID =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeGenomeId.Position].Value,
                        noveltyData.ChampGenomeID.GetType());
            noveltyData.ChampGenomeFitness =
                (double)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeFitness.Position].Value,
                        noveltyData.ChampGenomeFitness.GetType());
            noveltyData.ChampGenomeBirthGeneration =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeBirthGeneration.Position].Value,
                        noveltyData.ChampGenomeBirthGeneration.GetType());
            noveltyData.ChampGenomeConnectionGeneCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeConnectionGeneCount.Position].Value,
                        noveltyData.ChampGenomeConnectionGeneCount.GetType());
            noveltyData.ChampGenomeNeuronGeneCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeNeuronGeneCount.Position].Value,
                        noveltyData.ChampGenomeNeuronGeneCount.GetType());
            noveltyData.ChampGenomeTotalGeneCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeTotalGeneCount.Position].Value,
                        noveltyData.ChampGenomeTotalGeneCount.GetType());
            if (noveltyData.ChampGenomeEvaluationCount != null)
            noveltyData.ChampGenomeEvaluationCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeEvaluationCount.Position].Value,
                        noveltyData.ChampGenomeEvaluationCount.GetType());
            if (noveltyData.ChampGenomeBehavior1 != null)
            noveltyData.ChampGenomeBehavior1 =
                (double)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeBehaviorX.Position].Value,
                        noveltyData.ChampGenomeBehavior1.GetType());
            if (noveltyData.ChampGenomeBehavior2 != null)
            noveltyData.ChampGenomeBehavior2 =
                (double)
                    Convert.ChangeType(
                        combinedElements[NoveltyEvolutionFieldElements.ChampGenomeBehaviorY.Position].Value,
                        noveltyData.ChampGenomeBehavior2.GetType());

            _dbContext.NoveltyExperimentEvaluationDatas.Add(noveltyData);
            _dbContext.SaveChanges();
        }

        public void Close()
        {
            _dbContext.Dispose();
        }

        #endregion
    }
}