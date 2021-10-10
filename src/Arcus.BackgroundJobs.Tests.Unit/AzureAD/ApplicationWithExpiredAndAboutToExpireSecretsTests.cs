using System;
using Arcus.BackgroundJobs.AzureAD.ClientSecretExpiration;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AzureAD.ClientSecretExpiration
{
    [Trait(name: "Category", value: "Unit")]
    public class ApplicationWithExpiredAndAboutToExpireSecretsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateApplicationWithExpiredAndAboutToExpireSecrets_WithBlankName_Throws(string name)
        {
            // Arrange
            Guid keyId = Guid.NewGuid();
            DateTime endDateTime = DateTime.UtcNow;
            double remainingValidDays = 100;

            // Assert
            Assert.ThrowsAny<ArgumentException>(() => new ApplicationWithExpiredAndAboutToExpireSecrets(name, keyId, endDateTime, remainingValidDays));
        }

        [Fact]
        public void CreateApplicationWithExpiredAndAboutToExpireSecrets_WithValidArguments_Succeeds()
        {
            // Arrange
            string name = "some name";
            Guid keyId = Guid.NewGuid();
            DateTime endDateTime = DateTime.UtcNow;
            double remainingValidDays = 100;

            // Act
            var applicationWithExpiredAndAboutToExpireSecrets = new ApplicationWithExpiredAndAboutToExpireSecrets(name, keyId, endDateTime, remainingValidDays);

            // Assert
            Assert.Equal(name, applicationWithExpiredAndAboutToExpireSecrets.Name);
            Assert.Equal(keyId, applicationWithExpiredAndAboutToExpireSecrets.KeyId);
            Assert.Equal(endDateTime, applicationWithExpiredAndAboutToExpireSecrets.EndDateTime);
            Assert.Equal(remainingValidDays, applicationWithExpiredAndAboutToExpireSecrets.RemainingValidDays);
        }
    }
}
