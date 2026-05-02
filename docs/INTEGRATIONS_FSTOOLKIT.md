---
title: Replacing FsToolkit.ErrorHandling
description: How FsFlow replaces the common FsToolkit.ErrorHandling workflow path.
---

# Replacing FsToolkit.ErrorHandling

This page shows how FsFlow replaces the common `Result` plus `AsyncResult` plus `TaskResult` workflow path while staying narrower than `FsToolkit.ErrorHandling` as a whole.

If you already have a codebase built around `Result`, `Async<Result<_,_>>`, or `Task<Result<_,_>>`, FsFlow does not need to force a rewrite of the pure parts.
It can take over the orchestration layer while leaving existing pure helpers alone.

`FsToolkit.ErrorHandling` remains a broad helper toolbox.
FsFlow is a smaller, coherent execution model.

## Main Difference

FsToolkit often means separate worlds:

```text
Result
Async<Result>
Task<Result>
```

FsFlow presents one path:

```text
Check -> Result -> Validation -> Flow -> AsyncFlow -> TaskFlow
```

That means:

- the same validation helpers can stay plain `Result`
- `let!` and `do!` can lift those results directly into flows
- the runtime gets richer without forcing a new validation vocabulary per wrapper shape

## Comparison

| FsToolkit.ErrorHandling | FsFlow |
| --- | --- |
| `Result.requireTrue` | `Check.okIf |> Result.mapErrorTo` |
| `Result.requireSome` | `Check.okIfSome |> Result.mapErrorTo` |
| `asyncResult {}` | `asyncFlow {}` |
| `taskResult {}` | `taskFlow {}` |
| Separate APIs per wrapper shape | Plain `Result` lifts into flows |
| No env model | `Flow.read`, `AsyncFlow.read`, `TaskFlow.read` |
| No runtime policy model | Runtime helpers for retry, timeout, cancellation, logging |
| Accumulated validation helpers | `Validation` and `validate {}` |

## Keep The Pure Pieces Pure

Existing `Result` helpers can stay `Result` helpers.

That means:

- pure validation stays pure
- transformation helpers stay pure
- effectful orchestration moves into `Flow`, `AsyncFlow`, or `TaskFlow`
- sibling validation can move into `Validation` when accumulation matters

That is the migration sweet spot: move orchestration, not every helper.

## What Usually Moves

- the boundary code that chooses between sync, async, and task runtime shapes
- the environment or request context that needs to flow through the boundary
- the final error-provisioning step when the error itself needs effectful work

## What Usually Stays

- existing `Result` and `Async<Result<_,_>>` helpers
- module functions that already work well on the current types
- existing computation-expression builders when they are already readable in the codebase

## Move The Boundary, Not The Whole Codebase

If a function currently returns `Async<Result<'value, 'error>>`, the most direct FsFlow migration is usually `AsyncFlow<'env, 'error, 'value>`.

If a function is already task-based, `TaskFlow<'env, 'error, 'value>` is the natural endpoint.

Use the same migration rule in either case:

1. keep pure validation and mapping in plain functions
2. lift the honest runtime boundary into FsFlow
3. keep the result shape unchanged until you have a reason to rename it

## Bridge Patterns

Typical bridges look like this:

- `Result<'value, unit>` validation helpers become `FsFlow.Check` calls
- `Async<Result<'value, 'error>>` binds directly in `asyncFlow {}`
- `Task<Result<'value, 'error>>` binds directly in `taskFlow {}`
- `Async<Result<'value, unit>>` or `Result<'value, unit>` can use `orElse*` bridges when error creation itself needs environment or runtime work

## Example

The first bridge keeps validation pure and only moves the boundary:

```fsharp
type AppError =
    | NameRequired
    | UserInactive
    | LoadFailed

let validateName name =
    name
    |> Check.notBlank
    |> Result.mapErrorTo NameRequired

let validateActive user =
    user.IsActive
    |> Check.okIf
    |> Result.mapErrorTo UserInactive
```

These validations stay reusable as plain `Result` logic.
They lift unchanged into `flow {}`, `asyncFlow {}`, and `taskFlow {}`.

The next bridge takes an `Async<Result<_,_>>` boundary and keeps the async shape intact:

```fsharp
type AppEnv =
    { Prefix: string
      LoadUser: int -> Async<Result<User, AppError>>
      LoadGreeting: int -> Task<Result<string, AppError>> }

let loadGreeting : AsyncFlow<AppEnv, AppError, string> =
    asyncFlow {
        let! env = AsyncFlow.env
        let! loadUser = AsyncFlow.read _.LoadUser
        let! user = loadUser 42
        do! validateActive user
        return $"{env.Prefix} {user.Name}"
    }
```

Use this shape when the legacy code already returns `Async<Result<_,_>>` and FsFlow should only own the environment and boundary.

The task-shaped version follows the same rule:

```fsharp
let publishGreeting : TaskFlow<AppEnv, string, string> =
    taskFlow {
        let! env = TaskFlow.env
        let! name = env.LoadGreeting 42
        return $"{env.Prefix} {name}"
    }
```

The `TaskFlow` builder can bind `Task<Result<_,_>>` directly, so you can keep the task boundary honest while moving the orchestration into FsFlow.

## Keep Started Work Started

If the code already has a started task, keep it as a task and bind it directly:

```fsharp
let started = Task.FromResult (Ok "Ada")

let alreadyRunning : TaskFlow<unit, string, string> =
    taskFlow {
        let! name = started
        return name
    }
```

Use the explicit bridge helpers in non-builder pipelines when you need them, but keep the normal docs path focused on direct binds inside the computation expression.

## Semantic Boundary

FsFlow flows are short-circuiting.
They are not a replacement for accumulated validation helpers.

If your current `FsToolkit.ErrorHandling` usage leans on independent validation that should report multiple errors,
keep that concern explicit with `Validation` and `validate {}` instead of trying to hide it inside `flow {}` or `taskFlow {}`.

## When FsToolkit Still Wins

Keep `FsToolkit.ErrorHandling` where its existing combinators already make the code clearer.

FsFlow is not trying to delete that ecosystem. It is trying to make the boundary explicit when the runtime and environment shape matter.
