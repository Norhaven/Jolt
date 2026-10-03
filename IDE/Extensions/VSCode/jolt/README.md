Welcome to the Jolt language extension! This provides syntax coloring, method completion, and variable completion for your Jolt transformers when using the `*.jolt.json` file extension to make reading and managing your transforms a bit easier. The Jolt language interpreter is written in C# and published to NuGet. Please see the project's [GitHub repository](https://github.com/Norhaven/Jolt) for further information and complete documentation.

## Disambiguation

This is _not_ part of the much older and unrelated Java-based JSON transformation library, by coincidence also called Jolt.

## Features

### Syntax Coloring

The Jolt language is embedded inside JSON strings, and editing transforms with the default JSON coloring can make it more difficult to develop and maintain. This language extension provides syntax coloring to make that easier.

![Match Expression](IDE/Extensions/VSCode/jolt/images/match-with-test-colorization.png)

![Foreach Loop Expression](IDE/Extensions/VSCode/jolt/images/foreach-test-colorization.png)

### Method & Variable Completion

It's also a bit difficult to know at a glance what the available Jolt Library methods are and their signatures without some help, especially when trying to understand which methods are valid in which areas of your transformer. When you type either a method start symbol `#` or a piped method symbol `->`, you will get a list of available library methods with their signatures. Selecting one will provide a snippet to help get you going.

The same holds true for variables and understanding which are currently in scope for you to use and which are not. This extension provides context-sensitive method and variable completions to help speed up your development and maintenance abilities.

![Method And Variable Completions](IDE/Extensions/VSCode/jolt/images/method-and-variable-completions.gif)

### Method Information On Hover

Lastly, you may forget what a particular method is for and need a quick refresher on the documented method information. It also slows down your development if you have to continually refer to the README file, so the documented definition for a given method is available when hovering over it.

![Method Information](IDE/Extensions/VSCode/jolt/images/method-information-on-hover.png)

## Requirements

There are no additional requirements or dependencies at the moment aside from naming your transformer files appropriately, using a supported theme, and using Jolt!

## Extension Settings

This extension offers default colorization settings via the `editor.tokenColorCustomizations` setting. This supports the following themes:
* **Dark Mode** (via `Dark 2026`, `Default Dark Modern`, `Default Dark+`, or `Visual Studio Dark`)
* **Light Mode** (via `Light 2026`, `Default Light Modern`, `Default Light+`, or `Visual Studio Light`)

## Current Limitations

There are a few use cases not covered by the extension, most notably that your custom methods will not be available in either the method completion dropdown or the hover documentation due to them being registered at runtime (which could change the implementation, intended usage, and other areas that aren't visible at compile time). 

> We're currently looking into how to provide this to you in the best way possible, but for now they won't be available.

## Release Notes

### 1.0.0

Initial release of the Jolt language extension. This includes:
- Language syntax colorization within `*.jolt.json` files.

### 1.1.0

Followup release of the Jolt language extension. This includes:
- Method completions
- Variable completions
- On-Hover documentation

---

## For more information

* Please visit the [Jolt GitHub page](https://github.com/Norhaven/Jolt) for complete documentation on the Jolt language.