# Samples

Hands-on samples for [`Aspire.Hosting.Netlify`](../src/Aspire.Hosting.Netlify) — pick
a starting point that fits how you want to learn.

## Pick a starting point

| Sample | What it is | Best for |
| --- | --- | --- |
| [`Quickstart.AppHost`](Quickstart.AppHost/) | Minimal C# AppHost that publishes a single Astro site using the directory-only `PublishAsNetlifySite("dist", authToken)` overload. | First-time users who want the smallest possible "deploy a site to Netlify" demo in C#. |
| [`Quickstart.AppHost.TypeScript`](Quickstart.AppHost.TypeScript/) | The same single-Astro deploy authored as a TypeScript AppHost via the [Aspire Type System](https://aspire.dev/architecture/multi-language-architecture/). | Showing the multi-language surface and validating ATS exports. |
| [`AllFrameworks.AppHost`](AllFrameworks.AppHost/) | Six-site demo that publishes Angular, Astro, Next.js, React, Svelte, and Vue sites in a single AppHost. | Showcasing the integration end-to-end and validating multi-site deploys. |

## Framework demos

The six frontend apps used by `AllFrameworks.AppHost` live alongside the AppHost so
they're easy to discover, edit, and run on their own:

- [`angular/`](angular/) — Angular CLI app with browser builder output.
- [`astro/`](astro/) — Astro static site generator.
- [`next/`](next/) — Next.js with `output: 'export'` for static deploys.
- [`react/`](react/) — Vite + React + TypeScript SPA.
- [`svelte/`](svelte/) — Vite + Svelte + TypeScript SPA.
- [`vue/`](vue/) — Vite + Vue 3 SPA.

Each folder contains its own `README.md` with framework-specific run/build steps.

## Run a sample

```sh
# Single-site quickstart (C#)
aspire run --apphost samples/Quickstart.AppHost/Quickstart.AppHost.csproj

# Single-site quickstart (TypeScript)
aspire run --apphost samples/Quickstart.AppHost.TypeScript/apphost.ts

# Six-site demo
aspire run --apphost samples/AllFrameworks.AppHost/AllFrameworks.AppHost.csproj
```

By default, the repo's [`aspire.config.json`](../aspire.config.json) points at
`AllFrameworks.AppHost`, so a bare `aspire run` from the repo root launches the
full demo.
