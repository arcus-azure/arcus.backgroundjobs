using Arcus.BackgroundJobs.AzureActiveDirectory;
using System;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AzureActiveDirectory
{
    [Trait(name: "Category", value: "Unit")]
    public class ClientSecretExpirationJobOptionsTests
    {
        [Fact]
        public void CreateClientSecretExpirationJobOptions_WithValidArguments_Succeeds()
        {
            // Arrange
            Uri eventUri = new Uri("https://github.com/arcus-azure/arcus.backgroundjobs");
            int expirationThreshold = 1;
            int runAtHour = 0;
            bool runImmediately = false;

            // Act
            var options = new ClientSecretExpirationJobOptions()
            {
                EventUri = eventUri,
                ExpirationThreshold = expirationThreshold,
                RunAtHour = runAtHour,
                RunImmediately = runImmediately
            };

            // Assert
            Assert.Equal(eventUri, options.EventUri);
            Assert.Equal(expirationThreshold, options.ExpirationThreshold);
            Assert.Equal(runAtHour, options.RunAtHour);
            Assert.Equal(runImmediately, options.RunImmediately);
        }

        [Fact]
        public void CreateClientSecretExpirationJobOptions_WithInvalidExpirationThreshold_Throws()
        {
            // Assert
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => new ClientSecretExpirationJobOptions()
            {
                ExpirationThreshold = -1
            });
        }

        [Fact]
        public void CreateClientSecretExpirationJobOptions_WithInvalidRunAtHour_Throws()
        {
            // Assert
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => new ClientSecretExpirationJobOptions()
            {
                RunAtHour = 100
            });
        }
    }
}
