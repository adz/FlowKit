# Coming From FsToolkit

Read this page when your current baseline is `Async<Result<_,_>>` with FsToolkit and you want to know whether Effect.FS is a better fit for a particular workflow.

## When To Stay With FsToolkit

FsToolkit is still a strong fit when:

- your main problem is composing `Async<Result<_,_>>`
- dependencies are simple function arguments
- the natural application boundary is already `Async<Result<_,_>>`
- you do not need retry, timeout, cleanup, or logging in the workflow model

If your current code is already clear, there is no reason to force a migration.

## When Effect.FS Starts To Help

Effect.FS becomes more useful when:

- the workflow needs an application environment
- the code mixes `Result`, `Async`, `Task`, and `Task<Result<_,_>>`
- you want operational steps close to the workflow
- you want expected failures in the type while leaving defects as defects

The main difference is the workflow type:

```fsharp
Async<Result<'value, 'error>>
```

becomes:

```fsharp
Effect<'env, 'error, 'value>
```

That extra environment parameter is the point. It keeps dependency requirements visible instead of pushing them into outer plumbing.

## Side-By-Side Example

FsToolkit-style code:

```fsharp
let runCommand config =
    asyncResult {
        let! validated = validate config |> AsyncResult.ofResult
        let! payload = loadPayload validated
        do! AsyncResult.mapError InfrastructureError (writeAudit validated)
        return payload
    }
```

Effect.FS version:

```fsharp
let runCommand : Effect<AppEnv, AppError, Payload> =
    effect {
        let! env = Effect.environment
        let! validated = validate env.Config |> Result.mapError ValidationFailed
        let! payload =
            env.Gateway.Load(validated, System.Threading.CancellationToken.None)
            |> Effect.fromTaskResultValue
            |> Effect.mapError GatewayFailed
        return payload
    }
```

The gain is not just a different builder. The workflow type now shows:

- which environment it needs
- which typed failures it produces
- which value it returns

## Migrate Incrementally

You do not need to rewrite a codebase in one pass.

Available bridges:

- `Effect.fromAsyncResult`
- `Effect.toAsyncResult`
- `AsyncResultCompat.ofAsyncResult`
- `AsyncResultCompat.toAsyncResult`

Inside `effect {}` you can also bind directly from:

- `Result`
- `Async`
- `Async<Result<_,_>>`
- `Task`
- `Task<Result<_,_>>`

That lets you migrate one boundary or one use case at a time.

## A Practical Migration Pattern

Start by wrapping the existing workflow:

```fsharp
let loadUserEffect userId : Effect<AppEnv, AppError, User> =
    oldLoadUser userId
    |> Effect.fromAsyncResult
    |> Effect.mapError LegacyFailed
```

Then move environment access and operational concerns into the effect over time:

```fsharp
let loadUser userId : Effect<AppEnv, AppError, User> =
    effect {
        let! env = Effect.environment
        do! Effect.log env.WriteLog LogLevel.Information $"Loading user {userId}"
        return! oldLoadUser userId |> Effect.fromAsyncResult |> Effect.mapError LegacyFailed
    }
```

## What Not To Migrate

Leave these shapes alone unless there is a clear benefit:

- pure validation code
- tiny scripts
- code that is already readable with plain `Result`
- boundaries that should stay as direct `Task<'T>`

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the core workflow model, then [`examples/README.md`](../examples/README.md) for full examples.
