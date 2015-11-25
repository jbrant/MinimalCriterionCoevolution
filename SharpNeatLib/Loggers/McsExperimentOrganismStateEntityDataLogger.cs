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
    ///     Entity data logger class for MCS experiment organism state data.
    /// </summary>
    public class McsExperimentOrganismStateEntityDataLogger : AbstractEntityDataLogger
    {
        #region Constructors

        /// <summary>
        ///     MCS experiment organism state data constructor (simply passes parameters to base constructor).
        /// </summary>
        /// <param name="experimentName">The unique name of the experiment configuration.</param>
        public McsExperimentOrganismStateEntityDataLogger(string experimentName) : base(experimentName)
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
                DbContext.MCSExperimentOrganismStateDatas.Count(
                    c => c.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID);

            // If there were previous runs, we need to figure out whether this is the primary portion of
            // a previous initializing run or a new run altogether
            if (experimentRuns > 0)
            {
                // Get a count of primary runs that have executed
                int experimentPrimaryRuns =
                    DbContext.MCSExperimentOrganismStateDatas.Count(
                        w =>
                            w.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID &&
                            w.RunPhase.RunPhaseName == RunPhase.Primary.ToString());

                // If primary algorithm runs have executed, this is a new run of the experiment, so increment the run counter
                if (experimentPrimaryRuns > 0)
                {
                    Run = DbContext.MCSExperimentOrganismStateDatas.Where(
                        w =>
                            w.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID &&
                            w.RunPhase.RunPhaseName == RunPhase.Primary.ToString()).Max(m => m.Run) + 1;
                }
                // Otherwise, this is the primary algorithm portion of a previous initialization run
                else
                {
                    Run = DbContext.MCSExperimentOrganismStateDatas.Where(
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
        ///     Maps applicable entity fields for the MCS experiment organism entity and persists to the database.
        /// </summary>
        /// <param name="loggableElements">The loggable elements (data) to persist.</param>
        public override void LogRow(params List<LoggableElement>[] loggableElements)
        {
            // Combine and sort the loggable elements
            LoggableElement[] combinedElements = ExtractLoggableElementArray(EvaluationFieldElements.NumFieldElements,
                loggableElements);

            MCSExperimentOrganismStateData mcsData = new MCSExperimentOrganismStateData
            {
                ExperimentDictionaryID = ExperimentConfiguration.ExperimentDictionaryID,
                Run = Run
            };

            mcsData.Generation =
                (int)
                    Convert.ChangeType(combinedElements[EvaluationFieldElements.Generation.Position].Value,
                        mcsData.Generation.GetType());
            mcsData.Evaluation = (int)
                Convert.ChangeType(combinedElements[EvaluationFieldElements.EvaluationCount.Position].Value,
                    mcsData.Evaluation.GetType());
            mcsData.RunPhase_FK = RunPhaseKey;
            mcsData.IsViable =
                (bool)
                    Convert.ChangeType(combinedElements[EvaluationFieldElements.IsViable.Position].Value,
                        mcsData.IsViable.GetType());
            mcsData.StopConditionSatisfied =
                (bool)
                    Convert.ChangeType(
                        combinedElements[EvaluationFieldElements.StopConditionSatisfied.Position].Value,
                        mcsData.StopConditionSatisfied.GetType());
            mcsData.DistanceToTarget =
                (double)
                    Convert.ChangeType(
                        combinedElements[EvaluationFieldElements.DistanceToTarget.Position].Value,
                        mcsData.DistanceToTarget.GetType());
            mcsData.AgentXLocation =
                (double)
                    Convert.ChangeType(combinedElements[EvaluationFieldElements.AgentXLocation.Position].Value,
                        mcsData.AgentXLocation.GetType());
            mcsData.AgentYLocation =
                (double)
                    Convert.ChangeType(combinedElements[EvaluationFieldElements.AgentYLocation.Position].Value,
                        mcsData.AgentYLocation.GetType());

            // Initialize new DB context and persist new record
            using (ExperimentDataEntities experimentContext = new ExperimentDataEntities
            {
                Configuration = {AutoDetectChangesEnabled = false, ValidateOnSaveEnabled = false}
            })
            {
                bool commitSuccessful = false;

                while (commitSuccessful == false)
                {
                    try
                    {
                        // Add the new organism state data
                        experimentContext.MCSExperimentOrganismStateDatas.Add(mcsData);

                        // Save the changes
                        experimentContext.SaveChanges();

                        commitSuccessful = true;
                    }
                    catch (Exception)
                    {
                        // Retry until record is successfully committed
                    }
                }
            }
        }

        #endregion
    }
}