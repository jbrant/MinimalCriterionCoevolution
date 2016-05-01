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
        ///     Experiment execution strategy constructor, accepting the specified number of retry counts and the maximum retry
        ///     interval (in milliseconds).
        /// </summary>
        /// <param name="retryCount">The retry count.</param>
        /// <param name="retryTimeSpan">The maximum time between retries.</param>
        public ExperimentDbExecutionStrategy(int retryCount, TimeSpan retryTimeSpan) : base(retryCount, retryTimeSpan)
        {
        }

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
                DateTime errorTime = DateTime.Now;

                // Log that exception occurred
                Console.Error.WriteLine("An exception occurred at: {0}", errorTime);

                // Print out all of the errors
                foreach (var error in sqlException.Errors)
                {
                    Console.Error.WriteLine("The following error occurred at time {0}: {1}", errorTime, error);
                }

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
                    Console.Error.WriteLine("Retrying query at time {0}", DateTime.Now);
                    retry = true;
                }
                else
                {
                    Console.Error.WriteLine("Not retrying query at time {0}", DateTime.Now);
                }
            }

            // Additionally, if the exception was specifically a timeout exception (of any code), always retry
            if (exception is TimeoutException)
            {
                Console.Error.WriteLine("Exception was a timeout exception, so automatically retrying at time {0}",
                    DateTime.Now);
                retry = true;
            }

            return retry;
        }
    }
}