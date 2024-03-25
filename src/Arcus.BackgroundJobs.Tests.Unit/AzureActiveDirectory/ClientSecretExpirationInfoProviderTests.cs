using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.BackgroundJobs.AzureActiveDirectory;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using Newtonsoft.Json;
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
                PasswordCredentials = new List<PasswordCredential> { credentials }
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
            DateTimeOffset expirationDate = BogusGenerator.Date.SoonOffset(refDate: DateTimeOffset.UtcNow.AddDays(5));
            
            IEnumerable<PasswordCredential> credentials = CreateStubbedCredentials(expirationDate);
            IEnumerable<Application> applications = GetStubbedApplications(credentials);
            GraphServiceClient client = CreateStubbedGraphClient(applications);
            int expirationThresholdInDays = 2;

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
                PasswordCredentials = credentials?.ToList()
            }, count);

            return applications.ToArray();
        }

        private static GraphServiceClient CreateStubbedGraphClient(IEnumerable<Application> applications)
        {
            return new GraphServiceClient(new StubAzureApplicationsRequestAdapter(applications));
        }

        private class StubAzureApplicationsRequestAdapter : IRequestAdapter
        {
            private readonly IEnumerable<Application> _applications;

            /// <summary>
            /// Initializes a new instance of the <see cref="StubAzureApplicationsRequestAdapter" /> class.
            /// </summary>
            public StubAzureApplicationsRequestAdapter(IEnumerable<Application> applications)
            {
                _applications = applications;
            }

            public ISerializationWriterFactory SerializationWriterFactory { get; }
            public string BaseUrl { get; set; }

            public void EnableBackingStore(IBackingStoreFactory backingStoreFactory)
            {
            }

            public Task<ModelType> SendAsync<ModelType>(
                RequestInformation requestInfo,
                ParsableFactory<ModelType> factory,
                Dictionary<string, ParsableFactory<IParsable>> errorMapping = null,
                CancellationToken cancellationToken = new CancellationToken()) where ModelType : IParsable
            {
                var response = new ApplicationCollectionResponse
                {
                    Value = _applications.ToList()
                };

                return Task.FromResult((ModelType) Convert.ChangeType(response, typeof(ModelType)));
            }

            public Task<IEnumerable<ModelType>> SendCollectionAsync<ModelType>(
                RequestInformation requestInfo,
                ParsableFactory<ModelType> factory,
                Dictionary<string, ParsableFactory<IParsable>> errorMapping = null,
                CancellationToken cancellationToken = new CancellationToken()) where ModelType : IParsable
            {
                throw new NotImplementedException();
            }

            public Task<ModelType> SendPrimitiveAsync<ModelType>(
                RequestInformation requestInfo,
                Dictionary<string, ParsableFactory<IParsable>> errorMapping = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<ModelType>> SendPrimitiveCollectionAsync<ModelType>(
                RequestInformation requestInfo,
                Dictionary<string, ParsableFactory<IParsable>> errorMapping = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task SendNoContentAsync(
                RequestInformation requestInfo,
                Dictionary<string, ParsableFactory<IParsable>> errorMapping = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<T> ConvertToNativeRequestAsync<T>(
                RequestInformation requestInfo,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }
        }

        private class StubAzureApplicationsHttpMessageHandler : HttpMessageHandler
        {
            private readonly IEnumerable<Application> _applications;

            public StubAzureApplicationsHttpMessageHandler(IEnumerable<Application> applications)
            {
                _applications = applications;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var collection = new ApplicationCollectionResponse
                {
                    Value = _applications.ToList()
                };

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(collection), Encoding.UTF8, MediaTypeNames.Application.Json)
                };

                return Task.FromResult(response);
            }
        }
    }
}
