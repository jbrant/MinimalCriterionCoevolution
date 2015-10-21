#region

using System;
using System.Collections.Generic;
using System.Linq;
using ExperimentEntities;

#endregion

namespace SharpNeat.Loggers
{
    /// <summary>
    ///     Entity data logger class for novelty experiment evaluation data.
    /// </summary>
    public class NoveltyExperimentEvaluationEntityDataLogger : AbstractEntityDataLogger
    {
        #region Constructors

        /// <summary>
        ///     Novelty experiment evaluation constructor (simply passes parameters to base constructor).
        /// </summary>
        /// <param name="experimentName">The unique name of the experiment configuration.</param>
        public NoveltyExperimentEvaluationEntityDataLogger(string experimentName)
            : base(experimentName)
        {
        }

        #endregion

        #region Logging Control Methods

        /// <summary>
        ///     Defers to the base method to instantiate the database connection and then reads in the maximum run ID so that we
        ///     have a starting point for the new experiment run.
        /// </summary>
        public override void Open()
        {
            // Call the base method to open the database connection and get the experiment configuration entity
            base.Open();

            // If there were previous runs, get the maximum existing run number and increment that by one
            // Otherwise, if there were no previous runs for this experiment, set the run ID to 1
            Run = ExperimentConfiguration.NoveltyExperimentEvaluationDatas.Count > 0
                ? ExperimentConfiguration.NoveltyExperimentEvaluationDatas.Max(x => x.Run) + 1
                : 1;
        }

        /// <summary>
        ///     Maps applicable entity fields for the novelty experiment evaluation entity and persists to the database.
        /// </summary>
        /// <param name="loggableElements">The loggable elements (data) to persist.</param>
        public override void LogRow(params List<LoggableElement>[] loggableElements)
        {
            // Initialize new DB context
            ExperimentDataEntities localDbContext = new ExperimentDataEntities
            {
                Configuration = {AutoDetectChangesEnabled = false, ValidateOnSaveEnabled = false}
            };

            // Combine and sort the loggable elements
            List<LoggableElement> combinedElements = ExtractSortedCombinedList(loggableElements);

            NoveltyExperimentEvaluationData noveltyData = new NoveltyExperimentEvaluationData
            {
                ExperimentID_FK = ExperimentConfiguration.ExperimentID,
                Run = Run
            };

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

            // Add the new evaluation data
            localDbContext.NoveltyExperimentEvaluationDatas.Add(noveltyData);

            // Save the changes and dispose of the context
            localDbContext.SaveChanges();
            localDbContext.Dispose();
        }

        #endregion
    }
}