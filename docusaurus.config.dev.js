const config = require('./docusaurus.config')

/** @type {import('@docusaurus/types').DocusaurusConfig} */
module.exports = {
  ...config,
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          routeBasePath: "/",
          includeCurrentVersion:true,
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
};
