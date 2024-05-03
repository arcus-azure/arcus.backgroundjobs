using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Azure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.KeyVault
{
    [Trait("Category", "Unit")]
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddAutoValidateManagedIdentity_WithSecretStore_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", "<sub-prefix>", "<namespace>");

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<MessageHandler>());
        }

        [Fact]
        public void AddAutoValidateManagedIdentity_WithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", "<sub-prefix>", "<namespace>");

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.ThrowsAny<InvalidOperationException>(() => provider.GetService<MessageHandler>());
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithoutClientIdAndOptions_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(topicName, "<sub-prefix>", "<namespace>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithoutClientIdAndOptions_WithoutSubscriptionPrefix_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", subscriptionPrefix, "<namespace>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithoutClientIdAndOptions_WithoutNamespace_Fails(string serviceBusNamespace)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", "<sub-prefix>", serviceBusNamespace));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithClientIdWithoutOptions_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(topicName, "<sub-prefix>", "<namespace>", "<client-id>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithClientIdWithoutOptions_WithoutSubscriptionPrefix_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", subscriptionPrefix, "<namespace>", "<client-id>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithoutClientIdWithoutOptions_WithoutNamespace_Fails(string serviceBusNamespace)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", "<sub-prefix>", serviceBusNamespace, "<client-id>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithoutClientIdWithOptions_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(topicName, "<sub-prefix>", "<namespace>", options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithoutClientIdWithOptions_WithoutSubscriptionPrefix_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", subscriptionPrefix, "<namespace>", options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithoutClientIdWithOptions_WithoutNamespace_Fails(string serviceBusNamespace)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", "<sub-prefix>", serviceBusNamespace, options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithClientIdAndOptions_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob(topicName, "<sub-prefix>", "<namespace>", "<client-id>", options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithClientIdAndOptions_WithoutSubscriptionPrefix_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", subscriptionPrefix, "<namespace>", "<client-id>", options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoValidateManagedIdentityWithClientIdAndOptions_WithoutNamespace_Fails(string serviceBusNamespace)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretUsingManagedIdentityBackgroundJob("<topic-name>", "<sub-prefix>", serviceBusNamespace, "<client-id>", options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoInvalidateKeyVaultSecretBackgroundJob_WithoutTopisSubscriptionPrefix_Fails(string subscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
                    subscriptionPrefix,
                    "<service-bus-topic-connection-string-secret-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoInvalidateKeyVaultSecretBackgroundJob_WithoutServiceBusTopicConnectionStringSecretKey_Fails(
            string serviceBusTopicConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
                    "<topic-subscription-prefix>",
                    serviceBusTopicConnectionStringSecretKey));
        }
    }
}
