using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardNet;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.Databricks.Extensions
{
    /// <summary>
    /// Extensions on the <see cref="ILogger"/> type related to Azure Databricks.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class ILoggerExtensions
    {
        /// <summary>
        /// Report a given <paramref name="jobRun"/> as a logging metric with the specified <paramref name="metricName"/>.
        /// </summary>
        /// <param name="logger">The instance to report the job outcome.</param>
        /// <param name="metricName">The name of the logging metric.</param>
        /// <param name="jobRun">The instance to report.</param>
        /// <param name="context">The additional contextual information related to the to be logged metric.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="metricName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="jobRun"/> is <c>null</c>.</exception>
        public static void LogMetricFinishedJobOutcome(this ILogger logger, string metricName, JobRun jobRun, Dictionary<string, object> context = null)
        {
            Guard.NotNull(logger, nameof(logger));
            Guard.NotNullOrWhitespace(metricName, nameof(metricName));
            Guard.NotNull(jobRun, nameof(jobRun));

            TextInfo text = new CultureInfo("en-US", useUserOverride: false).TextInfo;
            logger.LogInformation("Found finished job run with ID {RunId}", jobRun.Run.RunId);

            RunResultState? resultState = jobRun.Run.State.ResultState;
            if (resultState is null)
            {
                logger.LogWarning("Cannot find result state of finished job run with ID {RunId}", jobRun.Run.RunId);
            }
            else
            {
                string outcome = text.ToLower(resultState.ToString());

                var startContext = new Dictionary<string, object>
                {
                    { "Run Id", jobRun.Run.RunId },
                    { "Job Id", jobRun.Run.JobId },
                    { "Job Name", jobRun.JobName },
                    { "Outcome", outcome }
                };

                if (context != null)
                {
                    foreach (KeyValuePair<string, object> item in context)
                    {
                        if (startContext.ContainsKey(item.Key))
                        {
                            logger.LogWarning(
                                "Cannot add duplicate key '{KeyName}' to metric finished Databricks job context",
                                item.Key);
                        }
                        else
                        {
                            startContext.Add(item.Key, item.Value);
                        }
                    }
                }

                logger.LogMetric(metricName, value: 1, context: startContext);
            }
        }
    }
}
