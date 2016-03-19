#region

using System.Xml;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Domains
{
    public interface ICoevolutionExperiment
    {
        /// <summary>
        ///     Gets the name of the experiment.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets human readable explanatory text for the experiment.
        /// </summary>
        string Description { get; }

        /// <summary>
        ///     Gets the default first population size to use for the experiment.
        /// </summary>
        int DefaultPopulationSize1 { get; }

        /// <summary>
        ///     Gets the default second population size to use for the experiment.
        /// </summary>
        int DefaultPopulationSize2 { get; }

        /// <summary>
        ///     Initialize the experiment with some optional XML configutation data.
        /// </summary>
        void Initialize(string name, XmlElement xmlConfig, IDataLogger evolutionDataLogger = null,
            IDataLogger evaluationDataLogger = null);
    }
}