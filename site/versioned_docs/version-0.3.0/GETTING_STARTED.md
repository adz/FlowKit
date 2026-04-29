# Getting Started

This page shows the fastest path from plain `Result` code to the right FsFlow family for a real application boundary.

The core `FsFlow` package contains `Flow` and `AsyncFlow`.
`FsFlow.Net` adds `TaskFlow` for `.NET` task-oriented boundaries.

## 1. Choose The Computation Family First

Use:

- `Flow<'env, 'error, 'value>` when the computation itself is synchronous
- `AsyncFlow<'env, 'error, 'value>` when the computation is naturally `Async`-based
- `TaskFlow<'env, 'error, 'value>` when the computation is naturally `.NET Task`-based

Pick the family that matches the honest boundary of the code you are writing.
Avoid `TaskFlow` just because one helper somewhere happens to use `Task`.
Avoid `Flow` if the boundary is mainly async work with sync wrappers around it.

## 2. Start With Pure Code

Here `validateName` stays an ordinary `Result` helper:

```fsharp
type ValidationError =
    | MissingName

let validateName (name: string) =
    if System.String.IsNullOrWhiteSpace name then
        Error MissingName
    else
        Ok name
```

FsFlow can sit at the application boundary, not replace ordinary pure code.

## 3. Use `Flow` For Synchronous Boundaries

Use `Flow` when the computation needs dependencies and typed failure, but no async runtime:

```fsharp
type AppEnv =
    { Prefix: string }

let greet input : Flow<AppEnv, ValidationError, string> =
    flow {
        let! name = validateName input
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {name}"
    }
```

Run a `Flow` synchronously:

```fsharp
let result =
    greet "Ada"
    |> Flow.run { Prefix = "Hello" }
```

Choose `Flow` when:

- the computation body is sync
- you want the smallest representation
- carrying a runtime `CancellationToken` would be noise

## 4. Use `AsyncFlow` For `Async`-Based Boundaries

Use `AsyncFlow` when the computation itself is built around F# `Async`:

```fsharp
type AsyncEnv =
    { Prefix: string
      LoadName: int -> Async<string> }

let greetAsync userId : AsyncFlow<AsyncEnv, ValidationError, string> =
    asyncFlow {
        let! loadName = AsyncFlow.read _.LoadName
        let! loadedName = loadName userId
        let! validName = validateName loadedName
        let! prefix = AsyncFlow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

Run an `AsyncFlow` through `Async`:

```fsharp
let result =
    greetAsync 42
    |> AsyncFlow.toAsync
        { Prefix = "Hello"
          LoadName = fun _ -> async { return "Ada" } }
    |> Async.RunSynchronously
```

Choose `AsyncFlow` when:

- the surrounding code already uses `Async`
- the core package can stay free of `.NET Task` concepts
- `Async` is the natural runtime for the computation

## 5. Use `TaskFlow` For `.NET Task`-Based Boundaries

Use `TaskFlow` when the computation is task-oriented end to end:

```fsharp
type TaskEnv =
    { Prefix: string
      LoadName: int -> Task<string> }

let greetTask userId : TaskFlow<TaskEnv, ValidationError, string> =
    taskFlow {
        let! loadName = TaskFlow.read _.LoadName
        let! loadedName = loadName userId
        let! validName = validateName loadedName
        let! prefix = TaskFlow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

Run a `TaskFlow` through `Task`:

```fsharp
let result =
    greetTask 42
    |> TaskFlow.toTask
        { Prefix = "Hello"
          LoadName = fun _ -> Task.FromResult "Ada" }
        CancellationToken.None
    |> Async.AwaitTask
    |> Async.RunSynchronously
```

Choose `TaskFlow` when:

- the boundary is `.NET Task`
- task interop is central to the code path
- runtime cancellation can be part of execution

## 6. Read From The Environment

Each computation family has the same environment pattern:

- `Flow.read` / `Flow.env`
- `AsyncFlow.read` / `AsyncFlow.env`
- `TaskFlow.read` / `TaskFlow.env`

Use the projected form when you only need one dependency:

```fsharp
let greetWithPrefix input : Flow<AppEnv, ValidationError, string> =
    flow {
        let! name = validateName input
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {name}"
    }
```

Use the whole environment only when you genuinely need it:

```fsharp
let describe : AsyncFlow<AsyncEnv, ValidationError, string> =
    asyncFlow {
        let! env = AsyncFlow.env
        return env.Prefix
    }
```

## 7. Compose Upward, Not Sideways

The computation families are ordered from smaller to larger runtime commitments:

- `Flow` is the sync base
- `AsyncFlow` can lift `Flow`
- `TaskFlow` can lift both `Flow` and `AsyncFlow`

That means small sync boundaries can stay sync and be reused inside async or task-oriented boundaries:

```fsharp
let validateGreeting input : Flow<AppEnv, ValidationError, string> =
    flow {
        let! name = validateName input
        return name
    }

let greetTaskValidated input : TaskFlow<TaskEnv, ValidationError, string> =
    taskFlow {
        let! validName =
            validateGreeting input
            |> TaskFlow.fromFlow

        let! prefix = TaskFlow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

Keep the smallest honest computation at each boundary, then lift it only when the outer runtime really changes.

## 8. What To Read Next

Read [`docs/INTEGRATIONS.md`](./INTEGRATIONS.md) if you already use FsToolkit, Validus, IcedTasks, or FSharpPlus.
Read [`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for the direct binding surface in `asyncFlow {}`
and `taskFlow {}`, then [`docs/ENV_SLICING.md`](./ENV_SLICING.md) for environment design, then
[`docs/examples/README.md`](./examples/README.md) for reference examples.
