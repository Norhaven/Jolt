# Jolt documentation site

The source for https://norhaven.github.io/Jolt/, built with [Astro Starlight](https://starlight.astro.build).

```sh
npm install
npm run dev      # http://localhost:4321/Jolt/
npm run build    # static site in dist/
```

| What | Where |
| ---- | ----- |
| Guide, host-integration and reference pages | `src/content/docs/` |
| Library method metadata and examples | `data/methods.yaml` |
| Extra prose for one method | `data/method-details/<methodName>.md` |
| Jolt syntax highlighting | `src/grammars/` |
| Sidebar | `astro.config.mjs` |

Library pages under `src/content/docs/reference/library/` are generated from `data/methods.yaml` before every `dev` and `build` (`npm run generate` to do it by hand). They're gitignored; don't edit them.

Examples in `methods.yaml` that have `source`, `transformer` and `output` are also exported to `src/generated/doc-examples.json`. `tests-template/DocExamplesTests.cs` runs them against the engine as xUnit tests.

Deployment: `.github/workflows/docs.yml` builds and publishes on every push to `main` that touches `docs/`. Set **Settings → Pages → Source** to **GitHub Actions** once.
