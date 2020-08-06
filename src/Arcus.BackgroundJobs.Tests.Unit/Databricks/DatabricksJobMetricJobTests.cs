using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Databricks;
using Arcus.Security.Core;
using Arcus.Testing.Logging;
using Bogus;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DatabricksJobMetricsJob = Arcus.BackgroundJobs.Databricks.DatabricksJobMetricsJob;

namespace Arcus.BackgroundJobs.Tests.Unit.Databricks
{
    [Trait(name: "Category", value: "Unit")]
    public class DatabricksJobMetricJobTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public async Task ExecuteJob_WithDatabricksClientWithFinishedJobs_ReportsJobsAsMetric()
        {
            // Arrange
            DateTimeOffset startWindow = BogusGenerator.Date.RecentOffset();
            DateTimeOffset endWindow = BogusGenerator.Date.SoonOffset();

            IEnumerable<Run> runs = CreateRandomRuns(startWindow, endWindow);
            int jobCount = BogusGenerator.Random.Int(1, 10);
            IEnumerable<Job> jobs = BogusGenerator.Make(jobCount, () => new Job
            {
                Settings = new JobSettings { Name = Guid.NewGuid().ToString() }
            });

            var options = new StubDatabricksJobMetricJobSchedulerOptions(runs, jobs, startWindow, endWindow);
            var optionsStub = new Mock<IOptionsMonitor<DatabricksJobMetricsJobSchedulerOptions>>();
            optionsStub.Setup(s => s.Get(nameof(DatabricksJobMetricsJob)))
                       .Returns(options);

            var spyLogger = new InMemoryLogger<DatabricksJobMetricsJob>();
            var schedulerJob = new DatabricksJobMetricsJob(
                optionsStub.Object,
                Mock.Of<ISecretProvider>(),
                spyLogger);

            // Act
            await schedulerJob.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.NotEmpty(spyLogger.Messages);
            Assert.Equal(
                runs.Count(),
                spyLogger.Messages.Count(m => m.StartsWith("Metric Databricks Job Completed")));
        }

        private static IEnumerable<Run> CreateRandomRuns(DateTimeOffset startWindow, DateTimeOffset endWindow)
        {
            int range = BogusGenerator.Random.Int(5, 10);
            IEnumerable<Run> runs = Enumerable.Range(1, range).Select(i =>
            {
                DateTimeOffset startTime = BogusGenerator.Date.BetweenOffset(startWindow, endWindow);
                return new Run
                {
                    RunId = BogusGenerator.Random.Long(),
                    ExecutionDuration = 0,
                    SetupDuration = 0,
                    CleanupDuration = 0,
                    StartTime = startTime,
                    State = new RunState { ResultState = RunResultState.SUCCESS }
                };
            });

            return runs.ToArray();
        }
    }
}
