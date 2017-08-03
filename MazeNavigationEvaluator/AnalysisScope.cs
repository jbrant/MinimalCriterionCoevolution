#region

using System;

#endregion

namespace MazeNavigationEvaluator
{
    /// <summary>
    ///     Encapsulates the scope of the analysis being performed with regard to the amount of data and population members
    ///     being analyzed.
    /// </summary>
    public enum AnalysisScope
    {
        /// <summary>
        ///     Analyze every batch of genomes through the entire run.
        /// </summary>
        Full,

        /// <summary>
        ///     Select all of the distinct genomes produced throughout the run and analyze them in aggregate.
        /// </summary>
        Aggregate,

        /// <summary>
        ///     Analyze only the genomes in the last batch.
        /// </summary>
        Last
    }

    /// <summary>
    ///     Provides utility methods for determining the appropriate analysis scope.
    /// </summary>
    public static class AnalysisScopeUtil
    {
        /// <summary>
        ///     Determines the appropriate analysis scope based on the given string value.
        /// </summary>
        /// <param name="strAnalysisScope">The string-valued analysis scope.</param>
        /// <returns>The analysis scope domain type.</returns>
        public static AnalysisScope ConvertStringToAnalysisScope(string strAnalysisScope)
        {
            // Check if aggregate type specified
            if ("Aggregate".Equals(strAnalysisScope, StringComparison.InvariantCultureIgnoreCase))
            {
                return AnalysisScope.Aggregate;
            }

            // Check if last batch type specified
            if ("Last".Equals(strAnalysisScope, StringComparison.InvariantCultureIgnoreCase))
            {
                return AnalysisScope.Last;
            }

            // If nothing matches, default to full analysis
            return AnalysisScope.Full;
            ;
        }
    }
}