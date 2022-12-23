using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Identity;
using Azure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
#pragma warning disable CS0618

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
            Assert.NotNull(provider.GetService<IMessageHandler<CloudEvent, AzureServiceBusMessageContext>>());
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
            Assert.ThrowsAny<InvalidOperationException>(() => provider.GetService<IMessageHandler<CloudEvent, AzureServiceBusMessageContext>>());
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

        [Fact]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutReferencingMessagePump_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IConfiguration>());
            services.AddLogging();
            services.AddSingleton<IHostedService>(serviceProvider =>
            {
                var settings = new AzureServiceBusMessagePumpSettings(
                    "<entity-name>",
                    "<subscription-name>",
                    ServiceBusEntityType.Queue,
                    "<service-bus-namespace>",
                    new DefaultAzureCredential(),
                    new AzureServiceBusMessagePumpOptions {JobId = "No-the-same-job-id"},
                    serviceProvider);
                
                return new AzureServiceBusMessagePump(
                    settings,
                    Mock.Of<IConfiguration>(),
                    serviceProvider,
                    Mock.Of<IAzureServiceBusMessageRouter>(),
                    NullLogger<AzureServiceBusMessagePump>.Instance);
            });
            
            // Act
            services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                "<non-existing-job-id>",
                "<topic-subscription-prefix>",
                "<service-bus-topic-connection-string-secret-key>",
                "<message-pump-connection-string-key>");
            
            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.ThrowsAny<InvalidOperationException>(
                () => provider.GetRequiredService<IMessageHandler<CloudEvent, AzureServiceBusMessageContext>>());
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutJobId_Fails(string jobId)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    jobId,
                    "<topic-subscription-prefix>",
                    "<service-bus-connection-string-secret-key>",
                    "<message-pump-connection-string-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutTopicSubscriptionPrefix_Fails(string topicSubscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    topicSubscriptionPrefix,
                    "<service-bus-connection-string-secret-key>",
                    "<message-pump-connection-string-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutServiceBusConnectionStringSecretKey_Fails(
            string serviceBusConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    "<topic-subscription-prefix>",
                    serviceBusConnectionStringSecretKey,
                    "<message-pump-connection-string-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob_WithoutMessagePumpConnectionStringKey_Fails(
            string messagePumpConnectionStringKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    "<topic-subscription-prefix>",
                    "<service-bus-connection-string-secret-key>",
                    messagePumpConnectionStringKey));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJobOptions_WithoutJobId_Fails(string jobId)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    jobId,
                    "<topic-subscription-prefix>",
                    "<service-bus-connection-string-secret-key>",
                    "<message-pump-connection-string-key>",
                    configureBackgroundJob: null));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJobOptions_WithoutTopicSubscriptionPrefix_Fails(string topicSubscriptionPrefix)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    topicSubscriptionPrefix,
                    "<service-bus-connection-string-secret-key>",
                    "<message-pump-connection-string-key>",
                    configureBackgroundJob: null));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJobOptions_WithoutServiceBusConnectionStringSecretKey_Fails(
            string serviceBusConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    "<topic-subscription-prefix>",
                    serviceBusConnectionStringSecretKey,
                    "<message-pump-connection-string-key>",
                    configureBackgroundJob: null));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJobOptions_WithoutMessagePumpConnectionStringKey_Fails(
            string messagePumpConnectionStringKey)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddAutoRestartServiceBusMessagePumpOnRotatedCredentialsBackgroundJob(
                    "<job-id>",
                    "<topic-subscription-prefix>",
                    "<service-bus-connection-string-secret-key>",
                    messagePumpConnectionStringKey,
                    configureBackgroundJob: null));
        }
    }
}
