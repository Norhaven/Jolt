# Jolt

Welcome! This is a JSON transformation language inspired by XSLT and the wonderful .Net adaptation over at [JUST.Net](https://github.com/WorkMaze/JUST.net). This package provides an expression-based interpreter for the language and a highly extensible way of approaching the same problem, namely How To Transform JSON Into Different JSON.

# Syntax

The transformation language itself is pretty straightforward and primarily relies on defining and using methods to handle the transformation work needed, both as provided by the library within the package or external methods that you will create and register for use.

## Methods

All method calls begin with a `#` symbol and are case-sensitive. Open and close parentheses surround the method arguments, which are comma-separated. Let's take a look at a an example from the library.
```json
{
    "value": "#valueOf($.some.json.path)"
}
```
You'll notice the use of a JSON Path query as a parameter. This allows you to query the source document for the JSON value that exists at that path and use it in your transformation. Most of the library methods can take either a path, like above, or the value from the source document itself.
```json
{
    "valueExists": "#exists($.some.json.path)",
    "otherValueExists": "#exists(#valueOf($.some.json.path))"
}
```
You can also optionally use the piped method syntax to use the result of one method as the first parameter of another method. In that case, that parameter is removed from the second call.
```json
{
    "valueExists": "#valueOf($.some.json.path)->#exists()"
}
```
If you are familiar with C# extension methods, this is the same sort of concept. We can interchangeably use any method as either static or piped and pretend that a method call is an instance method for a little while to help with readability.

We'll go into the available library methods further down, but let's talk about how you'd possibly go about implementing your own method to use in a transformer.

## External Methods

These are methods that you have created in your code. In order to use them, you will need to register them prior to using the transformer. Let's assume that you have the following C# method that you would like to be able to use. The easiest way to approach this is to use the `JoltExternalMethod` attribute.
```csharp
public class TransformerMethods
{
    [JoltExternalMethod]
    public static bool IsNotNull(string value) => value != null;
}
```
You will need to create a JSON transformer and add your methods to it. The `Jolt.Json` package support either `Newtonsoft` or `System.Text.Json` implementations, so choose which flavor you would like to use and instantiate one. For our demonstration purposes here, we'll assume Newtonsoft. The `Jolt.Json.DotNet` namespace contains the other one.
```csharp
using Jolt.Json.Newtonsoft;

// Omitting boilerplate class definitions.

public void TransformationExample(string transformerJson)
{
    var transformer = JoltJsonTransformer.DefaultWith<TransformerMethods>(transformerJson);
}
```

