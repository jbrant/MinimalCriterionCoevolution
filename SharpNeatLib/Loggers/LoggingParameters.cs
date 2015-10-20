#region

using System;

#endregion

namespace SharpNeat.Loggers
{
    /// <summary>
    ///     Captures the different types of logging.
    /// </summary>
    public enum LoggingType
    {
        /// <summary>
        ///     A logger that records per-generation statistics about an entire population.
        /// </summary>
        Evolution,

        /// <summary>
        ///     A logger that records attributes of each individual being evaluated for each generation/evaluation.
        /// </summary>
        Evaluation,

        /// <summary>
        ///     A "null" logger (doesn't log anything).
        /// </summary>
        None
    }

    /// <summary>
    ///     Captures the destinations to which logs can be written.
    /// </summary>
    public enum LoggingDestination
    {
        /// <summary>
        ///     Indicates a flat file-based log destination.
        /// </summary>
        File,

        /// <summary>
        ///     Indicates a database logging destination.
        /// </summary>
        Database
    }

    /// <summary>
    ///     Utility methods for converting to logging type/destination domain enums.
    /// </summary>
    public static class LoggingParameterUtils
    {
        /// <summary>
        ///     Converts the given string into the appropriate logging type.
        /// </summary>
        /// <param name="strLoggingType">The string to convert.</param>
        /// <returns>The corresponding logging type enum.</returns>
        public static LoggingType ConvertStringToLoggingType(string strLoggingType)
        {
            // Check if this is evolution logging
            if (LoggingType.Evolution.ToString().Equals(strLoggingType, StringComparison.InvariantCultureIgnoreCase))
            {
                return LoggingType.Evolution;
            }
            // Check if this is evaluation logging
            if (LoggingType.Evaluation.ToString()
                .Equals(strLoggingType, StringComparison.InvariantCultureIgnoreCase))
            {
                return LoggingType.Evaluation;
            }

            // Otherwise, it's null (no logging)
            return LoggingType.None;
        }

        /// <summary>
        ///     Converts the given string into the appropriate logging destination.
        /// </summary>
        /// <param name="strLoggingDestination">The string to convert.</param>
        /// <returns>The corresponding logging destination enum.</returns>
        public static LoggingDestination ConvertStringToLoggingDestination(string strLoggingDestination)
        {
            // Check if this is a database destination
            if (LoggingDestination.Database.ToString()
                .Equals(strLoggingDestination, StringComparison.InvariantCultureIgnoreCase))
            {
                return LoggingDestination.Database;
            }

            // Otherwise, default to file destination
            return LoggingDestination.File;
        }
    }
}