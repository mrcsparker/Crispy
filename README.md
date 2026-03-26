Crispy
======

## Introduction

Crispy is a small expression-oriented scripting language that targets the .NET DLR. The codebase now builds and tests on `.NET 10`.

Crispy compiles down to .NET bytecode, so it is very, very fast.

It was developed as a prototype for a rules language.

For a longer language and embedding walkthrough, see [tutorial.md](./tutorial.md).

## Build and test

The repository is pinned to the SDK in [global.json](./global.json).

```bash
dotnet build Crispy.slnx /p:UseSharedCompilation=false /m:1
dotnet test Crispy.slnx /p:UseSharedCompilation=false /m:1
```

GitHub Actions CI runs the same build and test steps on Linux, macOS, and Windows using [ci.yml](./.github/workflows/ci.yml).

## REPL

Run the interactive REPL with:

```bash
dotnet run --project Crispy.Repl
```

Built-in commands:

+ `:help`
+ `:clear`
+ `:reset`
+ `:scope`
+ `:load <path>`
+ `:quit`

## Runtime API

For new code, prefer `CrispyRuntime`:

```csharp
var runtime = new CrispyRuntime(new[] { typeof(object).Assembly });
var result = runtime.ExecuteExpr("1 + 2", new ExpandoObject());
```

The older `Crispy` runtime type name still exists as a compatibility wrapper for existing callers.

## Examples

Sample scripts live in [examples](./examples) and are executed by the test suite.

Crispy is expression-oriented:

+ The last expression in a function, lambda, `if` branch, or block is the value it returns.
+ `return` is mainly for early exit, not the normal way to produce a result.
+ Closures, nested functions, and higher-order functions are first-class patterns.
+ Member access, method calls, and object construction go through the .NET DLR, so `ExpandoObject`, imported .NET types, and injected host objects all participate naturally.
+ `null` is a first-class literal and can be stored, compared, assigned, and returned like any other value.
+ Lists use `[...]` and dictionaries use `dict[...]`, and both produce real mutable .NET collections.
+ `loop` supports both `break` and `continue`.
+ `foreach (item in sequence) { ... }` is the structured iteration form for enumerables.
+ `try` / `catch` / `finally` and `throw` are available as value-producing control flow.
+ Functions and lambdas support trailing default parameters with positional argument binding.
+ `import` can be used in local scopes, where it binds a lexical local instead of leaking into module scope.
+ Bitwise operators `&`, `|`, `^^`, `~`, `<<`, and `>>` are supported; ternary `? :` is not.
+ Word-form aliases are supported for `and`, `or`, `eq`, `not`, `mod`, and `xor`.
+ `obj.Member(...)` and `Type.Member(...)` also work when `Member` is a delegate-valued property or field.
+ .NET interop overload resolution now prefers exact matches, then assignable matches, then common numeric widening, and raises clear errors for ambiguous, optional, generic, and `ref`/`out` cases.

Representative examples:

+ Classic algorithms: `factorial.crispy`, `fibonacci.crispy`, `gcd.crispy`
+ Fun scripts: `collatz.crispy`, `fizzbuzz.crispy`, `pyramid.crispy`
+ Advanced language usage: `compose.crispy`, `counter_factory.crispy`, `defaults.crispy`, `exceptions.crispy`, `foreach_totals.crispy`, `literals.crispy`, `memoized_fibonacci.crispy`, `pipeline.crispy`, `scoped_imports.crispy`, `scoreboard.crispy`
+ DLR and operator examples: `bitwise.crispy`, `dynamic_card.crispy`, `text_tools.crispy`, `word_operators.crispy`

## License

Crispy uses a MIT License, which means that you can do what you want with it:

+ Use it to learn about the .NET DLR
+ Use it in your FOSS program
+ Use it in your proprietary program

Please send me any bug fixes or enhancements.

## Syntax

Crispy supports both keyword-style and brace-style blocks.

An example Crispy syntax would be:

	if (time == now) then
		send("ready")
	endif

	if (1 > 2) then
		if (a or b) then
			...
		endif
	elsif (3 < 4) then
		...
	else
		...
	endif
	
Note that you can use if/then/else/elsif/endif or `{`, `}`.  Crispy doesn't really care how you like to group your expressions.

The other syntax has functions, variables, loops, arrays, lambda expressions, first class functions, namespaces, closures, and .NET integration.

Lists and dictionaries now have literal syntax:

```crispy
var values = [1, 2, 3]
var lookup = dict['name': 'crispy', 'count': 3]

values[1] = 20
lookup['count'] = values.Count
```

List literals create `ArrayList` instances, and `dict[...]` creates `Hashtable` instances, so existing indexing, assignment, and .NET member invocation work naturally.

Structured iteration uses `foreach`:

```crispy
var total = 0

foreach (value in [1, 2, 3, 4]) {
    if (value == 2) {
        continue
    }

    total = total + value
}
```

`foreach` works with common .NET enumerables such as list literals, `ArrayList`, and strings.

Exceptions use `try`, `catch`, `finally`, and `throw`:

```crispy
var result = try {
    throw 'boom'
} catch (err) {
    err
} finally {
    cleanup()
}
```

Exception mapping:

+ `throw` on a .NET exception throws that exception as-is.
+ `throw` on any other value wraps it in `CrispyThrownValueException` if it escapes to host code.
+ Inside `catch (err)`, Crispy gives you the original thrown value for Crispy-thrown values, or the exception object for .NET exceptions.
+ Bare `throw` inside a catch block rethrows the current exception.

Functions and lambdas can declare trailing default parameters:

```crispy
defun total(base, step = 3, extra = step + 1) {
    base + step + extra
}

var add = lambda(value, extra = 2) {
    value + extra
}

total(10)   // 17
total(10, 5) // 21
add(4)      // 6
```

Default-argument rules:

+ Arguments are positional only.
+ Default values are evaluated left to right.
+ A default value may refer to earlier parameters and outer scope.
+ Once a parameter has a default value, all following parameters must also have defaults.
+ Variadic parameters are not supported.

Imports can also be local to a function or block:

```crispy
defun makeFormatter(prefix) {
    import text_tools as text

    lambda(value) {
        var builder = new system.text.StringBuilder()
        builder.Append(text.bracket(prefix))
        builder.Append(' -> ')
        builder.Append(text.bracket(value))
        builder.ToString()
    }
}
```

Scoped-import rules:

+ A top-level `import` still binds on the module object.
+ A nested `import` binds in the current lexical scope and can be captured by closures.
+ Nested imports shadow outer names without mutating the module scope.
+ Re-importing a file-based module reuses the already loaded module object after the first load.

Additional operator rules:

+ `&`, `|`, `^^`, `~`, `<<`, and `>>` use .NET bitwise semantics.
+ `and` / `or` and `&&` / `||` remain the short-circuit logical operators.
+ Bitwise operators bind tighter than comparisons, so `5 & 3 == 1` evaluates as `(5 & 3) == 1`.
+ `:` is supported as the separator inside `dict[...]` literals.
+ Ternary `? :` is not supported and produces a parser error.
+ Supported word-form aliases are `and`, `or`, `eq`, `not`, `mod`, and `xor`.

Examples:

+ `not false`
+ `10 mod 4`
+ `5 xor 3`
+ `4 eq 4`

Delegate-valued members:

+ If `Member` resolves to a normal method, Crispy invokes the method as before.
+ If `Member` resolves to a property or field whose value is a delegate or other callable object, Crispy reads the member and invokes that value.
+ This works for both instance members and static members on imported .NET types.
+ Non-callable members still raise an invocation error when called like functions.

.NET overload resolution:

+ Crispy chooses overloaded .NET methods and constructors by preferring exact type matches first, then assignable/interface matches, then common numeric widening conversions.
+ Numeric widening covers common implicit .NET conversions such as `Int16` to `Int32`, `Int64`, `Double`, or `Decimal`.
+ Omitted optional .NET parameters are not filled automatically; pass all arguments explicitly.
+ Ambiguous overloads raise an `InvalidOperationException` that lists the competing signatures.
+ Generic methods and `ref` / `out` methods are not supported through Crispy call syntax.

## Current Limits

+ Ternary `? :` is intentionally unsupported.
+ Variadic parameters are unsupported.
+ Crispy does not fill omitted optional .NET parameters automatically; pass every argument explicitly.
+ Generic .NET methods and `ref` / `out` method parameters are unsupported through Crispy call syntax.
+ The parser and runtime are the authoritative language definition; there is no separate maintained grammar spec in the repo.

## Larger Example

```crispy
defun mapEach(fn, values) {
    var results = []

    foreach (value in values) {
        results.Add(fn(value))
    }

    results
}

defun run() {
    var totals = mapEach(lambda(value, offset = 1) {
        value + offset
    }, [1, 2, 3, 4])

    var summary = dict[
        'count': totals.Count,
        'last': totals[totals.Count - 1]
    ]

    summary['count'] + summary['last']
}
```
