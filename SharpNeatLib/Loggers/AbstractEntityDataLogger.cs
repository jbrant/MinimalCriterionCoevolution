#region

using System.Collections.Generic;
using System.Linq;
using ExperimentEntities;
using ExperimentEntities.entities;
using SharpNeat.Core;
using RunPhase = SharpNeat.Core.RunPhase;

#endregion

namespace SharpNeat.Loggers
{
    /// <summary>
    ///     Entity data logger base class.  All methods are inherited except the specifics of how data is mapped to entity
    ///     fields and persisted.
    /// </summary>
    public abstract class AbstractEntityDataLogger : IDataLogger
    {
        #region Constructors

        /// <summary>
        ///     Base entity data logger constructor.
        /// </summary>
        /// <param name="experimentName">The unique name of the experiment configuration.</param>
        protected AbstractEntityDataLogger(string experimentName)
        {
            ExperimentConfigurationName = experimentName;
        }

        #endregion

        #region Protected instance methods

        /// <summary>
        ///     Extracts all of the sub-lists, combines them into a single unified list of loggable elements, and positions them at
        ///     the appropriate location in a loggable elements array.
        /// </summary>
        /// <param name="loggableElements">The list(s) of loggable elements to combine.</param>
        /// <returns>Array of all loggable elements.</returns>
        protected LoggableElement[] ExtractLoggableElementArray(int fieldArrayLength,
            params List<LoggableElement>[] loggableElements)
        {
            var combinedElements = new List<LoggableElement>();
            var loggableElementArray = new LoggableElement[fieldArrayLength];

            // Combine everything into a single list
            foreach (var loggableElementList in loggableElements)
            {
                combinedElements.AddRange(loggableElementList);
            }

            foreach (var curLoggableElement in combinedElements)
            {
                // Place the loggable element in the array at the specified location
                loggableElementArray[curLoggableElement.FieldMetadata.Position] = curLoggableElement;
            }

            return loggableElementArray;
        }

        #endregion

        #region Logger Instance Fields

        /// <summary>
        ///     The database context class for persistence control.
        /// </summary>
        protected ExperimentDataContext DbContext;

        /// <summary>
        ///     The unique name of the experiment configuration (used for looking up the ID/primary key).
        /// </summary>
        protected readonly string ExperimentConfigurationName;

        /// <summary>
        ///     The unique experiment configuration entity.
        /// </summary>
        protected ExperimentDictionary ExperimentConfiguration;

        /// <summary>
        ///     The run number for the given experiment.
        /// </summary>
        protected int Run;

        /// <summary>
        ///     The run phase foreign key.
        /// </summary>
        protected int RunPhaseKey;

        #endregion

        #region Logging Control Methods

        /// <inheritdoc />
        /// <summary>
        ///     Instantiates a new database context (opens the connection) and gets the experiment configuration key.
        /// </summary>
        public virtual void Open()
        {
            // Open the database connection
            DbContext = new ExperimentDataContext();

            // Query for the experiment configuration entity
            ExperimentConfiguration =
                DbContext.ExperimentDictionary.Single(
                    expName => expName.ExperimentName == ExperimentConfigurationName);
        }

        /// <summary>
        ///     Return whether the database stream has been opened.
        /// </summary>
        /// <returns>Boolean flag indicating whether the database stream has been opened.</returns>
        public bool IsStreamOpen()
        {
            return DbContext != null;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Updates the run phase and sets the corresponding foreign key.
        /// </summary>
        /// <param name="runPhase">The run phase for the current experiment.</param>
        public void UpdateRunPhase(RunPhase runPhase)
        {
            RunPhaseKey =
                DbContext.RunPhase.First(
                    x => x.RunPhaseName == runPhase.ToString()).RunPhaseId;
        }

        /// <inheritdoc />
        /// <summary>
        ///     This doesn't do anything for database logging, but is defined for conformance with the interface.
        /// </summary>
        /// <param name="loggableElements">The loggable elements from which to extract the header title.</param>
        public void LogHeader(params List<LoggableElement>[] loggableElements)
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Logs the given loggable element data.  This method is abstract because the entity mapping will vary based on the
        ///     table, so the mapping will have to be implemented via a child mapping directly tied to that entity.
        /// </summary>
        /// <param name="loggableElements"></param>
        public abstract void LogRow(params List<LoggableElement>[] loggableElements);

        /// <inheritdoc />
        /// <summary>
        ///     Wipes out any existing log entries.
        /// </summary>
        public void ResetLog()
        {
            // TODO: Needs to be handled for entity/database logging
            throw new SharpNeatException("Reset log not implemented for entity logging.");
        }

        /// <inheritdoc />
        /// <summary>
        ///     Closes the database connection (not needed since we're creating/disposing the context on every transaction).
        /// </summary>
        public void Close()
        {
            DbContext.Dispose();
        }

        #endregion
    }
}