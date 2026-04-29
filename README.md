# FsFlow

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/content/img/fsflow-readme-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="docs/content/img/fsflow-readme-light.svg">
  <img alt="FsFlow" src="docs/content/img/fsflow-readme-light.svg" width="160">
</picture>

FsFlow blends `Result` with an added `'env` in a `flow { ... }` computation expression,
for threading dependencies or request context (for example, a trace ID) without globals. 
It also provides `taskFlow { ... }` and `asyncFlow { ... }` which in addition to `Result`
and `'env` blend in standard F# Task and Async types respectively.

[![ci](https://github.com/adz/FsFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/adz/FsFlow/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FsFlow.svg)](https://www.nuget.org/packages/FsFlow)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

## Why FsFlow

- Keep expected failures in the type instead of pushing them into exceptions
- Keep dependencies visible instead of hiding them in globals or service locators
- Blend `Async` or `Task` inline only when the boundary needs it
- Fit a booted app environment, explicit dependencies plus context, or a conventional `.NET` host
- Lift `Result`, `Async<Result<_,_>>`, `Task<Result<_,_>>`, and `ColdTask` so existing .NET code can fit without a rewrite
- Use `Reader`-style environment access with explicit context instead of global state
- Use `ColdTask` when task work should stay delayed and pick up the workflow cancellation token automatically

In addition, FsFlows pair well with normal Result libraries, and it provides a clean set of
functions in `FsFlow.Validate` for handling them fluently before lifting into a flow.

## Install

- `FsFlow` for `Flow` and `AsyncFlow`
- `FsFlow.Net` for `TaskFlow`

## What You Get

FsFlow relies on standard F# and .NET with small additions for workflow ergonomics:

- `flow { ... }` binds to `Result` and `Option`
- `asyncFlow { ... }` also binds to `Async` and `Async<Result<_,_>>`
- `taskFlow { ... }` binds to `Task`, `ValueTask`, `Task<_>`, `ValueTask<_>`, and `ColdTask`
- `Validate` works fluently with pure `Result` types before lifting into a flow

Because tasks are hot, FsFlow includes `ColdTask`: a small wrapper around `CancellationToken -> Task`.
`taskFlow` handles token passing for you (or yields it when you need direct control).
This enables helpers like `retry`, `delay` and `timeout` to work across Async and Task.

In all computation expressions, access `'env` with `let!` and provide it once at the boundary.

## Example: file reads with typed errors

This snippet shows the core shape. The full runnable example, including `main` and temp-directory setup,
is in [`examples/FsFlow.ReadmeExample/Program.fs`](./examples/FsFlow.ReadmeExample/Program.fs).

```bash
dotnet run --project examples/FsFlow.ReadmeExample/FsFlow.ReadmeExample.fsproj --nologo
```

Supporting types in the full example are just:

- `ReadmeEnv = { Root: string }`
- `FileReadError = NotFound`

```fsharp
let readTextFile (path: string) : TaskFlow<ReadmeEnv, FileReadError, string> =
    taskFlow {
        // In production, map access and path exceptions separately at the boundary.
        do! okIf (File.Exists path) |> orElse (NotFound path) // from Validate

        return! ColdTask(fun ct -> File.ReadAllTextAsync(path, ct)) // ColdTask<string>
    }

let program : TaskFlow<ReadmeEnv, FileReadError, string * string> =
    taskFlow {
        let! root = TaskFlow.read _.Root                       // ReadmeEnv.Root -> string
        let settingsFile = Path.Combine(root, "settings.json")
        let featureFlagsFile = Path.Combine(root, "feature-flags.json")

        let! settings = readTextFile settingsFile              // TaskFlow<ReadmeEnv, FileReadError, string>
        let! featureFlags = readTextFile featureFlagsFile      // TaskFlow<ReadmeEnv, FileReadError, string>

        return settings, featureFlags                          // TaskFlow<ReadmeEnv, FileReadError, string * string>
    }
```

It reads a `Root` value from `'env`, then reads two files in one `taskFlow {}` so the cancellation token is
passed implicitly into both reads. It uses a simple `File.Exists` guard in the example, with a note that
production code should map path and access failures more explicitly at the boundary.

## Getting Started

- [Docs site](https://adz.github.io/FsFlow) for guides and API reference
- [`examples/`](examples/) for runnable repo examples
- [`docs/TINY_EXAMPLES.md`](docs/TINY_EXAMPLES.md) for the smallest runnable snippets

See the docs-site [Integrations page](https://adz.github.io/FsFlow/INTEGRATIONS/) for coexistence and migration patterns.
