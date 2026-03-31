# Semantics

Read this page when you need to know exactly how EffectfulFlow behaves around failure, exceptions, cancellation, timeout, and cleanup.

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

It does not turn ordinary typed failures into exceptions or vice versa unless you ask it to.

## Cancellation

Cancellation is explicit in the execution model:

- `Flow.run environment cancellationToken flow` runs with the supplied token
- `Flow.Runtime.cancellationToken` reads that token inside the flow
- `Flow.Runtime.ensureNotCanceled` checks whether the token is already canceled and returns a typed failure if so
- `Flow.Runtime.catchCancellation` translates `OperationCanceledException` into a typed error

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

## Retry Attempts

`RetryPolicy.MaxAttempts` counts total attempts, including the first run.

So:

- `MaxAttempts = 1` means "run once, never retry"
- `MaxAttempts = 3` means "initial run plus up to two retries"

## What The Tests Cover

The test suite currently verifies:

- direct binding from `Result`, `Async`, and `Async<Result<_,_>>`
- cancellation token propagation into task factories
- timeout behavior
- retry attempt counting
- sync and async disposal through builder `use` / `use!`
- exception capture across synchronous and asynchronous boundaries

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the basic flow model, or [`src/EffectfulFlow/Flow.fs`](../src/EffectfulFlow/Flow.fs) for the full API surface.
