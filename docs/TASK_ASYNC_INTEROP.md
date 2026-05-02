---
title: Task and Async Interop
description: Direct binding rules for async and task work in FsFlow.
---

# Task and Async Interop

This page shows the direct binding surface for async work and helps you choose the right FsFlow family.

Task-oriented APIs on this page belong in the `FsFlow.Net` package.
The core `FsFlow` package keeps only sync and `Async` concepts.

## The Main Rule

Choose the family that matches the runtime shape of the boundary itself:

- `Flow` for synchronous boundaries
- `AsyncFlow` for `Async`-based boundaries
- `TaskFlow` for `.NET Task`-based boundaries

Use interop to cross boundaries.
Avoid keeping a task-oriented boundary in `Flow` just because a helper can be adapted.

Use `Check` for reusable predicates, `Result` for fail-fast validation, and `Validation` plus
`validate {}` when sibling failures should accumulate.

## Preferred Style Inside Computation Expressions

Inside `flow {}`, `asyncFlow {}`, and `taskFlow {}`, prefer direct binding:

```fsharp
taskFlow {
    let! user = loadUser
    do! validateUser user
    let! suffix = coldSuffix
    return user.Name + suffix
}
```

The builders already bind `Result`, `Flow`, `AsyncFlow`, `Task`, `Task<Result<_,_>>`, and `ColdTask` where supported.
That means normal docs examples should not reach for `TaskFlow.fromFlow` or `AsyncFlow.fromResult` inside the computation expression unless the point of the example is the bridge API itself.

## Auto-Lifts At A Glance

These are the values you can usually drop directly into `let!` in each builder.
The option and value-option cases auto-lift only when the computation error type is `unit`.

| Builder | Auto-lifts directly |
| --- | --- |
| `flow {}` | `Flow<'env, 'error, 'value>`, `Result<'value, 'error>`, `option<'value>` when `error = unit`, `voption<'value>` when `error = unit` |
| `asyncFlow {}` in core `FsFlow` | `Flow<'env, 'error, 'value>`, `AsyncFlow<'env, 'error, 'value>`, `Async<'value>`, `Async<Result<'value, 'error>>`, `Result<'value, 'error>`, `option<'value>` when `error = unit`, `voption<'value>` when `error = unit` |
| `asyncFlow {}` with `FsFlow.Net` referenced | everything above, plus `Task<'value>`, `Task<Result<'value, 'error>>`, `ValueTask<'value>`, `ValueTask<Result<'value, 'error>>`, `ColdTask<'value>`, `ColdTask<Result<'value, 'error>>` |
| `taskFlow {}` | `Flow<'env, 'error, 'value>`, `AsyncFlow<'env, 'error, 'value>`, `TaskFlow<'env, 'error, 'value>`, `Async<'value>`, `Async<Result<'value, 'error>>`, `Task<'value>`, `Task<Result<'value, 'error>>`, `ValueTask<'value>`, `ValueTask<Result<'value, 'error>>`, `ColdTask<'value>`, `ColdTask<Result<'value, 'error>>`, `Result<'value, 'error>`, `option<'value>` when `error = unit`, `voption<'value>` when `error = unit` |

### `flow {}`

The sync builder binds:

- `Flow<'env, 'error, 'value>`
- `Result<'value, 'error>`
- `Option<'value>` when the error type is `unit`
- `ValueOption<'value>` when the error type is `unit`

Use `flow {}` when the body is synchronous.

### `asyncFlow {}`

In the core `FsFlow` package, `asyncFlow {}` binds:

- `Flow<'env, 'error, 'value>`
- `AsyncFlow<'env, 'error, 'value>`
- `Async<'value>`
- `Async<Result<'value, 'error>>`
- `Result<'value, 'error>`
- `Option<'value>` when the error type is `unit`
- `ValueOption<'value>` when the error type is `unit`

When `FsFlow.Net` is referenced, `asyncFlow {}` also binds task-oriented inputs such as:

- `Task<'value>`
- `Task<Result<'value, 'error>>`
- `ValueTask<'value>`
- `ValueTask<Result<'value, 'error>>`
- `ColdTask<'value>`
- `ColdTask<Result<'value, 'error>>`

Example:

```fsharp
let computation : AsyncFlow<unit, string, string> =
    asyncFlow {
        let! a = async { return "a" }
        let! b = async { return Ok "b" }
        return a + b
    }
```

### `taskFlow {}`

`taskFlow {}` binds:

- `Flow<'env, 'error, 'value>`
- `AsyncFlow<'env, 'error, 'value>`
- `TaskFlow<'env, 'error, 'value>`
- `Async<'value>`
- `Async<Result<'value, 'error>>`
- `Task<'value>`
- `Task<Result<'value, 'error>>`
- `ValueTask<'value>`
- `ValueTask<Result<'value, 'error>>`
- `ColdTask<'value>`
- `ColdTask<Result<'value, 'error>>`
- `Result<'value, 'error>`
- `Option<'value>` when the error type is `unit`
- `ValueOption<'value>` when the error type is `unit`

Example:

```fsharp
let computation : TaskFlow<unit, string, int> =
    taskFlow {
        let! value = Task.FromResult 42
        return value
    }
```

## When Explicit Lifting Still Matters

For some types you only get the direct auto-lift when the computation error type can stay `unit`.
Use an explicit lift when you want to choose the error value yourself.

For example, `option<'value>` can bind directly in a `unit`-error computation:

```fsharp
let maybeName : string option = None

let autoLifted : Flow<unit, unit, string> =
    flow {
        let! name = maybeName
        return name
    }
```

If you want a typed error such as `"name is required"`, use `Flow.fromOption`, `AsyncFlow.fromOption`, or `TaskFlow.fromOption` instead:

```fsharp
let maybeName : string option = None

let typedError : Flow<unit, string, string> =
    flow {
        let! name = maybeName |> Flow.fromOption "name is required"
        return name
    }
```

Another approach to the same type is to user `orElse`

```fsharp
let typedError : Flow<unit, string, string> =
    flow {
        let! name = maybeName |> okIfSome |> orElse "name is required"
        return name
    }
```

## When To Choose `AsyncFlow`

Prefer `AsyncFlow` when:

- the outer application code already uses `Async`
- you want to stay in core `FsFlow`
- `Async` is the execution model for the computation

Use `AsyncFlow.toAsync` to run it.

## When To Choose `TaskFlow`

Prefer `TaskFlow` when:

- the public boundary is `.NET Task`
- task interop is central to the computation
- runtime cancellation belongs in execution
- `Task` is the execution model for the computation.

Use `TaskFlow.toTask` to run it.

Use `Flow.Runtime` or `AsyncFlow.Runtime` for shared operational helpers like `sleep`, `timeout`, `retry`, and `useWithAcquireRelease`.

Use `TaskFlow.Runtime` when you want the same helpers in a task-native shape.

Use `FsFlow.Check` for pure `Result<'value, unit>` validation.
Use `Result.mapErrorTo` when you want to turn a unit failure into a domain error.
Use `Validation` and `validate {}` when the checks should accumulate.

The builders bind `Result` directly, so extra bridge calls are only needed when the error value itself needs a different conversion shape.

## `ColdTask<'value>`

`ColdTask<'value>` is the delayed task shape used by `FsFlow.Net`:

```fsharp
CancellationToken -> Task<'value>
```

Use it when a helper can stay task-based but delayed until the boundary runs.

Example:

```fsharp
let readAll path : ColdTask<string> =
    ColdTask(fun ct -> System.IO.File.ReadAllTextAsync(path, ct))

let computation : TaskFlow<unit, string, string> =
    taskFlow {
        let! text = readAll "config.json"
        return text
    }
```

## Hot `Task` And `ValueTask` Versus `ColdTask`

Binding a started `Task<'value>` or `ValueTask<'value>` is not the same as binding a `ColdTask<'value>`.

Started task inputs are hot:

- the work may already be running before the boundary starts
- rerunning the boundary re-awaits the same started work
- the current cancellation token cannot be pushed into that work after the fact

`ColdTask` inputs are cold:

- the work starts when the boundary runs
- rerunning the boundary starts the work again from scratch
- the current cancellation token is passed into the `ColdTask` factory

Use a direct `Task` or `ValueTask` bind when you intentionally want to reuse existing started work.

Use `ColdTask` when the task helper is part of the boundary effect and can stay delayed, restartable, and cancellation-aware.

Example with a started task:

```fsharp
let started = Task.FromResult 42

let computation : TaskFlow<unit, string, int> =
    taskFlow {
        let! value = started
        return value
    }
```

Example with delayed task work:

```fsharp
let loadValue : ColdTask<int> =
    ColdTask(fun cancellationToken ->
        Task.FromResult 42)

let computation : TaskFlow<unit, string, int> =
    taskFlow {
        let! value = loadValue
        return value
    }
```

Read [`docs/SEMANTICS.md`](./SEMANTICS.md) when you need the exact rerun and cancellation behavior.

## Choosing Quickly

Use:

- `Flow` when the boundary is sync
- `AsyncFlow` when the boundary is `Async`-first
- `TaskFlow` when the boundary is `Task`-first
- `ColdTask<'value>` when a task helper can stay delayed, rerunnable, and cancellable at run time

If you are unsure between `AsyncFlow` and `TaskFlow`, choose the one that matches the boundary you
need to return and run today.

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the family overview,
[`docs/TROUBLESHOOTING_TYPES.md`](./TROUBLESHOOTING_TYPES.md) when the compiler complains,
and [`docs/SEMANTICS.md`](./SEMANTICS.md) for the exact runtime behavior.
