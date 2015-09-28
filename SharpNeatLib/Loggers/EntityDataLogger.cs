using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExperimentEntities;
using SharpNeat.Core;

namespace SharpNeat.Loggers
{
    public class EntityDataLogger : IDataLogger
    {
        #region Constructors

        public EntityDataLogger(int experimentConfiguration)
        {
            _experimentConfiguration = experimentConfiguration;
        }

        #endregion

        #region Logger Properties

        /// <summary>
        ///     The name of the log file.
        /// </summary>
        public string LogFileName { get; }

        #endregion

        #region Logger Instance Fields

        private ExperimentDataEntities _dbContext;

        private readonly int _experimentConfiguration;

        #endregion

        #region Logging Control Methods

        public void Open()
        {
            _dbContext = new ExperimentDataEntities();
        }

        public void LogHeader(params List<LoggableElement>[] loggableElements)
        {
        }

        public void LogRow(params List<LoggableElement>[] loggableElements)
        {
            // Combine and sort the loggable elements
            List<LoggableElement> combinedElements = extractSortedCombinedList(loggableElements);

            NoveltyExperimentEvaluationData noveltyData = new NoveltyExperimentEvaluationData();

            noveltyData.ExperimentID_FK = _experimentConfiguration;
            noveltyData.Generation = Int32.Parse(combinedElements[(int)FieldPosition.NoveltyEvaluationFieldPosition.CurrentGeneration].Value);
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        #endregion

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
    }
}
