# Migrating From AsyncResult

Read this page when your current baseline is `Async<Result<_,_>>` with or without FsToolkit and you want to move one workflow at a time.

## When Flow Starts To Help

EffectfulFlow becomes more useful when:

- the workflow needs an explicit environment
- the code mixes `Result`, `Async`, `Task`, and `Task<Result<_,_>>`
- retry, timeout, or cancellation belong close to the flow
- you want expected failures and defects to stay visibly separate

## The Core Shift

The main type is now:

```fsharp
Flow<'env, 'error, 'value>
```

That extra environment parameter is the point. It keeps dependency requirements visible instead of pushing them into outer plumbing.

## Direct Adapters

There is no separate compatibility module anymore. Migrate through the direct adapters:

- `Flow.fromAsyncResult`
- `Flow.toAsyncResult`

Example:

```fsharp
let oldLoadUser userId : Async<Result<User, LegacyError>> = ...

let loadUserFlow userId : Flow<AppEnv, AppError, User> =
    oldLoadUser userId
    |> Flow.fromAsyncResult
    |> Flow.mapError LegacyFailed
```

## Pull Task Boundaries Into The Flow Explicitly

When a dependency moves to `Task`, lift it where it happens:

```fsharp
let runCommand : Flow<AppEnv, AppError, Payload> =
    flow {
        let! gateway = Flow.read _.Gateway
        let! ct = Flow.Runtime.cancellationToken

        let! payload =
            gateway.Load(ct)
            |> Flow.Task.fromHotResult
            |> Flow.mapError GatewayFailed

        return payload
    }
```

## Prefer Smaller Flows

The migration target is not a single application-wide abstraction. Prefer small flows with explicit environment reads:

```fsharp
let loadUser userId : Flow<AppEnv, AppError, User> =
    flow {
        do! Flow.Runtime.log (fun env entry -> env.WriteLog entry) LogLevel.Information $"Loading user {userId}"
        return! oldLoadUser userId |> Flow.fromAsyncResult |> Flow.mapError LegacyFailed
    }
```

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the current public surface, then [`examples/README.md`](../examples/README.md) for runnable examples.
