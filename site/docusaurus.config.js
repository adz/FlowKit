const { themes } = require('prism-react-renderer');
const lightCodeTheme = themes.github;
const darkCodeTheme = themes.dracula;

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'FsFlow',
  tagline: 'Typed results, explicit context, and async/task interop for F#.',
  favicon: 'img/favicon-light.svg',
  url: 'https://adz.github.io',
  baseUrl: '/FsFlow/',
  organizationName: 'adz',
  projectName: 'FsFlow',
  trailingSlash: true,
  future: {
    faster: {
      rspackBundler: true,
    },
  },
  onBrokenLinks: 'throw',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },
  presets: [
    [
      'classic',
      {
        docs: {
          path: '../docs',
          routeBasePath: '/',
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: 'https://github.com/adz/FsFlow/tree/main/',
        },
        blog: false,
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
  themeConfig: {
    image: 'img/fsflow-readme-light.svg',
    navbar: {
      title: 'FsFlow',
      logo: {
        alt: 'FsFlow',
        src: 'img/fsflow-inline-light.svg',
        srcDark: 'img/fsflow-inline-dark.svg',
      },
      items: [
        {
          label: 'Home',
          to: '/',
          position: 'left',
        },
        {
          label: 'API Docs',
          to: '/reference/',
          position: 'left',
        },
        {
          type: 'docsVersionDropdown',
          position: 'left',
        },
        {
          href: 'https://github.com/adz/FsFlow',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            { label: 'Home', to: '/' },
            { label: 'API Docs', to: '/reference/' },
          ],
        },
        {
          title: 'Code',
          items: [
            { label: 'Source Repository', href: 'https://github.com/adz/FsFlow' },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Adam Davies`,
    },
    prism: {
      theme: lightCodeTheme,
      darkTheme: darkCodeTheme,
      additionalLanguages: ['fsharp'],
    },
    search: {
      provider: 'local',
    },
  },
};

module.exports = config;
