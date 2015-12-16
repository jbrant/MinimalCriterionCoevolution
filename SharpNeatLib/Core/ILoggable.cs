#region

using System.Collections.Generic;
using SharpNeat.Loggers;

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Defines a contract to which implementing class must comply for logging their state or key parameters.
    /// </summary>
    public interface ILoggable
    {
        /// <summary>
        ///     Method to returns the loggable elements of a particular implementing class.
        /// </summary>
        /// <param name="logFieldEnableMap">
        ///     Dictionary of logging fields that can be enabled or disabled based on the specification of the calling
        ///     routine.
        /// </param>
        /// <returns>List of LoggableElement objects.</returns>
        List<LoggableElement> GetLoggableElements(IDictionary<FieldElement, bool> logFieldEnableMap = null);
    }
}