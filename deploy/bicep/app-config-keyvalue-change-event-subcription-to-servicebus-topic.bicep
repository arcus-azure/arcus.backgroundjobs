@description('Specifies the name of the app configuration store.')
param configStoreName string

@description('Specifies the Azure location where the resources should be created.')
param location string = resourceGroup().location

@description('Name of the Service Bus namespace where the topic resides that receives the App Configuration events.')
param serviceBusNamespaceName string

@description('Name of the Service Bus topic that receives the App Configuration events.')
param serviceBusTopicName string

@description('Name of the Event Grid system topic to register on the App Configuration resource.')
param systemTopicName string = 'system-topic'

@description('Name of the Event Grid system topic event subscription on the App Configuration resource')
param systemTopicEventSubscriptionName string = 'system-topic-event-subscription'

@description('Event delivery schema for the Event Grid system topic event subscription on the App Configuration resource')
@allowed([
  'CloudEventSchemaV1_0'
  'EventGridSchema'
  'CustomInputSchema'
])
param eventDeliverySchema string = 'CloudEventSchemaV1_0'

resource configStore 'Microsoft.AppConfiguration/configurationStores@2021-10-01-preview' = {
  name: configStoreName
  location: location
  sku: {
    name: 'standard'
  }
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: serviceBusNamespaceName
  location: location
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2022-01-01-preview' = {
  name: '${serviceBusNamespace.name}/${serviceBusTopicName}'
}

resource systemTopic 'Microsoft.EventGrid/systemTopics@2022-06-15' = {
  name: '${configStoreName}-${systemTopicName}'
  location: location
  properties: {
    source: configStore.id
    topicType: 'Microsoft.AppConfiguration.ConfigurationStores'
  }
}

resource systemTopicEventSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2021-12-01' = {
  name: '${systemTopic.name}/${systemTopicEventSubscriptionName}'
  properties: {
    destination: {
      properties: {
        resourceId: topic.id
      }
      endpointType: 'ServiceBusTopic'
    }
    filter: {
      includedEventTypes: [
        'Microsoft.AppConfiguration.KeyValueModified'
        'Microsoft.AppConfiguration.KeyValueDeleted'
      ]
    }
    eventDeliverySchema: eventDeliverySchema
  }
}
