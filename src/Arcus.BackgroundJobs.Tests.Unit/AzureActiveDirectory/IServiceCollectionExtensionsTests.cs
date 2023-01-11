using System;
using Arcus.EventGrid.Publishing;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AzureActiveDirectory
{
    // ReSharper disable once InconsistentNaming
    [Trait(name: "Category", value: "Unit")]
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddClientSecretExpirationJob_WithoutOptionsWithArcusEventGridPublisher_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddSingleton(
                EventGridPublisherBuilder
                    .ForTopic("https://some-topic")
                    .UsingAuthenticationKey("<key>")
                    .Build());

            // Act
            services.AddClientSecretExpirationJob();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHostedService>());
        }

        [Fact]
        public void AddClientSecretExpirationJob_WithoutOptionsWithMicrosoftEventGridPublisherClient_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddCorrelation();
            services.AddSecretStore(stores => stores.AddInMemory());
            services.AddAzureClients(clients => clients.AddEventGridPublisherClient("http://topic-uri", "auth-secret-name"));

            // Act
            services.AddClientSecretExpirationJob();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHostedService>());
        }
       
        [Fact]
        public void AddClientSecretExpirationJob_WithoutOptionsWithMicrosoftEventGridPublisherClientWithoutCorrelation_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddSecretStore(stores => stores.AddInMemory());
            services.AddAzureClients(clients => clients.AddEventGridPublisherClient("http://topic-uri", "auth-secret-name"));

            // Act
            services.AddClientSecretExpirationJob();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => provider.GetService<IHostedService>());
        }

        [Fact]
        public void AddClientSecretExpirationJob_WithoutOptionsWithMicrosoftEventGridPublisherClientWithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddCorrelation();
            services.AddAzureClients(clients => clients.AddEventGridPublisherClient("http://topic-uri", "auth-secret-name"));

            // Act
            services.AddClientSecretExpirationJob();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => provider.GetService<IHostedService>());
        }

        [Fact]
        public void AddClientSecretExpirationJob_WithoutEventGridPublisher_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            // Act
            services.AddClientSecretExpirationJob();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => provider.GetService<IHostedService>());
        }
    }
}
