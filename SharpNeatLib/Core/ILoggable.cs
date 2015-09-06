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
        /// <returns></returns>
        List<LoggableElement> GetLoggableElements();
    }
}