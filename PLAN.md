# Effect.FS Plan

## Status

The original four-phase plan in this repository is now complete.

The codebase currently includes:

- the canonical core type `Effect<'env, 'error, 'value>`
- an `effect {}` computation expression with direct binding for `Result`, `Async`, `Async<Result<_,_>>`, `Task<'T>`, `Task`, and `Task<Result<_,_>>`
- explicit environment operations: `environment`, `read`, `provide`, and `withEnvironment`
- a typed logging surface with `LogEntry`, `LogLevel`, `log`, and `logWith`
- interop and migration helpers for FsToolkit-style `Async<Result<_,_>>` code
- cancellation helpers including token access, explicit token-aware execution, cancellation checks, and cancellation capture
- practical runtime helpers: `retry`, `timeout`, `bracket`, `bracketAsync`, `usingAsync`, and `tryFinally`
- runnable tests and a realistic example using gateway and persistence dependencies

## What The Plan Needed To Prove

The central question was:

"Can a small F# effect core beat `Async<Result<_,_>>` plus local conventions in day-to-day application code?"

The answer from the finished repository is:

- yes, when the code has explicit dependencies
- yes, when it mixes pure validation with async and task-based work
- yes, when retry, timeout, logging, and resource cleanup need to live near the workflow
- yes, when typed expected failures should remain distinct from defects and arbitrary exceptions

## Phase Completion

### Phase 1: Sharpen The Core UX

Completed:

- clearer public names such as `fromResult`, `fromAsync`, `fromTask`, and `environment`
- direct `effect {}` binding across the common F# and .NET wrapper shapes
- explicit execution helpers including token-aware execution
- tests that prove the builder ergonomics instead of only describing them

Outcome:

- the happy path reads like ordinary application code rather than wrapper choreography

### Phase 2: Prove Dependency And Logging Ergonomics

Completed:

- first-class environment access and projection helpers
- a minimal but usable logging model
- a realistic example that shows dependency access, logging, retry, timeout, persistence, and cleanup in one workflow

Outcome:

- dependencies stay visible in types
- the code reads like explicit F# wiring rather than a hidden DI container

### Phase 3: Define Compatibility Strategy

Completed:

- partial and explicit compatibility with FsToolkit-style `Async<Result<_,_>>`
- bridge helpers: `fromAsyncResult`, `toAsyncResult`, and `AsyncResultCompat`
- documentation that explains migration and where compatibility intentionally stops

Outcome:

- compatibility is a migration path, not an attempt to clone FsToolkit wholesale

### Phase 4: Deepen Practical Capabilities

Completed:

- `retry`
- `timeout`
- `bracket`
- `bracketAsync`
- `usingAsync`
- cancellation capture and explicit token support
- a more realistic example built around .NET `Task`-based dependencies

Outcome:

- the library is useful for small but real application workflows, not only toy combinator examples

## Product Position

Effect.FS should be judged against:

- plain `Async<Result<_,_>>`
- FsToolkit-style application code
- direct `Task`-based application code in mixed .NET systems

It should not be judged as a feature-peer to large runtimes yet.

The product direction remains:

- compact core
- explicit dependencies
- typed expected failures
- pragmatic .NET interop
- operational concerns modeled close to the workflow

## Error Modeling Position

Typed failures are for expected and modelable failures:

- validation
- parsing and decoding
- classified infrastructure failures
- business rule violations

Typed failures are not for:

- programmer defects
- broken invariants
- indiscriminate wrapping of arbitrary exceptions

The intended style is:

- keep error types small and local
- translate them explicitly between layers
- capture thrown exceptions only at deliberate boundaries

## Discussion Left Beyond The Plan

The original plan is complete, but there are still product decisions that deserve discussion before a larger expansion:

- whether `AsyncResultCompat` should stay in the core package or move to a dedicated compatibility package
- how far cancellation and structured concurrency should go before the library becomes runtime-heavy
- whether richer resource and scope abstractions add enough value over the current small helpers
- what a credible mixed F# / C# story should look like if the library grows beyond F#-only ergonomics
- whether the next examples should be HTTP/database integrations or a thinner application template
