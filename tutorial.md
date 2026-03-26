# Crispy Tutorial

This tutorial is a deeper guide to using Crispy as it exists in this repository today.

Crispy is a small expression-oriented scripting language built on the .NET DLR. It is designed to be embedded in a .NET host, interoperate naturally with .NET objects, and stay lightweight enough to use as a rules or scripting layer.

This document covers:

- how to run Crispy from C#
- how to use the REPL
- the language model and expression-oriented style
- variables, control flow, functions, closures, and defaults
- list and dictionary literals
- imports and multi-file scripts
- exceptions
- .NET interop and overload resolution
- current limitations

## 1. What Crispy Feels Like

The biggest thing to understand up front is that Crispy is expression-oriented.

That means:

- the last expression in a function is its return value
- the last expression in a block is the block value
- `if` branches produce values
- `return` exists, but you mainly use it for early exit

For example:

```crispy
defun classify(x) {
    if (x > 0) {
        'positive'
    } else {
        'zero-or-negative'
    }
}
```

There is no `return` here. The value of the chosen branch becomes the value of the `if`, and that becomes the value of the function.

## 2. Running Crispy From C#

For new code, use `CrispyRuntime`.

### Evaluate a snippet

```csharp
using System.Dynamic;
using Crispy;

var runtime = new CrispyRuntime(new[] { typeof(object).Assembly });
var scope = new ExpandoObject();

var result = runtime.ExecuteExpr(@"
    var x = 10
    var y = 32
    x + y
", scope);

Console.WriteLine(result); // 42
```

`ExecuteExpr`:

- parses the supplied text
- runs it in the provided module scope
- returns the value of the last expression

### Execute a file as a module

```csharp
var runtime = new CrispyRuntime(new[] { typeof(object).Assembly });
var module = runtime.ExecuteFile("examples/factorial.crispy");

var value = runtime.ExecuteExpr("factorial.run()", new ExpandoObject());
```

`ExecuteFile("path/to/file.crispy")`:

- executes the file in a fresh module scope
- registers that scope in `runtime.Globals`
- uses the file base name as the global name unless you provide one explicitly

So `examples/factorial.crispy` becomes available as `factorial`.

### Execute a file with a custom global alias

```csharp
var runtime = new CrispyRuntime(new[] { typeof(object).Assembly });
runtime.ExecuteFile("examples/text_tools.crispy", "text");

var output = runtime.ExecuteExpr("text.bracket('hello')", new ExpandoObject());
```

### Execute a file into an existing scope without registering it globally

```csharp
var runtime = new CrispyRuntime(new[] { typeof(object).Assembly });
var scope = new ExpandoObject();

runtime.ExecuteFileInScope("examples/text_tools.crispy", scope);
var output = runtime.ExecuteExpr("bracket('hello')", scope);
```

This is useful when you want file execution without mutating `runtime.Globals`.

### Compatibility wrapper

The old `Crispy` type still exists:

```csharp
var runtime = new Crispy.Crispy(new[] { typeof(object).Assembly });
```

That is a compatibility wrapper over `CrispyRuntime`. Prefer `CrispyRuntime` for new code.

## 3. Using The REPL

The repo now includes a standalone REPL:

```bash
dotnet run --project Crispy.Repl
```

When it starts, you get a persistent session scope. Definitions survive from one submission to the next:

```text
crispy> var x = 40
=> 40
crispy> x + 2
=> 42
```

If a submission looks incomplete, the prompt switches to a continuation prompt:

```text
crispy> defun add(a, b) {
.... a + b
.... }
=> <function>
crispy> add(3, 4)
=> 7
```

Supported REPL commands:

- `:help` shows help
- `:clear` discards the current multiline submission
- `:reset` resets the session runtime and scope
- `:scope` lists names introduced in the current session
- `:load <path>` loads a Crispy file as a module into the session
- `:quit` or `:exit` leaves the REPL

` :load ` uses the file base name as the module alias. For example:

```text
crispy> :load examples/text_tools.crispy
Loaded /.../examples/text_tools.crispy as text_tools.
crispy> text_tools.bracket('hello')
=> [[hello]]
```

## 4. Assemblies, Globals, and .NET Types

When you create the runtime, you give it assemblies:

```csharp
var runtime = new CrispyRuntime(new[] {
    typeof(object).Assembly,
    typeof(System.Dynamic.ExpandoObject).Assembly
});
```

Crispy walks the exported types from those assemblies and exposes them through `runtime.Globals` as a dynamic namespace tree.

That is why code like this works:

```crispy
var builder = new system.text.StringBuilder()
builder.Append('hello')
builder.ToString()
```

You do not have to pre-import `System.Text.StringBuilder` if the assembly was supplied to the runtime. The type is already reachable through the namespace tree.

## 5. Basic Syntax

### Comments

Crispy supports `//` line comments:

```crispy
// This is a comment.
var x = 10
```

### Strings

Both single-quoted and double-quoted strings are supported:

```crispy
'hello'
"world"
```

### Numbers

Numbers are parsed as .NET numeric values. In practice, Crispy handles the common integer and floating-point cases you would expect in scripts:

```crispy
1
3.14
10 + 5
```

### Booleans and null

```crispy
true
false
null
```

`null` is a normal first-class value. You can store it, return it, compare it, and place it in lists or dictionaries.

### Variables and assignment

Use `var` to introduce a local:

```crispy
var name = 'crispy'
var count = 3
```

Reassignment does not use `var`:

```crispy
var total = 0
total = total + 1
```

### Semicolons

Semicolons are optional:

```crispy
var x = 1
var y = 2;
x + y
```

### Blocks

Crispy supports both keyword-style and brace-style blocks.

Keyword style:

```crispy
if (x > 0) then
    'positive'
else
    'non-positive'
end
```

Brace style:

```crispy
if (x > 0) {
    'positive'
} else {
    'non-positive'
}
```

You can mix styles in the same codebase, though in practice it is best to be consistent within a given file.

## 6. Truthiness

Crispy truthiness is simple:

- `false` is falsey
- `null` is falsey
- everything else is truthy

That means `0`, empty strings, and empty collections are still truthy unless they are literally `false` or `null`.

Example:

```crispy
if (0) {
    'truthy'
} else {
    'falsey'
}
```

This evaluates to `'truthy'`.

## 7. Operators

### Arithmetic and comparison

```crispy
1 + 2
10 - 3
4 * 5
20 / 4
10 % 4

4 == 4
4 != 5
4 < 5
5 >= 5
```

### Logical operators

Symbolic forms:

```crispy
true && false
true || false
!false
```

Word forms:

```crispy
true and false
true or false
not false
4 eq 4
10 mod 4
5 xor 3
```

Supported word-form aliases are:

- `and`
- `or`
- `eq`
- `not`
- `mod`
- `xor`

### Bitwise operators

Crispy also supports:

- `&`
- `|`
- `^^`
- `~`
- `<<`
- `>>`

Example:

```crispy
var masked = 13 & 7
var shifted = (1 << 4) | (8 >> 2)
masked + shifted
```

### Not supported

Ternary `? :` is intentionally unsupported.

```crispy
true ? 1 : 2
```

This produces a parser error.

## 8. If Expressions

`if` is an expression, not only a control-flow statement.

```crispy
var message = if (score >= 60) {
    'pass'
} else {
    'fail'
}
```

Multiple branches work too:

```crispy
defun band(score) {
    if (score >= 90) {
        'A'
    } elseif (score >= 80) {
        'B'
    } elseif (score >= 70) {
        'C'
    } else {
        'D-or-lower'
    }
}
```

`elseif` is the supported keyword form.

## 9. Loops

### `loop`

`loop` is the low-level looping construct. It runs until you `break`.

```crispy
var index = 0
var total = 0

loop {
    if (index >= 5) {
        break
    }

    total = total + index
    index = index + 1
}

total
```

### `continue`

```crispy
var index = 0
var total = 0

loop {
    index = index + 1

    if (index == 2) {
        continue
    }

    if (index > 4) {
        break
    }

    total = total + index
}

total
```

### `foreach`

`foreach` is the structured iteration form:

```crispy
var total = 0

foreach (value in [1, 2, 3, 4]) {
    total = total + value
}

total
```

It works with common .NET enumerables, including:

- list literals
- `ArrayList`
- strings
- other .NET enumerable types

Nested `foreach`, `break`, and `continue` all work.

```crispy
var total = 0

foreach (group in [[1, 2, 3], [4, 5]]) {
    foreach (value in group) {
        if (value == 2) {
            continue
        }

        total = total + value
    }
}

total
```

## 10. Functions

### `defun`

Named functions are declared with `defun`:

```crispy
defun add(a, b) {
    a + b
}

add(3, 4)
```

The parser also accepts `function` as a legacy synonym, but `defun` is the style used throughout this repo.

### No-argument functions

```crispy
defun answer() {
    42
}
```

### Local variables inside functions

```crispy
defun total(a, b, c) {
    var partial = a + b
    partial + c
}
```

### Early return

Use `return` when you want to leave early:

```crispy
defun classify(x) {
    if (x > 0) {
        return 'positive'
    }

    'not-positive'
}
```

### Recursion

```crispy
defun factorial(n) {
    if (n <= 1) {
        1
    } else {
        n * factorial(n - 1)
    }
}
```

### Nested functions

Nested `defun` works and can capture outer variables:

```crispy
defun makeAccumulator(start, step) {
    var total = start

    defun next() {
        total = total + step
        total
    }

    next
}
```

## 11. Lambdas and First-Class Functions

Lambdas are introduced with `lambda(...) { ... }`.

```crispy
var add = lambda(a, b) {
    a + b
}

add(2, 3)
```

Functions are first-class values:

```crispy
defun add(a, b) {
    a + b
}

var fn = add
fn(10, 5)
```

Higher-order functions work naturally:

```crispy
defun apply(fn, value) {
    fn(value)
}

apply(lambda(x) { x * 3 }, 4)
```

Composition also works cleanly:

```crispy
defun compose(outer, inner) {
    lambda(value) {
        outer(inner(value))
    }
}
```

## 12. Closures

Closures are one of Crispy’s strongest features.

They can capture:

- module-level variables
- function locals
- loop locals
- imported aliases

### Capturing outer variables

```crispy
defun makeAdder(offset) {
    lambda(value) {
        value + offset
    }
}

var add5 = makeAdder(5)
add5(10)
```

### Capturing mutable state

```crispy
defun makeCounter(start) {
    var current = start

    lambda() {
        current = current + 1
        current
    }
}

var next = makeCounter(10)
next() + next()
```

### Capturing per-iteration values

Fresh loop and `foreach` locals can be captured correctly:

```crispy
var first = lambda() { 0 }
var second = lambda() { 0 }
var index = 0

foreach (value in [10, 20]) {
    if (index == 0) {
        first = lambda() { value }
    } else {
        second = lambda() { value }
    }

    index = index + 1
}

first() + second()
```

## 13. Default Parameters

Functions and lambdas support trailing default parameter values.

```crispy
defun total(base, step = 3, extra = step + 1) {
    base + step + extra
}

var add = lambda(value, extra = 2) {
    value + extra
}
```

Rules:

- arguments are positional only
- defaults are evaluated left to right
- a default may refer to earlier parameters
- a default may capture outer scope
- once one parameter has a default, all later parameters must also have defaults

Example with captured outer scope:

```crispy
defun makeAdder(offset) {
    lambda(value, extra = offset) {
        value + extra
    }
}
```

What is not supported:

- variadic parameters
- required parameters after optional parameters

These produce clear parser errors.

## 14. Lists and Dictionaries

Crispy has native literal syntax for mutable collections.

### Lists

```crispy
var values = [1, 2, 3]
```

List literals create `ArrayList` instances.

That means normal .NET members are available:

```crispy
var values = [1, 2]
values.Add(3)
values.Count
```

### Dictionaries

```crispy
var lookup = dict[
    'name': 'crispy',
    'count': 3
]
```

Dictionary literals create `Hashtable` instances.

### Indexing and assignment

```crispy
var values = [1, 2, 3]
values[1] = 20

var lookup = dict['x': 1]
lookup['y'] = values[1]
```

### Nesting

```crispy
var data = dict[
    'numbers': [1, 'two', null],
    'meta': dict['active': true, 'items': [10, 20]]
]

data['meta']['items'][1]
```

## 15. Objects and Dynamic Values

Because Crispy rides on the DLR, `ExpandoObject` works very naturally.

```crispy
var card = new system.dynamic.ExpandoObject()
card.Title = 'DLR'
card.Author = 'Crispy'
card.Title
```

You can also put functions on dynamic objects:

```crispy
var card = new system.dynamic.ExpandoObject()
card.Title = 'DLR'

card.Render = lambda() {
    card.Title
}

card.Render()
```

Delegate-valued and callable members can be invoked directly with normal call syntax:

```crispy
card.Render()
```

That applies both to instance members and static members on imported .NET types.

## 16. Imports and Modules

Crispy supports two common import patterns:

- importing a .NET namespace path
- importing a sibling Crispy script file

### Import a .NET namespace

```crispy
import system.collections as collections

defun makeList() {
    new collections.ArrayList()
}
```

### Import a sibling `.crispy` file

If `text_tools.crispy` is in the same directory:

```crispy
import text_tools as text

text.bracket('hello')
```

The runtime will look for modern `.crispy` files first and still supports legacy `.sympl` files.

### Top-level imports

A top-level import binds on the module object.

```crispy
import text_tools as text

defun run() {
    text.bracket('module scope')
}
```

### Local imports

A nested import is lexical and does not leak into module scope:

```crispy
defun makeFormatter(prefix) {
    import text_tools as text

    lambda(value) {
        text.bracket(prefix)
    }
}
```

That imported alias can still be captured by a closure.

## 17. Exceptions

Crispy supports:

- `throw`
- `try`
- `catch`
- `finally`

### Throwing and catching values

```crispy
try {
    throw 'boom'
} catch (err) {
    err
}
```

This evaluates to `'boom'`.

### Throwing and catching .NET exceptions

```crispy
try {
    throw new system.invalidOperationException('boom')
} catch (err) {
    err.Message
}
```

### `finally`

`finally` always runs:

```crispy
defun run() {
    var state = 0

    var value = try {
        10
    } finally {
        state = 1
    }

    state + value
}
```

### Bare rethrow

Inside a catch block, bare `throw` rethrows the current exception:

```crispy
try {
    try {
        throw new system.argumentException('bad')
    } catch (err) {
        throw
    }
} catch (outer) {
    outer.Message
}
```

### What the host sees

If Crispy throws a non-exception value and it escapes back into the host, it is wrapped in `CrispyThrownValueException`.

If Crispy throws a real .NET exception, that exception flows out unchanged.

## 18. .NET Interop

Interop is one of Crispy’s main reasons to exist.

### Constructing objects

```crispy
var builder = new system.text.StringBuilder()
```

### Accessing properties and fields

```crispy
builder.Length
system.environment.NewLine
```

### Calling methods

```crispy
builder.Append('hello')
builder.ToString()
```

### Static calls

```crispy
system.int16.Parse('3')
system.string.Concat('a', 'b')
```

### Overload resolution

Crispy’s overload resolution now behaves predictably:

- exact type matches win first
- assignable matches come next
- common numeric widening comes after that

For example, if a method has both `int` and `long` overloads, an `Int16` argument prefers the `int` overload.

### Important interop limits

These are intentional and documented:

- Crispy does not auto-fill omitted optional .NET parameters
- generic .NET methods are unsupported through Crispy call syntax
- `ref` / `out` .NET methods are unsupported through Crispy call syntax
- ambiguous overloads raise a clear `InvalidOperationException`

So this works:

```crispy
system.int16.Parse('3')
```

But this style is not supported if the target API requires omitted optional parameters, generic method inference, or `ref`/`out`.

## 19. Injecting Host Objects

You can also inject concrete host instances when you construct the runtime.

```csharp
var runtime = new CrispyRuntime(
    new[] { typeof(object).Assembly },
    new object[] { myModel, myService });
```

Injected instance methods become callable from script.

Depending on how you want to write the script, you can use them:

- directly by method name
- or through the injected object’s type name namespace

Examples from the current test suite:

```crispy
UpperCaseName()
ReadSales()
MetricsModel.ReadVolume()
```

This is useful when you want to expose a narrow, host-controlled API into Crispy without forcing the script to construct everything itself.

## 20. Line-Break and Formatting Rules That Matter

Most whitespace is flexible, but one parser rule matters in practice:

- a newline before `.`, `(`, or `[` ends the current expression

So this is fine:

```crispy
builder.Append('hello')
values[0]
fn(1, 2)
```

And this is fine because the call or index starts on the same logical line:

```crispy
builder.Append(
    'hello'
)
```

But this is not treated as a continuation:

```crispy
builder
    .Append('hello')
```

Likewise:

```crispy
values
    [0]
```

and:

```crispy
fn
    (1, 2)
```

are treated as new statements, not as a continuation of the previous line.

The safe rule is simple: keep `.`, `(`, and `[` attached to the expression they continue.

## 21. Worked Example

This example brings together functions, closures, collections, iteration, imports, and .NET interop.

```crispy
import system.collections as collections

defun range(start, stop) {
    var values = new collections.ArrayList()
    var current = start

    loop {
        if (current > stop) {
            break
        }

        values.Add(current)
        current = current + 1
    }

    values
}

defun map(values, projector) {
    var result = []

    foreach (value in values) {
        result.Add(projector(value))
    }

    result
}

defun makeScaler(factor) {
    lambda(value, offset = 1) {
        value * factor + offset
    }
}

defun run() {
    var values = range(1, 5)
    var scaled = map(values, makeScaler(3))

    var summary = dict[
        'count': scaled.Count,
        'first': scaled[0],
        'last': scaled[scaled.Count - 1]
    ]

    summary['count'] + summary['first'] + summary['last']
}
```

This example shows:

- constructing a .NET `ArrayList`
- building a range with `loop`
- using `foreach` to map over values
- closures with default arguments
- list and dictionary literals
- .NET member access on Crispy-created values

## 22. Current Limits

The current runtime is capable, but there are still explicit boundaries:

- ternary `? :` is unsupported
- variadic parameters are unsupported
- Crispy does not auto-fill omitted optional .NET parameters
- generic .NET methods are unsupported through Crispy call syntax
- `ref` / `out` .NET methods are unsupported through Crispy call syntax
- the parser and runtime are the authoritative language definition; there is no separate maintained formal grammar file

## 23. Where To Look Next

If you want to learn by reading code:

- start with [README.md](./README.md) for the short project overview
- inspect [examples](./examples) for runnable scripts
- look at [Crispy.Tests](./Crispy.Tests) for precise language behavior

Good files to start with:

- `examples/counter_factory.crispy`
- `examples/literals.crispy`
- `examples/scoped_imports.crispy`
- `examples/dynamic_card.crispy`
- `examples/pipeline.crispy`
- `Crispy.Tests/FunctionTest.cs`
- `Crispy.Tests/ProgramTest.cs`
- `Crispy.Tests/OverloadResolutionTest.cs`

If you want a practical first script to write, start with this:

```crispy
defun run() {
    var values = [1, 2, 3, 4]
    var total = 0

    foreach (value in values) {
        total = total + value
    }

    total
}
```

Then add one feature at a time:

- turn `run` into a closure factory
- move logic into another file and `import` it
- replace the list literal with a .NET collection
- wrap part of the computation in `try` / `catch`
