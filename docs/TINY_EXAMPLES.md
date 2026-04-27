# Tiny Examples

Read this page when you want the smallest useful `Flow` examples without the larger application setup from the main guides.

These examples are intentionally small.
They are the quickest way to sanity-check the model before moving on to the longer docs.

## Plain `Result` In `flow {}`

```fsharp
let validatePort value =
    if value > 0 then Ok value else Error "port must be positive"

let workflow : Flow<unit, string, int> =
    flow {
        let! port = validatePort 8080
        return port
    }
```

## Read A Dependency From The Environment

```fsharp
type AppEnv = { Prefix: string }

let workflow : Flow<AppEnv, string, string> =
    flow {
        let! prefix = Flow.read _.Prefix
        return $"{prefix} world"
    }
```

## Bind A Cold Task Factory

```fsharp
let readText path : ColdTask<string> =
    fun ct -> System.IO.File.ReadAllTextAsync(path, ct)

let workflow : Flow<unit, string, string> =
    flow {
        let! text = readText "config.json"
        return text
    }
```

Use `ColdTask<'value>` when work should start only when the flow runs and should receive the runtime cancellation token.

## Keep A Task Boundary Explicit

```fsharp
let started = Task.FromResult 42

let workflow : Flow<unit, string, int> =
    started
    |> Flow.Task.fromHot
```

Use `fromHot*` for already-created task values.
Use `fromCold*` for task factories that should start at run time.

## Run The Flow Explicitly

```fsharp
let result =
    workflow
    |> Flow.toAsync () CancellationToken.None
    |> Async.RunSynchronously
```

`Flow` values are cold.
Nothing runs until you call `Flow.toAsync`.

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the first full walkthrough,
[`docs/FSTOOLKIT_MIGRATION.md`](./FSTOOLKIT_MIGRATION.md) if you are adopting `Flow` incrementally,
and [`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for the complete boundary-shape map.
