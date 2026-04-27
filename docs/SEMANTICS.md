# Semantics

This page describes how FsFlow behaves around failure, exceptions, cancellation,
timeout, and cleanup.

FsFlow does not replace the underlying async or task machinery.
It composes existing `Result`, `Async`, and `.NET Task` behavior behind one workflow type,
while keeping execution explicit through `Flow.toAsync`.

## Success And Typed Failure

- `Flow.succeed value` returns `Ok value`
- `Flow.fail error` returns `Error error`
- `Flow.map` only transforms successful values
- `Flow.mapError` only transforms typed failures

## Exceptions

Use `Flow.catch` to convert exceptions into typed errors.

`Flow.catch` can handle exceptions thrown:

- while building delayed work
- during async execution
- during task execution lifted through `Flow.Task`
- before an awaited async or task boundary produces a value
- after the workflow has already entered async execution

It does not turn ordinary typed failures into exceptions or vice versa unless you ask it to.

## Cancellation

Cancellation is explicit in the execution model:

- `Flow.toAsync environment cancellationToken flow` runs with the supplied token
- `Flow.Runtime.cancellationToken` reads that token inside the flow
- `Flow.Runtime.ensureNotCanceled` checks whether the token is already canceled and returns a typed failure if so
- `Flow.Runtime.catchCancellation` translates `OperationCanceledException` into a typed error

`Flow.Runtime.catchCancellation` handles cancellation thrown while the workflow is already running:

- from `Flow.Runtime.sleep`
- from cold task factories that observe the runtime token
- from task or async work that raises `OperationCanceledException`

`Flow.Runtime.ensureNotCanceled` does not catch exceptions.
It checks the token up front and returns a typed error immediately if cancellation has already been requested.

If a task or async operation ignores cancellation, Flow does not invent cancellation behavior for it.

## Timeout

`Flow.Runtime.timeout after timeoutError flow` returns `Error timeoutError` when the flow does not complete before the timeout.

Timeout does not cancel underlying work by itself. If the underlying work continues independently, it may still complete later.

## Cleanup

- `Flow.tryFinally` runs the compensation action on success, typed failure, and exception
- builder `use` and `use!` dispose resources after the flow body completes
- when a resource implements `IAsyncDisposable`, the builder prefers async disposal
- `Flow.Runtime.useWithAcquireRelease` runs the release action on success, typed failure, exception, and cancellation

## Task Temperature

Task helpers come in two groups:

- cold factories such as `Flow.Task.fromCold` and `Flow.Task.fromColdResult`
- already-created task values such as `Flow.Task.fromHot` and `Flow.Task.fromHotResult`

Use cold task helpers when work should start at flow execution time.

Use hot task helpers only when you already have a task value on purpose.

`ColdTask<'value>` binds directly in `flow {}`. `ColdTaskResult<'value, 'error>` stays
explicit through `Flow.Task.fromColdResult` so result-shaped cold task functions do not create
ambiguous builder behavior.

## Retry Attempts

`RetryPolicy.MaxAttempts` counts total attempts, including the first run.

So:

- `MaxAttempts = 1` means "run once, never retry"
- `MaxAttempts = 3` means "initial run plus up to two retries"

## What The Tests Cover

The test suite currently verifies:

- direct binding from `Result`, `Async`, `Async<Result<_,_>>`, `Task`, and `ColdTask`
- cancellation token propagation into task factories
- timeout behavior
- retry attempt counting
- sync and async disposal through builder `use` / `use!`
- explicit release on cancellation through `useWithAcquireRelease`
- exception capture across synchronous and asynchronous boundaries

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the basic flow model, or [`src/FsFlow/Flow.fs`](../src/FsFlow/Flow.fs) for the full API surface.
