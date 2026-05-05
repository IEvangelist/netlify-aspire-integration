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
        "Deploy any JavaScript frontend to Netlify with three lines of C#. A .NET Aspire integration with first-class deploy pipeline support and multi-language (TypeScript) AppHost compatibility.",
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
        { label: "Home", link: "/" },
        {
          label: "Guides",
          items: [
            { label: "Install", link: "/guides/install/" },
            { label: "Quickstart (C#)", link: "/guides/quickstart-csharp/" },
            { label: "Quickstart (TypeScript)", link: "/guides/quickstart-typescript/" },
            { label: "Configuration", link: "/guides/configuration/" },
            { label: "Authentication", link: "/guides/auth/" },
            { label: "Deploy pipeline", link: "/guides/pipeline/" },
            { label: "Multi-language", link: "/guides/multi-language/" },
            { label: "CI/CD", link: "/guides/ci-cd/" },
            { label: "Versioning", link: "/guides/versioning/" },
            { label: "Troubleshooting", link: "/guides/troubleshooting/" },
          ],
        },
        {
          label: "Frameworks",
          items: [
            { label: "Angular", link: "/frameworks/angular/" },
            { label: "Astro", link: "/frameworks/astro/" },
            { label: "Next.js", link: "/frameworks/next/" },
            { label: "React", link: "/frameworks/react/" },
            { label: "Svelte", link: "/frameworks/svelte/" },
            { label: "Vue", link: "/frameworks/vue/" },
          ],
        },
        {
          label: "API Reference",
          link: "/api/",
          attrs: { target: "_self" },
        },
        {
          label: "Release notes",
          items: [
            { label: "13.2 upgrade", link: "/release-notes/13-2-upgrade/" },
            { label: "Changelog", link: "/release-notes/changelog/" },
          ],
        },
      ],
      customCss: ["./src/styles/custom.css"],
    }),
  ],
});
