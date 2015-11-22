#region

using System;
using System.Collections.Generic;
using System.Linq;
using ExperimentEntities;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace SharpNeat.Loggers
{
    /// <summary>
    ///     Entity data logger class for MCS experiment evaluation data.
    /// </summary>
    public class McsExperimentEvaluationEntityDataLogger : AbstractEntityDataLogger
    {
        #region Constructors

        /// <summary>
        ///     MCS experiment evaluation constructor (simply passes parameters to base constructor).
        /// </summary>
        /// <param name="experimentName">The unique name of the experiment configuration.</param>
        public McsExperimentEvaluationEntityDataLogger(string experimentName) : base(experimentName)
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

            // Get any previous runs for this experiment
            int experimentRuns =
                DbContext.MCSExperimentEvaluationDatas.Count(
                    c => c.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID);

            // If there were previous runs, we need to figure out whether this is the primary portion of
            // a previous initializing run or a new run altogether
            if (experimentRuns > 0)
            {
                // Get a count of primary runs that have executed
                int experimentPrimaryRuns =
                    DbContext.MCSExperimentEvaluationDatas.Count(
                        w =>
                            w.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID &&
                            w.RunPhase.RunPhaseName == RunPhase.Primary.ToString());

                // If primary algorithm runs have executed, this is a new run of the experiment, so increment the run counter
                if (experimentPrimaryRuns > 0)
                {
                    Run = DbContext.MCSExperimentEvaluationDatas.Where(
                        w =>
                            w.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID &&
                            w.RunPhase.RunPhaseName == RunPhase.Primary.ToString()).Max(m => m.Run) + 1;
                }
                // Otherwise, this is the primary algorithm portion of a previous initialization run
                else
                {
                    Run = DbContext.MCSExperimentEvaluationDatas.Where(
                        w =>
                            w.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID &&
                            w.RunPhase.RunPhaseName == RunPhase.Initialization.ToString()).Max(m => m.Run);
                }
            }
            // If there were no runs at all for this experiment, this is the first run by definition
            else
            {
                Run = 1;
            }
        }

        /// <summary>
        ///     Maps applicable entity fields for the MCS experiment evaluation entity and persists to the database.
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
            LoggableElement[] combinedElements = ExtractLoggableElementArray(EvolutionFieldElements.NumFieldElements,
                loggableElements);

            MCSExperimentEvaluationData mcsData = new MCSExperimentEvaluationData
            {
                ExperimentDictionaryID = ExperimentConfiguration.ExperimentDictionaryID,
                Run = Run
            };

            mcsData.Generation =
                (int)
                    Convert.ChangeType(combinedElements[EvolutionFieldElements.Generation.Position].Value,
                        mcsData.Generation.GetType());
            mcsData.RunPhase_FK = RunPhaseKey;

            mcsData.OffspringCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.TotalOffspringCount.Position].Value,
                        mcsData.OffspringCount.GetType());

            mcsData.MaxComplexity =
                (int)
                    Convert.ChangeType(combinedElements[EvolutionFieldElements.MaxComplexity.Position].Value,
                        mcsData.MaxComplexity.GetType());
            mcsData.MeanComplexity =
                (double)
                    Convert.ChangeType(combinedElements[EvolutionFieldElements.MeanComplexity.Position].Value,
                        mcsData.MeanComplexity.GetType());

            mcsData.TotalEvaluations =
                (int)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.TotalEvaluations.Position].Value,
                        typeof (int));
            mcsData.EvaluationsPerSecond =
                (int)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.EvaluationsPerSecond.Position].Value,
                        typeof (int));

            mcsData.ClosestGenomeID =
                (int)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.ChampGenomeGenomeId.Position].Value,
                        mcsData.ClosestGenomeID.GetType());

            mcsData.ClosestGenomeConnectionGeneCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.ChampGenomeConnectionGeneCount.Position].Value,
                        mcsData.ClosestGenomeConnectionGeneCount.GetType());
            mcsData.ClosestGenomeNeuronGeneCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.ChampGenomeNeuronGeneCount.Position].Value,
                        mcsData.ClosestGenomeNeuronGeneCount.GetType());
            mcsData.ClosestGenomeTotalGeneCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.ChampGenomeTotalGeneCount.Position].Value,
                        mcsData.ClosestGenomeTotalGeneCount.GetType());
            mcsData.ClosestGenomeEvaluationCount =
                (int)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.ChampGenomeEvaluationCount.Position].Value,
                        typeof (int));

            mcsData.ClosestGenomeDistanceToTarget =
                (double)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.ChampGenomeFitness.Position].Value,
                        typeof (double));

            mcsData.ClosestGenomeEndPositionX =
                (double)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.ChampGenomeBehaviorX.Position].Value,
                        typeof (double));
            mcsData.ClosestGenomeEndPositionY =
                (double)
                    Convert.ChangeType(
                        combinedElements[EvolutionFieldElements.ChampGenomeBehaviorY.Position].Value,
                        typeof (double));

            mcsData.ClosestGenomeXml =
                (string)
                    Convert.ChangeType(combinedElements[EvolutionFieldElements.ChampGenomeXml.Position].Value,
                        typeof (string));

            // Add the new evaluation data
            localDbContext.MCSExperimentEvaluationDatas.Add(mcsData);

            // Save the changes and dispose of the context
            localDbContext.SaveChanges();
            localDbContext.Dispose();
        }

        #endregion
    }
}