using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.Databricks;
using Bogus;
using Microsoft.Azure.Databricks.Client;
using Moq;
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
            DatabricksClient client = CreateDatabricksClient(Enumerable.Empty<Run>(), Enumerable.Empty<Job>());
            var provider = new DatabricksInfoProvider(client);

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
            IEnumerable<Run> tooFarRuns = CreateRandomRuns(endWindow, BogusGenerator.Date.FutureOffset());
            IEnumerable<Job> jobs = includedRuns.Select(r => new Job
            {
                JobId = r.JobId,
                Settings = new JobSettings { Name = Guid.NewGuid().ToString() }
            }).ToArray();

            DatabricksClient client = CreateDatabricksClient(includedRuns.Concat(tooFarRuns), jobs);
            var provider = new DatabricksInfoProvider(client);

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

            DatabricksClient client = CreateDatabricksClient(Enumerable.Empty<Run>(), Enumerable.Empty<Job>());
            var provider = new DatabricksInfoProvider(client);

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
            IEnumerable<Run> tooFarRuns = CreateRandomRuns(endWindow, BogusGenerator.Date.FutureOffset());
            IEnumerable<Job> jobs = includedRuns.Select(r => new Job
            {
                JobId = r.JobId, 
                Settings = new JobSettings { Name = Guid.NewGuid().ToString() }
            }).ToArray();

            DatabricksClient client = CreateDatabricksClient(includedRuns.Concat(tooFarRuns), jobs);
            var provider = new DatabricksInfoProvider(client);
            
            // Act
            IEnumerable<JobRun> finishedJobs = await provider.GetFinishedJobRunsAsync(startWindow, endWindow);

            // Assert
            Assert.NotNull(finishedJobs);
            Assert.NotEmpty(finishedJobs);
            Assert.Equal(finishedJobs.Count(), includedRuns.Count());
            Assert.All(finishedJobs, job => 
            {
                Assert.Contains(includedRuns, run => run.RunId == job.Run.RunId);
                Assert.DoesNotContain(tooFarRuns, run => run.RunId == job.Run.RunId);
                Job expectedJob = Assert.Single(jobs, j => j.Settings.Name == job.JobName);
                Assert.NotNull(expectedJob);
                Assert.Equal(expectedJob.JobId, job.Run.JobId);
            });
        }

        private static DatabricksClient CreateDatabricksClient(IEnumerable<Run> runs, IEnumerable<Job> jobs)
        {
            var jobsStub = new Mock<IJobsApi>();
            jobsStub.Setup(j => j.RunsList(null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new RunList { Runs = runs });

            jobsStub.Setup(j => j.List(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jobs);

            var client = DatabricksClient.CreateClient(
                clusterApi: null,
                jobsApi: jobsStub.Object,
                dbfsApi: null,
                secretsApi: null,
                groupsApi: null,
                librariesApi: null,
                tokenApi: null,
                workspaceApi: null,
                instancePoolApi: null);

            return client;
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
