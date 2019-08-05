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
        ///     A logger that records the genome IDs in the extant population at each interval over the course of a run.
        /// </summary>
        Population,

        /// <summary>
        ///     A logger that records the genome definitions of each distinct member of the population over the course of a run.
        /// </summary>
        Genome,

        /// <summary>
        ///     A "null" logger (doesn't log anything).
        /// </summary>
        None
    }

    /// <summary>
    ///     Captures the destinations to which logs can be written. Currently, a flat file is the only destination, but others could be added (e.g. database, message queue, etc).
    /// </summary>
    public enum LoggingDestination
    {
        /// <summary>
        ///     Indicates a flat file-based log destination.
        /// </summary>
        File
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

            // Check if this is population logging
            if (LoggingType.Population.ToString()
                .Equals(strLoggingType, StringComparison.InvariantCultureIgnoreCase))
            {
                return LoggingType.Population;
            }

            // Check if this is genome logging
            if (LoggingType.Genome.ToString()
                .Equals(strLoggingType, StringComparison.InvariantCultureIgnoreCase))
            {
                return LoggingType.Genome;
            }

            // Otherwise, it's null (no logging)
            return LoggingType.None;
        }
    }
}