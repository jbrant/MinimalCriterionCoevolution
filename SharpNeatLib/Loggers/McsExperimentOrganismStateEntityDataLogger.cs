#region

using System;
using System.Collections.Generic;
using System.Linq;
using ExperimentEntities;

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

            // If there were previous runs, get the maximum existing run number and increment that by one
            // Otherwise, if there were no previous runs for this experiment, set the run ID to 1            
            Run =
                DbContext.NoveltyExperimentOrganismStateDatas.Count(
                    c => c.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID) > 0
                    ? DbContext.NoveltyExperimentOrganismStateDatas.Where(
                        w => w.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID).Max(m => m.Run) +
                      1
                    : 1;
        }

        /// <summary>
        ///     Maps applicable entity fields for the MCS experiment organism entity and persists to the database.
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
            LoggableElement[] combinedElements = ExtractLoggableElementArray(EvaluationFieldElements.NumFieldElements,
                loggableElements);

            MCSExperimentOrganismStateData mcsData = new MCSExperimentOrganismStateData
            {
                ExperimentDictionaryID = ExperimentConfiguration.ExperimentDictionaryID,
                Run = Run
            };

            // Get the reference to the current run phase (e.g. initialization or primary)
            string runPhaseName = combinedElements[EvaluationFieldElements.RunPhase.Position].Value.ToString();
            int runPhaseId =
                DbContext.RunPhases.First(
                    x => x.RunPhaseName == runPhaseName).RunPhaseID;

            mcsData.Generation =
                (int)
                    Convert.ChangeType(combinedElements[EvaluationFieldElements.Generation.Position].Value,
                        mcsData.Generation.GetType());
            mcsData.Evaluation = (int)
                Convert.ChangeType(combinedElements[EvaluationFieldElements.EvaluationCount.Position].Value,
                    mcsData.Evaluation.GetType());
            mcsData.RunPhase_FK = runPhaseId;
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
            mcsData.AgentXml =
                (string)
                    Convert.ChangeType(combinedElements[EvaluationFieldElements.AgentXml.Position].Value,
                        typeof (string));

            // Add the new organism state observation
            localDbContext.MCSExperimentOrganismStateDatas.Add(mcsData);

            // Save the changes and dispose of the context
            localDbContext.SaveChanges();
            localDbContext.Dispose();
        }

        #endregion
    }
}