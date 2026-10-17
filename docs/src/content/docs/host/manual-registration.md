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
    new[] { staticRegistration, instanceRegistration },
    new TransformerMethods());
```

Call `Transform` with your source document as usual.
