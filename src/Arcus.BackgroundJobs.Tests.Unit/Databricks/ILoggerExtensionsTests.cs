using System;
using System.Collections.Generic;
using System.Text;
using Arcus.BackgroundJobs.Databricks;
using Arcus.BackgroundJobs.Databricks.Extensions;
using Bogus;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.Databricks
{
    [Trait(name: "Category", value: "Unit")]
    public class ILoggerExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void LogMetricFinishedJobOutcome_WithBlankMetricName_Throws(string metricName)
        {
            // Arrange
            var spyLogger = new SpyLogger<DatabricksJobMetricsJob>();
            var jobRun = new JobRun(BogusGenerator.Random.Word(), new Run());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => spyLogger.LogMetricFinishedJobOutcome(metricName: metricName, jobRun: jobRun));
        }

        [Fact]
        public void LogMetricFinishedJobOutcome_WithoutJobRun_Throws()
        {
            // Arrange
            var spyLogger = new SpyLogger<DatabricksJobMetricsJob>();
            string metricName = BogusGenerator.Random.Word();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => spyLogger.LogMetricFinishedJobOutcome(metricName, jobRun: null));
        }

        [Fact]
        public void LogMetricFinishedJobOutcome_WithoutRunOutcome_LogsWarning()
        {
            // Arrange
            var spyLogger = new SpyLogger<DatabricksJobMetricsJob>();
            string metricName = BogusGenerator.Random.Word();
            var run = new Run { State = new RunState { ResultState = null } };
            var jobRun = new JobRun(BogusGenerator.Random.Word(), run);

            // Act
            spyLogger.LogMetricFinishedJobOutcome(metricName, jobRun);

            // Assert
            Assert.Single(spyLogger.Entries, entry => entry.Level == LogLevel.Warning);
            Assert.DoesNotContain(spyLogger.Messages, msg => msg.StartsWith("Metric" + metricName));
        }

        [Fact]
        public void LogMetricFinishedJobOutcome_WithRunOutcome_LogsRunOutcome()
        {
            // Arrange
            var spyLogger = new SpyLogger<DatabricksJobMetricsJob>();
            string metricName = BogusGenerator.Random.Word();
            var resultState = BogusGenerator.PickRandom<RunResultState>();
            var run = new Run { State = new RunState { ResultState = resultState } };
            var jobRun = new JobRun(BogusGenerator.Random.Word(), run);

            // Act
            spyLogger.LogMetricFinishedJobOutcome(metricName, jobRun);

            // Assert
            Assert.All(spyLogger.Entries, entry => Assert.Equal(LogLevel.Information, entry.Level));
            Assert.Single(spyLogger.Messages, msg => msg.Contains(resultState.ToString(), StringComparison.OrdinalIgnoreCase));
            Assert.Single(spyLogger.Messages, msg => msg.StartsWith("Metric " + metricName));
        }

        [Fact]
        public void LogMetricFinishedJobOutcome_WithDuplicateContextKey_DiscardsContextItem()
        {
            // Arrange
            var spyLogger = new SpyLogger<DatabricksJobMetricsJob>();
            string metricName = BogusGenerator.Random.Word();
            var resultState = BogusGenerator.PickRandom<RunResultState>();
            var run = new Run { State = new RunState { ResultState = resultState } };
            var jobRun = new JobRun(BogusGenerator.Random.Word(), run);
            string discardedValue = $"discarded-{Guid.NewGuid()}";
            string includedValue = $"included-{Guid.NewGuid()}";
            var context = new Dictionary<string, object>
            {
                ["Outcome"] = discardedValue,
                ["NewKey"] = includedValue
            };

            // Act
            spyLogger.LogMetricFinishedJobOutcome(metricName, jobRun, context);

            // Assert
            Assert.DoesNotContain(spyLogger.Messages, msg => msg.Contains(discardedValue));
            Assert.Contains(spyLogger.Messages, msg => msg.Contains(includedValue));
        }
    }
}
