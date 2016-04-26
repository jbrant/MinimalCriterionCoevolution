#region

using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

#endregion

namespace ExperimentEntities
{
    /// <summary>
    ///     Implements a "resilient" execution strategy for the experiment database that determines when to retry failed
    ///     transactions.
    /// </summary>
    internal class ExperimentDbExecutionStrategy : DbExecutionStrategy
    {
        /// <summary>
        ///     Deadlock error code.
        /// </summary>
        private const int Deadlock = 1205;

        /// <summary>
        ///     Connection failure error code.
        /// </summary>
        private const int ConnectionFailure = 1225;

        /// <summary>
        ///     Timeout error code.
        /// </summary>
        private const int Timeout = -2;

        /// <summary>
        ///     Determines whether an error that occurred during a database transaction should be retried (i.e. connection
        ///     resiliency).
        /// </summary>
        /// <param name="exception">The SQL exception that was thrown.</param>
        /// <returns>Indicator of whether or not the operation should be retried.</returns>
        protected override bool ShouldRetryOn(Exception exception)
        {
            bool retry = false;

            // Grab the exception and determine whether to retry if there was an exception thrown
            SqlException sqlException = exception as SqlException;
            if (sqlException != null)
            {
                // Only retry for the recognized timeout/deadlock related codes
                int[] errorsToRetry =
                {
                    Deadlock,
                    ConnectionFailure,
                    Timeout
                };

                // If the error matches one of these codes, then retry
                if (sqlException.Errors.Cast<SqlError>().Any(x => errorsToRetry.Contains(x.Number)))
                {
                    retry = true;
                }
            }

            // Additionally, if the exception was specifically a timeout exception (of any code), always retry
            if (exception is TimeoutException)
            {
                retry = true;
            }

            return retry;
        }
    }
}