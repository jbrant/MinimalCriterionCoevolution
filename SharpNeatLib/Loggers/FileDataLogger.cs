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

        public FileDataLogger(String filename, String delimiter = ",")
        {
            LogFileName = filename;
            rowElementDelimiter = delimiter;
        }

        #endregion

        #region Logger Properties

        public string LogFileName { get; }

        #endregion

        #region Logger Instance Fiels

        private readonly string rowElementDelimiter;

        private StreamWriter writer;

        #endregion

        #region Logging Control Methods

        public void Open()
        {
            writer = new StreamWriter(LogFileName);
        }

        public void LogHeader(params List<LoggableElement>[] loggableElements)
        {
            // Combine and sort the loggable elements
            List<LoggableElement> combinedElements = extractSortedCombinedList(loggableElements);

            // Write the header
            writer.WriteLine(string.Join(rowElementDelimiter, extractHeaderNames(combinedElements)));

            // Immediatley flush to the log file
            writer.Flush();
        }

        public void LogRow(params List<LoggableElement>[] loggableElements)
        {
            // Combine and sort the loggable elements
            List<LoggableElement> combinedElements = extractSortedCombinedList(loggableElements);

            // Write observation row
            writer.WriteLine(string.Join(rowElementDelimiter, extractDataPoints(combinedElements)));

            // Immediatley flush to the log file
            writer.Flush();
        }

        public void Close()
        {
            writer.Close();
        }

        #endregion

        #region Private Helper Methods

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

        private List<String> extractHeaderNames(List<LoggableElement> loggableElements)
        {
            return loggableElements.Select(loggableElement => loggableElement.Header).ToList();
        }

        private List<String> extractDataPoints(List<LoggableElement> loggableElements)
        {
            return loggableElements.Select(LoggableElement => LoggableElement.Value).ToList();
        }

        #endregion
    }
}