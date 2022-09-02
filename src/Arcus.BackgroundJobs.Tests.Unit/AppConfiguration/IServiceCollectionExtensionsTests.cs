using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.BackgroundJobs.Tests.Unit.AppConfiguration
{
    [Trait("Category", "Unit")]
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithoutOptions_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    topicName,
                    "<subscription-name-prefix>",
                    "<service-bus-namespace>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithoutOptions_WithoutSubscriptionPrefix_Fails(string subscriptionNamePrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    "<topic-name>",
                    subscriptionNamePrefix,
                    "<service-bus-namespace>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithoutOptions_WithoutServiceBusNamespace_Fails(string serviceBusNamespace)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    "<topic-name>",
                    "<subscription-name-prefix>",
                    serviceBusNamespace));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithOptions_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    topicName,
                    "<subscription-name-prefix>",
                    "<service-bus-namespace>",
                    options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithOptions_WithoutSubscriptionPrefix_Fails(string subscriptionNamePrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    "<topic-name>",
                    subscriptionNamePrefix,
                    "<service-bus-namespace>",
                    options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithOptions_WithoutServiceBusNamespace_Fails(string serviceBusNamespace)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    "<topic-name>",
                    "<subscription-name-prefix>",
                    serviceBusNamespace,
                    options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithClientIdWithoutOptions_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    topicName,
                    "<subscription-name-prefix>",
                    "<service-bus-namespace>",
                    "<client-id>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithClientIdWithOptionsOptions_WithoutSubscriptionPrefix_Fails(string subscriptionNamePrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    "<topic-name>",
                    subscriptionNamePrefix,
                    "<service-bus-namespace>",
                    "<client-id>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithClientIdWithoutOptions_WithoutServiceBusNamespace_Fails(string serviceBusNamespace)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    "<topic-name>",
                    "<subscription-name-prefix>",
                    serviceBusNamespace,
                    "<client-id>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithClientIdWithOptions_WithoutTopicName_Fails(string topicName)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    topicName,
                    "<subscription-name-prefix>",
                    "<service-bus-namespace>",
                    "<client-id>",
                    options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithClientIdWithOptions_WithoutSubscriptionPrefix_Fails(string subscriptionNamePrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    "<topic-name>",
                    subscriptionNamePrefix,
                    "<service-bus-namespace>",
                    "<client-id>",
                    options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobUsingManagedIdentityWithClientIdWithOptions_WithoutServiceBusNamespace_Fails(string serviceBusNamespace)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJobUsingManagedIdentity(
                    "<topic-name>",
                    "<subscription-name-prefix>",
                    serviceBusNamespace,
                    "<client-id>",
                    options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobWithoutOptions_WithoutSubscriptionPrefix_Fails(string subscriptionNamePrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJob(
                    subscriptionNamePrefix,
                    "<service-bus-topic-connection-string-secret-key>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobWithoutOptions_WithoutServiceBusTopicConnectionStringSecretKey_Fails(string serviceBusTopicConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJob(
                    "<subscription-name-prefix>",
                    serviceBusTopicConnectionStringSecretKey));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobWithOptions_WithoutSubscriptionPrefix_Fails(string subscriptionNamePrefix)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJob(
                    subscriptionNamePrefix,
                    "<service-bus-topic-connection-string-secret-key>",
                    options => { }));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJobWithOptions_WithoutServiceBusTopicConnectionStringSecretKey_Fails(string serviceBusTopicConnectionStringSecretKey)
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAutoRefreshAppConfigurationBackgroundJob(
                    "<subscription-name-prefix>",
                    serviceBusTopicConnectionStringSecretKey,
                    options => { }));
        }
    }
}
