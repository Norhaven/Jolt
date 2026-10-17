---
title: External methods
description: Writing C# methods that transformers can call, including instance methods and lambda parameters.
sidebar:
  order: 1
---

External methods are methods you write in C# and register so transformers can call them. The easiest way is the `JoltExternalMethod` attribute from the `Jolt.Library` namespace.

## Static methods

```csharp
using Jolt.Library;

namespace JoltExample;

public class TransformerMethods
{
    [JoltExternalMethod]
    public static bool IsNotNull(string value) => value != null;
}
```

To give the method a different name inside transformers, pass an alias: `[JoltExternalMethod("differentName")]`.

Register the class when you create the transformer:

```csharp
using Jolt.Json.Newtonsoft;

public string TransformationExample(string transformerJson, string sourceJsonDocument)
{
    var transformer = JoltJsonTransformer.DefaultWith<TransformerMethods>(transformerJson);
    return transformer.Transform(sourceJsonDocument);
}
```

Then call it like any library method:

```jolt title="transformer.json"
{
    "result": "#IsNotNull(#valueOf($.stringValue))"
}
```

```json title="source.json"
{
    "stringValue": "not null"
}
```

```json title="output.json"
{
    "result": true
}
```

## Return values

A method whose result is written directly to the output, as above, must return one of:

- a JSON-compatible value (string, boolean, number, …)
- an `IJsonToken`
- an `EvaluationResult`
- any other object, which is serialized to JSON

Custom types are serialized with **camelCase** property names, so output style stays consistent without per-context configuration.

## Instance methods

External methods can also be instance methods.

```csharp
[JoltExternalMethod]
public string ReverseString(string value) => new string(value.Reverse().ToArray());
```

Pass an instance when you create the transformer. It's used for every instance-method call during transformation.

```csharp
var transformer = JoltJsonTransformer.DefaultWith<TransformerMethods>(transformerJson, new TransformerMethods());
```

External methods support [piping](../../guide/methods-and-piping/#piping) like any other method:

```jolt
{
    "reversed": "#valueOf($.stringValue)->#ReverseString()"
}
```

With the same source document as before, the output is:

```json
{
    "reversed": "llun ton"
}
```

## Lambdas as parameters

External methods can accept a Jolt [lambda](../../guide/lambdas/). Declare the parameter as a `Func<>` whose type arguments are any types Jolt external methods support. Lambdas with one or two parameters and a return value are currently supported.

### One parameter

```csharp
[JoltExternalMethod("convertAllStringsToLongs")]
public static IEnumerable<long> ConvertAllStringsToLongs(IEnumerable<string> sequence, Func<string, long> convertStringToLong)
{
    foreach (var value in sequence)
    {
        yield return convertStringToLong(value);
    }
}
```

```jolt
{
    "converted": "#valueOf($.some.stringArray)->#convertAllStringsToLongs(@x: #toInteger(@x))"
}
```

### Two parameters with a custom type

```csharp
public sealed class ComplexObject
{
    public int Id { get; set; }
    public long Value { get; set; }
}

[JoltExternalMethod("accumulateWithCustomType")]
public static IEnumerable<ComplexObject> AccumulateWithCustomType(IEnumerable<long> sequence, Func<ComplexObject, long, long> accumulate)
{
    var complexObject = new ComplexObject { Id = 0, Value = 0 };

    foreach (var value in sequence)
    {
        complexObject.Id++;
        complexObject.Value = accumulate(complexObject, value);

        yield return complexObject;
    }
}
```

```jolt
{
    "accumulated": "#valueOf($.some.integerArray)->#accumulateWithCustomType(@x;@y: @x.value + @y)"
}
```

With `integerArray` set to `[1, 2, 3, 4]`, the result is:

```json
{
    "accumulated": [
        { "id": 1, "value": 1 },
        { "id": 2, "value": 3 },
        { "id": 3, "value": 6 },
        { "id": 4, "value": 10 }
    ]
}
```

Notice that inside the lambda, `@x.value` uses the camelCase name of the C# `Value` property.

:::tip
If you have a use case for lambdas with more than two parameters, [raise an issue](https://github.com/Norhaven/Jolt/issues/new/choose).
:::

## Registering without the attribute

See [manual registration](../manual-registration/) to register methods explicitly with `MethodRegistration`.
