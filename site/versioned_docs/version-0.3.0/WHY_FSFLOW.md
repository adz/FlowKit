# Why FsFlow

This page shows when the FsFlow computation family is a better fit than manual environment threading, `Async<Result<_,_>>`, or `Task<Result<_,_>>` for ordinary F# application code.

FsFlow is aimed at a specific F# problem:

- a use case needs dependencies
- validation already returns `Result`
- one or more async or task boundaries show up
- expected failures should stay in the type

At that point, many codebases end up with one of these shapes:

```fsharp
AppEnv -> Result<'value, 'error>
AppEnv -> Async<Result<'value, 'error>>
AppEnv -> CancellationToken -> Task<Result<'value, 'error>>
```

Those shapes work, but they tend to spread the same concerns across several layers:

- dependency threading
- wrapper-shape adaptation
- helper modules for mapping and binding
- extra noise around the happy path

FsFlow gives those combined shapes one aligned family:

```fsharp
Flow<'env, 'error, 'value>
AsyncFlow<'env, 'error, 'value>
TaskFlow<'env, 'error, 'value>
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

Once the same computation also needs environment access or async work, pick the computation family that
matches the honest runtime.

For a synchronous use case:

```fsharp
let greet name : Flow<AppEnv, AppError, string> =
    flow {
        let! validName = validateName name
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

The important part is that `validateName` still returns a plain `Result`.
You do not have to wrap it in a separate result-specific abstraction first.

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

The same computation in `TaskFlow` keeps the happy path in one CE when the boundary is task-oriented:

```fsharp
let fetchUser userId : TaskFlow<AppEnv, AppError, User> =
    taskFlow {
        let! loadUser = TaskFlow.read _.LoadUser
        let! validId = validateUserId userId
        let! user = loadUser validId
        return user
    }
```

That is the core pitch of the library: keep dependencies, typed failures, and the real runtime
shape in one explicit computation family without pushing the code into a larger framework.

## Adoption Rule

Use FsFlow by default in the effectful application layer:

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

- env access is visible through `Flow.read`, `AsyncFlow.read`, or `TaskFlow.read`
- execution is visible through `Flow.run`, `AsyncFlow.toAsync`, or `TaskFlow.toTask`
- expected failures stay in the type
- the computation family tells you whether the use case is sync, `Async`, or `.NET Task`

This is the difference from more imported-feeling Reader encodings: the code still reads
like ordinary F# application code rather than a general FP framework.

## Why This Is Low Risk

Adopting FsFlow does not mean betting on a replacement runtime.

- the underlying async and task work still runs on F# `Async` and `.NET Task`
- execution is still explicit
- the library stays narrow and DX-focused rather than growing into a concurrency platform

The goal is not to compete with the BCL or the F# core library.
The goal is to make mixed application computations easier to write and easier to read.

## When Not To Use It

Do not introduce FsFlow early just because a dependency might appear later.

Stay with plain F# when:

- the code is mostly pure
- a direct function parameter is clearer
- plain `Result` already says everything
- a single `Task<'T>` or `Async<'T>` boundary is the simplest honest shape

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the computation-family overview,
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for boundary-shape interop, and
[`docs/examples/README.md`](./examples/README.md) for reference examples.
