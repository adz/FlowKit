---
title: The FsFlow Model
description: The core FsFlow progression from Validate and Result into Flow, AsyncFlow, and TaskFlow.
---

# The FsFlow Model

This page shows why FsFlow is best understood as one scalable model for Result-based programs rather than a small helper for application boundaries.

The core progression is:

```text
Validate -> Result -> Flow -> AsyncFlow -> TaskFlow
```

The validation vocabulary stays the same while the execution context grows.

- start with pure validation and plain `Result`
- lift into `Flow` when the boundary needs explicit environment access
- lift again into `AsyncFlow` or `TaskFlow` when the runtime becomes asynchronous

That matters because many F# codebases end up with separate worlds:

```fsharp
Result<'value, 'error>
Async<Result<'value, 'error>>
Task<Result<'value, 'error>>
```

Those shapes work, but they often split the same program across separate helper modules, separate builders,
and repeated adaptation between pure validation and effectful orchestration.

FsFlow gives those shapes one coherent family:

```fsharp
Flow<'env, 'error, 'value>
AsyncFlow<'env, 'error, 'value>
TaskFlow<'env, 'error, 'value>
```

The point is not to replace `Result`, `Async`, or `Task`.
The point is to let one Result-based style scale into real application boundaries without changing the mental model.

## The Main Claim

FsFlow unifies Result-based programming across pure logic and effectful execution.

- write validation and domain checks once with `Validate` and `Result`
- lift them directly into flows when you need environment, async, task, cancellation, retries, logging, or resource handling
- keep the smallest honest runtime shape at each boundary

## Before The Runtime Grows

Start with a plain validation helper:

```fsharp
type RegistrationError =
    | EmailMissing

let validateEmail (email: string) : Result<unit, RegistrationError> =
    email
    |> FsFlow.Validate.okIfNotBlank
    |> Result.map ignore
    |> FsFlow.Validate.orElse EmailMissing
```

This is already enough for pure code and should stay plain when the surrounding logic is still plain.

## When The Boundary Grows

When that same use case needs dependencies and task work, keep the validation as-is and lift the boundary:

```fsharp
open System.Threading.Tasks

type RegistrationEnv =
    { LoadUser: int -> Task<Result<User, RegistrationError>>
      SaveUser: User -> Task<Result<unit, RegistrationError>> }

let register userId : TaskFlow<RegistrationEnv, RegistrationError, unit> =
    taskFlow {
        let! loadUser = TaskFlow.read _.LoadUser
        let! saveUser = TaskFlow.read _.SaveUser

        let! user = loadUser userId
        do! validateEmail user.Email
        return! saveUser user
    }
```

`validateEmail` is still just `Result<unit, RegistrationError>`.
There is no separate task-result validation vocabulary to switch to first.

## What This Replaces

FsFlow is strongest when you would otherwise spread the same use case across:

- plain `Result` helpers
- `Async<Result<_,_>>` or `Task<Result<_,_>>` wrappers
- extra helper modules for each wrapper shape
- manual environment threading or ad hoc service lookups

Instead, the same logic can move upward through the computation families while keeping the same typed-failure story.

## Adoption Rule

Use FsFlow by default in the effectful application layer where the boundary genuinely needs more than plain `Result`:

- handlers
- use cases
- service orchestration
- infrastructure-facing application services

Keep the domain plain F# by default:

- domain models
- pure business rules
- small validation helpers
- plain `Result` when it already reads clearly

## Short-Circuiting Is Intentional

`Validate`, `Result`, `Flow`, `AsyncFlow`, and `TaskFlow` are short-circuiting.
They stop on the first typed failure.

That is a feature, not a missing applicative layer.

If you need accumulated validation, keep that explicit with a dedicated validation type or a library such as Validus.
FsFlow does not currently provide applicative accumulated validation in `Validate` or the flow builders.

## What Keeps It Readable

The design stays explicit in the places that matter for teams:

- env access is visible through `Flow.read`, `AsyncFlow.read`, or `TaskFlow.read`
- execution is visible through `Flow.run`, `AsyncFlow.toAsync`, or `TaskFlow.toTask`
- expected failures stay in the type
- the computation family tells you whether the use case is sync, `Async`, or `.NET Task`

This keeps the code close to ordinary F# application code instead of turning each runtime shape into a new mini-ecosystem.

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

Read [`docs/VALIDATE_AND_RESULT.md`](./VALIDATE_AND_RESULT.md) for the validation-first story,
[`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the computation-family overview,
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for boundary-shape interop, and
[`docs/examples/README.md`](./examples/README.md) for reference examples.
