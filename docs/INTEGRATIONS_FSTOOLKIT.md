---
title: FsToolkit ErrorHandling
description: How FsFlow fits beside existing FsToolkit.ErrorHandling code.
---

# FsToolkit.ErrorHandling

This page shows how FsFlow can fit beside existing `FsToolkit.ErrorHandling` code, especially `AsyncResult`, `TaskResult`, and result-heavy application code.

If you already have a codebase built around `Result`, `Async<Result<_,_>>`, or `Task<Result<_,_>>`, FsFlow does not need to force a rewrite of the pure parts. It can take over the boundary when you want explicit environment threading and typed runtime execution.

`FsToolkit.ErrorHandling` remains a strong fit because it stays close to core types, uses familiar module functions and computation-expression builders, and keeps overhead low for codebases that already speak in `Result`, `AsyncResult`, and `TaskResult`.

## Keep The Pure Pieces Pure

Existing `Result` helpers can stay `Result` helpers.

That means:

- pure validation stays pure
- transformation helpers stay pure
- effectful orchestration moves into `Flow`, `AsyncFlow`, or `TaskFlow`

This mirrors the way `FsToolkit.ErrorHandling` already encourages separation between validation and orchestration.

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

- `Result<'value, unit>` validation helpers become `FsFlow.Validate` calls
- `Async<Result<'value, 'error>>` becomes `AsyncFlow.fromAsyncResult`
- `Task<Result<'value, 'error>>` becomes `TaskFlow.fromTaskResult`
- `Async<Result<'value, unit>>` or `Result<'value, unit>` can use `orElse*` bridges when error creation itself needs environment or runtime work

## Example

The first bridge keeps validation pure and only moves the boundary:

```fsharp
let validateName name =
    if System.String.IsNullOrWhiteSpace name then
        Error ()
    else
        Ok name

let validatedName : Result<string, string> =
    validateName "Ada"
    |> Validate.orElse "name required"
```

Use this shape when the check itself stays simple but the final application error needs to be the one you actually surface.

The next bridge takes an `Async<Result<_,_>>` boundary and keeps the async shape intact:

```fsharp
type AppEnv =
    { Prefix: string
      LoadName: int -> Async<Result<string, string>>
      LoadGreeting: int -> Task<Result<string, string>> }

let loadGreeting : AsyncFlow<AppEnv, string, string> =
    asyncFlow {
        let! env = AsyncFlow.env
        let! loadName = AsyncFlow.read _.LoadName
        let! name = loadName 42 |> AsyncFlow.fromAsyncResult
        return $"{env.Prefix} {name}"
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

If you prefer the explicit bridge in module form, the same task result shape can be lifted with `TaskFlow.fromTaskResult`.

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

Use `TaskFlow.fromTaskResult` when you need the explicit bridge in a non-builder pipeline, and use the direct bind when the code is already clearly task-shaped.

## When FsToolkit Still Wins

Keep `FsToolkit.ErrorHandling` where its existing combinators already make the code clearer.

FsFlow is not trying to delete that ecosystem. It is trying to make the boundary explicit when the runtime and environment shape matter.
