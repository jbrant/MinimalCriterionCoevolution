#region

using System.Collections.Generic;
using System.Linq;
using ExperimentEntities;
using SharpNeat.Core;

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
        ///     Extracts all of the sub-lists and combines them into a single unified list of loggable elements.
        /// </summary>
        /// <param name="loggableElements">The list(s) of loggable elements to combine.</param>
        /// <returns>Unified, sorted lists of all loggable elements.</returns>
        protected List<LoggableElement> ExtractSortedCombinedList(params List<LoggableElement>[] loggableElements)
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

        #endregion

        #region Logger Instance Fields

        /// <summary>
        ///     The database context class for persistence control.
        /// </summary>
        protected ExperimentDataEntities DbContext;

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

        #endregion

        #region Logging Control Methods

        /// <summary>
        ///     Instantiates a new database context (opens the connection) and gets the experiment configuration key.
        /// </summary>
        public virtual void Open()
        {
            // Open the database connection
            DbContext = new ExperimentDataEntities();

            // Query for the experiment configuration entity
            ExperimentConfiguration =
                DbContext.ExperimentDictionaries.Single(expName => expName.ExperimentName == ExperimentConfigurationName);
        }

        /// <summary>
        ///     This doesn't do anything for database logging, but is defined for conformance with the interface.
        /// </summary>
        /// <param name="loggableElements">The loggable elements from which to extract the header title.</param>
        public void LogHeader(params List<LoggableElement>[] loggableElements)
        {
        }

        /// <summary>
        ///     Logs the given loggable element data.  This method is abstract because the entity mapping will vary based on the
        ///     table, so the mapping will have to be implemented via a child mapping directly tied to that entity.
        /// </summary>
        /// <param name="loggableElements"></param>
        public abstract void LogRow(params List<LoggableElement>[] loggableElements);

        /// <summary>
        ///     Closes the database connection.
        /// </summary>
        public void Close()
        {
            DbContext.Dispose();
        }

        #endregion
    }
}