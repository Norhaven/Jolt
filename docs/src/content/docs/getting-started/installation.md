---
title: Installation
description: Choose between the Newtonsoft and System.Text.Json packages and add Jolt to your project.
sidebar:
  order: 2
---

Jolt ships as three NuGet packages. Most projects need exactly one of the first two.

| Package | Use it when | Version |
| ------- | ----------- | ------- |
| `Jolt.Json.Newtonsoft` | Your app already uses Newtonsoft.Json | ![NuGet](https://img.shields.io/nuget/v/Jolt.Json.Newtonsoft) |
| `Jolt.Json.DotNet` | Your app uses System.Text.Json | ![NuGet](https://img.shields.io/nuget/v/Jolt.Json.DotNet) |
| `Jolt` | You're building your own JSON integration on top of the core language | ![NuGet](https://img.shields.io/nuget/v/Jolt) |

The first two depend on `Jolt`, so you only reference it directly when building your own integration, as those packages have done.

```sh
# Newtonsoft.Json
dotnet add package Jolt.Json.Newtonsoft

# System.Text.Json
dotnet add package Jolt.Json.DotNet
```

## Supported targets

All packages are multi-targeted and support .NET Standard 2.0, .NET Standard 2.1, .NET 8.0, and .NET 10.0.

## Note on System.Text.Json

`Jolt.Json.DotNet` has one extra dependency, [JsonPath.Net](https://github.com/json-everything/json-everything), because System.Text.Json doesn't include JSON Path support out of the box.

## Build status

[![Latest Build](https://github.com/Norhaven/Jolt/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/Norhaven/Jolt/actions/workflows/build-and-release.yml)

Next: [write your first transformer](../first-transformer/).
