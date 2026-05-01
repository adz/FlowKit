# FsFlow

> [!WARNING]
> API Still stabilising - wait for 1.0 to avoid breaking changes

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/content/img/fsflow-readme-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="docs/content/img/fsflow-readme-light.svg">
  <img alt="FsFlow" src="docs/content/img/fsflow-readme-light.svg" width="160">
</picture>

FsFlow is a single model for Result-based programs in F#.
Write validation and typed-error logic once, keep it as plain `Result` while the code is pure,
then lift the same logic into `Flow`, `AsyncFlow`, or `TaskFlow` when the boundary needs
environment access, async work, task interop, cancellation, or runtime policy.

[![ci](https://github.com/adz/FsFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/adz/FsFlow/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FsFlow.svg)](https://www.nuget.org/packages/FsFlow)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

## Core Model

FsFlow is built around one progression:

```text
Validate -> Result -> Flow -> AsyncFlow -> TaskFlow
```

The validation vocabulary stays the same while the execution context grows.

- Start with plain `Result` and pure validation helpers.
- Use `flow {}` when the boundary needs typed failure and environment, but not async runtime.
- Use `asyncFlow {}` when the boundary is naturally `Async`.
- Use `taskFlow {}` when the boundary is naturally `.NET Task`.
- Keep expected failures typed all the way through instead of switching helper families at each runtime shape.

This is the key difference from split models like `Result`, `Async<Result<_,_>>`, and `Task<Result<_,_>>`
that need separate helper modules, separate builders, and repeated adaptation at the boundary.

## Install

- `FsFlow` for `Flow` and `AsyncFlow`
- `FsFlow.Net` for `TaskFlow`

## Example

Start with pure validation:

```fsharp
open System.Threading.Tasks
open FsFlow.Validate

type RegistrationError =
    | EmailMissing
    | SaveFailed of string

let validateEmail (email: string) : Result<unit, RegistrationError> =
    email
    |> okIfNotBlank
    |> Result.map ignore
    |> orElse EmailMissing
```

Use the same `Result` directly inside a task-oriented workflow:

```fsharp
open System.Threading.Tasks
open FsFlow.Net

type User =
    { Email: string }

type RegistrationEnv =
    { LoadUser: int -> Task<Result<User, RegistrationError>>
      SaveUser: User -> Task<Result<unit, RegistrationError>> }

let registerUser userId : TaskFlow<RegistrationEnv, RegistrationError, unit> =
    taskFlow {
        let! loadUser = TaskFlow.read _.LoadUser
        let! saveUser = TaskFlow.read _.SaveUser

        let! user = loadUser userId
        do! validateEmail user.Email

        return! saveUser user
    }
```

`validateEmail` is just `Result<unit, RegistrationError>`.
`taskFlow` lifts it directly with `do!`.
There is no separate task-result validation vocabulary to learn first.

## Semantic Boundary

FsFlow is for short-circuiting, ordered workflows:

- `Validate`, `Result`, `Flow`, `AsyncFlow`, and `TaskFlow` stop on the first typed failure.
- They are for orchestration, dependency access, async or task execution, and runtime concerns.
- They are not accumulated validation builders.

If you need accumulated validation, keep that explicit with a dedicated validation library or bridge it in at the edge.

## What You Get

FsFlow stays close to standard F# and .NET:

- `flow { ... }` binds to `Result` and `Option`
- `asyncFlow { ... }` also binds to `Async` and `Async<Result<_,_>>`
- `taskFlow { ... }` binds to `Task`, `ValueTask`, `Task<_>`, `ValueTask<_>`, and `ColdTask`
- `Validate` works as plain `Result` logic before lifting into a workflow

Because tasks are hot, FsFlow includes `ColdTask`: a small wrapper around `CancellationToken -> Task`.
`taskFlow` handles token passing for you and keeps reruns explicit.

This is the file-oriented example shape. The full runnable example is in
[`examples/FsFlow.ReadmeExample/Program.fs`](./examples/FsFlow.ReadmeExample/Program.fs).

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

It reads `Root` from `'env`, performs two file reads in one `taskFlow {}`, and keeps failure typed at the boundary.

## Getting Started

- [Docs site](https://adz.github.io/FsFlow) for guides and API reference
- [`docs/VALIDATE_AND_RESULT.md`](docs/VALIDATE_AND_RESULT.md) for the validation-first story
- [`examples/`](examples/) for runnable repo examples
- [`docs/TINY_EXAMPLES.md`](docs/TINY_EXAMPLES.md) for the smallest runnable snippets
