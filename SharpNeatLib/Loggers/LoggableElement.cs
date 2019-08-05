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
        /// <param name="fieldElement">The position and name (and possibly other metadata) of the field to be logged.</param>
        /// <param name="value">The value of the LoggableElement.</param>
        public LoggableElement(FieldElement fieldElement, object value)
        {
            FieldMetadata = fieldElement;
            Value = value;
        }

        /// <summary>
        ///     The position of the given field in the file/table output (used for keying into domain-specific enum map).
        /// </summary>
        public FieldElement FieldMetadata { get; }

        /// <summary>
        ///     The value of the LoggableElement.
        /// </summary>
        public object Value { get; }

        /// <inheritdoc />
        /// <summary>
        ///     Compares to another (presumably) LoggableElement in order to support lexicographical sorting by the header value.
        /// </summary>
        /// <param name="obj">The LoggableElement against which to compare.</param>
        /// <returns>The comparative lexicographical ordering of the two elements.</returns>
        public int CompareTo(object obj)
        {
            switch (obj)
            {
                // If null, then this element is lexicographically larger than
                // that to which it is being compared
                case null:
                    return 1;
                // If the cast is valid, perform the comparison
                case LoggableElement otherElement:
                    return FieldMetadata.Position.CompareTo(otherElement.FieldMetadata.Position);
                default:
                    // Otherwise, we can't compare against a non-LoggableElement
                    throw new ArgumentException("Object is not a LoggableElement");
            }
        }
    }
}