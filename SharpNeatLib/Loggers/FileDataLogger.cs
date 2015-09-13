#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Loggers
{
    public class FileDataLogger : IDataLogger
    {
        #region Constructors

        /// <summary>
        ///     FileDataLogger constructor.
        /// </summary>
        /// <param name="filename">The filename (possibly including path) to log.</param>
        /// <param name="delimiter">The delimiter character to use for separating fields.</param>
        public FileDataLogger(String filename, String delimiter = ",")
        {
            LogFileName = filename;
            _rowElementDelimiter = delimiter;
        }

        #endregion

        #region Logger Properties

        /// <summary>
        ///     The name of the log file.
        /// </summary>
        public string LogFileName { get; }

        #endregion

        #region Logger Instance Fiels

        /// <summary>
        ///     The delimiter to use for record separation.
        /// </summary>
        private readonly string _rowElementDelimiter;

        /// <summary>
        ///     The StreamWriter instance.
        /// </summary>
        private StreamWriter _writer;

        #endregion

        #region Logging Control Methods

        /// <summary>
        ///     Opens the StreamWriter (using the given filename) for writing.
        /// </summary>
        public void Open()
        {
            _writer = new StreamWriter(LogFileName);
        }

        /// <summary>
        ///     Logs the file header.
        /// </summary>
        /// <param name="loggableElements">The LoggableElements which include the header text.</param>
        public void LogHeader(params List<LoggableElement>[] loggableElements)
        {
            // Combine and sort the loggable elements
            List<LoggableElement> combinedElements = extractSortedCombinedList(loggableElements);

            // Write the header
            _writer.WriteLine(string.Join(_rowElementDelimiter, extractHeaderNames(combinedElements)));

            // Immediatley flush to the log file
            _writer.Flush();
        }

        /// <summary>
        ///     Logs a data row (observation).
        /// </summary>
        /// <param name="loggableElements">
        ///     The LoggableElements which contains both the header (value description metadata) and the
        ///     value itself.
        /// </param>
        public void LogRow(params List<LoggableElement>[] loggableElements)
        {
            // Combine and sort the loggable elements
            List<LoggableElement> combinedElements = extractSortedCombinedList(loggableElements);

            // Write observation row
            _writer.WriteLine(string.Join(_rowElementDelimiter, extractDataPoints(combinedElements)));

            // Immediatley flush to the log file
            _writer.Flush();
        }

        /// <summary>
        ///     Closes the StreamWriter.
        /// </summary>
        public void Close()
        {
            _writer.Close();
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

        /// <summary>
        ///     Extracts the header names from a list of LoggableElements.
        /// </summary>
        /// <param name="loggableElements">The list of LoggableElements containing the header values.</param>
        /// <returns>The header names from the given LoggableElements.</returns>
        private List<String> extractHeaderNames(List<LoggableElement> loggableElements)
        {
            return loggableElements.Select(loggableElement => loggableElement.Header).ToList();
        }

        /// <summary>
        ///     Extracts the values (observations) from the list of LoggableElements.
        /// </summary>
        /// <param name="loggableElements">The list of LoggableElements containing the observation values.</param>
        /// <returns>The values (observations) from the given LoggableElements.</returns>
        private List<String> extractDataPoints(List<LoggableElement> loggableElements)
        {
            return loggableElements.Select(LoggableElement => LoggableElement.Value).ToList();
        }

        #endregion
    }
}