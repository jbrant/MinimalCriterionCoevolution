﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Loggers
{
    /// <summary>
    ///     Handles logging experiment data to a flat file.
    /// </summary>
    public class FileDataLogger : IDataLogger
    {
        #region Constructors

        /// <summary>
        ///     FileDataLogger constructor.
        /// </summary>
        /// <param name="filename">The filename (possibly including path) to log.</param>
        /// <param name="delimiter">The delimiter character to use for separating fields.</param>
        public FileDataLogger(string filename, string delimiter = ",")
        {
            _logFileName = filename;
            _rowElementDelimiter = delimiter;
            _isHeaderWritten = false;
        }

        #endregion

        #region Logger Instance Fields

        /// <summary>
        ///     The name of the log file.
        /// </summary>
        private readonly string _logFileName;

        /// <summary>
        ///     The delimiter to use for record separation.
        /// </summary>
        private readonly string _rowElementDelimiter;

        /// <summary>
        ///     The StreamWriter instance.
        /// </summary>
        private StreamWriter _writer;

        /// <summary>
        ///     Indicates whether the header has been written or not (used in multipart EAs where the open method could be called
        ///     more than once).
        /// </summary>
        private bool _isHeaderWritten;

        #endregion

        #region Logging Control Methods

        /// <inheritdoc />
        /// <summary>
        ///     Opens the StreamWriter (using the given filename) for writing.
        /// </summary>
        public void Open()
        {
            if (_writer != null) return;
            _writer = new StreamWriter(_logFileName) {AutoFlush = true};
        }

        /// <summary>
        ///     Return whether the file stream has been opened.
        /// </summary>
        /// <returns>Boolean flag indicating whether the file stream has been opened.</returns>
        public bool IsStreamOpen()
        {
            return _writer != null;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Logs the run phase (initialization or primary).
        /// </summary>
        /// <param name="runPhase">The run phase (initialization or primary).</param>
        public void UpdateRunPhase(RunPhase runPhase)
        {
            // Nothing to be done for the file logger
        }

        /// <inheritdoc />
        /// <summary>
        ///     Logs the file header.
        /// </summary>
        /// <param name="loggableElements">The LoggableElements which include the header text.</param>
        public void LogHeader(params List<LoggableElement>[] loggableElements)
        {
            try
            {
                // Do nothing if the header has already been written to the output file
                if (_isHeaderWritten)
                    return;

                // Combine and sort the loggable elements
                var combinedElements = extractSortedCombinedList(loggableElements);

                // Write the header
                _writer.WriteLine(string.Join(_rowElementDelimiter, extractHeaderNames(combinedElements)));

                // Update the header written state
                _isHeaderWritten = true;

                // Immediatley flush to the log file
                _writer.Flush();
            }
            catch (Exception)
            {
                Debug.WriteLine($"Exception occurred while writing header to log file [{_logFileName}]");
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Logs a data row (observation).
        /// </summary>
        /// <param name="loggableElements">
        ///     The LoggableElements which contains both the header (value description metadata) and the
        ///     value itself.
        /// </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void LogRow(params List<LoggableElement>[] loggableElements)
        {
            // Combine and sort the loggable elements
            var combinedElements = extractSortedCombinedList(loggableElements);

            // Write observation row
            _writer.WriteLine(string.Join(_rowElementDelimiter, extractDataPoints(combinedElements)));

            // Immediatley flush to the log file
            _writer.Flush();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Wipes out any existing log entries in preparation for a complete restart.
        /// </summary>
        public void ResetLog()
        {
            // Close any handles on the log file
            if (_writer != null)
            {
                Close();
            }

            // Delete the file
            File.Delete(_logFileName);

            // Reset header written status and null out writer
            _isHeaderWritten = false;
            _writer = null;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Closes and disposes of the StreamWriter.
        /// </summary>
        public void Close()
        {
            _writer.Close();
            _writer.Dispose();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        ///     Combines all of the LoggableElement lists and sorts them based on header lexicographical order.
        /// </summary>
        /// <param name="loggableElements">The array of LoggableElement lists.</param>
        /// <returns>Combined and sorted list of LoggableElements.</returns>
        private List<LoggableElement> extractSortedCombinedList(params List<LoggableElement>[] loggableElements)
        {
            var combinedElements = new List<LoggableElement>();

            // Combine everything into a single list
            foreach (var loggableElementList in loggableElements)
            {
                if (loggableElementList == null) continue;
                combinedElements.AddRange(loggableElementList);
            }

            // Remove all null from combined list
            combinedElements.RemoveAll(item => item == null);

            // Sort the elements so that everything logged is kept in the same order
            combinedElements.Sort();

            return combinedElements;
        }

        /// <summary>
        ///     Extracts the header names from a list of LoggableElements.
        /// </summary>
        /// <param name="loggableElements">The list of LoggableElements containing the header values.</param>
        /// <returns>The header names from the given LoggableElements.</returns>
        private IEnumerable<string> extractHeaderNames(List<LoggableElement> loggableElements)
        {
            return loggableElements.Select(loggableElement => loggableElement.FieldMetadata.FriendlyName).ToList();
        }

        /// <summary>
        ///     Extracts the values (observations) from the list of LoggableElements.
        /// </summary>
        /// <param name="loggableElements">The list of LoggableElements containing the observation values.</param>
        /// <returns>The values (observations) from the given LoggableElements.</returns>
        private IEnumerable<string> extractDataPoints(List<LoggableElement> loggableElements)
        {
            return loggableElements.Select(loggableElement => Convert.ToString(loggableElement.Value)).ToList();
        }

        #endregion
    }
}