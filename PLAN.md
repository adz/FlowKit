# Effect.FS Plan

## Status

The initial implementation plan in this repository is complete.

The repo now has:

- a canonical public core type: `Effect<'env, 'error, 'value>`
- an ergonomic `effect {}` builder
- direct binding and lifting for `Result`, `Async`, `Async<Result<_,_>>`, `Task<'T>`, and `Task`
- explicit environment access with `Effect.environment`, `Effect.read`, `Effect.provide`, and `Effect.withEnvironment`
- a basic logging story with `LogEntry`, `LogLevel`, `Effect.log`, and `Effect.logWith`
- practical helpers for `retry`, `timeout`, `bracket`, and `tryFinally`
- runnable tests and examples that demonstrate the intended usage style
- written positioning relative to FsToolkit and Effect-TS

## What This Plan Was Trying To Prove

The key question was never whether an effect type could be represented in F#.

The real question was:

"Can a small effect core make normal F# application code easier to read and evolve than `Async<Result<_,_>>` plus local conventions?"

The current repo answers that question with a deliberate "yes, for the right kind of code":

- code with explicit dependency requirements
- code that mixes pure validation with async and task-based work
- code that needs operational behaviors like retry, timeout, and logging
- code that benefits from visible typed failure boundaries

## Delivered Outcomes By Phase

### Phase 1: Core UX

Delivered:

- clearer public names such as `fromResult`, `fromAsync`, `fromTask`, and `environment`
- direct `effect {}` binding for `Result`, `Async`, `Async<Result<_,_>>`, and `Task`
- explicit execution and conversion helpers
- tests that exercise the builder ergonomics directly

Result:

- ordinary workflows read like application code instead of wrapper choreography

### Phase 2: Dependency And Logging Ergonomics

Delivered:

- explicit environment access and projection helpers
- a small logging model
- an example that uses environment, logging, retry, timeout, and typed failures together

Result:

- dependencies stay visible in types
- the library supports application-style wiring without drifting into hidden DI

### Phase 3: Compatibility Strategy

Delivered:

- explicit compatibility guidance for FsToolkit users
- bridge functions for `Async<Result<_,_>>`
- documentation that defines where compatibility stops

Result:

- compatibility is partial and intentional
- the goal is migration and interop, not cloning the FsToolkit programming model wholesale

### Phase 4: Practical Capabilities

Delivered:

- `retry`
- `timeout`
- `bracket`
- `tryFinally`

Result:

- the library is useful for small but real application workflows, not just toy mapping examples

## Product Position

Effect.FS should be judged against:

- plain `Async<Result<_,_>>`
- FsToolkit-style workflow code
- direct `Task`-based application code in mixed .NET systems

It should not be judged as a feature-peer to larger runtimes yet.

The point is not to build the biggest abstraction surface first.

The point is to have a compact F#-native core that is obviously useful in day-to-day code.

## Error Modeling Position

Typed failures are for expected, modelable failures:

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

## What Comes Next

The next roadmap should be treated as follow-on work, not part of the completed initial plan:

- improve cancellation ergonomics
- add richer resource helpers where the benefit is clear
- explore more realistic HTTP and persistence examples
- decide whether a separate compatibility package is worthwhile
- evaluate how far structured concurrency should go in an F#-native design
