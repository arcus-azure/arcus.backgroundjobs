using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Databricks;
using Bogus;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.Databricks
{
    [Trait(name: "Category", value: "Unit")]
    public class DatabricksInfoProviderTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public async Task GetFinishedJobRuns_WithEndTimeLessThanStartTime_Throws()
        {
            // Arrange
            DateTimeOffset startWindow = BogusGenerator.Date.SoonOffset();
            DateTimeOffset endWindow = BogusGenerator.Date.RecentOffset();
            DatabricksClient client = DatabricksClientFactory.Create(Enumerable.Empty<Run>(), Enumerable.Empty<Job>());
            var provider = new DatabricksInfoProvider(client, NullLogger.Instance);

            // Act / Assert
            await Assert.ThrowsAnyAsync<ArgumentException>(() => provider.GetFinishedJobRunsAsync(startWindow, endWindow));
        }

        [Fact]
        public async Task GetFinishedJobRuns_OutsideTimeWindow_ReturnsNoFinishedJobs()
        {
            // Arrange
            DateTimeOffset startWindow = BogusGenerator.Date.RecentOffset();
            DateTimeOffset endWindow = BogusGenerator.Date.SoonOffset();

            IEnumerable<Run> includedRuns = CreateRandomRuns(startWindow, endWindow);
            IEnumerable<Run> tooLateRuns = CreateRandomRuns(endWindow, BogusGenerator.Date.FutureOffset());
            IEnumerable<Run> allRuns = includedRuns.Concat(tooLateRuns);
            IEnumerable<Job> jobs = includedRuns.Select(r => new Job
            {
                JobId = r.JobId,
                Settings = new JobSettings { Name = Guid.NewGuid().ToString() }
            }).ToArray();

            DatabricksClient client = DatabricksClientFactory.Create(allRuns, jobs);
            var provider = new DatabricksInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<JobRun> finishedJobs = await provider.GetFinishedJobRunsAsync(BogusGenerator.Date.PastOffset(), startWindow);

            // Assert
            Assert.Empty(finishedJobs);
        }

        [Fact]
        public async Task GetFinishedJobRuns_WithNoAvailableFinishedJobs_ReturnsNoFinishedJobs()
        {
            // Arrange
            DateTimeOffset startWindow = BogusGenerator.Date.RecentOffset();
            DateTimeOffset endWindow = BogusGenerator.Date.SoonOffset();

            DatabricksClient client = DatabricksClientFactory.Create(Enumerable.Empty<Run>(), Enumerable.Empty<Job>());
            var provider = new DatabricksInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<JobRun> finishedJobs = await provider.GetFinishedJobRunsAsync(startWindow, endWindow);

            // Assert
            Assert.Empty(finishedJobs);
        }

        [Fact]
        public async Task GetFinishedJobRuns_WithinTimeWindow_OnlyReturnsFinishedJobsWithinTheTimeWindow()
        {
            // Arrange
            DateTimeOffset startWindow = BogusGenerator.Date.RecentOffset();
            DateTimeOffset endWindow = BogusGenerator.Date.SoonOffset();

            IEnumerable<Run> includedRuns = CreateRandomRuns(startWindow, endWindow);
            IEnumerable<Run> tooLateRuns = CreateRandomRuns(endWindow, BogusGenerator.Date.FutureOffset());
            IEnumerable<Run> allRuns = includedRuns.Concat(tooLateRuns);
            IEnumerable<Job> jobs = includedRuns.Select(r => new Job
            {
                JobId = r.JobId, 
                Settings = new JobSettings { Name = Guid.NewGuid().ToString() }
            }).ToArray();

            DatabricksClient client = DatabricksClientFactory.Create(allRuns, jobs);
            var provider = new DatabricksInfoProvider(client, NullLogger.Instance);
            
            // Act
            IEnumerable<JobRun> finishedJobs = await provider.GetFinishedJobRunsAsync(startWindow, endWindow);

            // Assert
            Assert.NotNull(finishedJobs);
            Assert.NotEmpty(finishedJobs);
            Assert.Equal(finishedJobs.Count(), includedRuns.Count());
            Assert.All(finishedJobs, job => 
            {
                Assert.Contains(includedRuns, run => run.RunId == job.Run.RunId);
                Assert.DoesNotContain(tooLateRuns, run => run.RunId == job.Run.RunId);
                Job expectedJob = Assert.Single(jobs, j => j.Settings.Name == job.JobName);
                Assert.NotNull(expectedJob);
                Assert.Equal(expectedJob.JobId, job.Run.JobId);
            });
        }

        [Fact]
        public async Task MeasureJobOutcomes_WithNoAvailableFinishedJobs_ReturnsNoFinishedJobs()
        {
            // Arrange
            DateTimeOffset startWindow = BogusGenerator.Date.RecentOffset();
            DateTimeOffset endWindow = BogusGenerator.Date.SoonOffset();
            string metricName = BogusGenerator.Random.Word();

            DatabricksClient client = DatabricksClientFactory.Create(Enumerable.Empty<Run>(), Enumerable.Empty<Job>());
            var spyLogger = new SpyLogger<DatabricksJobMetricsJob>();
            var provider = new DatabricksInfoProvider(client, spyLogger);

            // Act
            await provider.MeasureJobOutcomesAsync(metricName, startWindow, endWindow);

            // Assert
            Assert.DoesNotContain(spyLogger.Messages, msg => msg.StartsWith("Metric " + metricName));
        }

        [Fact]
        public async Task MeasureJobOutcomes_WithinTimeWindow_OnlyReturnsFinishedJobsWithinTheTimeWindow()
        {
            // Arrange
            DateTimeOffset startWindow = BogusGenerator.Date.RecentOffset();
            DateTimeOffset endWindow = BogusGenerator.Date.SoonOffset();
            string metricName = BogusGenerator.Random.Word();

            IEnumerable<Run> includedRuns = CreateRandomRuns(startWindow, endWindow);
            IEnumerable<Run> tooLateRuns = CreateRandomRuns(endWindow, BogusGenerator.Date.FutureOffset());
            IEnumerable<Run> allRuns = includedRuns.Concat(tooLateRuns);
            IEnumerable<Job> jobs = allRuns.Select(r => new Job
            {
                JobId = r.JobId,
                Settings = new JobSettings { Name = Guid.NewGuid().ToString() }
            }).ToArray();

            DatabricksClient client = DatabricksClientFactory.Create(allRuns, jobs);
            var spyLogger = new SpyLogger<DatabricksJobMetricsJob>();
            var provider = new DatabricksInfoProvider(client, spyLogger);

            // Act
            await provider.MeasureJobOutcomesAsync(metricName, startWindow, endWindow);

            // Assert
            Assert.All(includedRuns, run =>
            {
                Assert.Contains(spyLogger.Messages, msg => msg.Contains(run.RunId.ToString()));
                Assert.Contains(spyLogger.Messages, msg => msg.Contains(run.JobId.ToString()));
                Job job = Assert.Single(jobs, j => j.JobId == run.JobId);
                Assert.NotNull(job);
                Assert.Contains(spyLogger.Messages, msg => msg.Contains(job.Settings.Name));
            });
            Assert.All(tooLateRuns, run => 
            {
                Assert.DoesNotContain(spyLogger.Messages, msg => msg.Contains(run.RunId.ToString()));
                Assert.DoesNotContain(spyLogger.Messages, msg => msg.Contains(run.JobId.ToString()));
                Job job = Assert.Single(jobs, j => j.JobId == run.JobId);
                Assert.NotNull(job);
                Assert.DoesNotContain(spyLogger.Messages, msg => msg.Contains(job.Settings.Name));
            });
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
                    JobId = BogusGenerator.Random.Long(),
                    State = new RunState { ResultState = RunResultState.SUCCESS }
                };
            });

            return runs.ToArray();
        }
    }
}
