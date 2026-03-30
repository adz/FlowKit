# Effect.FS

Effect.FS is a small F# library for workflows that need:

- dependencies from an application environment
- typed failures
- a mix of `Result`, `Async`, and `Task`
- operational steps such as logging, retry, timeout, and cleanup

The core type is:

```fsharp
Effect<'env, 'error, 'value>
```

Use it when plain `Result` is no longer enough, but `Async<Result<_,_>>` plus helper modules is starting to hide the happy path.

## What You Get

- one workflow type for dependencies, async work, and typed failures
- one computation expression: `effect {}`
- direct binding from `Result`, `Async`, `Async<Result<_,_>>`, `Task`, and `Task<Result<_,_>>`
- explicit environment access
- built-in helpers for retry, timeout, logging, and resource cleanup

Effects are cold by default. Building an effect does not run it.

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

let greet name : Effect<AppEnv, AppError, string> =
    effect {
        let! env = Effect.environment
        let! validName = validateName name
        return $"{env.Prefix} {validName}"
    }

let result =
    greet "Ada"
    |> Effect.execute { Prefix = "Hello" }
    |> Async.RunSynchronously
```

## When Effect.FS Fits Well

Effect.FS is a good fit when:

- a workflow needs 2 to 5 dependencies
- validation, IO, and error translation all belong in one use case
- your code touches both `Async` and .NET `Task`
- you want expected failures in the type rather than scattered exception handling
- retry, timeout, and cleanup belong close to the business flow

Effect.FS is usually not worth it when:

- the code is mostly pure
- plain `Result` already reads well
- a direct `Task<'T>` boundary is the clearest option

## If You Already Use FsToolkit

Effect.FS is not a replacement for every FsToolkit workflow.

Stay with FsToolkit when your main problem is composing `Async<Result<_,_>>` and your dependency story is already simple.

Effect.FS starts to help when you also want:

- environment requirements in the type
- one workflow model across `Result`, `Async`, and `Task`
- operational helpers close to the workflow
- a clearer split between expected failures and defects

Migration can happen one workflow at a time through:

- `Effect.fromAsyncResult`
- `Effect.toAsyncResult`
- `AsyncResultCompat`

See [`docs/FSTOOLKIT_MIGRATION.md`](docs/FSTOOLKIT_MIGRATION.md).

## Learn The Library In This Order

1. [`docs/GETTING_STARTED.md`](docs/GETTING_STARTED.md)
2. [`examples/README.md`](examples/README.md)
3. [`docs/FSTOOLKIT_MIGRATION.md`](docs/FSTOOLKIT_MIGRATION.md)
4. [`docs/WEIRD_SHAPES.md`](docs/WEIRD_SHAPES.md)
5. [`docs/SEMANTICS.md`](docs/SEMANTICS.md)
6. [`src/EffectFs/Effect.fs`](src/EffectFs/Effect.fs)

## Run The Repo

Run the test suite:

```bash
dotnet run --project tests/EffectFs.Tests/EffectFs.Tests.fsproj --nologo
```

Run the main example:

```bash
dotnet run --project examples/EffectFs.Examples/EffectFs.Examples.fsproj --nologo
```

Run the maintenance example:

```bash
dotnet run --project examples/EffectFs.MaintenanceExamples/EffectFs.MaintenanceExamples.fsproj --nologo
```
