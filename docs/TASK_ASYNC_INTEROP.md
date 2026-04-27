# Task And Async Interop

This page lays out the task and async boundary shapes that `flow {}` can bind directly and
the ones that should stay explicit.

This page is about boundary shapes, not business logic. The goal is to help you choose the
right shape up front and avoid confusing compiler errors later.

## The Common Shapes

These shapes already have names:

- `Task<'value>`
- `Task<Result<'value, 'error>>`
- `Async<'value>`
- `Async<Result<'value, 'error>>`

These shapes are named by FsFlow because F# does not give them a useful built-in name:

- `ColdTask<'value>`
- `ColdTaskResult<'value, 'error>`

## Which Shapes Bind Directly

These bind directly inside `flow {}`:

- `Flow<'env, 'error, 'value>`
- `Result<'value, 'error>`
- `Async<'value>`
- `Async<Result<'value, 'error>>`
- `Task<'value>`
- `Task<Result<'value, 'error>>`
- `ColdTask<'value>`

Example:

```fsharp
let readAll path : ColdTask<string> =
    fun ct -> System.IO.File.ReadAllTextAsync(path, ct)

let workflow : Flow<unit, string, string> =
    flow {
        let! a = async { return "a" }
        let! b = Task.FromResult "b"
        let! c = readAll "config.json"
        return a + b + c
    }
```

## Cold Task Shapes

`ColdTask<'value>` is:

```fsharp
CancellationToken -> Task<'value>
```

Use it when work should start only when the flow runs and should observe the runtime
cancellation token.

Example:

```fsharp
let readAll path : ColdTask<string> =
    fun ct -> System.IO.File.ReadAllTextAsync(path, ct)
```

`ColdTaskResult<'value, 'error>` is:

```fsharp
CancellationToken -> Task<Result<'value, 'error>>
```

Use it when the cold boundary also returns typed failures.

Example:

```fsharp
let loadText path : ColdTaskResult<string, string> =
    fun ct ->
        task {
            let! text = System.IO.File.ReadAllTextAsync(path, ct)
            return Ok text
        }
```

## When To Stay Explicit

Use the explicit helpers when you want the boundary shape to stay visible:

- `Flow.Task.fromCold`
- `Flow.Task.fromColdResult`
- `Flow.Task.fromHot`
- `Flow.Task.fromHotResult`
- `Flow.Task.fromColdUnit`
- `Flow.Task.fromHotUnit`

`ColdTaskResult<'value, 'error>` stays explicit on purpose:

```fsharp
let workflow : Flow<unit, string, string> =
    loadText "config.json"
    |> Flow.Task.fromColdResult
```

That rule avoids ambiguity with `ColdTask<Result<'value, 'error>>`, which would otherwise
make builder overload resolution and compiler errors worse.

## Hot Task Shapes

Hot tasks are already started task values:

```fsharp
let started = Task.FromResult 42
```

Use hot task binding when you already have the task value on purpose:

```fsharp
flow {
    let! value = started
    return value
}
```

If you want the boundary to stay visible, use `Flow.Task.fromHot` or
`Flow.Task.fromHotResult`.

There is no separate `HotTask<'value>` alias because `Task<'value>` already names the shape.

## Async Shapes

Async shapes also bind directly:

```fsharp
flow {
    let! a = async { return 42 }
    let! b = async { return Ok 1 }
    return a + b
}
```

If you want to stay explicit, use:

- `Flow.fromAsync`
- `Flow.fromAsyncResult`

There is no special alias for async because `Async<'value>` and `Async<Result<'value, 'error>>`
already name those shapes directly.

## Choosing A Shape

Use:

- `ColdTask<'value>` when you define a new cancellation-aware task helper and want good DX in `flow {}`
- `ColdTaskResult<'value, 'error>` when the cold helper also returns typed failures
- `Task<'value>` or `Task<Result<'value, 'error>>` when you already have a started task value
- `Async<'value>` or `Async<Result<'value, 'error>>` when you are crossing an existing F# async boundary

If you are unsure, prefer the more explicit helper first and simplify later.

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the first examples, then
[`docs/TROUBLESHOOTING_TYPES.md`](./TROUBLESHOOTING_TYPES.md) when the compiler complains
about one of these shapes.
