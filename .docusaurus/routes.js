
import React from 'react';
import ComponentCreator from '@docusaurus/ComponentCreator';
export default [
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
