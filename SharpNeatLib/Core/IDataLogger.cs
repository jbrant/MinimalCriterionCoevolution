#region

using System.Collections.Generic;
using SharpNeat.Loggers;

#endregion

namespace SharpNeat.Core
{
    public interface IDataLogger
    {
        /// <summary>
        ///     Opens the log file for writing.
        /// </summary>
        void Open();

        /// <summary>
        ///     Sets the current run phase.
        /// </summary>
        /// <param name="runPhase">The run phase (i.e. initialization or the primary algorithm).</param>
        void UpdateRunPhase(RunPhase runPhase);

        /// <summary>
        ///     Writes the header row of the log file.
        /// </summary>
        /// <param name="loggableElements">The list of loggable elements (containing the header text) to write.</param>
        void LogHeader(params List<LoggableElement>[] loggableElements);

        /// <summary>
        ///     Writes a data row (observation) in the log file.
        /// </summary>
        /// <param name="loggableElements">The list of loggable elements (including the header and data) to write.</param>
        void LogRow(params List<LoggableElement>[] loggableElements);

        /// <summary>
        ///     Closes the log file.
        /// </summary>
        void Close();
    }
}