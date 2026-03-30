# Effect.FS

Practical F# effect handling for application code that mixes dependencies, async work, tasks, logging, retries, timeouts, resource cleanup, and typed failures.

The design target is not academic purity. The design target is whether ordinary F# code becomes easier to read and evolve than the usual `Async<Result<_,_>>` plus helper-module approach.

## What It Is

The core type is:

```fsharp
Effect<'env, 'error, 'value>
```

Read that as:

- `'env`: the dependencies or context the workflow needs
- `'error`: the typed failures callers are expected to handle
- `'value`: the success value

The library is cold by default. Constructing an effect does not run it.

## Why It Exists

The common F# path today is often:

- plain `Result` for pure validation
- `Async<Result<_,_>>` when IO arrives
- FsToolkit builders to recover readability
- ad hoc records or containers for dependencies

That works, but the costs show up over time:

- the happy path gets buried inside wrapper handling
- dependency access is not modeled directly
- logging, retry, timeout, and cleanup become conventions instead of API
- `Async` and `Task` interop keeps leaking into business code

Effect.FS is trying to give that code a cleaner center:

- one effect type
- one computation expression
- explicit environment access
- direct interop with `Result`, `Async`, `Async<Result<_,_>>`, and `Task`
- practical helpers for operational concerns

## Where It Really Helps

Effect.FS is most useful when:

- a workflow depends on 2-5 services or context values
- validation, IO, logging, and retries all live in the same use case
- the code touches both F# `Async` and .NET `Task`
- you want expected failures modeled explicitly
- you want dependencies visible in types instead of hidden behind lookup

It is less useful when:

- the code is mostly pure
- plain `Result` is already enough
- a direct `Task<'T>` is the clearest boundary type

## Current Capability Surface

The core currently includes:

- `effect {}`
- `fromResult`, `fromAsync`, `fromAsyncResult`, `fromTask`, and `fromTaskResult`
- direct `let!` binding for common wrapper types
- `environment`, `read`, `provide`, and `withEnvironment`
- `log` and `logWith`
- `retry`
- `timeout`
- `bracket`, `bracketAsync`, and `usingAsync`
- `executeWithCancellation`, `ensureNotCanceled`, and `catchCancellation`
- `AsyncResultCompat` as a migration bridge for FsToolkit-style code

## Minimal Example

```fsharp
let runCommand : Effect<AppEnv, AppError, Response> =
    effect {
        let! env = Effect.environment<AppEnv, AppError>
        let! validated = validate env.Config |> Result.mapError ValidationFailed

        do! Effect.log (fun e entry -> e.WriteLog entry) LogLevel.Information "validated request"

        let! response =
            env.Gateway.Ping(validated, CancellationToken.None)
            |> Effect.fromTaskResultValue
            |> Effect.mapError GatewayFailed

        return response
    }
```

The full runnable example in [`examples/EffectFs.Examples/Program.fs`](examples/EffectFs.Examples/Program.fs) goes further:

- gateway dependency
- persistence dependency
- logging
- retries
- timeout
- async scope cleanup
- typed failure translation

## Coming From FsToolkit

Effect.FS is not trying to replace every use of FsToolkit.

FsToolkit is a strong fit when the main problem is wrapper composition around `Async<Result<_,_>>`.

Effect.FS becomes more compelling when you also want:

- explicit dependency access in the type
- one workflow model across `Result`, `Async`, and `Task`
- operational behaviors like retry and timeout close to the business flow
- a deliberate distinction between typed failures and thrown defects

Migration support is built in:

- `Effect.fromAsyncResult`
- `Effect.toAsyncResult`
- `AsyncResultCompat`

See [`docs/FSTOOLKIT_MIGRATION.md`](docs/FSTOOLKIT_MIGRATION.md).

## Running It

Run the test harness:

```bash
dotnet run --project tests/EffectFs.Tests/EffectFs.Tests.fsproj --nologo
```

Run the example application:

```bash
dotnet run --project examples/EffectFs.Examples/EffectFs.Examples.fsproj --nologo
```

## Orientation

Start here:

1. [`examples/EffectFs.Examples/Program.fs`](examples/EffectFs.Examples/Program.fs)
2. [`src/EffectFs/Effect.fs`](src/EffectFs/Effect.fs)
3. [`docs/FSTOOLKIT_MIGRATION.md`](docs/FSTOOLKIT_MIGRATION.md)
4. [`docs/EFFECT_TS_COMPARISON.md`](docs/EFFECT_TS_COMPARISON.md)
5. [`PLAN.md`](PLAN.md)
