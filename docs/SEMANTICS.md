---
weight: 20
title: Execution Semantics
description: Exact execution rules for Flow and ColdTask.
---


# Execution Semantics

This page shows the exact execution model of Flow and ColdTask.

Task-oriented semantics on this page refer to the task surface that ships in the main `FsFlow` package.
The public surface now includes sync, async, and task concepts together.
The task surface means the Flow builder plus its task-aware extension members for `Task`, `ValueTask`, and ColdTask.

## Success And Typed Failure

- `Flow.succeed value` returns `Ok value`
- `fail` produces the typed `Error`
- `map` only transforms successful values
- `mapError` only transforms typed failures
- `tapError` runs only on typed failure and preserves the original error when the tap succeeds
- `orElse` switches to a fallback computation when the first computation returns a typed failure

All of these semantics are short-circuiting.
The first typed failure stops the current workflow unless you explicitly recover from it.

## Short-Circuiting Versus Accumulated Validation

Check, Result, [`result {}`]({{< relref "builders-result.md" >}}), and Flow model ordered workflows.
They stop on the first typed failure unless you explicitly recover from it.

Validation and [`validate {}`]({{< relref "builders-validate.md" >}}) are the accumulating path.
They merge sibling failures into the diagnostics graph instead of stopping at the first one.

Use the short-circuiting model when:

- later steps depend on earlier successful values
- the workflow should stop on the first failure
- you are orchestrating environment access, async work, tasks, retries, cancellation, or resource usage

Use the validation model when:

- independent checks should all run
- you need a structured collection of failures instead of the first failure
- the problem is sibling validation rather than workflow orchestration

## Cold By Default

The flow computation is cold.
Building a computation does not run it.

Rerun behavior:

- rerunning a computation runs it from scratch each time you call `Flow.run`
- direct `Async` or `Task` values bound inside the workflow follow their own execution semantics

The `delay` combinator preserves that behavior in each family.

## Execution Is Explicit

Run the flow with `Flow.run env flow`.
Use `Flow.runFull env cancellationToken flow` when you need to pass an explicit cancellation token.

## Exceptions

`Flow.catch` converts exceptions into typed errors.

This handles exceptions that occur while the computation is being executed.
Typed failures still stay in Result.

## Environments

Flow reads dependencies explicitly:

- `env` reads the whole environment
- `read` projects one dependency
- `localEnv` runs a smaller computation inside a larger environment

## Pairing And Small Composition

Each family also exposes low-ceremony helpers for common composition shapes:

- `zip` runs two computations in sequence and returns a tuple
- `map2` runs two computations in sequence and combines both successful values with a mapper
- both helpers short-circuit on the first typed failure

These helpers are useful when a full computation expression would add more ceremony than value.

## Task Temperature

Flow distinguishes between:

- already-started task values such as `Task<'value>` and `ValueTask<'value>`
- delayed task work represented by `ColdTask<'value>`

`ColdTask<'value>` means:

```fsharp
CancellationToken -> Task<'value>
```

That distinction matters because reruns behave differently:

- rerunning a computation that binds a started `Task` or `ValueTask` re-awaits the same started work
- rerunning a computation that binds a ColdTask calls the factory again

It also affects cancellation:

- a started `Task` or `ValueTask` is already running before the computation executes
- the current computation `CancellationToken` cannot be injected into that already-started work
- a ColdTask starts when the computation runs, so the current computation `CancellationToken` can be passed in

Use a hot task input only when reusing the same already-started work is the behavior you want.
Use ColdTask when the effect should start at computation execution time, rerun from scratch, or observe the runtime cancellation token.

Example with a started task:

```fsharp
let started = Task.FromResult 42

let computation : Flow<unit, string, int> =
    flow {
        let! value = started
        return value
    }
```

Each run re-awaits `started`.
It does not create a new task.

Example with a ColdTask:

```fsharp
let readValue : ColdTask<int> =
    ColdTask(fun cancellationToken ->
        Task.FromResult 42)

let computation : Flow<unit, string, int> =
    flow {
        let! value = readValue
        return value
    }
```

Each run calls the ColdTask factory again and passes in the current computation cancellation token.

## Runtime Helpers

Operational helpers for logging, timeout, retry, and resource handling are grouped into `Runtime` modules:

- **`Flow.Runtime`**: Helpers for Flow computations (cancellation, sleep, log, timeout, retry).
- **`RuntimeContext`**: Helpers for building and reshaping runtime contexts.

There is no `Flow.Runtime` because synchronous flows are intended for pure logic that does not
require operational runtime support like cancellation or timeouts.

## Validation Helpers

`FsFlow.Check` provides pure `Result<'value, unit>` checks for booleans, options, value options,
nulls, collections, equality, and strings.

Use `Check.orError` to attach a typed error after the pure validation step.

When the same source should bind directly in `flow {}`, use `Guard.Of` for `bool`, `option`,
`voption`, `Result<'value, unit>`, and `Validation<'value, unit>` sources, or `Guard.MapError`
when the source already carries an error value.

When the error value itself needs environment or effectful evaluation, use the bridge helpers on
Flow.

Use Validation and [`validate {}`]({{< relref "builders-validate.md" >}}) when the failures should accumulate into a structured
`Diagnostics` graph instead of short-circuiting.

## Family Direction

The flow model intentionally composes upward:

- Flow can lift sync values directly
- Flow can bind `Async`, `Task`, `ValueTask`, and `ColdTask` directly
- keep the smallest honest computation at each boundary

## What The Tests Cover

The test suite currently verifies:

- sync execution
- async and task direct-binding execution
- rerun behavior for `delay`
- direct binding across the supported wrapper shapes
- ColdTask hot and cold adaptation behavior
- cancellation-token propagation into ColdTask
- environment projection through `localEnv`
- option and value-option behavior across all builders

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the computation overview,
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for the direct binding surface,
or [`src/FsFlow/Flow.fs`](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs) and
[`src/FsFlow/Builders.fs`](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Builders.fs)
for the full API surface.
