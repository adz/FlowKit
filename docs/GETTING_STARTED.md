# Getting Started

This page shows the fastest path from plain `Result` code to the right FsFlow family as the execution context grows.

The core `FsFlow` package contains `Flow` and `AsyncFlow`.
`FsFlow.Net` adds `TaskFlow` for `.NET` task-oriented boundaries.

## 1. Start With The Continuum

FsFlow is meant to scale one Result-based style through richer boundaries:

```text
Validate -> Result -> Flow -> AsyncFlow -> TaskFlow
```

Start as small as possible, then lift only when the boundary truly needs more runtime context.

## 2. Start With Pure Validation

Here `validateName` stays an ordinary `Result` helper:

```fsharp
open FsFlow.Validate

type ValidationError =
    | MissingName

let validateName (name: string) : Result<string, ValidationError> =
    name
    |> okIfNotBlank
    |> orElse MissingName
```

FsFlow works best when this kind of code stays plain until there is a real need to add runtime concerns.

## 3. Choose The Smallest Honest Boundary

Use:

- `Flow<'env, 'error, 'value>` when the computation itself is synchronous
- `AsyncFlow<'env, 'error, 'value>` when the computation is naturally `Async`-based
- `TaskFlow<'env, 'error, 'value>` when the computation is naturally `.NET Task`-based

Pick the family that matches the honest boundary of the code you are writing.
Avoid `TaskFlow` just because one helper somewhere happens to use `Task`.
Avoid `Flow` if the boundary is mainly async work with sync wrappers around it.

## 4. Use `Flow` For Synchronous Boundaries

Use `Flow` when the computation needs dependencies and typed failure, but no async runtime.
The validation code stays exactly the same:

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

## 5. Use `AsyncFlow` For `Async`-Based Boundaries

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

## 6. Use `TaskFlow` For `.NET Task`-Based Boundaries

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

## 7. Read From The Environment

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

## 8. Compose Upward, Not Sideways

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
        let! validName = validateGreeting input
        let! prefix = TaskFlow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

Keep the smallest honest computation at each boundary, then lift it only when the outer runtime really changes.
Inside the computation expression, prefer direct binds like this over `TaskFlow.fromFlow`.

## 9. Keep The Semantic Boundary Clear

`Validate`, `Result`, `Flow`, `AsyncFlow`, and `TaskFlow` are short-circuiting.
They are for ordered workflows that stop on the first typed failure.

If you need accumulated validation, keep that explicit with a dedicated validation type or a bridge to a validation library.
FsFlow does not currently provide applicative accumulated validation in `Validate` or the flow builders.
Do not assume that a flow builder is trying to merge independent failures.

## 10. What To Read Next

Read [`docs/VALIDATE_AND_RESULT.md`](./VALIDATE_AND_RESULT.md) for the validation-first story.
Read [`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for the direct binding surface in `asyncFlow {}`
and `taskFlow {}`, then [`docs/ENV_SLICING.md`](./ENV_SLICING.md) for environment design, then
[`docs/examples/README.md`](./examples/README.md) for reference examples.
