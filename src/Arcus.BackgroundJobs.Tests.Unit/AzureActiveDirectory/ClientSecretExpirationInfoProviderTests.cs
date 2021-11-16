using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureActiveDirectory;
using Azure.Core;
using Azure.Identity;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Moq;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AzureActiveDirectory
{
    [Trait(name: "Category", value: "Unit")]
    public class ClientSecretExpirationInfoProviderTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void CreateClientSecretExpirationInfoProvider_WithNullGraphServiceClient_Throws()
        {
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => new ClientSecretExpirationInfoProvider(null, NullLogger.Instance));
        }

        [Fact]
        public void CreateClientSecretExpirationInfoProvider_WithNullLogger_Throws()
        {
            // Arrange
            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", BogusGenerator.Random.ToString());

                    return Task.CompletedTask;
                })
            );

            // Assert
            Assert.ThrowsAny<ArgumentException>(() => new ClientSecretExpirationInfoProvider(client, null));
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithExpirationThresholdLessThanZero_Throws()
        {
            // Arrange
            int expirationThreshold = -1;
            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", BogusGenerator.Random.ToString());

                    return Task.CompletedTask;
                })
            );
            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act / Assert
            await Assert.ThrowsAnyAsync<ArgumentException>(() => provider.GetApplicationsWithPotentialExpiredSecrets(expirationThreshold));
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithExpiredSecret_ReturnsSecret()
        {
            // Arrange
            DateTimeOffset expirationDate = BogusGenerator.Date.RecentOffset(days: 5, DateTimeOffset.UtcNow);
            var credentials = new PasswordCredential
            {
                EndDateTime = expirationDate,
                KeyId = BogusGenerator.Random.Guid(),
            };
            var application = new Application
            {
                DisplayName = BogusGenerator.Commerce.ProductName(),
                PasswordCredentials = new [] { credentials }
            };
            
            GraphServiceClient client = CreateStubbedGraphClient(new [] { application });
            int expirationThresholdInDays = 1;
            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<AzureApplication> actualApplications = await provider.GetApplicationsWithPotentialExpiredSecrets(expirationThresholdInDays);

            // Assert
            Assert.NotEmpty(actualApplications);
            AzureApplication actualApplication = Assert.Single(actualApplications);
            Assert.Equal(application.DisplayName, actualApplication.Name);
            Assert.Equal(credentials.KeyId, actualApplication.KeyId);
            Assert.Equal(expirationDate, actualApplication.EndDateTime);
            Assert.True(actualApplication.RemainingValidDays < 0);
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithExpiredSecrets_ReturnsAll()
        {
            // Arrange
            DateTimeOffset expirationDate = BogusGenerator.Date.RecentOffset(days: 5);

            IEnumerable<PasswordCredential> credentials = CreateStubbedCredentials(expirationDate);
            Application[] applications = GetStubbedApplications(credentials);
            GraphServiceClient client = CreateStubbedGraphClient(applications);
            int expirationThresholdInDays = BogusGenerator.Random.Int(min: 1);

            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<AzureApplication> actualApplications = await provider.GetApplicationsWithPotentialExpiredSecrets(expirationThresholdInDays);

            // Assert
            Assert.NotEmpty(actualApplications);
            Assert.Equal(applications.SelectMany(app => app.PasswordCredentials).Count(), actualApplications.Count());
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithSoonExpiredSecrets_ReturnsAll()
        {
            // Arrange
            DateTimeOffset expirationDate = BogusGenerator.Date.RecentOffset(days: 1);

            IEnumerable<PasswordCredential> credentials = CreateStubbedCredentials(expirationDate);
            Application[] applications = GetStubbedApplications(credentials);
            GraphServiceClient client = CreateStubbedGraphClient(applications);
            int expirationThresholdInDays = 1;

            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<AzureApplication> actualApplications = await provider.GetApplicationsWithPotentialExpiredSecrets(expirationThresholdInDays);

            // Assert
            Assert.NotEmpty(actualApplications);
            Assert.Equal(applications.SelectMany(app => app.PasswordCredentials).Count(), actualApplications.Count());
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithStillValidSecrets_ReturnsEmpty()
        {
            // Arrange
            DateTimeOffset expirationDate = BogusGenerator.Date.SoonOffset(days: 5);
            
            IEnumerable<PasswordCredential> credentials = CreateStubbedCredentials(expirationDate);
            IEnumerable<Application> applications = GetStubbedApplications(credentials);
            GraphServiceClient client = CreateStubbedGraphClient(applications);
            int expirationThresholdInDays = BogusGenerator.Random.Int(min: 1, max: 2);

            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<AzureApplication> actualApplications = await provider.GetApplicationsWithPotentialExpiredSecrets(expirationThresholdInDays);

            // Assert
            Assert.Empty(actualApplications);
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithSecretsWithNoExpirationCredentials_ReturnsEmpty()
        {
            // Arrange
            IEnumerable<PasswordCredential> credentials = CreateStubbedCredentials(expirationDate: null);
            IEnumerable<Application> applications = GetStubbedApplications(credentials);
            GraphServiceClient client = CreateStubbedGraphClient(applications);
            int expirationThresholdInDays = BogusGenerator.Random.Int(min: 1);

            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<AzureApplication> actualApplications = await provider.GetApplicationsWithPotentialExpiredSecrets(expirationThresholdInDays);

            // Assert
            Assert.Empty(actualApplications);
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithSecretsWithNoCredentials_ReturnsEmpty()
        {
            // Arrange
            IEnumerable<Application> applications = GetStubbedApplications(credentials: null);
            GraphServiceClient client = CreateStubbedGraphClient(applications);
            int expirationThresholdInDays = BogusGenerator.Random.Int(min: 1);
            
            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);
            
            // Act
            IEnumerable<AzureApplication> actualApplications = await provider.GetApplicationsWithPotentialExpiredSecrets(expirationThresholdInDays);
            
            // Assert
            Assert.Empty(actualApplications);
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithSecretsWithEmptyCredentials_ReturnsEmpty()
        {
            // Arrange
            IEnumerable<Application> applications = GetStubbedApplications(Array.Empty<PasswordCredential>());
            GraphServiceClient client = CreateStubbedGraphClient(applications);
            int expirationThresholdInDays = BogusGenerator.Random.Int(min: 1);

            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<AzureApplication> actualApplications = await provider.GetApplicationsWithPotentialExpiredSecrets(expirationThresholdInDays);

            // Assert
            Assert.Empty(actualApplications);
        }

        [Fact]
        public async Task GetApplicationsWithPotentialExpiredSecrets_WithNoApplications_ReturnsEmpty()
        {
            // Arrange
            GraphServiceClient client = CreateStubbedGraphClient(Array.Empty<Application>());
            int expirationThresholdInDays = BogusGenerator.Random.Int(min: 1);

            var provider = new ClientSecretExpirationInfoProvider(client, NullLogger.Instance);

            // Act
            IEnumerable<AzureApplication> actualApplications = await provider.GetApplicationsWithPotentialExpiredSecrets(expirationThresholdInDays);

            // Assert
            Assert.Empty(actualApplications);
        }

        private static PasswordCredential[] CreateStubbedCredentials(DateTimeOffset? expirationDate)
        {
            int count = BogusGenerator.Random.Int(1, 10);
            IEnumerable<PasswordCredential> credentials = Enumerable.Repeat(new PasswordCredential
            {
                EndDateTime = expirationDate
            }, count);

            return credentials.ToArray();
        }

        private static Application[] GetStubbedApplications(IEnumerable<PasswordCredential> credentials)
        {
            int count = BogusGenerator.Random.Int(1, 10);
            IEnumerable<Application> applications = Enumerable.Repeat(new Application
            {
                DisplayName = BogusGenerator.Commerce.ProductName(),
                PasswordCredentials = credentials
            }, count);

            return applications.ToArray();
        }

        private static GraphServiceClient CreateStubbedGraphClient(IEnumerable<Application> applications)
        {
            var clientStub = new Mock<GraphServiceClient>(new DefaultAzureCredential(), null, null);

            var pageStub = new Mock<IGraphServiceApplicationsCollectionPage>();
            pageStub.Setup(page => page.GetEnumerator())
                    .Returns(() => applications.GetEnumerator());

            var responseStub = new Mock<IGraphServiceApplicationsCollectionRequest>();
            responseStub.Setup(response => response.GetAsync(default))
                        .ReturnsAsync(pageStub.Object);
            
            var requestStub = new Mock<IGraphServiceApplicationsCollectionRequestBuilder>();
            requestStub.Setup(request => request.Request())
                       .Returns(responseStub.Object);

            clientStub.Setup(client => client.Applications)
                      .Returns(() => requestStub.Object);
            
            return clientStub.Object;
        }
    }
}
