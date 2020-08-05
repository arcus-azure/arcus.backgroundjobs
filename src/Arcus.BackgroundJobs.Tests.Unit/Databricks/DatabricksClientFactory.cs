using System.Collections.Generic;
using System.Threading;
using GuardNet;
using Microsoft.Azure.Databricks.Client;
using Moq;

namespace Arcus.BackgroundJobs.Tests.Unit.Databricks
{
    /// <summary>
    /// Factory to create <see cref="DatabricksClient"/> instances.
    /// </summary>
    public static class DatabricksClientFactory
    {
        /// <summary>
        /// Creates a <see cref="DatabricksClient"/> instance with a <see cref="IJobsApi"/> implementation that returns the given <paramref name="runs"/> and <paramref name="jobs"/>.
        /// </summary>
        /// <param name="runs">The stubbed runs the <see cref="IJobsApi.RunsList"/> should return.</param>
        /// <param name="jobs">The stubbed jobs that the <see cref="IJobsApi.List"/> should return.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="runs"/> or <paramref name="jobs"/> is <c>null</c>.</exception>
        public static DatabricksClient Create(IEnumerable<Run> runs, IEnumerable<Job> jobs)
        {
            Guard.NotNull(runs, nameof(runs));
            Guard.NotNull(jobs, nameof(jobs));

            var jobsStub = new Mock<IJobsApi>();
            jobsStub.Setup(j => j.RunsList(null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new RunList { Runs = runs });

            jobsStub.Setup(j => j.List(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jobs);

            var client = DatabricksClient.CreateClient(
                clusterApi: Mock.Of<IClustersApi>(),
                jobsApi: jobsStub.Object,
                dbfsApi: Mock.Of<IDbfsApi>(),
                secretsApi: Mock.Of<ISecretsApi>(),
                groupsApi: Mock.Of<IGroupsApi>(),
                librariesApi: Mock.Of<ILibrariesApi>(),
                tokenApi: Mock.Of<ITokenApi>(),
                workspaceApi: Mock.Of<IWorkspaceApi>(),
                instancePoolApi: Mock.Of<IInstancePoolApi>());

            return client;
        }
    }
}
