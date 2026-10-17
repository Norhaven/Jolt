---
title: Writing documentation
description: How this site is built, where each kind of content lives, and how method pages are generated.
sidebar:
  order: 2
---

This site is built with [Astro Starlight](https://starlight.astro.build) from the `docs/` folder of the Jolt repository, and deployed to GitHub Pages on every push to `main` that touches `docs/`.

## Running it locally

```sh
cd docs
npm install
npm run dev      # http://localhost:4321/Jolt/
npm run build    # static output in docs/dist
```

## Where content lives

| Content | Location |
| ------- | -------- |
| Guide and host pages | `src/content/docs/**/*.md` — edit directly |
| Library method metadata and examples | `data/methods.yaml` |
| Longer prose for one method | `data/method-details/<methodName>.md` |
| Jolt syntax highlighting | `src/grammars/*.tmLanguage.json` |
| Sidebar structure | `astro.config.mjs` |

Pages in `src/content/docs/reference/library/` are **generated**. Don't edit them; they're overwritten on every build and aren't committed.

## Adding or changing a library method

Each entry in `data/methods.yaml` becomes one page, a row in the [all methods](../../reference/library/) table, and an entry in the sidebar under its category.

```yaml
- name: toUpperCase
  category: strings
  summary: Returns the uppercase version of a string value.
  signature: "#toUpperCase(value)"
  parameters:
    - name: value
      type: path | string
      description: The string to convert.
  returns:
    type: string
    description: The uppercase string.
  validIn: [property-value]
  examples:
    - title: Uppercase a name
      source: { "name": "ada" }
      transformer: { "result": "#toUpperCase($.name)" }
      output: { "result": "ADA" }
  related: [toLowerCase]
```

| Field | Required | Notes |
| ----- | -------- | ----- |
| `name` | yes | Exactly as called, without `#`. |
| `category` | yes | One of the keys under `categories:` at the top of the file. |
| `summary` | yes | One sentence. Used in tables, search, and page descriptions. |
| `signature` | yes | Use `?` for optional parameters and `@x: body` for lambdas. |
| `parameters` | no | `optional: true` marks optional parameters. |
| `returns` | no | |
| `validIn` | yes | Any of `property-value`, `property-name`, `statement`, `match-case`. |
| `unsafe` | no | `true` for methods that need `WithUnsafeAllowed`. |
| `examples` | no | Either `expression` alone, or `source` + `transformer` + `output`. |
| `notes` | no | Markdown list items shown under **Notes**. |
| `related` | no | Other method names. |

## Examples are tests

Every example with `source`, `transformer`, and `output` is exported to `src/generated/doc-examples.json` when the site builds. `tests-template/DocExamplesTests.cs` shows how to run them as xUnit tests against the real engine, so a documented example that drifts from actual behavior fails the build.

## Longer explanations

For anything beyond a summary and examples, such as a walkthrough, edge cases, or design rationale, create `data/method-details/<name>.md`. Its contents are appended to the generated page under **Details**. See `data/method-details/summarizeWith.md` for an example.
