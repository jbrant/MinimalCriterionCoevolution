using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentEntities;

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

            // If there were previous runs, get the maximum existing run number and increment that by one
            // Otherwise, if there were no previous runs for this experiment, set the run ID to 1
            Run = ExperimentConfiguration.NoveltyExperimentEvaluationDatas.Count > 0
                ? ExperimentConfiguration.NoveltyExperimentEvaluationDatas.Max(x => x.Run) + 1
                : 1;
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
                Configuration = { AutoDetectChangesEnabled = false, ValidateOnSaveEnabled = false }
            };

            // Combine and sort the loggable elements
            List<LoggableElement> combinedElements = ExtractSortedCombinedList(loggableElements);

            MCSExperimentEvaluationData mcsData = new MCSExperimentEvaluationData()
            {
                ExperimentDictionaryID = ExperimentConfiguration.ExperimentDictionaryID,
                Run = Run
            };

            // TODO: Add field assignemnts
            RunPhase runPhase = DbContext.RunPhases.First(w => w.RunPhaseName == "Initialization");
            
        }

        #endregion
    }
}
