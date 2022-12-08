const buildConfig = require('./docusaurus.config');

module.exports = {
  ...buildConfig,
  themeConfig: {
    ...buildConfig.themeConfig,
    algolia: {
      appId: process.env.ALGOLIA_APP_ID,
      apiKey: process.env.ALGOLIA_API_KEY,
      indexName: 'arcus-azure',
      contextualSearch: true,
      searchParameters: {
        facetFilters: ["tags:background-jobs"]
      },
    },
  }
}
