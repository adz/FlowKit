<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/content/img/flowkit-dark.svg">
  <img alt="flow{kit}" src="docs/content/img/flowkit-light.svg" height="72">
</picture>

Simple to use F# flow {} computation expression for unifying Dependencies (Reader), 
Error Handling (Result), and Async/Task.

[![ci](https://github.com/adz/FlowKit/actions/workflows/ci.yml/badge.svg)](https://github.com/adz/FlowKit/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FlowKit.svg)](https://www.nuget.org/packages/FlowKit)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

When one F# use case starts mixing `Result`, `async {}`, `.NET Task`, and dependency management,
the code often stops reading like the happy path.

## Why This Exists In F#

Most real F# application code ends up mixing:

- dependencies passed through several layers, whether as one app environment or explicit feature dependencies
- `Result` for expected business errors
- `Async` or `.NET Task` for IO

That often turns into one of these shapes:

```fsharp
AppEnv -> Async<Result<'value, 'error>>
```

or:

```fsharp
Deps -> Input -> Async<Result<'value, 'error>>
```

plus helper modules, adapters, and wrapper-specific boilerplate.

FlowKit is a minimal, idiomatic way to represent that shape directly in F#.

It gives that use case one shape:

```fsharp
Flow<'env, 'error, 'value>
```

and one workflow:

```fsharp
flow { ... }
```

so dependency access, typed failures, `Async`, and `Task` stay in one place instead of spreading
across helper modules, adapters, and wrapper-specific CEs.

## Before And After

Before:

```fsharp
let handle (deps: UserDeps) userId =
    async {
        let! loaded = deps.LoadName userId |> Async.AwaitTask

        match loaded with
        | Error error ->
            return Error (GatewayFailed error)
        | Ok loadedName ->
            match validateName loadedName with
            | Error error ->
                return Error error
            | Ok validName ->
                return Ok $"{deps.Prefix} {validName}"
    }
```

After:

```fsharp
let handle (deps: UserDeps) userId : Flow<RequestContext, AppError, string> =
    flow {
        let! loadedName =
            deps.LoadName userId
            |> Flow.mapError GatewayFailed
        let! validName = validateName loadedName
        return $"{deps.Prefix} {validName}"
    }
```

This is the same application flow without the plumbing taking over the happy path. If your codebase prefers a single booted app environment, that style works too. FlowKit supports both.

## What It Actually Is

FlowKit is a small, focused F# library built around composable flows:

- explicit environment requirements
- typed failures via Result
- cancellation-aware execution without passing tokens through every step
- direct `Async` and `.NET Task` interop
- helpers for retry, timeout, logging, and scoped cleanup

The point is to keep that code in one place, with one workflow type, while staying in ordinary F#:

- one computation expression: `flow {}`
- one CE that binds `Result`, `Async`, `Task`, and env access in one place
- explicit environment access through `Flow.read` and `Flow.env`
- explicit execution through `Flow.run env cancellationToken flow`

It does not replace F# `Async`, `.NET Task`, or `Result`.
It gives you a smaller, more consistent way to compose them in application code.
Cancellation stays explicit at the runtime boundary and in cold task signatures, but usually disappears inside `flow {}` itself.

## What It Is Not

FlowKit is not trying to become a new runtime platform.

- it does not reimplement `Async` or `Task`
- it does not introduce its own concurrency system
- it does not hide when effects run
- it stays explicit at the execution boundary with `Flow.run`

The library is intentionally narrow:

- better DX for mixed application workflows
- better readability across `Result` / `Async` / `Task` code
- less wrapper and adapter noise around the happy path

Reader-style composition can feel imported in F# when it arrives as a larger FP stack.
FlowKit keeps the same practical benefits in plain F# terms:

- one computation expression
- plain functions
- explicit environment access
- no transformer stacks or typeclass machinery

Flows are cold by default. Building a flow does not run it.

## Full Code

```fsharp
type AppEnv =
    { Prefix: string
      LoadName: int -> Task<Result<string, AppError>> }

type AppError =
    | MissingName
    | GatewayFailed of string

let validateName (name: string) =
    if System.String.IsNullOrWhiteSpace name then
        Error MissingName
    else
        Ok name

let greet userId : Flow<AppEnv, AppError, string> =
    flow {
        let! loadName = Flow.read _.LoadName
        let! loadedName =
            loadName userId
            |> Flow.mapError GatewayFailed

        let! validName = validateName loadedName
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }

let result =
    greet 42
    |> Flow.run
        { Prefix = "Hello"
          LoadName = fun _ -> Task.FromResult(Ok "Ada") }
        System.Threading.CancellationToken.None
    |> Async.RunSynchronously
```

This full example shows the intended shape in one place:

- one env dependency through `Flow.read`
- one plain `Result` function
- one `Task<Result<_,_>>` boundary
- one happy-path workflow in `flow {}`

## Where To Use It

Use FlowKit at the effectful application boundary:

- handlers
- use cases
- service orchestration
- infrastructure-facing application services

Keep the domain plain F# where possible:

- plain functions
- plain domain types
- plain `Result` when that already reads well

Keep domain code plain. Use `flow {}` by default in the application layer.

## Supported Architectural Styles

FlowKit supports three valid architectural styles:

- Booted App Environment
- Explicit Dependencies + Context
- Standard `.NET` AppHost + DI

The library does not require one application shape.
Choose the style that fits your codebase and team:

- use Booted App Environment when app composition simplicity matters most
- use Explicit Dependencies + Context when feature locality and testability matter most
- use standard `.NET` AppHost + DI when familiarity and incremental adoption matter most

Read [`docs/ARCHITECTURAL_STYLES.md`](docs/ARCHITECTURAL_STYLES.md) for examples and trade-offs.

## When FlowKit Fits Well

FlowKit is a good fit when:

- a workflow needs 2 to 5 dependencies
- validation, IO, and error translation all belong in one use case
- your code touches both `Async` and `.NET Task`
- you want expected failures in the type rather than scattered exception handling
- retry, timeout, and cleanup belong close to the business flow

FlowKit is usually not worth it when:

- the code is mostly pure
- plain `Result` already reads well
- a direct `Task<'T>` boundary is the clearest option

## Learn The Library In This Order

1. [`docs/GETTING_STARTED.md`](docs/GETTING_STARTED.md)
2. [`docs/TINY_EXAMPLES.md`](docs/TINY_EXAMPLES.md)
3. [`docs/ARCHITECTURAL_STYLES.md`](docs/ARCHITECTURAL_STYLES.md)
4. [`docs/WHY_FLOWKIT.md`](docs/WHY_FLOWKIT.md)
5. [`docs/TASK_ASYNC_INTEROP.md`](docs/TASK_ASYNC_INTEROP.md)
6. [`docs/FSTOOLKIT_MIGRATION.md`](docs/FSTOOLKIT_MIGRATION.md)
7. [`docs/ENV_SLICING.md`](docs/ENV_SLICING.md)
8. [`docs/SEMANTICS.md`](docs/SEMANTICS.md)
9. [`examples/README.md`](examples/README.md)
10. [`docs/TROUBLESHOOTING_TYPES.md`](docs/TROUBLESHOOTING_TYPES.md)
11. [`src/FlowKit/Flow.fs`](src/FlowKit/Flow.fs)

## Compatibility

### AOT Verified
NativeAOT is verified in this repo through a small publish-and-run probe application.

### .NET only - no Fable story
The design is `.NET`-first. Cancellation is explicit in the `Flow` execution model, and task interop is part of the first-class public surface.
This means we don't have a Fable story (yet).

### Existing Shapes
FlowKit builds on existing F# and .NET primitives rather than replacing them.
If a direct `Result`, `Async<'T>`, or `Task<'T>` boundary is already the clearest shape,
use that shape directly.

If you already have `Async<Result<_,_>>` or FsToolkit-style workflows, you can adopt `Flow`
per use case and interoperate through adapters like:

- `Flow.fromAsyncResult`
- `Flow.toAsyncResult`

## Run The Repo

Run the examples:

```bash
# Longer main example
dotnet run --project examples/FlowKit.Examples/FlowKit.Examples.fsproj

# Maintenance example:
dotnet run --project examples/FlowKit.MaintenanceExamples/FlowKit.MaintenanceExamples.fsproj

# Minimal playground example:
dotnet run --project examples/FlowKit.Playground/FlowKit.Playground.fsproj
```

Run the test suite:

```bash
dotnet run --project tests/FlowKit.Tests/FlowKit.Tests.fsproj
```

Run the NativeAOT probe:

```bash
bash scripts/run-aot-probe.sh
```
