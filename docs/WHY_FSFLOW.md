# Why FsFlow

Read this page when you want to decide whether `Flow` is a better fit than manual environment threading, `Async<Result<_,_>>`, or `Task<Result<_,_>>` for ordinary F# application code.

FsFlow is aimed at a specific F# problem:

- a use case needs dependencies
- validation already returns `Result`
- one or more async or task boundaries show up
- expected failures should stay in the type

At that point, many codebases end up with one of these shapes:

```fsharp
AppEnv -> Result<'value, 'error>
AppEnv -> Async<Result<'value, 'error>>
Task<Result<'value, 'error>>
```

Those shapes work, but they tend to spread the same concerns across several layers:

- dependency threading
- wrapper-shape adaptation
- helper modules for mapping and binding
- extra noise around the happy path

FsFlow gives that combined shape one explicit representation:

```fsharp
Flow<'env, 'error, 'value>
```

This is a DX layer over the primitives F# and .NET already provide.

- it composes `Result`, `Async`, and `Task`
- it does not replace them with a new runtime model
- it stays explicit about env access and execution

## Before And After

### Manual Dependency Threading Plus `Result`

```fsharp
type AppEnv =
    { Prefix: string }

type AppError =
    | MissingName

let validateName name =
    if System.String.IsNullOrWhiteSpace name then
        Error MissingName
    else
        Ok name

let greet env name =
    result {
        let! validName = validateName name
        return $"{env.Prefix} {validName}"
    }
```

This is still the right shape when the code is small and mostly pure.

Once the same workflow also needs async work or more dependencies, `Flow` keeps the same
success-path style without introducing a second workflow for environment access:

```fsharp
let greet name : Flow<AppEnv, AppError, string> =
    flow {
        let! validName = validateName name
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

The important part is that `validateName` still returns a plain `Result`. You do not have
to wrap it in a separate result-specific abstraction first.

### `Async<Result<_,_>>` Plus Helpers

```fsharp
let fetchUser userId : AppEnv -> Async<Result<User, AppError>> =
    fun env ->
        async {
            match validateUserId userId with
            | Error error -> return Error error
            | Ok validId ->
                let! result = env.LoadUser validId |> Async.AwaitTask
                return result |> Result.mapError GatewayFailed
        }
```

This works, but the shape gets harder to read as retries, timeout, cancellation, cleanup,
and additional environment access show up.

The same workflow in `Flow` keeps the happy path in one CE:

```fsharp
let fetchUser userId : Flow<AppEnv, AppError, User> =
    flow {
        let! validId = validateUserId userId
        let! loadUser = Flow.read _.LoadUser
        let! user =
            loadUser validId
            |> Flow.Task.fromHotResult
            |> Flow.mapError GatewayFailed
        return user
    }
```

That is the core pitch of the library: one computation expression for dependencies, typed
failures, and the common application wrapper shapes without pushing the code into a full
Reader-style stack.

## Adoption Rule

Use `Flow` by default in the effectful application layer:

- handlers
- use cases
- service orchestration
- infrastructure-facing application services

Keep the domain plain F# by default:

- domain models
- pure business rules
- small validation helpers
- plain `Result` when it already reads clearly

## What Makes It Readable

The design stays explicit in the places that matter for teams:

- env access is visible through `Flow.read`, `Flow.env`, and `Flow.localEnv`
- execution is visible through `Flow.toAsync`
- expected failures stay in the type
- `flow {}` binds the common shapes already present in app code, including `Result`, `Async`, `Task`, and selected mixed wrappers
- task and runtime boundaries stay named under `Flow.Task` and `Flow.Runtime`

This is the difference from more imported-feeling Reader encodings: the code still reads
like ordinary F# application code rather than a general FP framework.

## Why This Is Low Risk

Adopting `Flow` does not mean betting on a replacement runtime.

- the underlying async and task work still runs on F# `Async` and `.NET Task`
- cancellation still comes from the `CancellationToken` you pass to `Flow.toAsync`
- execution is still explicit
- the library stays narrow and DX-focused rather than growing into a concurrency platform

The goal is not to compete with the BCL or the F# core library.
The goal is to make mixed application workflows easier to write and easier to read.

## When Not To Use It

Do not introduce `Flow` early just because a dependency might appear later.

Stay with plain F# when:

- the code is mostly pure
- a direct function parameter is clearer
- plain `Result` already says everything
- a single `Task<'T>` boundary is the simplest honest shape

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the smallest useful workflow,
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for wrapper-shape interop, and
[`examples/README.md`](../examples/README.md) for runnable application-shaped flows.
