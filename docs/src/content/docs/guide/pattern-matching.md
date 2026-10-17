---
title: Pattern matching
description: Branching on the type and shape of data with #match, #is, #given and #default.
sidebar:
  order: 9
---

Most transformations need only what's covered so far and the library methods. When you need to decide what to output based on the **shape** of your data, use a [`#match`](../../reference/library/match/) expression.

A `#match` binds a value to a variable, then tries each case from top to bottom. The value of the **first** case that matches becomes the result.

## Type patterns

[`#is`](../../reference/library/is/) with a type name checks the value's JSON type:

```jolt
{
    "#match($.somePath as @x) into 'result'": [
        { "#is(object)": "'Found an object'" },
        { "#is(array)": "'Found an array'" },
        { "#is(string)": "'Found a string'" },
        { "#is(integer)": "'Found an integer'" },
        { "#is(decimal)": "'Found a decimal'" },
        { "#is(boolean)": "'Found a boolean'" },
        { "#is(null)": "'Found a null value'" }
    ]
}
```

## Arbitrary conditions

For more flexibility, [`#given`](../../reference/library/given/) evaluates any boolean expression. It can replace `#is` or be combined with it. [`#default()`](../../reference/library/default/) always matches, so use it as the last case to catch everything else.

```jolt
{
    "#match($.somePath as @x) into 'result'": [
        { "#given(#isObject(@x) && #valueOf($.someOther) > 5)": "'Found a specific object'" },
        { "#default()": "'Found something else'" }
    ]
}
```

## Array patterns

For finer control over arrays, `#is` accepts an array pattern.

```jolt
{
    "#match($.somePath as @x) into 'result'": [
        { "#is([])": "'Found an empty array'" },
        { "#is([_])": "'Found an array with one or more elements'" },
        { "#is(['test', _])": "'Found an array starting with test, followed by at least one more element'" },
        { "#is([_, _])": "'Found an array with at least two elements'" }
    ]
}
```

- `_` is a **discard**: it matches any value.
- Literal values such as `'test'` must match exactly.
- A discard at the **end** of an array pattern matches one or more remaining elements.

## Object patterns

Object patterns work the same way and can mix in discards.

```jolt
{
    "#match($.somePath as @x) into 'result'": [
        { "#is({})": "'Found an object with zero or more properties'" },
        { "#is({ 'someName': 'someValue' })": "'Found an object where someName is someValue'" },
        { "#is({ 'someName': { 'nested': _ } })": "'Found an object where someName has a nested property'" },
        { "#is({ 'someName': [_, 'someValue'] })": "'Found an object where someName is an array whose second element is someValue'" }
    ]
}
```

An object pattern requires the properties you list. The object may have any number of other properties.

## Binding variables in patterns

An undeclared variable inside an array or object pattern is set to the value at that position. Variables are bound in the order they appear, so one bound earlier in a case can be used later in the same case. Like lambda variables, they exist only within their case.

```jolt
{
    "#match($.somePath as @x) into 'result'": [
        { "#is({ 'name': @name, 'values': [@firstValue, _] })": "#append('Name is ', @name, ' and first value is ', @firstValue)" },
        { "#is({ 'function': @f }) && #given(#isObject(@f))": "'Found a function object'" }
    ]
}
```

<!--
  TODO(docs): The README doesn't specify:
  - whether `#is(...) && #given(...)` means case keys are general boolean expressions over match predicates;
  - whether a trailing `_` really means "one or more" (so `[_]` never matches `[]`), or zero-or-more;
  - how to escape a single quote inside a '...' string literal.
  Confirm against the parser and document here.
-->
