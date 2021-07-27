
import React from 'react';
import ComponentCreator from '@docusaurus/ComponentCreator';
export default [
{
  path: '/__docusaurus/debug',
  component: ComponentCreator('/__docusaurus/debug','3d6'),
  exact: true,
},
{
  path: '/__docusaurus/debug/config',
  component: ComponentCreator('/__docusaurus/debug/config','914'),
  exact: true,
},
{
  path: '/__docusaurus/debug/content',
  component: ComponentCreator('/__docusaurus/debug/content','c28'),
  exact: true,
},
{
  path: '/__docusaurus/debug/globalData',
  component: ComponentCreator('/__docusaurus/debug/globalData','3cf'),
  exact: true,
},
{
  path: '/__docusaurus/debug/metadata',
  component: ComponentCreator('/__docusaurus/debug/metadata','31b'),
  exact: true,
},
{
  path: '/__docusaurus/debug/registry',
  component: ComponentCreator('/__docusaurus/debug/registry','0da'),
  exact: true,
},
{
  path: '/__docusaurus/debug/routes',
  component: ComponentCreator('/__docusaurus/debug/routes','244'),
  exact: true,
},
{
  path: '/0.1',
  component: ComponentCreator('/0.1','8ec'),
  
  routes: [
{
  path: '/0.1/features/cloudevent/receive-cloudevents-job',
  component: ComponentCreator('/0.1/features/cloudevent/receive-cloudevents-job','f01'),
  exact: true,
},
{
  path: '/0.1/features/security/auto-invalidate-secrets',
  component: ComponentCreator('/0.1/features/security/auto-invalidate-secrets','3f6'),
  exact: true,
},
{
  path: '/0.1/index',
  component: ComponentCreator('/0.1/index','56d'),
  exact: true,
},
]
},
{
  path: '/next',
  component: ComponentCreator('/next','6e7'),
  
  routes: [
{
  path: '/next/',
  component: ComponentCreator('/next/','5c6'),
  exact: true,
},
{
  path: '/next/features/cloudevent/receive-cloudevents-job',
  component: ComponentCreator('/next/features/cloudevent/receive-cloudevents-job','086'),
  exact: true,
},
{
  path: '/next/features/databricks/gain-insights',
  component: ComponentCreator('/next/features/databricks/gain-insights','298'),
  exact: true,
},
{
  path: '/next/features/databricks/job-metrics',
  component: ComponentCreator('/next/features/databricks/job-metrics','507'),
  exact: true,
},
{
  path: '/next/features/security/auto-invalidate-secrets',
  component: ComponentCreator('/next/features/security/auto-invalidate-secrets','367'),
  exact: true,
},
]
},
{
  path: '/',
  component: ComponentCreator('/','ade'),
  
  routes: [
{
  path: '/',
  component: ComponentCreator('/','87b'),
  exact: true,
},
{
  path: '/Features/Databricks/gain-insights',
  component: ComponentCreator('/Features/Databricks/gain-insights','c54'),
  exact: true,
},
{
  path: '/Features/Databricks/job-metrics',
  component: ComponentCreator('/Features/Databricks/job-metrics','38d'),
  exact: true,
},
{
  path: '/Features/General/receive-cloudevents-job',
  component: ComponentCreator('/Features/General/receive-cloudevents-job','a55'),
  exact: true,
},
{
  path: '/Features/Security/auto-invalidate-secrets',
  component: ComponentCreator('/Features/Security/auto-invalidate-secrets','ec9'),
  exact: true,
},
]
},
{
  path: '*',
  component: ComponentCreator('*')
}
];
