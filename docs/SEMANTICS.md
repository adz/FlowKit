# Semantics

Read this page when you need to know exactly how Effect.FS behaves around failure, exceptions, cancellation, timeout, and cleanup.

## Success And Typed Failure

- `Effect.succeed value` returns `Ok value`.
- `Effect.fail error` returns `Error error`.
- `map` changes only the success value.
- `mapError` changes only the typed error.
- `bind` stops on the first typed error and does not run the next step.

## Exceptions

Exceptions are not turned into typed errors automatically.

That is deliberate. Expected failures should live in the typed error channel. Defects and unexpected exceptions should stay visible unless you decide to translate them.

Use:

- `Effect.catch` to convert exceptions into typed errors
- `Effect.catchCancellation` to convert `OperationCanceledException` into typed errors

`Effect.catch` can handle exceptions thrown:

- before async work starts
- during async or task execution

## Cancellation

Cancellation is explicit:

- `Effect.execute` runs with `CancellationToken.None`
- `Effect.executeWithCancellation` passes the provided token through the workflow
- `Effect.cancellationToken` reads that token inside the workflow
- `Effect.ensureNotCanceled` checks whether the token is already canceled and returns a typed error if so
- `Effect.catchCancellation` translates `OperationCanceledException` into a typed error

If a task or async operation ignores cancellation, Effect.FS does not invent cancellation behavior for it.

## Timeout

`Effect.timeout after timeoutError workflow` returns `Error timeoutError` when the workflow does not complete before the timeout.

Important:

- timeout does not automatically cancel the underlying work
- timeout only changes the result observed by the caller
- if you need actual cancellation, combine timeout with a dependency or workflow that observes cancellation tokens

## Cleanup

Cleanup helpers always run their release logic after acquisition:

- `Effect.tryFinally` runs the compensation action on success, typed failure, and exception
- `Effect.bracket` runs the synchronous release action on success, typed failure, and exception
- `Effect.bracketAsync` runs the asynchronous release action on success, typed failure, exception, and cancellation
- `Effect.usingAsync` is built on `bracketAsync` and disposes the resource after use

If the release function itself throws, that exception escapes just like any other defect.

## Retry

`RetryPolicy.MaxAttempts` counts the total number of executions, including the first one.

That means:

- `MaxAttempts = 1` means "run once, no retries"
- `MaxAttempts = 3` means "first run plus up to two retries"

Retries happen only when:

- the workflow returns `Error error`
- the current attempt is below `MaxAttempts`
- `ShouldRetry error` returns `true`

## Cold And Hot Tasks

Task helpers come in two groups:

- cold factories such as `fromTask`, `fromTaskResult`, `fromColdTask`, and `fromColdTaskResult`
- already-created task values such as `fromTaskValue`, `fromTaskResultValue`, and `fromTaskUnit`

Use cold factories for dependency calls so work starts when the effect executes.

Use task-value helpers only when you already have a task value on purpose.

## Verification In This Repo

The test suite covers:

- timeout returning a typed error
- timeout not canceling underlying work by itself
- cleanup on success, typed failure, and cancellation
- exception capture in synchronous and asynchronous cases
- cancellation token propagation into task factories

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the basic workflow model, or [`src/EffectFs/Effect.fs`](../src/EffectFs/Effect.fs) for the full API surface.
