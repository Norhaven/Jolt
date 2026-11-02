### Jolt EBNF Grammar
### Derived from the Jolt language documentation and the parser in Jolt/Parsing.

### Notation used throughout:
###   =          defines a production rule
###   |          alternation (ordered: first match wins)
###   { … }      zero or more repetitions
###   [ … ]      optional (zero or one occurrence)
###   ( … )      grouping
###   " … "      terminal string / keyword
###   (* … *)    comment / annotation


### ══════════════════════════════════════════════════════════════════
### §1  TOP-LEVEL TRANSFORMER STRUCTURE
### ══════════════════════════════════════════════════════════════════

### A Jolt transformer is a JSON object whose keys and string values
### carry Jolt expressions.  Non-expression values (plain JSON objects,
### arrays, numbers, booleans, null) are passed through unchanged.

transformer
    = json-object
    ;

json-object
    = "{" [ property-entry { "," property-entry } ] "}"
    ;

property-entry
    = property-key ":" property-value
    ;

### Keys are JSON strings, but many encode a Jolt key-expression.
property-key
    = '"' key-expression '"'
    ;

### Values are either Jolt expressions (JSON strings), nested objects /
### arrays, raw JSON literals, or — when inside a using-key — a
### statement-block array.
property-value
    = '"' value-expression '"'     (* Jolt expression          *)
    | json-object                  (* nested transformer object *)
    | json-array                   (* template or literal array *)
    | statement-block              (* only valid under using-key *)
    | match-case-list              (* only valid under match-key *)
    | json-scalar                  (* passthrough literal        *)
    ;

json-array
    = "[" [ property-value { "," property-value } ] "]"
    ;

json-scalar
    = "true" | "false" | "null" | json-number
    ;


### ══════════════════════════════════════════════════════════════════
### §2  VALUE EXPRESSIONS
### ══════════════════════════════════════════════════════════════════

### An expression is a binary expression tree whose leaves are postfix
### expressions.  Unary logical-NOT is a prefix on any postfix expression.
### Pipe-chains and index/slice suffixes bind more tightly than any
### operator, including "!", and are attached directly to their source.

value-expression
    = null-coalesce-expr
    ;

null-coalesce-expr
    = logical-or-expr { "??" logical-or-expr }
    ;

logical-or-expr
    = logical-and-expr { "||" logical-and-expr }
    ;

logical-and-expr
    = equality-expr { "&&" equality-expr }
    ;

equality-expr
    = relational-expr { ( "==" | "!=" ) relational-expr }
    ;

relational-expr
    = additive-expr { ( "<=" | ">=" | "<" | ">" ) additive-expr }
    ;

additive-expr
    = multiplicative-expr { ( "+" | "-" ) multiplicative-expr }
    ;

multiplicative-expr
    = unary-expr { ( "*" | "/" ) unary-expr }
    ;

### Unary logical-NOT prefixes the entire postfix-expr, pipe chain included.
unary-expr
    = [ "!" ] postfix-expr
    ;

### A postfix-expr is a pipe-source optionally followed by a chain of
### piped method calls, or a primary that cannot start a pipe chain.
postfix-expr
    = pipe-source { pipe-suffix }
    | json-path                    (* $.some.path — not pipeable, see §10  *)
    | string-literal               (* 'text'                                *)
    | number-literal
    | boolean-literal
    | "null"
    ;

### The expressions a pipe chain can start from.
pipe-source
    = method-call [ index-suffix ] (* #m(…), #m(…)[…]                      *)
    | var-reference                (* @name, @name.prop, @name?.prop,
                                      @name[…]                              *)
    | array-literal                (* [ … ]   inline array                  *)
    | object-literal               (* { … }   inline object                 *)
    | "(" value-expression ")"
    ;


### ══════════════════════════════════════════════════════════════════
### §3  PIPE SUFFIX AND INDEX SUFFIX
### ══════════════════════════════════════════════════════════════════

### Pipe: threads the preceding result as the first argument of the
### next call.  When used as a pipe target, that first argument is
### omitted from the explicit argument list.  A piped call's result may
### itself be indexed or sliced before the chain continues.
pipe-suffix
    = pipe-operator "#" method-name "(" [ argument-list ] ")" [ index-suffix ]
    ;

pipe-operator
    = "->"     (* pipe: always calls the method, even with a null or
                  missing value                                        *)
    | "?->"    (* null-conditional pipe: when the value is null or
                  missing, skips this call and the rest of the chain,
                  and the chain gives null                             *)
    ;

### Index / slice: postfix bracket notation on a variable (§10) or on a
### method-call result, including a piped one.
index-suffix
    = "[" index-or-range "]"
    ;

index-or-range
    = range-expression
    ;


### ══════════════════════════════════════════════════════════════════
### §4  METHOD CALLS
### ══════════════════════════════════════════════════════════════════

method-call
    = "#" method-name "(" [ argument-list ] ")"
    ;

method-name
    = identifier
    ;

argument-list
    = argument { "," argument }
    ;

### An argument is one of: a general value expression, a lambda, or a
### bare range expression (for slice / substring parameters).
argument
    = lambda-expression
    | range-expression             (* only where a range is the sole argument *)
    | value-expression
    ;


### ══════════════════════════════════════════════════════════════════
### §5  LAMBDA EXPRESSIONS
### ══════════════════════════════════════════════════════════════════

### Lambdas carry one or two bound variables and a body expression.
### One-variable form:   @x : body
### Two-variable form:   @x ; @y : body
### The semicolon-separated pair is used for (accumulator ; current)
### lambdas such as #reduce and #zip, as well as two-parameter
### external Func<> lambdas.

lambda-expression
    = lambda-params ":" value-expression
    ;

lambda-params
    = var-name [ ";" var-name ]
    ;


### ══════════════════════════════════════════════════════════════════
### §6  RANGE / INDEX EXPRESSIONS
### ══════════════════════════════════════════════════════════════════

### Mirrors C### System.Range semantics.
### Examples:
###   1..3   slice from index 1 up to (not including) 3
###   ..2    from start to index 2
###   ^3..   from the third-from-last to end
###   ^1     single index from end (used for character / element access)
###   2      single index from start

range-expression
    = [ range-index ] ".." [ range-index ]   (* slice *)
    | range-index                             (* single index *)
    ;

range-index
    = [ "^" ] range-index-value
    ;

range-index-value
    = integer-literal
    | var-reference
    | method-call
    ;


### ══════════════════════════════════════════════════════════════════
### §7  PROPERTY-KEY EXPRESSIONS
### ══════════════════════════════════════════════════════════════════

### These productions are only valid on the left-hand (key) side of a
### JSON property entry.

key-expression
    = foreach-key          (* "#foreach(@x in …) into …"      *)
    | using-key            (* "#using(… as @x) into …"         *)
    | match-key            (* "#match(… as @x) into …"         *)
    | include-if-key       (* "#includeIf(…) into …"           *)
    | name-of-key          (* "#nameOf(@x)"                    *)
    | dual-position-key    (* "#joinWith(…)", "#valueOf(…)", … *)
    | match-arm-key        (* "#is(…)"  "#given(…)"  "#default()" *)
    | var-declaration-key  (* "@varName"                        *)
    | plain-string-key     (* ordinary JSON property name       *)
    ;

### ── §7.1  foreach ─────────────────────────────────────────────────

foreach-key
    = "#foreach" "(" foreach-binding "in" iterable-source ")"
      "into" output-target
    ;

### Binding: single variable, or value;index pair.
foreach-binding
    = var-name [ ";" var-name ]
    ;

iterable-source
    = json-path
    | var-reference
    ;

### ── §7.2  using ───────────────────────────────────────────────────

using-key
    = "#using" "(" using-source "as" var-name ")"
      "into" output-target
    ;

using-source
    = json-path
    | var-reference
    ;

### ── §7.3  match ───────────────────────────────────────────────────

match-key
    = "#match" "(" match-source "as" var-name ")"
      "into" output-target
    ;

match-source
    = json-path
    | var-reference
    ;

### ── §7.4  includeIf ───────────────────────────────────────────────

include-if-key
    = "#includeIf" "(" value-expression ")" "into" string-literal
    ;

### ── §7.5  nameOf ──────────────────────────────────────────────────

name-of-key
    = "#nameOf" "(" var-reference ")"
    ;

### ── §7.6  Dual-position methods ──────────────────────────────────

### Methods valid in both a property name and a property value; in a
### key, the result becomes the output property's name.  The arguments
### are value expressions as usual.
dual-position-key
    = "#" dual-position-method "(" [ argument-list ] ")"
    ;

dual-position-method
    = "valueOf" | "if" | "try" | "substring" | "joinWith"
    | "eval"                       (* UNSAFE, opt-in required *)
    ;

### ── §7.7  Match-arm keys (valid only inside a match-case-list) ────

match-arm-key
    = is-key
    | given-key
    | default-key
    ;

is-key
    = "#is" "(" is-pattern ")"
      [ "&&" "#given" "(" value-expression ")" ]  (* optional guard *)
    ;

given-key
    = "#given" "(" value-expression ")"
    ;

default-key
    = "#default" "(" ")"
    ;

### ── §7.8  Variable declaration (suppressed from output) ───────────

var-declaration-key
    = var-name
    ;

### ── §7.9  Plain JSON key ──────────────────────────────────────────

plain-string-key
    = string-literal
    ;

### ── §7.10  Shared: output-target ─────────────────────────────────

### "into" directs result to a named output property or an intermediate
### variable (which is then suppressed from the final JSON output).
output-target
    = string-literal   (* named output property — becomes a JSON key *)
    | var-name         (* intermediate variable, removed from output *)
    ;


### ══════════════════════════════════════════════════════════════════
### §8  PATTERN MATCHING
### ══════════════════════════════════════════════════════════════════

### The match-case-list is the array value associated with a match-key.
### Cases are evaluated top-to-bottom; the first truthy arm wins.

match-case-list
    = "[" [ match-case { "," match-case } ] "]"
    ;

### Each case is a single-property JSON object: key = arm, value = result.
match-case
    = "{" match-arm-key ":" property-value "}"
    ;

### ── §8.1  is-pattern ──────────────────────────────────────────────

### Type literals match by runtime kind.
### Array and object patterns allow structural matching with discards
### and capture variables.

is-pattern
    = type-literal
    | array-pattern
    | object-pattern
    | scalar-pattern
    ;

type-literal
    = "object"
    | "array"
    | "string"
    | "integer"
    | "decimal"
    | "boolean"
    | "null"
    ;

### Array patterns: empty [], wildcard [_], head+tail, fixed elements.
array-pattern
    = "[" [ array-pattern-element { "," array-pattern-element } ] "]"
    ;

array-pattern-element
    = discard              (* _ — matches any single element           *)
    | pattern-capture      (* @name — captures value into a variable   *)
    | scalar-pattern       (* 'text', 42, true, null — exact match     *)
    | array-pattern        (* nested structural match                  *)
    | object-pattern       (* nested structural match                  *)
    ;

### Object patterns: empty {}, property-presence, value matching.
### Unspecified properties are ignored (open-world assumption).
object-pattern
    = "{" [ object-pattern-pair { "," object-pattern-pair } ] "}"
    ;

object-pattern-pair
    = string-literal ":" object-pattern-value
    ;

object-pattern-value
    = discard
    | pattern-capture
    | scalar-pattern
    | array-pattern
    | object-pattern
    ;

### A scalar-pattern matches an exact value.
scalar-pattern
    = string-literal
    | number-literal
    | boolean-literal
    | "null"
    ;

### Discard: matches any value, binds nothing.
discard
    = "_"
    ;

### Capture: matches any value and binds it to the variable for use
### in the arm's result expression.  Variables are populated in
### left-to-right, top-to-bottom pattern order within the arm.
pattern-capture
    = var-name
    ;


### ══════════════════════════════════════════════════════════════════
### §9  STATEMENT BLOCKS  (value of a using-key)
### ══════════════════════════════════════════════════════════════════

### The value paired with a using-key is a JSON array of statement
### strings.  Statements are only valid in this position.

statement-block
    = "[" [ '"' statement '"' { "," '"' statement '"' } ] "]"
    ;

statement
    = remove-at-stmt
    | set-at-stmt
    | when-stmt
    ;

remove-at-stmt
    = "#removeAt" "(" var-path ")"
    ;

### set-at now accepts outer-scope variables and method calls as its
### value argument, not only literals.
set-at-stmt
    = "#setAt" "(" var-path "," set-at-value ")"
    ;

set-at-value
    = value-expression     (* includes var-references, method calls, literals *)
    ;

### when executes one or more statements conditionally.
when-stmt
    = "#when" "(" value-expression "," statement { "," statement } ")"
    ;

### A var-path is a scoped variable followed by dot-separated property
### steps (which may not yet exist — setAt creates missing nodes).
var-path
    = var-name { ( "." | "?." ) property-name }
    ;


### ══════════════════════════════════════════════════════════════════
### §10  VARIABLES AND PATHS
### ══════════════════════════════════════════════════════════════════

### A var-reference may be used as a standalone expression (the variable
### itself), dereferenced further with dot-steps and null-safe steps, or
### indexed / sliced.  Indexing applies to the variable itself and
### cannot be combined with dereference steps.  A single index gives one
### element or character ("@x[1]" on [8, null] gives null, so
### "@x[1]?->#m()" skips #m); a range always gives an array or string,
### even when it holds only one element ("@x[1..2]" gives [null]).
### Indexing a null variable is an error.

var-reference
    = var-chain
    | sliced-variable
    ;

var-chain
    = var-name { var-path-step }
    ;

sliced-variable
    = var-name index-suffix
    ;

### Dereferencing a property that doesn't exist gives null; dereferencing
### a property of a null or missing value is an error (JLT530) unless the
### step is null-safe.  As in C#, "?." guards only the value to its left,
### and when that value is null or missing it short-circuits the rest of
### the chain — further steps and any methods piped from it — giving null.
var-path-step
    = "." property-name       (* ordinary property dereference  *)
    | "?." property-name      (* null-safe property dereference *)
    ;

var-name
    = "@" identifier
    ;

### JSON Path — subset of RFC 9535 / Goessner path notation.  The path
### text is passed through to the JSON Path engine and runs until
### whitespace, "," or ")"; Jolt operators such as "?." and "->" are not
### recognized inside it.  To pipe from a path or guard it against null,
### wrap it: "#valueOf($.a)?->#trim()", "#valueOf($.a) ?? 'default'".
json-path
    = "$" { path-step }
    ;

path-step
    = "." identifier                          (* child          *)
    | ".." identifier                         (* recursive desc *)
    | "[" ( integer-literal | "*" | string-literal ) "]"
    ;


### ══════════════════════════════════════════════════════════════════
### §11  INLINE COMPLEX LITERALS
### ══════════════════════════════════════════════════════════════════

### Array and object literals may appear anywhere a value-expression
### is valid (including as arguments to method calls and as #transformWith
### parameter objects).  Array and object literals may also start a
### pipe chain (§3).

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


### ══════════════════════════════════════════════════════════════════
### §12  TERMINALS
### ══════════════════════════════════════════════════════════════════

identifier
    = letter { letter | digit | "_" }
    ;

### A dereferenced property name may also contain hyphens
### (e.g. "@x.first-name").  A "-" immediately followed by ">" is not
### part of the name; it starts a pipe ("@x.name->#trim()").
property-name
    = property-name-char { property-name-char }
    ;

property-name-char
    = letter | digit | "_" | "-"
    ;

### Jolt string literals use single quotes inside expression strings.
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
    = "A" | … | "Z" | "a" | … | "z"
    ;

digit
    = "0" | … | "9"
    ;

json-number
    = (* standard JSON number — integer or floating point *)
    ;


### ══════════════════════════════════════════════════════════════════
### §13  LIBRARY METHOD CATALOGUE  (non-normative)
### ══════════════════════════════════════════════════════════════════
#
### All methods are invoked via the method-call production (§4).
### "λ" = accepts a lambda-expression argument.
### "R" = accepts a range-expression argument.
### "?" = argument is optional.
#
### ── Existence / Type Testing ──────────────────────────────────────
###   valueOf(path)
###   exists(path|expr)
###   isNull(path|expr)
###   isMissing(path)
###   isInteger(path|expr)
###   isString(path|expr)
###   isDecimal(path|expr)
###   isBoolean(path|expr)
###   isArray(path|expr)
###   isObject(path|expr)
###   isEmpty(path|expr)
###   any(path|expr [, λ?])
#
### ── String Methods ────────────────────────────────────────────────
###   toUpperCase(path|expr)
###   toLowerCase(path|expr)
###   trim(path|expr)
###   trimStart(path|expr)
###   trimEnd(path|expr)
###   replace(path|expr, old, new)
###   startsWith(path|expr, prefix)
###   endsWith(path|expr, suffix)
###   substring(path|expr, R)
###   isRegexMatch(path|expr, pattern)
###   regexReplace(path|expr, pattern, replacement)
###   joinWith(path|expr, delimiter)
###   splitOn(path|expr, delimiter)
###   append(path|expr, value {, value})
###   reverse(path|expr)
###   indexOf(path|expr, value)
###   length(path|expr)
#
### ── Numeric / Conversion ──────────────────────────────────────────
###   toInteger(path|expr)
###   toDecimal(path|expr)
###   toBoolean(path|expr)
###   toString(path|expr)
###   roundTo(path|expr, places)
###   max(path|expr [, λ?])
###   min(path|expr [, λ?])
###   sum(path|expr [, λ?])
###   average(path|expr [, λ?])
#
### ── Date / Time ───────────────────────────────────────────────────
###   currentDateTime()
###   currentDateTimeUtc()
###   parseDateTime(path|expr [, format])
###   formatDateTime(path|expr, format)
###   addDays(path|expr, n)
###   addHours(path|expr, n)
###   addMinutes(path|expr, n)
#
### ── Array / Sequence ──────────────────────────────────────────────
###   slice(path|expr, R)
###   flatten(path|expr)
###   contains(path|expr, value)
###   distinct(path|expr [, λ?])
###   where(path|expr, λ)
###   select(path|expr, λ)
###   orderBy(path|expr [, λ?])
###   orderByDesc(path|expr [, λ?])
###   take(path|expr, count)
###   skip(path|expr, count)
###   takeWhile(path|expr, λ)
###   skipWhile(path|expr, λ)
###   reduce(path|expr, λ(@acc;@cur) [, seed])
###   zip(path|expr, path|expr, λ(@x;@y))
###   keysFrom(path|expr)
###   valuesFrom(path|expr)
###   groupBy(path|expr, λ)
###   summarizeWith(path|expr, λ)
###   merge(path|expr {, path|expr})
###   append(path|expr {, value})
#
### ── Control Flow ──────────────────────────────────────────────────
###   if(cond, then-expr, else-expr)
###   includeIf(cond) into 'name'  — key-position only
###   try(expr, λ(@e))
###   nameOf(@var)                 — key-position only
#
### ── Pattern Matching ─────────────────────────────────────────────
###   match(path|var as @x) into target  — key-position only
###   is(pattern)                         — match-arm key only
###   given(expr)                         — match-arm key only
###   default()                           — match-arm key only
#
### ── Mutation Statements (using-block only) ────────────────────────
###   removeAt(@x.path)
###   setAt(@x.path, value-expression)
###   when(cond, stmt {, stmt})
#
### ── Composition ───────────────────────────────────────────────────
###   transformWith(path|expr, 'name' [, object-literal])
###   foreach(binding in source) into target   — key-position only
###   using(source as @var) into target        — key-position only
###   eval(expr)                               — UNSAFE, opt-in required


### ══════════════════════════════════════════════════════════════════
### §14  OPERATOR PRECEDENCE  (normative — from the README table)
### ══════════════════════════════════════════════════════════════════
#
### Higher level = tighter binding.  The grammar in §2 encodes these
### levels directly through the nesting of expression non-terminals.
#
###  Level │ Operator(s)         │ Notes
###  ──────┼─────────────────────┼────────────────────────────────────
###    0   │  ??                 │ null-coalescing (lowest precedence;
###         │                     │ right side evaluated only when needed)
###    1   │  ||                 │ logical OR (short-circuits on true)
###    2   │  &&                 │ logical AND (short-circuits on false)
###    3   │  ==  !=             │ equality
###    4   │  <  >  <=  >=       │ relational
###    5   │  +  -               │ addition / subtraction
###         │                     │ (+  also overloaded as string concat)
###    6   │  *  /               │ multiplication / division
###    7   │  !  (unary prefix)  │ logical NOT (highest operator; negates
###         │                     │ a single postfix-expr, see §2)
#
### Not in the README table: the postfix forms "->", "?->", "." , "?."
### and "[…]" (§3, §10) are part of the operand itself, so they bind
### tighter than every operator above — "!@x->#isEmpty()" negates the
### result of the whole pipe chain.


### ══════════════════════════════════════════════════════════════════
### §15  RESERVED IMPLICIT VARIABLES
### ══════════════════════════════════════════════════════════════════
#
### @params  — injected automatically when a sub-transformer is invoked
###            via #transformWith(path, 'Name', { … }); contains the object
###            literal passed as the third argument.  Not user-declarable.


### ══════════════════════════════════════════════════════════════════
### §16  DESIGN NOTES AND OPEN QUESTIONS
### ══════════════════════════════════════════════════════════════════
#
### [N-1]  MULTI-VARIABLE LAMBDAS (NEW)
###        The updated README shows that lambda-params now officially
###        supports two variables separated by a semicolon:
###          @x;@y : body
###        for #reduce, #zip, and two-parameter external Func<,> methods.
###        The grammar in §5 captures this.  The previous revision only
###        documented single-variable lambdas in this position.
#
### [N-2]  TWO-PARAMETER EXTERNAL Func<> LAMBDAS (NEW)
###        External methods may declare a Func<T1,TResult> or
###        Func<T1,T2,TResult> parameter.  The latter uses the
###        @x;@y : body form.  Up to two generic type parameters are
###        currently supported; the docs note that higher arities may
###        be added in a future release.
#
### [N-3]  PATTERN MATCHING (NEW)
###        #match / #is / #given / #default form a complete new
###        key-position construct.  Several sub-features deserve
###        explicit callout:
#
###        (a) Guard clauses: #is(…) && #given(…) in the same key is
###            documented by example but the exact precedence of the
###            && within a key string (versus within an expression
###            string) should be clarified.  The grammar models it as
###            a special production on is-key rather than a plain
###            binary expression to make the intent unambiguous.
#
###        (b) Capture variables in patterns are populated left-to-right
###            within a single arm and are scoped to that arm's result
###            expression only.  They shadow outer variables of the
###            same name within the arm.
#
###        (c) Tail discard semantics: a trailing _ in an array pattern
###            means "one or more remaining elements", not "exactly one".
###            This is a notable departure from typical ML-style patterns
###            where _ matches exactly one value.
#
###        (d) Object patterns use an open-world assumption: extra
###            properties on the subject object are silently ignored.
#
### [N-4]  #setAt VALUE ARGUMENT EXTENDED (NEW)
###        The updated README shows that the value argument of #setAt
###        may now be an outer-scope variable (@newValue) or a full
###        method call (#valueOf(…)), not only a literal.  The grammar
###        uses value-expression to reflect this.
#
### [N-5]  #take AND #skip (NEW)
###        Simple count-based equivalents of #takeWhile / #skipWhile,
###        added to the library method table.
#
### [N-6]  #reduce AND #zip (NEW)
###        Both take two-parameter lambdas (@acc;@current and @x;@y
###        respectively).  #reduce also accepts an optional seed value
###        as a third argument.
#
### [N-7]  #keysFrom AND #valuesFrom (NEW)
###        Complement to the existing object-handling methods.
#
### [N-8]  #isObject (NEW)
###        Added alongside the existing #isArray, #isString, etc.
#
### [N-9]  AGGREGATE LAMBDA OVERLOADS (NEW)
###        #max, #min, #sum, #average now optionally accept a projection
###        lambda as a second argument, consistent with #where / #select.
#
### [N-10] DISAMBIGUATION: @x.otherComplexObject (FIXED)
###        The previous README contained "@.otherComplexObject" (missing
###        the variable name) in a #when example; the updated README
###        corrects this to "@x.otherComplexObject".  The grammar's
###        var-path production was already correct.
#
### [N-11] SHORT-CIRCUIT EVALUATION (CLARIFIED)
###        The updated README explicitly states that || short-circuits on
###        the first true and && short-circuits on the first false.  This
###        is a semantic property and does not change the grammar, but
###        is worth noting for implementors.
#
### [N-12] CAMEL-CASE SERIALIZATION OF EXTERNAL TYPES (CLARIFIED)
###        Custom C### types returned by external methods are serialized
###        using camel-case property names.  Grammar-neutral; relevant
###        to runtime implementors.
#
### [N-13] NULL-CONDITIONAL PIPE ?-> (NEW)
###        pipe-operator (§3) adds "?->" alongside "->".  When the piped
###        value is null or missing, the call and every later call in the
###        same chain are skipped and the chain gives null.  Skipping
###        stops at the end of the chain: when the chain is another
###        method's argument, that method is still called with null.
#
### [N-14] MORE PIPE SOURCES (NEW)
###        A pipe chain may now start from an object literal, a
###        parenthesized expression, a dereferenced variable property
###        ("@x.name->#trim()") or an indexed / sliced variable
###        ("@x[1..]?->#length()"), as well as the existing method calls,
###        variables and array literals.  pipe-source (§2) lists these.
###        JSON paths still cannot start a chain, and the pipe target
###        always begins with "#" (the previous revision omitted it from
###        pipe-suffix).
#
### [N-15] NULL-SAFE DEREFERENCE ALIGNED WITH C# (CHANGED)
###        "?." now short-circuits the rest of its chain, including any
###        methods piped from it, rather than only the next step.  Each
###        "?." still guards only the value to its left.  "?." is a
###        variable dereference only; the previous revision also listed
###        it as a JSON path step, which the parser does not support.
#
### [N-16] PROPERTY NAMES WITH HYPHENS (CLARIFIED)
###        Dereferenced property names use property-name (§12), which
###        allows "-", rather than identifier.
#
### [N-17] DUAL-POSITION METHODS (CORRECTED)
###        #valueOf, #if, #try, #substring and #eval are valid in a
###        property name as well as #joinWith; §7.6 now covers them all.
#
### [N-18] #transformWith, NOT #transform (CORRECTED)
###        The library registers the composition method as
###        #transformWith; §13 and §15 now use that name.  #orderBy and
###        #orderByDesc take an optional key lambda, and #valueOf takes
###        only a path.
