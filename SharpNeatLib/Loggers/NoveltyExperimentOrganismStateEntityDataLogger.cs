#region

using System;
using System.Collections.Generic;
using System.Linq;
using ExperimentEntities;

#endregion

namespace SharpNeat.Loggers
{
    /// <inheritdoc />
    /// <summary>
    ///     Entity data logger class for novelty experiment evaluation data.
    /// </summary>
    public class NoveltyExperimentOrganismStateEntityDataLogger : AbstractEntityDataLogger
    {
        #region Constructors

        /// <inheritdoc />
        /// <summary>
        ///     Novelty experiment organism state constructor (simply passes parameters to base constructor).
        /// </summary>
        /// <param name="experimentName">The unique name of the experiment configuration.</param>
        public NoveltyExperimentOrganismStateEntityDataLogger(string experimentName)
            : base(experimentName)
        {
        }

        #endregion

        #region Logging Control Methods

        /// <inheritdoc />
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
                              w => w.ExperimentDictionaryID == ExperimentConfiguration.ExperimentDictionaryID)
                          .Max(m => m.Run) +
                      1
                    : 1;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Maps applicable entity fields for the novelty experiment organism state entity and persists to the database.
        /// </summary>
        /// <param name="loggableElements">The loggable elements (data) to persist.</param>
        public override void LogRow(params List<LoggableElement>[] loggableElements)
        {
            // Initialize new DB context
            var localDbContext = new ExperimentDataEntities
            {
                Configuration = {AutoDetectChangesEnabled = false, ValidateOnSaveEnabled = false}
            };

            // Combine and sort the loggable elements
            var combinedElements = ExtractLoggableElementArray(EvaluationFieldElements.NumFieldElements,
                loggableElements);

            var noveltyData = new NoveltyExperimentOrganismStateData
            {
                ExperimentDictionaryID = ExperimentConfiguration.ExperimentDictionaryID,
                Run = Run
            };

            noveltyData.Generation =
                (int)
                Convert.ChangeType(combinedElements[EvaluationFieldElements.Generation.Position].Value,
                    noveltyData.Generation.GetType());
            noveltyData.Evaluation =
                (int)
                Convert.ChangeType(combinedElements[EvaluationFieldElements.EvaluationCount.Position].Value,
                    noveltyData.Evaluation.GetType());
            noveltyData.StopConditionSatisfied =
                (bool)
                Convert.ChangeType(
                    combinedElements[EvaluationFieldElements.StopConditionSatisfied.Position].Value,
                    noveltyData.StopConditionSatisfied.GetType());
            noveltyData.DistanceToTarget =
                (double)
                Convert.ChangeType(
                    combinedElements[EvaluationFieldElements.DistanceToTarget.Position].Value,
                    noveltyData.DistanceToTarget.GetType());
            noveltyData.AgentXLocation =
                (double)
                Convert.ChangeType(combinedElements[EvaluationFieldElements.AgentXLocation.Position].Value,
                    noveltyData.AgentXLocation.GetType());
            noveltyData.AgentYLocation =
                (double)
                Convert.ChangeType(combinedElements[EvaluationFieldElements.AgentYLocation.Position].Value,
                    noveltyData.AgentYLocation.GetType());

            // Add the new organism state observation
            localDbContext.NoveltyExperimentOrganismStateDatas.Add(noveltyData);

            // Save the changes and dispose of the context
            localDbContext.SaveChanges();
            localDbContext.Dispose();
        }

        #endregion
    }
}