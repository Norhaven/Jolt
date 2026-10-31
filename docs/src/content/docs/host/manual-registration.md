---
title: Manual registration
description: Registering external methods explicitly with MethodRegistration instead of the attribute.
sidebar:
  order: 2
---

The `[JoltExternalMethod]` attribute hides the registration step. To register methods yourself, use the `MethodRegistration` class. It wraps everything the transformer needs to know to call your method, for both static and instance methods.

Registering the methods from [external methods](../external-methods/):

```csharp
var staticRegistration = MethodRegistration.FromStaticMethod(typeof(TransformerMethods), nameof(TransformerMethods.IsNotNull));
var instanceRegistration = MethodRegistration.FromInstanceMethod(nameof(TransformerMethods.ReverseString));
```

An instance registration only needs the method name, not a type. The transformer resolves it against whatever method context instance you supply when creating the transformer.

Pass the registrations, plus an instance if you have instance methods:

```csharp
var transformer = JoltJsonTransformer.DefaultWith(
    transformerJson,
    new TransformerMethods(),
    new[] { staticRegistration, instanceRegistration });
```

Call `Transform` with your source document as usual.

## Delegates

To skip creating a class for your methods, register a delegate with `MethodRegistration.FromDelegate`, giving it the name that the transformer calls it by. Its parameters are those of the delegate, so it works like a method on a class, lambda parameters included, and it can use any state it captures:

```csharp
var prefix = "Hello, ";
var greet = MethodRegistration.FromDelegate("greet", (string name) => prefix + name);
var sum = MethodRegistration.FromDelegate("sumOf", (IEnumerable<long> values, Func<long, long> selector) => values.Select(selector).Sum());

var transformer = JoltJsonTransformer.DefaultWith(transformerJson, null, new[] { greet, sum });
```

The transformer can then call `#greet(#valueOf($.name))` or `#sumOf(#valueOf($.numbers), @x: @x * 2)`.

A delegate whose last parameter is a `params` array accepts any number of arguments for it. When that array (or any parameter) is of type `object`, a lambda argument is passed as the `LambdaMethod` itself rather than being rejected.
