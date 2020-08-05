using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Databricks;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.Databricks.Client;

namespace Arcus.BackgroundJobs.Tests.Unit.Databricks
{
    /// <summary>
    /// Stub <see cref="DatabricksJobMetricsJobSchedulerOptions"/> implementation to simulate a <see cref="DatabricksJobMetricsJob"/> registration.
    /// </summary>
    public class StubDatabricksJobMetricJobSchedulerOptions : DatabricksJobMetricsJobSchedulerOptions
    {
        private readonly IEnumerable<Run> _runs;
        private readonly IEnumerable<Job> _jobs;
        private readonly DateTimeOffset _startWindow, _endWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubDatabricksJobMetricJobSchedulerOptions"/> class.
        /// </summary>
        /// <param name="runs">The runs the created <see cref="DatabricksClient"/> should return.</param>
        /// <param name="jobs">The jobs the created <see cref="DatabricksClient"/> should return.</param>
        /// <param name="startWindow">The start of the time window in which the <see cref="DatabricksJobMetricsJob"/> queried jobs should have run.</param>
        /// <param name="endWindow">The end of the time window in which the <see cref="DatabricksJobMetricsJob"/> queried jobs should have run.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="runs"/> or <paramref name="jobs"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="endWindow"/> is less than the <paramref name="startWindow"/>.</exception>
        public StubDatabricksJobMetricJobSchedulerOptions(
            IEnumerable<Run> runs, 
            IEnumerable<Job> jobs,
            DateTimeOffset startWindow,
            DateTimeOffset endWindow)
        {
            Guard.NotNull(runs, nameof(runs));
            Guard.NotNull(jobs, nameof(jobs));
            Guard.NotLessThan(endWindow, startWindow, nameof(endWindow));

            _runs = runs;
            _jobs = jobs;
            _startWindow = startWindow;
            _endWindow = endWindow;
        }

        /// <summary>
        /// Creates an <see cref="DatabricksClient"/> instance using the predefined values.
        /// </summary>
        /// <param name="secretProvider">The provider to retrieve the token during the creation of the instance.</param>
        public override Task<DatabricksClient> CreateDatabricksClientAsync(ISecretProvider secretProvider)
        {
            DatabricksClient client = DatabricksClientFactory.Create(_runs, _jobs);
            return Task.FromResult(client);
        }

        /// <summary>
        /// Determining the next time window in which the job runs should be retrieved.
        /// </summary>
        public override (DateTimeOffset last, DateTimeOffset next) DetermineNextTimeWindow()
        {
            return (_startWindow, _endWindow);
        }
    }
}
