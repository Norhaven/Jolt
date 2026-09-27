# Jolt README

Welcome to the Jolt language extension! At the moment, this provides syntax coloring for your Jolt transformers when using the `*.jolt.json` file extension to make reading and managing your transforms a bit easier. The Jolt language interpreter is written in C# and published to NuGet. Please see the project's [GitHub repository](https://github.com/Norhaven/Jolt) for further information and complete documentation.

## Disambiguation

This is _not_ related to the much older and unrelated Java-based JSON transformation library, by coincidence also called Jolt.

## Features

Right now, this language extension solely provides syntax coloring for your Jolt transformers. For example:

![Match Expression\](IDE/Extensions/VSCode/jolt/images/match-with-test-colorization.png)

![Foreach Loop Expression\](IDE/Extensions/VSCode/jolt/images/foreach-test-colorization.png)

> As a heads up, additional work on the roadmap for this extension intends to offer further functionality such as code completion and transformer validation, among other features.

## Requirements

There are no additional requirements or dependencies at the moment aside from naming your transformer files appropriately, using a supported theme, and using Jolt!

## Extension Settings

This extension offers default colorization settings via the `editor.tokenColorCustomizations` setting. This supports the following themes:
* **Dark Mode** (via `Default Dark Modern`, `Default Dark+`, or `Visual Studio Dark`)
* **Light Mode** (via `Default Light Modern`, `Default Light+`, or `Visual Studio Light`)

## Release Notes

### 1.0.0

Initial release of the Jolt language extension. This includes:
- Language syntax colorization within `*.jolt.json` files.

---

## For more information

* Please visit the [Jolt GitHub page](https://github.com/Norhaven/Jolt) for complete documentation on the Jolt language.