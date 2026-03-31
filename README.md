# EffectfulFlow

EffectfulFlow is a small F# library built around composable flows:

- explicit environment requirements
- typed failures
- explicit cancellation
- direct `Async` and `.NET Task` interop
- runtime helpers for retry, timeout, logging, and scoped cleanup

The core type is:

```fsharp
Flow<'env, 'error, 'value>
```

Use it when plain `Result` is no longer enough, but `Async<Result<_,_>>` plus helper modules is starting to hide the happy path.

## What You Get

- one workflow type for dependencies, async work, and typed failures
- one computation expression: `flow {}`
- explicit environment access through `Flow.env` and `Flow.read`
- explicit execution through `Flow.run env cancellationToken flow`
- task interop in `Flow.Task`
- runtime helpers in `Flow.Runtime`

Flows are cold by default. Building a flow does not run it.

## A Small Example

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

let greet name : Flow<AppEnv, AppError, string> =
    flow {
        let! validName = validateName name
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }

let result =
    greet "Ada"
    |> Flow.run { Prefix = "Hello" } System.Threading.CancellationToken.None
    |> Async.RunSynchronously
```

## When EffectfulFlow Fits Well

EffectfulFlow is a good fit when:

- a workflow needs 2 to 5 dependencies
- validation, IO, and error translation all belong in one use case
- your code touches both `Async` and `.NET Task`
- you want expected failures in the type rather than scattered exception handling
- retry, timeout, and cleanup belong close to the business flow

EffectfulFlow is usually not worth it when:

- the code is mostly pure
- plain `Result` already reads well
- a direct `Task<'T>` boundary is the clearest option

## Learn The Library In This Order

1. [`docs/GETTING_STARTED.md`](docs/GETTING_STARTED.md)
2. [`docs/SEMANTICS.md`](docs/SEMANTICS.md)
3. [`examples/README.md`](examples/README.md)
4. [`docs/FSTOOLKIT_MIGRATION.md`](docs/FSTOOLKIT_MIGRATION.md)
5. [`docs/TROUBLESHOOTING_TYPES.md`](docs/TROUBLESHOOTING_TYPES.md)
6. [`src/EffectfulFlow/Flow.fs`](src/EffectfulFlow/Flow.fs)

## Compatibility

The current design is `.NET`-first. Cancellation is explicit in the `Flow` execution model, and task interop is part of the first-class public surface.

The repo no longer keeps separate compatibility modules for old `Async<Result<_,_>>` or FsToolkit-style workflows. Migration is through direct adapters such as:

- `Flow.fromAsyncResult`
- `Flow.toAsyncResult`

NativeAOT is still verified in this repo through a small publish-and-run probe application.

## Run The Repo

Run the test suite:

```bash
dotnet run --project tests/EffectfulFlow.Tests/EffectfulFlow.Tests.fsproj --nologo
```

Run the main example:

```bash
dotnet run --project examples/EffectfulFlow.Examples/EffectfulFlow.Examples.fsproj --nologo
```

Run the maintenance example:

```bash
dotnet run --project examples/EffectfulFlow.MaintenanceExamples/EffectfulFlow.MaintenanceExamples.fsproj --nologo
```

Run the playground example:

```bash
dotnet run --project examples/EffectfulFlow.Playground/EffectfulFlow.Playground.fsproj --nologo
```

Run the NativeAOT probe:

```bash
bash scripts/run-aot-probe.sh
```
