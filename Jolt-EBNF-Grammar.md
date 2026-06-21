# Jolt EBNF Grammar

Derived from the Jolt language documentation. Jolt expressions are embedded inside
JSON string values and JSON property-name keys. The grammar below describes the
expression sub-language that lives inside those quoted strings.

Notation conventions used here:
- `=`         defines a production rule
- `|`         alternation
- `{ … }`     zero or more repetitions
- `[ … ]`     optional (zero or one)
- `( … )`     grouping
- `" … "`     terminal string literal
- `' … '`     terminal string literal (alternate quotes, for readability)
- `(* … *)`   comment / annotation

---

## 1  Top-Level Transformer Structure

```ebnf
(* A Jolt transformer is a JSON object whose keys and values
   carry Jolt expressions. *)

transformer
    = json-object
    ;

json-object
    = "{" { property-entry [ "," ] } "}"
    ;

property-entry
    = property-key ":" property-value
    ;

(* Keys are ordinary JSON strings, but many are Jolt expressions. *)
property-key
    = '"' key-expression '"'
    ;

(* Values are ordinary JSON scalars, objects, arrays, or Jolt expressions
   (always encoded as JSON strings). *)
property-value
    = '"' value-expression '"'
    | json-object
    | json-array
    | json-literal
    | statement-block         (* only inside a using-block *)
    ;

json-array
    = "[" { ( property-value | value-expression ) [ "," ] } "]"
    ;

json-literal
    = "true" | "false" | "null" | json-number
    ;
```

---

## 2  Expressions (Property Values)

Expressions can appear in property values (and in some property keys).
An expression is built from primaries, binary operators, pipe-chains,
index/slice suffixes, and the unary logical-NOT operator.

```ebnf
value-expression
    = unary-expression { binary-operator unary-expression }
    ;

unary-expression
    = [ "!" ] primary { pipe-suffix } { index-suffix }
    ;

primary
    = method-call
    | var-reference { var-path-step }
    | json-path
    | array-literal
    | object-literal
    | string-literal
    | number-literal
    | boolean-literal
    | "null"
    | "(" value-expression ")"
    ;

(* The pipe-suffix threads the preceding result into the first argument
   of the next method call. *)
pipe-suffix
    = "->" method-call-name "(" [ argument-list ] ")"
    ;

(* Index and slice suffixes can follow a primary or a pipe result. *)
index-suffix
    = "[" index-or-range "]"
    ;
```

---

## 3  Binary Operators and Precedence

Operators are listed lowest-to-highest precedence (higher level = tighter binding).

```ebnf
binary-operator
    = null-coalesce-op        (* level 0: ??            *)
    | logical-or-op           (* level 1: ||            *)
    | logical-and-op          (* level 2: &&            *)
    | equality-op             (* level 3: == !=         *)
    | comparison-op           (* level 4: < > <= >=     *)
    | additive-op             (* level 5: + -           *)
    | multiplicative-op       (* level 6: * /           *)
    ;

null-coalesce-op    = "??" ;
logical-or-op       = "||" ;
logical-and-op      = "&&" ;
equality-op         = "==" | "!=" ;
comparison-op       = "<" | ">" | "<=" | ">=" ;
additive-op         = "+" | "-" ;
multiplicative-op   = "*" | "/" ;
```

---

## 4  Method Calls

```ebnf
method-call
    = "#" method-name "(" [ argument-list ] ")"
    ;

(* When used as a pipe target the callee omits its first argument. *)
method-call-name
    = "#" method-name
    ;

method-name
    = identifier
    ;

argument-list
    = argument { "," argument }
    ;

argument
    = value-expression
    | lambda-expression
    | range-expression        (* for slice/substring *)
    ;
```

---

## 5  Lambda Expressions

Lambdas are used as arguments to higher-order methods (`where`, `select`,
`any`, `orderBy`, `groupBy`, `try`, etc.).

```ebnf
lambda-expression
    = var-name ":" value-expression
    ;
```

---

## 6  Range / Index Expressions

```ebnf
(* A range expression denotes a C#-style index or slice. *)
range-expression
    = [ range-index ] ".." [ range-index ]   (* slice: start..end *)
    | range-index                             (* single index      *)
    ;

range-index
    = [ "^" ] range-index-value
    ;

range-index-value
    = integer-literal
    | var-reference
    | method-call
    ;

index-or-range
    = range-expression
    ;
```

---

## 7  Property-Key Expressions

Several constructs are only valid as a property *key* (left-hand side of a
JSON key-value pair).

```ebnf
key-expression
    = var-declaration-key       (* "@varName"                  *)
    | foreach-key               (* "#foreach(…) into …"        *)
    | using-key                 (* "#using(…) into …"          *)
    | include-if-key            (* "#includeIf(…) into …"      *)
    | name-of-key               (* "#nameOf(@x)"               *)
    | join-with-key             (* "#joinWith(…)"              *)
    | plain-key                 (* ordinary JSON property name *)
    ;

(* Assigning the result of an expression to a variable and
   suppressing the property from the output. *)
var-declaration-key
    = var-name
    ;

(* foreach loops over an array or object. *)
foreach-key
    = "#foreach" "(" foreach-binding "in" ( json-path | var-reference ) ")"
      "into" output-target
    ;

foreach-binding
    = var-name [ ";" var-name ]   (* value [; index] *)
    ;

(* using blocks scope a path/variable to a range variable. *)
using-key
    = "#using" "(" ( json-path | var-reference ) "as" var-name ")"
      "into" output-target
    ;

(* includeIf conditionally includes the associated value object. *)
include-if-key
    = "#includeIf" "(" value-expression ")" "into" string-literal
    ;

(* nameOf returns the property name of the current loop variable. *)
name-of-key
    = "#nameOf" "(" var-reference ")"
    ;

(* joinWith can appear in either position. *)
join-with-key
    = "#joinWith" "(" argument-list ")"
    ;

plain-key
    = string-literal
    ;

(* The "into" target is either a named output property or a variable. *)
output-target
    = string-literal   (* named output property *)
    | var-name         (* intermediate variable, suppressed from output *)
    ;
```

---

## 8  Statement Blocks (inside `using` keys)

The value associated with a `using-key` is a JSON array of statement strings.

```ebnf
statement-block
    = "[" { '"' statement '"' [ "," ] } "]"
    ;

statement
    = remove-at-statement
    | set-at-statement
    | when-statement
    ;

remove-at-statement
    = "#removeAt" "(" var-path ")"
    ;

set-at-statement
    = "#setAt" "(" var-path "," value-expression ")"
    ;

(* when conditionally executes one or more statements. *)
when-statement
    = "#when" "(" value-expression "," statement { "," statement } ")"
    ;

(* A var-path is a variable reference followed by dot-path steps. *)
var-path
    = var-name { "." identifier }
    ;
```

---

## 9  Variables and Paths

```ebnf
var-reference
    = var-name { var-path-step }
    ;

var-path-step
    = ( "." | "?." ) identifier   (* ordinary or null-safe dereference *)
    | index-suffix
    ;

var-name
    = "@" identifier
    ;

(* JSON Path as used by Jolt — a subset of the JSONPath spec. *)
json-path
    = "$" { path-step }
    ;

path-step
    = "." identifier              (* child property *)
    | ".." identifier             (* recursive descent *)
    | "[" ( integer-literal | "*" | string-literal ) "]"
    | "?." identifier             (* null-safe child *)
    ;
```

---

## 10  Inline Complex Literals

Both array and object literals may appear anywhere a `value-expression` is
expected (inside a property-value string).

```ebnf
array-literal
    = "[" [ literal-element { "," literal-element } ] "]"
    ;

literal-element
    = value-expression
    ;

object-literal
    = "{" [ object-literal-pair { "," object-literal-pair } ] "}"
    ;

object-literal-pair
    = string-literal ":" value-expression
    ;
```

---

## 11  Terminals

```ebnf
identifier
    = letter { letter | digit | "_" }
    ;

string-literal
    = "'" { any-char-except-single-quote } "'"
    ;

number-literal
    = integer-literal
    | decimal-literal
    ;

integer-literal
    = [ "-" ] digit { digit }
    ;

decimal-literal
    = [ "-" ] digit { digit } "." digit { digit }
    ;

boolean-literal
    = "true" | "false"
    ;

letter
    = "A" … "Z" | "a" … "z"
    ;

digit
    = "0" … "9"
    ;
```

---

## 12  Library Method Reference (non-normative)

The built-in methods recognised by the interpreter are listed below.
All are invoked via the `method-call` production above.

### Value / Scalar Methods
`valueOf`, `exists`, `isNull`, `isMissing`, `eval`, `length`, `indexOf`,
`contains`, `startsWith`, `endsWith`, `isEmpty`, `any`, `isInteger`,
`isString`, `isDecimal`, `isBoolean`, `isArray`, `isRegexMatch`

### String Methods
`toUpperCase`, `toLowerCase`, `trim`, `trimStart`, `trimEnd`, `replace`,
`regexReplace`, `substring`, `joinWith`, `splitOn`, `append`, `reverse`,
`toString`

### Numeric / Math Methods
`toInteger`, `toDecimal`, `toBoolean`, `roundTo`, `max`, `min`, `sum`, `average`

### Date / Time Methods
`currentDateTime`, `currentDateTimeUtc`, `parseDateTime`, `formatDateTime`,
`addDays`, `addHours`, `addMinutes`

### Array / Object Methods
`slice`, `flatten`, `reverse`, `append`, `groupBy`, `summarizeWith`,
`orderBy`, `orderByDesc`, `takeWhile`, `skipWhile`, `distinct`, `where`,
`select`, `merge`

### Control-Flow Methods
`if`, `includeIf`, `try`, `nameOf`

### Composition Methods
`transform`     — delegates to a named registered sub-transformer  
`foreach`       — array/object iteration (used as a key-expression)  
`using`         — scoped mutation block (used as a key-expression)  
`when`          — conditional statement (used inside a statement-block)  
`removeAt`      — mutation statement  
`setAt`         — mutation / creation statement  

### Unsafe Methods *(require explicit opt-in via `JoltOptions.WithUnsafeAllowed()`)*
`eval`

---

## 13  Design Notes

The following points are worth flagging from a language-design perspective:

1. **Object literal syntax in `#transform` calls.** The `#transform` method
   accepts an optional third argument that is an inline object literal
   (`{ 'key': value }`). This is the same `object-literal` production and is
   already covered, but worth calling out explicitly since it is the only
   built-in method whose third argument is a structured literal rather than a
   scalar.

2. **`@params` implicit variable.** The name `@params` is a reserved implicit
   variable injected by the runtime when a sub-transformer is invoked via
   `#transform`. It is not user-declarable and should be noted as a keyword in
   the reserved-name set.

3. **Null-safe `?.` on `var-path-step`.** The grammar allows `?.` inside
   variable path steps but at the moment is not permitted inside `json-path` steps.

4. **Statement scope restriction.** The `statement` production is only valid
   inside a `statement-block` (the array value of a `using-key`). Attempting to
   use `#removeAt` or `#setAt` outside this context is a semantic (not syntactic)
   error; the grammar cannot express this restriction without attribute grammars
   or a separate context check.
```
