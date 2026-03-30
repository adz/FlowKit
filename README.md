# Effect.FS

Practical F# effect handling for code that has to deal with dependencies, async work, tasks, logging, retries, timeouts, and typed failures without collapsing into `Async<Result<_,_>>` plumbing.

## Motivation

The common F# path today is usually one of these:

- plain `Result` for pure validation
- `Async<Result<_,_>>` when IO arrives
- FsToolkit builders to reduce some of the nesting
- ad hoc records, function arguments, or DI containers for dependencies

That works, but application code starts to spread concerns across several layers:

- the success path lives inside wrapper-management code
- environment access is not a first-class part of the model
- logging, retry, timeout, and resource safety become custom conventions
- `Task` and `Async` interop keeps leaking into business code

Effect.FS is an attempt to keep the useful parts of that world while giving F# a cleaner center of gravity:

- one cold effect type: `Effect<'env, 'error, 'value>`
- one computation expression: `effect {}`
- direct lifting and binding for `Result`, `Async`, `Async<Result<_,_>>`, and `Task`
- explicit dependency access through the environment type
- small practical helpers for logging, retry, timeout, and resource safety

## Where It Helps

Effect.FS helps most when your code has all of these properties:

- the happy path is easy to understand, but wrapper noise is growing
- you want dependencies visible in types instead of hidden behind service lookup
- you need to mix pure validation with `Async`, `Task`, and ordinary .NET APIs
- you care about typed expected failures, but you do not want to pretend every exception is a domain error
- you want logging and retry behavior close to the workflow instead of scattered around infrastructure glue

It helps less when:

- your code is mostly pure and ordinary `Result` is enough
- you only have one async call and no meaningful dependency story
- a direct `Task` or `Async` is already the clearest representation

## Current Shape

The core type is:

```fsharp
Effect<'env, 'error, 'value>
```

Read that as:

- `'env`: the dependencies or context the workflow needs
- `'error`: the typed failures callers are expected to handle
- `'value`: the success value

The library is intentionally small and cold by default. Creating an effect does not run it. Execution is explicit.

## Example

```fsharp
type AppEnv =
    { Config: AppConfig
      WriteLog: LogEntry -> unit
      AttemptCount: int ref }

let validateConfig : Effect<AppConfig, AppError, RequestPlan> =
    effect {
        let! config = Effect.environment<AppConfig, AppError>
        let! apiBaseUrl = requireNonEmpty "apiBaseUrl" config.ApiBaseUrl |> Result.mapError ValidationFailed
        let! apiKey = requireNonEmpty "apiKey" config.ApiKey |> Result.mapError ValidationFailed

        return
            { Banner = $"{config.Prefix} :: {apiKey}".ToUpperInvariant()
              Url = $"{apiBaseUrl}/ping"
              RetryCount = config.RetryCount
              RequestTimeout = config.RequestTimeout }
    }

let fetchResponse plan : Effect<AppEnv, AppError, Response> =
    effect {
        let! env = Effect.environment<AppEnv, AppError>
        let attempt = env.AttemptCount.Value + 1
        env.AttemptCount.Value <- attempt

        do! Effect.log (fun e entry -> e.WriteLog entry) LogLevel.Information $"attempt={attempt}"

        if attempt <= env.Config.FailuresBeforeSuccess then
            return! Error(TransientFailure attempt)

        let! body = Task.FromResult $"GET {plan.Url}"
        return { StatusCode = 200; Body = body }
    }
    |> Effect.retry { MaxAttempts = plan.RetryCount + 1; Delay = fun _ -> TimeSpan.Zero; ShouldRetry = function TransientFailure _ -> true | _ -> false }
    |> Effect.timeout plan.RequestTimeout TimedOut
```

That example is doing several things that usually get split apart in ordinary F# code:

- plain `Result` validation
- environment access
- logging
- task interop
- typed retry behavior
- timeout handling

See the runnable version in [`examples/EffectFs.Examples/Program.fs`](examples/EffectFs.Examples/Program.fs).

## Coming From FsToolkit

Effect.FS is not trying to erase the value of FsToolkit. It is trying to solve a slightly wider application problem.

FsToolkit is excellent when you mainly want better ergonomics around wrapper types like `Async<Result<_,_>>`.

Effect.FS becomes interesting when you want one place to model:

- wrapper composition
- dependency access
- logging and operational concerns
- typed failure boundaries
- practical interop with both `Async` and `Task`

The compatibility story is deliberate:

- you can lift `Async<Result<_,_>>` with `Effect.fromAsyncResult`
- you can return to `Async<Result<_,_>>` with `Effect.toAsyncResult`
- inside `effect {}`, you can bind `Result`, `Async`, `Async<Result<_,_>>`, `Task<'T>`, and `Task`

Migration guidance lives in [`docs/FSTOOLKIT_MIGRATION.md`](docs/FSTOOLKIT_MIGRATION.md).

## Running It

Run the executable test harness:

```bash
dotnet run --project tests/EffectFs.Tests/EffectFs.Tests.fsproj --nologo
```

Run the example application:

```bash
dotnet run --project examples/EffectFs.Examples/EffectFs.Examples.fsproj --nologo
```

## Project Docs

- [`examples/README.md`](examples/README.md)
- [`docs/FSTOOLKIT_MIGRATION.md`](docs/FSTOOLKIT_MIGRATION.md)
- [`docs/EFFECT_TS_COMPARISON.md`](docs/EFFECT_TS_COMPARISON.md)
- [`PLAN.md`](PLAN.md)
