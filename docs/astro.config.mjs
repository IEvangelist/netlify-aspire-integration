// @ts-check
import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";

// https://astro.build/config
export default defineConfig({
  site: "https://ievangelist.github.io",
  base: "/netlify-aspire-integration",
  integrations: [
    starlight({
      title: "Aspire.Hosting.Netlify",
      description:
        "Deploy any JavaScript frontend to Netlify with three lines of C#. An Aspire integration with first-class deploy pipeline support and multi-language (TypeScript) AppHost compatibility.",
      logo: { src: "./src/assets/logo.svg", replacesTitle: false },
      favicon: "/favicon.svg",
      social: [
        {
          icon: "github",
          label: "GitHub",
          href: "https://github.com/IEvangelist/netlify-aspire-integration",
        },
      ],
      editLink: {
        baseUrl:
          "https://github.com/IEvangelist/netlify-aspire-integration/edit/main/docs/",
      },
      lastUpdated: true,
      pagination: true,
      sidebar: [
        { label: "Home", link: "/", attrs: { "data-icon": "home" } },
        {
          label: "Guides",
          items: [
            { label: "Install", link: "/guides/install/", attrs: { "data-icon": "package" } },
            { label: "Quickstart", link: "/guides/quickstart/", attrs: { "data-icon": "rocket" } },
            { label: "Configuration", link: "/guides/configuration/", attrs: { "data-icon": "settings" } },
            { label: "Authentication", link: "/guides/auth/", attrs: { "data-icon": "shield" } },
            { label: "Deploy pipeline", link: "/guides/pipeline/", attrs: { "data-icon": "workflow" } },
            { label: "Multi-language", link: "/guides/multi-language/", attrs: { "data-icon": "languages" } },
            { label: "CI/CD", link: "/guides/ci-cd/", attrs: { "data-icon": "server" } },
            { label: "Versioning", link: "/guides/versioning/", attrs: { "data-icon": "tag" } },
            { label: "Troubleshooting", link: "/guides/troubleshooting/", attrs: { "data-icon": "lifebuoy" } },
          ],
        },
        {
          label: "Frameworks",
          items: [
            { label: "Angular", link: "/frameworks/angular/", attrs: { "data-icon": "angular" } },
            { label: "Astro", link: "/frameworks/astro/", attrs: { "data-icon": "astro" } },
            { label: "Next.js", link: "/frameworks/next/", attrs: { "data-icon": "next" } },
            { label: "React", link: "/frameworks/react/", attrs: { "data-icon": "react" } },
            { label: "Svelte", link: "/frameworks/svelte/", attrs: { "data-icon": "svelte" } },
            { label: "Vue", link: "/frameworks/vue/", attrs: { "data-icon": "vue" } },
          ],
        },
        {
          label: "API Reference",
          link: "/api/",
          attrs: { target: "_self", "data-icon": "code" },
        },
        {
          label: "Release notes",
          items: [
            { label: "13.2 upgrade", link: "/release-notes/13-2-upgrade/", attrs: { "data-icon": "arrow-up-circle" } },
            { label: "Changelog", link: "/release-notes/changelog/", attrs: { "data-icon": "list" } },
          ],
        },
      ],
      customCss: [
        "@fontsource-variable/inter/index.css",
        "@fontsource-variable/geist/index.css",
        "@fontsource-variable/geist-mono/index.css",
        "./src/styles/custom.css",
      ],
      head: [
        {
          tag: "meta",
          attrs: { name: "theme-color", content: "#0b0a14" },
        },
      ],
    }),
  ],
});

