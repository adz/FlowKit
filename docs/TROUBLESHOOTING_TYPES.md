# Troubleshooting Types

This page explains the wrapper-shape mismatches that usually sit behind `flow {}` compiler
errors.

Most of the time, the problem is not that Flow is doing something exotic. The problem is that the compiler sees one wrapper shape and you intended another.

## Error: A Unique Overload For Method `Bind` Could Not Be Determined

This usually happens when the compiler cannot tell whether a `let!` value is:

- `Async<'T>`
- `Async<Result<'T, 'Error>>`

Example:

```fsharp
let nested : Async<Async<Result<int, string>>> =
    async {
        return async { return Ok 42 }
    }

let workflow : Flow<unit, string, int> =
    flow {
        let! next = nested
        let! value = next
        return value
    }
```

The second `let!` is ambiguous.

Fix it by adding a type annotation:

```fsharp
let workflow : Flow<unit, string, int> =
    flow {
        let! next = nested
        let! (value: int) = next
        return value
    }
```

## Error: This Expression Was Expected To Have Type `Flow<...>` But Here Has Type `Task<...>`

That usually means a `.NET Task` value crossed the boundary without being lifted through `Flow.Task`.

Example:

```fsharp
flow {
    let! gateway = Flow.read _.Gateway
    return! gateway.Load()
}
```

If you already have a started `Task<'T>`, it can bind directly:

```fsharp
flow {
    let! response = gateway.Load()
    return response
}
```

If you want the boundary to stay explicit, lift the task through `Flow.Task`:

```fsharp
flow {
    let! gateway = Flow.read _.Gateway
    return!
        gateway.Load()
        |> Flow.Task.fromHot
}
```

If the task should start only when the flow runs, prefer a cold task factory instead:

```fsharp
flow {
    let! load = Flow.read _.Load
    let! response = load
    return response
}
```

That works when `load` has the shape `ColdTask<'value>`, which means:

```fsharp
CancellationToken -> Task<'value>
```

If you want the boundary to stay explicit, this is still valid:

```fsharp
flow {
    let! load = Flow.read _.Load
    return!
        load
        |> Flow.Task.fromCold
}
```

## Error: The Type `CancellationToken` Does Not Match The Type You Passed To `Flow.Task`

Cold task helpers expect a factory:

```fsharp
CancellationToken -> Task<'T>
```

So this is correct:

```fsharp
load
|> Flow.Task.fromCold
```

And this is a different shape:

```fsharp
let startedTask = load CancellationToken.None
startedTask |> Flow.Task.fromHot
```

Use:

- `fromCold` when you have a function that needs the runtime token
- `fromHot` when you already have a started `Task<'T>`

The same rule applies to:

- `fromColdResult` / `fromHotResult`
- `fromColdUnit` / `fromHotUnit`

`ColdTaskResult<'value, 'error>` stays explicit through `fromColdResult`.

## Error: The Flow Requires A Different Environment Type

This usually means you wrote a smaller flow against one env type and are trying to run it inside a larger env.

Example:

```fsharp
type SmallEnv = { Prefix: string }
type BigEnv = { App: SmallEnv; RequestId: string }

let greet : Flow<SmallEnv, string, string> =
    flow {
        let! prefix = Flow.read _.Prefix
        return $"{prefix} world"
    }
```

If you want to run it in `BigEnv`, derive the smaller local env from the outer one:

```fsharp
let greetInBigEnv : Flow<BigEnv, string, string> =
    greet |> Flow.localEnv _.App
```

## Error: Disposal Or Cleanup Does Not Fit The Flow Shape

For normal resource lifetime management, prefer `use` and `use!` directly in `flow {}`:

```fsharp
flow {
    use scope = new RequestScope()
    return 42
}
```

```fsharp
flow {
    use! scope = openScope
    return 42
}
```

Use `Flow.Runtime.useWithAcquireRelease` only when you need custom acquire/release behavior that does not fit plain builder `use`.

## When Type Errors Usually Mean An API Boundary Problem

If the compiler error mentions one of these shapes, check the boundary first:

- `Result<...>`
- `Async<...>`
- `Async<Result<...>>`
- `Task<...>`
- `Task<Result<...>>`
- `Flow<...>`

Most fixes are one of:

- add a type annotation to disambiguate `let!`
- lift `Task` values through `Flow.Task`
- derive a smaller local environment with `Flow.localEnv`
- move back to plain `Result` until the real boundary appears

## Next

Read [`examples/FsFlow.MaintenanceExamples/Program.fs`](../examples/FsFlow.MaintenanceExamples/Program.fs) for runnable versions of these shapes, then [`docs/SEMANTICS.md`](./SEMANTICS.md) for exact runtime behavior.
