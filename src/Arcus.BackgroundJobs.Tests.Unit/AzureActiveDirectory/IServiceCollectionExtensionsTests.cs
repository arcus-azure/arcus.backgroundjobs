using System;
using Arcus.EventGrid.Publishing;
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
        public void AddClientSecretExpirationJob_WithoutOptions_Succeeds()
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
    }
}
