#region

using System.Data.Entity;

#endregion

namespace ExperimentEntities
{
    /// <summary>
    ///     Overrides the default database configuration specifically in order to add a resilient execution strategy for the
    ///     experiment database.
    /// </summary>
    public class ExperimentDbConfiguration : DbConfiguration
    {
        /// <summary>
        ///     Experiment database configuration constructor.
        /// </summary>
        public ExperimentDbConfiguration()
        {
            // Register the experiment database execution strategy
            SetExecutionStrategy("System.Data.SqlClient", () => new ExperimentDbExecutionStrategy());
        }
    }
}