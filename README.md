# Jolt Language

![Logo](https://raw.githubusercontent.com/Norhaven/Jolt/main/JoltLogo.png)

Welcome! This is a JSON transformation language inspired by XSLT and the wonderful .Net JSON adaptation of it over at [JUST.Net](https://github.com/WorkMaze/JUST.net). This project provides an expression-based interpreter for the language and a highly extensible way of approaching the same problem, namely How To Transform JSON Into Different JSON.

There are two `.Net Standard 2.1` packages available: `Jolt.Json.Newtonsoft` and `Jolt.Json.DotNet`. These use `Newtonsoft` or `System.Text.Json` functionality, respectively.

## Build/Release Status

[![Latest Build](https://github.com/Norhaven/Jolt/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/Norhaven/Jolt/actions/workflows/build-and-release.yml)

| Package Name | NuGet Version |
| ------------ | ------------- |
| Jolt.Json.DotNet | ![NuGet Version](https://img.shields.io/nuget/v/Jolt.Json.DotNet) |
| Jolt.Json.Newtonsoft | ![NuGet Version](https://img.shields.io/nuget/v/Jolt.Json.Newtonsoft) |

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
    "otherValueExists": "#exists(#valueOf($.some.other.json.path))"
}
```
You can also optionally use the piped method syntax with an arrow in order to use the result of one method as the first parameter of another method. In that case, that parameter is removed from the second call.
```json
{
    "valueExists": "#valueOf($.some.json.path)->#exists()"
}
```
If you are familiar with C# extension methods, this is the same sort of concept. We can interchangeably use any method (including your own external methods) as either standalone or piped and pretend that a method call is an instance method for a little while to help with readability. We'll go into the available library methods further down, but let's talk first about how you'd possibly go about implementing your own method to use in a transformer.

## External Methods

These are methods that you have created in your code. In order to use them, you will need to register them prior to using the transformer. Let's assume that you have the following C# method that you would like to be able to use. The easiest way to approach this is to use the `JoltExternalMethod` attribute from the `Jolt.Library` namespace. If the method name needs to be different when used from a transformer, specify an alias in the attribute constructor, such as `[JoltExternalMethod("differentName")]`.
```csharp
using Jolt.Library;

namespace JoltExample;

public class TransformerMethods
{
    [JoltExternalMethod]
    public static bool IsNotNull(string value) => value != null;
}
```
You will need to create a JSON transformer and register your methods with it prior to the actual transformation. There are two `.Net Standard 2.1` packages available by default: `Jolt.Json.Newtonsoft` and `Jolt.Json.DotNet`, which use either `Newtonsoft` or `System.Text.Json` as their particular flavor of implementation. Also, note that the `Jolt.Json.DotNet` package has an additional dependency, namely on [JsonPath.Net](https://github.com/json-everything/json-everything), due to there being no default JSON Path support out of the box in `System.Text.Json` as of yet. 

For our demonstration purposes here, we'll assume Newtonsoft.
```csharp
using Jolt.Json.Newtonsoft;

// Omitting boilerplate class definition.

public string TransformationExample(string transformerJson, string sourceJsonDocument)
{
    var transformer = JoltJsonTransformer.DefaultWith<TransformerMethods>(transformerJson);
    return transformer.Transform(sourceJsonDocument);
}
```
Now that we've got the transformer set up in code and the method registered, let's create a JSON transformer that makes use of the new method.
```json
{
    "result": "#IsNotNull(#valueOf($.stringValue))"
}
```
You'll notice that the JSON path above is to a property called `stringValue`. This transformer assumes that you know that a string property with that name exists on the source document so, for example, this document here could be your source.
```json
{
    "stringValue": "not null"
}
```
That will execute your method with the value "not null" and create the following output.
```json
{
    "result": true
}
```
Your external methods that provide their output at the root level, as in the example above, must return a value that is either an acceptable JSON value (e.g. string, boolean), an `IJsonToken` instance, an `EvaluationResult` instance, or an instance that will be serialized to JSON. Your other non-root external methods may return and consume whatever instance types they want.

The external methods you create may be either static or instance methods. We took a look at how a static method would be used above, and you can follow the same path for instance methods with one extra step. Let's assume we added the following instance method to the `TransformerMethods` class above.
```csharp
[JoltExternalMethod]
public string ReverseString(string value) => value.Reverse();
```
In order to use the instance method, we need to pass an instance of `TransformerMethods` to the transformer creation call.
```csharp
var transformer = JoltJsonTransformer.DefaultWith<TransformerMethods>(transformerJson, new TransformerMethods());
```
This instance will be used for all instance method calls during the transformation step, so you could easily modify your JSON transformer to include this in the same way as before. Keep in mind that your own methods can also take advantage of the piped method syntax, so let's demonstrate that here.
```json
{
    "reversed": "#valueOf($.stringValue)->#ReverseString()"
}
```
This will create the following output when used to transform the same source JSON we used earlier.
```json
{
    "reversed": "llun ton"
}
```

# Math And Equality

The usual basic math operators are implemented, namely addition, subtraction, multiplication, and division, using `+`, `-`, `*`, and `/` respectively, along with comparison and equality as `=`, `!=`, `>`, `>=`, `<`, and `<=`. Operations can be parenthesized as well. These are all used much like you're used to and can take the results of methods as operands. For example, assuming the following source JSON:
```json
{
    "first": 5,
    "second": 4,
    "third": 3,
    "fourth": 2,
    "fifth": 1
}
```
You could create a transformer that would look something like this:
```json
{
    "result": "#valueOf($.first) * #valueOf($.second) - #valueOf($.third) + #valueOf($.fourth) / #valueOf($.fifth) = 19"
}
```
And after transformation it would look like this:
```json
{
    "result": true
}
```
It's also worth noting that the addition operator `+` is overloaded and can serve as a shortcut to string concatenation, either with a source document value or a literal string. For example, in the following source JSON:
```json
{
    "value": "example"
}
```
And assuming that you have a transformer that looks like this:
```json
{
    "result": "#valueOf($.value) + #valueOf($.value) + 'hello'"
}
```
The output would look like this:
```json
{
    "result": "exampleexamplehello"
}
```

# Range Variables

There are times that you may want to perform a particular transform and then do something additional to it, whether that's simple math or looping over it. Most of the time, you'd also rather that the intermediary transform not stick around in the final JSON output, it's just there to do some preprocessing. This is an area where you may find that range variables work for your needs.

You can declare and set a variable in the property name one of two ways, either as the direct result of an expression or as the output of a loop. Let's take a look at both.
```json
{
    "@firstVar": "#valueOf($.someInteger)",
    "@secondVar": "#valueOf($.someOtherInteger)",
    "sum": "@firstVar + @secondVar"
}
```
This is one way to directly declare and use variables in your transformations. All variables are prefixed with the `@` symbol and specifying it in the property name indicates that you would like to set it and also remove that JSON node from the transformed result. Let's see how you would be able to do some preprocessing on an array now.
```json
{
    "#foreach(@x in $.someArray)->@tempResult": [
        {
            "intermediateValue": "@x.someInteger * 2"
        }
    ],
    "#foreach(@x in @tempResult)->'actualResult'": [
        {
            "finalValue": "@x.intermediateValue + 5"
        }
    ]
}
```
The first loop will set the variable `@tempResult` to the value of the transformed array, which will use the provided content template to transform each array element it finds, then removes the node from the output. The second loop takes the variable results as input and loops over that intermediary array, applying additional transformation to the results, naming the final array property as `actualResult`.

You can also declare variables within the content template of a loop, but they only exist within it and on a per-iteration basis (meaning that the variable is redeclared, set, and thrown away every time through the loop). Let's see an example of that here.
```json
{
    "#foreach(@x in $.someArray)->'finalResult'": [
        {
            "@scopedVar": "@x.someInteger / 2",
            "subtractedValue": "@scopedVar - 3"
        }
    ],
    "invalidValue": "@scopedVar"
}
```
In the resulting JSON output, the `finalResult` property will be populated by the array loop but the `invalidValue` property will be null due to accessing a variable which has already been destroyed.

### Range Variables: Lambda Expressions

There are also another use for range variables, namely in lambda expressions. Several methods in the library will take a lambda in order to better filter and/or refine the JSON data, used in the form of declaring a range variable and a body expression separated by a colon `:`. The variable's lifetime is scoped to the lambda body and will not be accessible outside of it. Let's take a quick look at the methods which allow this usage.
```json
{
    "hasValues": "#valueOf($.some.integerArray)->#any(@x: @x > 5)",
    "filteredData": "#valueOf($.some.complexArray)->#where(@x: @x.some.path < 10)",
    "projectedData": "#valueOf($.some.complexArray)->#select(@x: @x.some.path)"
}
```
Assuming that we have the following data:
```json
{
    "some": {
        "integerArray": [ 1, 2, 6, 10 ],
        "complexArray": [
            {
                "some": {
                    "path": 5
                }
            },
            {
                "some": {
                    "path": 30
                }
            }
        ]
    }
}
```
Using the previous transform, the output would look like:
```json
{
    "hasValues": true,
    "filteredData": [
        {
            "some": {
                "path": 5
            }
        }
    ],
    "projectedData": [ 5, 30 ]
}
```

### Range Variables: Using blocks and statements

A final use for range variables is in the case where you have a path or variable that needs direct modification to include, modify, or exclude specific JSON nodes. For example, you might not know the entire shape of the incoming JSON document but you know that it needs to stay the same except for a few changes, or perhaps you might want to replicate a few parts of the source document within the transformer output along with some tweaks to its data and/or structure. This would mean that you can't necessarily stand up an entire transformer that manually contains every node ahead of time. Let's take a look at a way you could approach this using the following source JSON.
```json
{
    "some": {
        "integerArray": [ 1, 2, 6, 10 ],
        "complexObject": {            
            "some": {
                "path": 5
            }
        }
    }
}
```
In this example, let's create a transformer that will take the properties from the `some` node, remove the `integerArray` property, add a property to the `complexObject` object, and change the value of the `path` property.
```json
{
    "#using($.some as @x)->'result'": [
        "#removeAt(@x.integerArray)",
        "#setAt(@x.complexObject.new.value, 3)",
        "#setAt(@x.complexObject.some.path, 20)"
    ]
}
```
This would output the following JSON:
```json
{
    "result": {
        "complexObject": {
            "new": {
                "value": 3
            },
            "some": {
                "path": 20
            }
        }
    }
}
```
It's worth noting that statements are only allowed within a `using` block and must operate on the scoped range variable specified there. You may also notice that the `#setAt()` call was allowed to take a path that did not actually exist at the time of use. This is because that particular method will take care of creating the missing pieces of the JSON hierarchy for you if they don't currently exist.

Lastly, you can take advantage of pre-processing variables with a `using` block much the same way as a `foreach` loop does by assigning the output to a variable instead of a named property, which can be used either in a subsequent `using` block or other valid variable uses.
```json
{
    "#using($.some.path as @x)->@tempResult": [
        "#removeAt(@x.integerArray)"
    ],
    "#using(@tempResult as @x)->'actualResult'": [
        "#setAt(@x.some.path, 'text')"
    ]
}
```

# Library Methods

This package comes with quite a few methods built into it to get you started, all of which are documented here and represent the most common things that you may want to do when transforming a JSON file. If you find that an opportunity for a new library method exists, please raise an issue and it will be considered. For more detail on the individual library methods, please see the Jolt wiki [over here.](https://github.com/Norhaven/Jolt/wiki)

| Method | Description | Example | Valid On
| ------ | ----------- | ------- | --------
| valueOf | Gets the value of a property at the specified path | `#valueOf($.some.path)` | Property Value
| exists | Returns true if the provided value is not null, false otherwise | `#exists($.some.path)` | Property Value
| if | Takes three parameters: a boolean condition, an expression to evaluate when that condition is true, and an expression to evaluate when false | `#if(#valueOf($.some.path), 'Yes', 'No')` | Property Value
| includeIf | Takes a path or boolean condition and will evaluate and include the property's object if true, returning null otherwise | `"#includeIf($.some.path)->'someName'": { "nestedValue": "#valueOf($.other.path)" }` | Property Name
| eval | Evaluates an arbitrary expression, either from a path or literal value, and returns the result | `#eval('1 + 2 = 3')` | Property Name/Value
| foreach | Evaluates a path and loops over the array elements or object properties it finds there to create its property values, naming the property as the string literal referred to by the arrow | `"#foreach(@x in $.some.path)->'result'": [ { "templateValue": "#valueOf($.other.path)" } ]` | Property Name
| nameOf | Returns the name of the property being evaluated by the provided loop variable | `"#nameOf(@x)": "#valueOf($.some.path)"` | Property Name
| indexOf | Returns the zero-based index value of the first occurrence of the provided value | `#indexOf(#valueOf($.some.path), 'some string')` | Property Value
| length | Returns the length of a string or array value | `#length($.some.path)` | Property Value
| substring | Returns the string value that falls within the provided range in a given string | `#substring(1..2)` | Property Value
| groupBy | Returns a JSON object that represents the grouping of an array's contents by its individual property values | `#groupBy($.some.path, 'propertyName')` | Property Value
| orderBy | Returns an array in ascending order as determined by its individual property values | `#orderBy($.some.path, 'propertyName')` | Property Value
| orderByDesc | Returns an array in descending order as determined by its individual property values | `#orderByDesc($.some.path, 'propertyName')` | Property Value
| contains | Returns true when an array or string contains the provided value | `#contains($.some.path, 'some string')` | Property Value
| roundTo | Returns the value of a provided number rounded to the specified decimal places | `#roundTo($.some.path, 2)` | Property Value
| max | Returns the maximum value found within an array of numbers | `#max($.some.path)` | Property Value
| min | Returns the minimum value found within an array of numbers | `#min($.some.path)` | Property Value
| sum | Returns the total value found within an array of numbers | `#sum($.some.path)` | Property Value
| average | Returns the average value found within an array of numbers | `#average($.some.path)` | Property Value
| joinWith | Returns a string that joins all elements of an array with the provided delimiter | `#joinWith($.some.path, ',')` | Property Name/Value
| splitOn | Returns an array of substrings from a string value splitting on the provided delimiter | `#splitOn($.some.path, ',')` | Property Value
| append | Returns a string or array made from appending one or more strings or arrays onto them | `#append($.some.path, 'one', 'two')` | Property Value
| isInteger | Returns true if the value is represented as a whole number | `#isInteger($.some.path)` | Property Value
| isString | Returns true if the value is represented as a string | `#isString($.some.path)` | Property Value
| isDecimal | Returns true if the value is represented as a floating point number | `#isDecimal($.some.path)` | Property Value
| isBoolean | Returns true if the value is represented as a boolean | `#isBoolean($.some.path)` | Property Value
| isArray | Returns true if the value is represented as an array, false otherwise | `#isArray($.some.path)` | Property Value
| isEmpty | Returns true if the value is an array or string with no contents, false otherwise | `#isEmpty($.some.path)` | Property Value
| toInteger | Returns a value converted to a whole number | `#toInteger($.some.path)` | Property Value
| toString | Returns the string representation of a value | `#toString($.some.path)` | Property Value
| toDecimal | Returns a value converted to a floating point number | `#toDecimal($.some.path)` | Property Value
| toBoolean | Returns a value converted to a boolean | `#toBoolean($.some.path)` | Property Value
| any | Returns true if the value is an array or string with contents, false otherwise | `#any($.some.path)` | Property Value
| where | Returns an array of objects that match a predicate | `#where($.some.path, @x: @x.other.path > 2)` | Property Value
| select | Returns an array of objects that are the result of a projection | `#select($.some.path, @x: @x.other.path)` | Property Value
| removeAt | Removes a JSON node from the provided variable-based path | `#removeAt(@x.some.path)` | Statement
| setAt | Adds or modifies a JSON node specified with the provided variable-based path | `#setAt(@x.some.path, 5)` | Statement

# Alternate External Method Registrations

There are additional ways available to register your external methods, let's take a look at another one.

## Method Registration

The slight bit of added complexity that the previous way (using the `[JoltExternalMethod]` attribute) hid from you is the registration process. If you'd like to do this process manually, you will need a few things.

The first thing is the `MethodRegistration` class. This wraps up all of the relevant information about your external method in a way that the transformer can understand in order to call it. You can either choose static or instance methods. Let's register the ones we defined earlier.

```csharp
var staticRegistration = MethodRegistration.FromStaticMethod(typeof(TransformerMethods), nameof(TransformerMethods.IsNotNull));
var instanceRegistration = MethodRegistration.FromInstanceMethod(nameof(TransformerMethods.ReverseString));
```
You'll notice that you don't have to provide an instance type for the instance method, just the name of the method itself. This is because the transformer will assume that you sent in an appropriate method context instance when it was created and will just use whatever you provide during method resolution. Include the registrations and an instance for your methods (if needed) when you create the `JoltJsonTransformer` instance.
```csharp
var transformer = JoltJsonTransformer.DefaultWith(transformerJson, new[] { staticRegistration, instanceRegistration }, new TransformerMethods());
```
That's it! You can pass in your source JSON document to the `Transform` method call just like before and collect the transformed result.
