using System;
using System.Net;
using Arcus.BackgroundJobs.Databricks;
using Microsoft.Azure.Databricks.Client;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.Databricks
{
    [Trait(name: "Category", value: "Unit")]
    public class JobRunTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void CreateJobRun_WithBlankJobName_Throws(string jobName)
        {
            Assert.ThrowsAny<ArgumentException>(() => new JobRun(jobName, new Run()));
        }

        [Fact]
        public void CreateJobRun_WithoutRun_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => new JobRun("some job name", run: null));
        }

        [Fact]
        public void CreateJobRun_WithValidArguments_Succeeds()
        {
            // Arrange
            var jobName = "some job name";
            var run = new Run();
        
            // Act
            var jobRun = new JobRun(jobName, run);

            // Assert
            Assert.Equal(jobName, jobRun.JobName);
            Assert.Same(run, jobRun.Run);
        }
    }
}
