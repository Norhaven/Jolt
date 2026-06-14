# Jolt - A JSON Transformation Language

![Logo](https://raw.githubusercontent.com/Norhaven/Jolt/main/JoltLogo.png)

Welcome! This is an imperative JSON transformation language inspired by XSLT and the wonderful .Net JSON adaptation of it over at [JUST.Net](https://github.com/WorkMaze/JUST.net). This project provides an expression-based interpreter for the language and a highly extensible way of approaching the same problem, namely How To Transform JSON Into Different JSON.

There are two packages generally available for this: `Jolt.Json.Newtonsoft` and `Jolt.Json.DotNet`. These use `Newtonsoft` or `System.Text.Json` functionality, respectively. All of the packages are multitargeted and support `.Net Standard 2.0`, `.Net Standard 2.1`, `.Net 8.0`, and `.Net 10.0`. 

_<h6>The third package, which the first two depend on, is called `Jolt` and should only need to be directly referenced if you are building your own integration on top of it as the other packages have done.</h6>_

### Disambiguation
Were you looking for the much older and unrelated .Net port of the Java JSON transformation library (by coincidence, also called Jolt) instead? [That's over here!](https://github.com/blushingpenguin/Jolt.Net)

## Build/Release Status

[![Latest Build](https://github.com/Norhaven/Jolt/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/Norhaven/Jolt/actions/workflows/build-and-release.yml)

| Package Name | NuGet Version |
| ------------ | ------------- |
| Jolt | ![NuGet Version](https://img.shields.io/nuget/v/Jolt) |
| Jolt.Json.DotNet | ![NuGet Version](https://img.shields.io/nuget/v/Jolt.Json.DotNet) |
| Jolt.Json.Newtonsoft | ![NuGet Version](https://img.shields.io/nuget/v/Jolt.Json.Newtonsoft) |

## That's Great, But What Could I Use It For?

Let's say you have a third-party JSON-based REST API you need to call. Your data may already be in JSON, for instance in a NoSQL database, but the structure of your data doesn't match up with what the API endpoint is expecting. You need to transform your JSON into different JSON! That's an ideal use case for this project. You write a transformer in JSON that will take your data as input, make calls to any transformation business logic in your code to aid in the process, and provide you with the third-party API contract JSON as a result.

Any time you have JSON on hand that needs to be modified, whether it's merely adjusting the values and structure slightly or creating something completely different, that's an opportunity for a JSON transformer that uses Jolt. 

## Okay, So What Stops Me From Doing This Myself All In C#?

Absolutely nothing. If your changes are very minor, it may very well be easier and better to do that than to maintain transformer artifacts. 

I would suggest, however, that there will be some future headaches in doing that as your transformation needs grow. The largest pain points with restructuring JSON are ensuring that 1) The values and structure are easily readable, maintainable, and reusable across time, and that 2) Your business logic is not mixed in with the restructuring logic. It's incredibly easy, especially taking AI into account, to just throw some code together that walks the JSON tree and makes a new thing. It's incredibly difficult to deal with the aftermath of doing that and ensure that you and other developers can understand and work with what you've done.

Let's take a quick tour of the language features and you can decide if this fits your own use case.

# Syntax

The transformation language itself is pretty straightforward and primarily relies on defining and using methods to handle the transformation work needed, both as provided by the library within the package or external methods that you will create and register for use.

## Methods

All method calls begin with a `#` symbol and are case-sensitive. Open and close parentheses surround the method arguments, which are comma-separated. Let's take a look at an example from the library.
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
You will need to create a JSON transformer and register your methods with it prior to the actual transformation. There are two packages available by default: `Jolt.Json.Newtonsoft` and `Jolt.Json.DotNet`, which use either `Newtonsoft` or `System.Text.Json` as their particular flavor of implementation. Also, note that the `Jolt.Json.DotNet` package has an additional dependency, namely on [JsonPath.Net](https://github.com/json-everything/json-everything), due to there being no default JSON Path support out of the box in `System.Text.Json` as of yet. 

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

The usual basic math operators are implemented, namely addition, subtraction, multiplication, and division, using `+`, `-`, `*`, and `/` respectively, along with comparison and equality as `==`, `!=`, `>`, `>=`, `<`, and `<=`. Operations can be parenthesized as well. These are all used much like you're used to and can take the results of methods as operands. For example, assuming the following source JSON:
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
    "result": "#valueOf($.first) * #valueOf($.second) - #valueOf($.third) + #valueOf($.fourth) / #valueOf($.fifth) == 19"
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

# Logical Operations

The logical operators `&&` and `||` are also implemented for use in your transformers. They take boolean operands and return a boolean result. For example, assuming the following source JSON:
```json
{
    "first": true,
    "second": false,
    "third": true
}
```
The logical OR operator `||` will return `true` if at least one of the operands is `true`, and `false` otherwise. The logical AND operator `&&` will return `true` if both operands are `true`, and `false` otherwise. For example:
```json
{
    "orResult": "#valueOf($.first) || #valueOf($.second) || #valueOf($.third)",
    "andResult": "#valueOf($.first) && #valueOf($.second) && #valueOf($.third)",
    "combinedResult": "(#valueOf($.first) && #valueOf($.second)) || #valueOf($.third)"
}
```
The `orResult` property will evaluate to `true` because at least one of the operands is `true`, while the `andResult` property will evaluate to `false` because not all of the operands are `true`, and the `combinedResult` property will be `true` because the logical AND operators will be evaluated before the OR operator. The resulting JSON would look like this:
```json
{
    "orResult": true,
    "andResult": false,
    "combinedResult": true
}
```
And lastly, as you may be used to, you can also use a logical NOT operator `!` to negate a boolean value. For example:
```json
{
    "notFirst": "!#valueOf($.first)",
    "notSecond": "!#valueOf($.second)"
}
```

# Conditional Logic

The library also provides a few ways to approach conditional logic in your transformers. The first is the `#if` method, which takes three parameters: a boolean condition, an expression to evaluate when that condition is true, and an expression to evaluate when false. For example:
```json
{
    "result": "#if(#valueOf($.someValue) > 5, 'Greater than 5', 'Less than or equal to 5')"
}
```
The `result` property will evaluate to "Greater than 5" if the value of `$.someValue` is greater than 5, and "Less than or equal to 5" otherwise.

Secondly, there is the `#includeIf` method, which takes a path or boolean condition and will evaluate and include the property's object if true, returning null otherwise. For example:
```json
{
    "#includeIf($.someValue > 5) into 'result'": {
        "message": "Greater than 5"
    }
}
```
This will include the `result` property with the nested `message` property if the value of `$.someValue` is greater than 5, and will return null for `result` otherwise.

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
    "#foreach(@x in $.someArray) into @tempResult": [
        {
            "intermediateValue": "@x.someInteger * 2"
        }
    ],
    "#foreach(@x;i in @tempResult) into 'actualResult'": [
        {
            "finalValue": "@x.intermediateValue + 5",
            "currentIndex": "@i"
        }
    ]
}
```
The first loop will set the variable `@tempResult` to the value of the transformed array, which will use the provided content template to transform each array element it finds, then removes the node from the output. The second loop takes the variable results as input and loops over that intermediary array, applying additional transformation to the results, naming the final array property as `actualResult`. Note that you can optionally get both the current value at the current iteration as well as the index by declaring two variables separated by a semicolon, the left is the value and the right is the index.

You can also declare variables within the content template of a loop, but they only exist within it and on a per-iteration basis (meaning that the variable is redeclared, set, and thrown away every time through the loop). Let's see an example of that here.
```json
{
    "#foreach(@x in $.someArray) into 'finalResult'": [
        {
            "@scopedVar": "@x.someInteger / 2",
            "subtractedValue": "@scopedVar - 3"
        }
    ],
    "invalidValue": "@scopedVar"
}
```
In the resulting JSON output, the `finalResult` property will be populated by the array loop but the `invalidValue` property will be null due to accessing a variable which has already been destroyed. Variables of the same name as outer variables will shadow them, so you may want to avoid that.

Looping over an object's properties with a range variable is also possible, and in that case the variable will be set to the property value for each iteration. For example:
```json
{
    "#foreach(@x in $.someObject) into 'result'": [
        {
            "propertyValue": "@x"
        }
    ]
}
```
If you hardcode the property name like that, however, you will overwrite the same property with the current value each iteration through. You may want to keep the original property name as well in that case, and you can do that by using the `#nameOf` method with the range variable as its parameter, which will return the name of the property currently being iterated on. For example:
```json
{
    "#foreach(@x in $.someObject) into 'result'": [
        {
            "#nameOf(@x)": "@x"
        }
    ]
}
```

### Range Variables: Lambda Expressions

There is also another use for range variables, namely in lambda expressions. Several methods in the library will take a lambda in order to better filter and/or refine the JSON data, used in the form of declaring a range variable and a body expression separated by a colon `:`. The variable's lifetime is scoped to the lambda body and will not be accessible outside of it. Let's take a quick look at the methods which allow this usage.
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

### Range Variables: Indexing and Slicing

You are also welcome to use a range expression to index into a string or array at a specific point or range as shorthand for calling the `substring` or `slice` library methods. Let's say we have the following source JSON:
```json
{
    "text": "this is an example",
    "numbers": [ 1, 9, 3, 2, 6 ]
}
```
We can use the following transformer to obtain subsections of the string and array.
```json
{
    "@someText": "#valueOf($.text)",
    "@someNumbers": "#valueOf($.numbers)",
    "textStart": "@someText[..2]",
    "textMiddle": "@someText[1..3]",
    "textEnd": "@someText[^2..]",
    "textCharacter": "@someText[^1]",
    "arrayStart": "@someNumbers[..3]",
    "arrayMiddle": "@someNumbers[2..4]",
    "arrayEnd": "@someNumbers[^3..]",
    "arrayElement": "@someNumbers[1]"  
}
```
And that will create the resulting JSON:
```json
{
    "textStart": "th",
    "textMiddle": "hi",
    "textEnd": "le",
    "textCharacter": "e",
    "arrayStart": [ 1, 9, 3 ],
    "arrayMiddle": [ 3, 2 ],
    "arrayEnd": [ 3, 2, 6 ],
    "arrayElement": [ 9 ]
}
```
Range expressions immediately follow a variable and are enclosed in square brackets `[` and `]` which indicate indexing. They behave much the same as C# range expressions that you may already be used to, where a literal integer, range variable, or method return result indicates an offset index from the beginning of the string or array and the caret `^` indicates an index that is offset from the end of it. It's important to note that there are some standard library methods that take a range as a parameter, such as `substring` or `slice`, and in those cases you don't need to use square brackets and can specify the range expression as a first class citizen of the method call parameter, such as `#substring($.some.text, 2..5)` or `#slice($.some.array, ^3..)`.

It's also worth noting that you can directly index into a string or array result of a method without going through variables at all. For example, `#valueOf($.someString)[..2]` would give the first two characters of the string retrieved by the `valueOf` method.

### Range Variables: Using Blocks and Statements

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
    "#using($.some as @x) into 'result'": [
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

You can also conditionally execute statements within a `using` block by using the `#when` method, which takes a boolean expression and at least one statement method.
```json
{
    "#using($.some as @x) into 'result'": [
        "#when(#exists(@x.integerArray), #removeAt(@x.integerArray), #removeAt(@.otherComplexObject))",
        "#when(#valueOf(@x.complexObject.some.path) > 3, #setAt(@x.complexObject.some.other.path, 20))"
    ]
}
```
Lastly, you can take advantage of pre-processing variables with a `using` block much the same way as a `foreach` loop does by assigning the output to a variable instead of a named property, which can be used either in a subsequent `using` block or other valid variable uses.
```json
{
    "#using($.some.path as @x) into @tempResult": [
        "#removeAt(@x.integerArray)"
    ],
    "#using(@tempResult as @x) into 'actualResult'": [
        "#setAt(@x.some.path, 'text')"
    ]
}
```

# Error Handling

By default, if an error is encountered during the transformation process then the entire transformation will fail and an exception will be thrown. However, there are a few ways that you can handle errors within your transformers to allow for more graceful degradation of results in the case of unexpected input or other issues.
```json
{
    "default": "#try(#valueOf($.stringValue)->#toInteger(), @e: #valueOf($.defaultValue))",
    "nullOnError": "#try(#valueOf($.stringValue)->#toInteger(), @e: null)",
}
```
The `#try` method takes two parameters: the first is the expression to evaluate and the second is a lambda that takes an exception parameter and returns the value to use in the case of an error. In the example above, the `default` property will attempt to convert a string value to an integer and if it fails it will return the value of `$.defaultValue` instead, while the `nullOnError` property will return null in the case of any error.

# Creating Complex Literals

You can create arrays directly within your transformer by using square brackets `[` and `]` to enclose the array elements, which can be either literal values (including nested object and array literals), range variables, or the results of method calls. For example:
```json
{
    "@x": "#valueOf($.some.path)",
    "array": "[ 1, 'two', null, @x, #valueOf($.some.path) ]"
}
```
Creating complex objects directly within the transformer is also possible by using curly braces `{` and `}` to enclose the object properties and their values. The property name must be a string literal, whereas the property value can be literal values (including nested object and array literals), range variables, or method call results. For example:
```json
{
    "@x": "#valueOf($.some.path)",
    "object": "{ 'integerValue': 5, 'stringValue': 'test', 'booleanValue': true, 'rangeVariable': @x, 'methodCallResult': #valueOf($.some.path) }",
    "nestedObject": "{ 'nested': { 'array': [ 1, 2, 3 ], 'object': { 'key': @x } } }"
}
```
These features allow you to construct complex JSON structures directly within your transformer without needing to rely on the shape of the input JSON document. These are available anywhere an expression in a property value is supported.

# Additional Operations

As a final note, there are a few additional operations that you can take advantage of in your transformers that don't necessarily fit into the previous categories. The first is the null-coalescing operator `??` which returns the left-hand operand if it is not null, otherwise it returns the right-hand operand. For example:
```json
{
    "coalescingResult": "#valueOf($.some.nullableValue) ?? 'default value'"
}
```
You can also use the null-safe path dereferencing operator `?.` in your JSON paths to prevent null reference errors when traversing the source document, as below, which will return null when the property is null instead of throwing an error.
```json
{
    "@x": "#valueOf($.some.nullableObject.property)",
    "safeDereference": "@x.some.nullableObject?.property"
}
```

# Library Methods

This package comes with quite a few methods built into it to get you started, all of which are documented here and represent the most common things that you may want to do when transforming a JSON file. If you find that an opportunity for a new library method exists, please raise an issue and it will be considered. For more detail on the individual library methods, please see the Jolt wiki [over here.](https://github.com/Norhaven/Jolt/wiki)

Additionally, keep in mind that you can pass range variables as parameters into nearly all standard library methods. You can also use the piped method syntax directly off of a range variable, so for example the following transformer represents a valid use of both of those concepts together.

```json
{
    "@someVariable": "#valueOf($.some.arrayPath)",
    "result": "@someVariable->#select(@y: @y->length())"
}
```

| Method | Description | Example | Valid On
| ------ | ----------- | ------- | --------
| valueOf | Gets the value of a property at the specified path | `#valueOf($.some.path)` | Property Value
| exists | Returns true if the provided value is not null, false otherwise | `#exists($.some.path)` | Property Value
| isNull | Returns true if the provided value is null, false otherwise | `#isNull($.some.path)` | Property Value
| isMissing | Returns true if the provided path does not exist in the source document, false otherwise | `#isMissing($.some.path)` | Property Value
| if | Takes three parameters: a boolean condition, an expression to evaluate when that condition is true, and an expression to evaluate when false | `#if(#valueOf($.some.path), 'Yes', 'No')` | Property Value
| includeIf | Takes a path or boolean condition and will evaluate and include the property's object if true, returning null otherwise | `"#includeIf($.some.path) into 'someName'": { "nestedValue": "#valueOf($.other.path)" }` | Property Name
| eval | Evaluates an arbitrary expression, either from a path or literal value, and returns the result | `#eval('1 + 2 == 3')` | Property Name/Value, Unsafe*
| foreach | Evaluates a path and loops over the array elements or object properties it finds there to create its property values, naming the property as the string literal referred to by the arrow | `"#foreach(@x in $.some.path) into 'result'": [ { "templateValue": "#valueOf($.other.path)" } ]` | Property Name
| nameOf | Returns the name of the property being evaluated by the provided loop variable | `"#nameOf(@x)": "#valueOf($.some.path)"` | Property Name
| indexOf | Returns the zero-based index value of the first occurrence of the provided value | `#indexOf(#valueOf($.some.path), 'some string')` | Property Value
| length | Returns the length of a string or array value | `#length($.some.path)` | Property Value
| toUpperCase | Returns the uppercase version of a string value | `#toUpperCase($.some.path)` | Property Value
| toLowerCase | Returns the lowercase version of a string value | `#toLowerCase($.some.path)` | Property Value
| trim | Returns the string value with all leading and trailing whitespace removed | `#trim($.some.path)` | Property Value
| trimStart | Returns the string value with all leading whitespace removed | `#trimStart($.some.path)` | Property Value
| trimEnd | Returns the string value with all trailing whitespace removed | `#trimEnd($.some.path)` | Property Value
| replace | Returns the string value with all occurrences of a specified substring replaced with another value | `#replace($.some.path, 'old', 'new')` | Property Value
| startsWith | Returns true if a string value starts with the provided substring, false otherwise | `#startsWith($.some.path, 'start')` | Property Value
| endsWith | Returns true if a string value ends with the provided substring, false otherwise | `#endsWith($.some.path, 'end')` | Property Value
| substring | Returns the string value that falls within the provided range in a given string | `#substring($.some.path, 1..2)` | Property Value
| isRegexMatch | Returns true if a string value matches the provided regular expression pattern, false otherwise | `#isRegexMatch($.some.path, '^pattern$')` | Property Value
| regexReplace | Returns the string value with all occurrences of a specified regular expression pattern replaced with another value | `#regexReplace($.some.path, '^pattern$', 'new')` | Property Value
| slice | Returns the array slice that falls within the provided range in a given array | `#slice($.some.path, 1..2)` | Property Value
| reverse | Returns the string or array value with its elements in reverse order | `#reverse($.some.path)` | Property Value
| flatten | Returns a single array that is the result of recursively flattening an array of nested arrays | `#flatten($.some.path)` | Property Value
| groupBy | Returns a JSON object that represents the grouping of an array's contents by its individual property values | `#groupBy($.some.path, @x: @x.propertyName)` | Property Value
| summarizeWith | Returns an object array that's the result of applying an aggregate method to a grouped array's results | `#summarizeWith($.some.group, @seq: #someAggregateMethod(@seq))` | Property Value
| orderBy | Returns an array in ascending order as determined by its individual property values | `#orderBy($.some.path, @x: @x.propertyName)` | Property Value
| orderByDesc | Returns an array in descending order as determined by its individual property values | `#orderByDesc($.some.path, @x: @x.propertyName)` | Property Value
| takeWhile | Returns an array containing the leading elements of an array that satisfy a specified condition | `#takeWhile($.some.path, @x: @x.propertyName > 5)` | Property Value
| skipWhile | Returns an array excluding the leading elements of an array that satisfy a specified condition | `#skipWhile($.some.path, @x: @x.propertyName > 5)` | Property Value
| distinct | Returns an array of items associated with distinct values of a specified property or scalar values | `#distinct($.some.path, @x: @x.propertyName)` | Property Value
| contains | Returns true when an array or string contains the provided value | `#contains($.some.path, 'some string')` | Property Value
| roundTo | Returns the value of a provided number rounded to the specified decimal places | `#roundTo($.some.path, 2)` | Property Value
| try | Evaluates an expression and returns the result, or if an error is encountered it evaluates the provided lambda with the exception as a parameter and returns that result instead | `#try(#valueOf($.some.path)->#toInteger(), @e: 'default value')` | Property Value
| max | Returns the maximum value found within an array of numbers | `#max($.some.path)` | Property Value
| min | Returns the minimum value found within an array of numbers | `#min($.some.path)` | Property Value
| sum | Returns the total value found within an array of numbers | `#sum($.some.path)` | Property Value
| average | Returns the average value found within an array of numbers | `#average($.some.path)` | Property Value
| joinWith | Returns a string that joins all elements of an array with the provided delimiter | `#joinWith($.some.path, ',')` | Property Name/Value
| splitOn | Returns an array of substrings from a string value splitting on the provided delimiter | `#splitOn($.some.path, ',')` | Property Value
| append | Returns a string or array made from appending one or more strings or arrays onto them | `#append($.some.path, 'one', 'two')` | Property Value
| currentDateTime | Returns the current date and time as a string in ISO 8601 format | `#currentDateTime()` | Property Value
| currentDateTimeUtc | Returns the current date and time in UTC as a string in ISO 8601 format | `#currentDateTimeUtc()` | Property Value
| parseDateTime | Returns a string in ISO 8601 format that represents the date and time value of the provided string, requires exact format with optional format string | `#parseDateTime($.some.path, 'yyyy-MM-dd')` | Property Value
| formatDateTime | Returns a string that represents the date and time value of the provided string formatted according to the provided format string | `#formatDateTime($.some.path, 'yyyy-MM-dd')` | Property Value
| addDays | Returns a string in ISO 8601 format that represents the date and time value of the provided string with the specified number of days added to it | `#addDays($.some.path, 7)` | Property Value
| addHours | Returns a string in ISO 8601 format that represents the date and time value of the provided string with the specified number of hours added to it | `#addHours($.some.path, 5)` | Property Value
| addMinutes | Returns a string in ISO 8601 format that represents the date and time value of the provided string with the specified number of minutes added to it | `#addMinutes($.some.path, 30)` | Property Value
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
| merge | Returns an object that is the result of merging two or more objects together with a deep copy, with later values taking precedence over earlier ones | `#merge($.some.path, $.some.other.path)` | Property Value
| any | Returns true if the value is an array or string with contents, false otherwise (lambda parameter is optional) | `#any($.some.path)` | Property Value
| where | Returns an array of objects that match a predicate | `#where($.some.path, @x: @x.other.path > 2)` | Property Value
| select | Returns an array of objects that are the result of a projection | `#select($.some.path, @x: @x.other.path)` | Property Value
| transform | Returns an object that is the result of transforming a document sub-path using a standalone transformer | `#transform($.some.path, 'SomeNamedTransformer')` | Property Value
| using | Assigns a specific path to a range variable and allows statements to operate on it | `"#using($.some.path as @x) into 'result'":[ "#setAt(@x.other.path, 5)" ]` | Property Name
| removeAt | Removes a JSON node from the provided variable-based path | `#removeAt(@x.some.path)` | Statement
| setAt | Adds or modifies a JSON node specified with the provided variable-based path | `#setAt(@x.some.path, 5)` | Statement
| when | Conditionally executes a statement based on a boolean expression | `#when(#exists(@x.integerArray), #removeAt(@x.integerArray))` | Statement

<h6>* The `eval` method is considered unsafe because it can execute any expression, including ones that may have unwanted side effects or security implications. It should be used with caution and only with trusted input. In order to enable unsafe method usage, the `JoltOptions` instance that can be passed into your JoltJsonTransformer has a method called `WithUnsafeAllowed` that will enable this. Use with caution!</h6>

# Operator Precedence

Also, here is a small table of how Jolt regards levels of operator precedence for binary expressions to help you understand how your expressions may be evaluated at runtime. The higher the precedence level, the tighter it binds (e.g. `*` binds tighter than `+` and so multiplication is performed first).

| Operator | Description | Level
| -------- | ----------- | -----
| Null Coalescing | `??` | 0
| Logical Or | `||` | 1
| Logical And | `&&` | 2
| Equals | `==` | 3
| Not Equals | `!=` | 3
| Less Than | `<` | 4
| Greater Than | `>` | 4
| Less Than Or Equals | `<=` | 4
| Greater Than Or Equals | `>=` | 4
| Addition | `+` | 5
| Subtraction | `-` | 5
| Multiplication | `*` | 6
| Division | `/` | 6

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
That's it! You can pass in your source JSON document to the `transform` method call just like before and collect the transformed result.

## Transformer Registration

As you go, you may run into the issue where the transformer is ultimately too complex to reason about in a single file. In that case, you can break it up into multiple transformers and then call them from each other using the `#transform` method. This method takes a path to the sub-document to transform and the name of the transformer to use, which must be registered with the `JoltJsonTransformer` instance. Let's see how we can register a transformer and then call it from another one.

Registration is fairly straightforward, you need a name and the transformer JSON that will be used.
```csharp
var registration = new TransformerRegistration("PartialTransformer", partialTransformerJson);
context.RegisterTransformer(registration);
```
And then in your primary transformer, on the property value side you can call `#transform($.some.path, 'PartialTransformer')` to execute the registered transformer against the sub-document at `$.some.path` and return the result as the value of that evaluation.

Variables from the primary transformer will not be available in the referenced transformer, although it can receive a JSON object as parameters through the `#transform` method, which will store the object in an implicitly created variable called `@params` and allow access to the properties from there. This allows you to create more dynamic and reusable transformers that can be configured at runtime with different parameters. For example, you could have a transformer that formats a date according to a provided format string, and then call it with different format strings as needed. Here's an example using the object literal syntax.
```json
{
    "formattedDate": "#transform($.date, 'DateFormatter', { 'format': 'MM/dd/yyyy' })"
}
```
And the referenced transformer could access the `format` property from the `@params` variable like this:
```json
{
    "date": "#formatDateTime($.some.date, @params.format)"
}
```
This is definitely recommended for composability and readability as your transforms grow larger and become unwieldy.

# Validating Your Transformer Syntax

As a final note, transformers may prove difficult to test for correctness without actually obtaining a source document and running through it, which can take time and be specific to a source document. There is a way, however, to get a quick sanity check on your syntax and expression shapes and that's through the `Validate()` method on the `JoltTransformer`. First, you create the `JoltJsonTransformer` instance, using the `IJsonContext` that contains the transformer string you'd like to validate, and then call `Validate()` and iterate through the issues that come back, if any. This will not execute any evaluations against a source document, rather it would identify issues (such as syntax misuse) in how you've constructed your transformer.

# How To Contribute To This Project

If you happen to have the desire to help out with this (and I would love it if you do), there are several ways to do so that I'd recommend.

## Raising Issues

You are more than welcome to [raise an issue](https://github.com/Norhaven/Jolt/issues/new/choose) when you think you've found a bug, a mismatch between documentation and behavior, an idea for a new feature or library method that you'd like to see, or even just a question on intended behavior if it doesn't meet your expectations (there's always room to improve).

## Submitting Pull Requests

Please [raise an issue](https://github.com/Norhaven/Jolt/issues/new/choose) prior to submitting a PR so we can discuss and arrive at an ideal solution that you're then encouraged to help out with, namely so that you don't end up spending your time on fixing or adding something that may not turn out to be needed or intended in the end. I look forward to hearing from you!

## Writing Tests

Testing is crucial and also the thing that tends to be lacking in this project. Please refer to the documentation in this README, the ones in the [Tests](https://github.com/Norhaven/Jolt/tree/main/Tests) folder, and also the [Jolt Wiki](https://github.com/Norhaven/Jolt/wiki) to identify test cases that are missing or incorrect, and then [raise an issue](https://github.com/Norhaven/Jolt/issues/new/choose) (as per above) with your concerns and findings so that we can discuss and decide how to move forward.
