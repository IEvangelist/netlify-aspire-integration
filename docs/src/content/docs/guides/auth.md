---
title: Authentication
description: How the integration resolves Netlify auth tokens at deploy time.
---

`Aspire.Hosting.Netlify` resolves a Netlify auth token at deploy time and falls
back to interactive `ntl login` if nothing is configured.

## Precedence

1. **Parameter** — the `authToken` argument passed to `PublishAsNetlifySite(...)`.
2. **Environment variable** — `NETLIFY_AUTH_TOKEN`.
3. **Interactive login** — the pipeline calls `ntl login` once per `aspire deploy`
   run if neither of the above produced a non-empty token.

:::caution[Behaviour change in 13.2.x]
Earlier versions let `NETLIFY_AUTH_TOKEN` clobber a parameter token. As of this
release the parameter wins. If both are set with different values, the pipeline
emits a warning naming the affected deployment.
:::

## Configure with user secrets

Use the [`aspire secret`](https://aspire.dev/reference/cli/commands/aspire-secret/)
command from the directory containing your AppHost. The CLI auto-discovers the
AppHost project (or pass `--apphost <path>` to be explicit) and persists the
value in the standard user-secrets store.

```sh
aspire secret set Parameters:netlify-token "$(cat ~/.config/netlify/auth)"
```

Inspect or remove a stored value at any time:

```sh
aspire secret list
aspire secret get Parameters:netlify-token
aspire secret delete Parameters:netlify-token
```

```csharp
var authToken = builder.AddParameterFromConfiguration(
    "netlify-token", "NETLIFY_AUTH_TOKEN", secret: true);

builder.AddJavaScriptApp("astro", "../astro")
    .PublishAsNetlifySite("dist", authToken);
```

## Configure for CI

In GitHub Actions, set the env var on the deploy job:

```yaml
- name: Aspire deploy
  env:
    NETLIFY_AUTH_TOKEN: ${{ secrets.NETLIFY_AUTH_TOKEN }}
  run: aspire deploy
```

## Tokens, not passwords

A Netlify auth token is created at
[app.netlify.com/user/applications#personal-access-tokens](https://app.netlify.com/user/applications#personal-access-tokens).
Do **not** check it in — the integration logs Site IDs and tokens with the
`<redacted>` placeholder, but only because it never sees the literal value
outside of the configured parameter / env var.
