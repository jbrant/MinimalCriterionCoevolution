using System;

namespace MCC_Domains
{
    /// <summary>
    ///     Custom exception to be thrown during validity checking of experiment configuration.
    /// </summary>
    [Serializable]
    public class ConfigurationException : Exception
    {
        public ConfigurationException()
        {
        }

        public ConfigurationException(string message) : base(message)
        {
        }
    }
}