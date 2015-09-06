#region

using System;

#endregion

namespace SharpNeat.Loggers
{
    /// <summary>
    ///     Encapsulates properties of a cell to be logged into an evolution data log file.
    /// </summary>
    public class LoggableElement : IComparable
    {
        /// <summary>
        ///     LoggableElement constructor, which sets the header and value.
        /// </summary>
        /// <param name="header">The header of the LoggableElement.</param>
        /// <param name="value">The string-based value of the LoggableElement.</param>
        public LoggableElement(String header, String value)
        {
            Header = header;
            Value = value;
        }

        /// <summary>
        ///     The header label of the LoggableElement.
        /// </summary>
        public String Header { get; }

        /// <summary>
        ///     The string-value of the LoggableElement.
        /// </summary>
        public String Value { get; private set; }

        /// <summary>
        ///     Compares to another (presumably) LoggableElement in order to support lexicographical sorting by the header value.
        /// </summary>
        /// <param name="obj">The LoggableElement against which to compare.</param>
        /// <returns>The comparative lexicographical ordering of the two elements.</returns>
        public int CompareTo(object obj)
        {
            // If null, then this element is lexicographically larger than
            // that to which it is being compared
            if (obj == null)
                return 1;

            // Cast to LoggableElement
            LoggableElement otherElement = obj as LoggableElement;

            // If the cast was valid, perform the comparison
            if (otherElement != null)
            {
                return Header.CompareTo(otherElement.Header);
            }

            // Otherwise, we can't compare against a non-LoggableElement
            throw new ArgumentException("Object is not a LoggableElement");
        }
    }
}