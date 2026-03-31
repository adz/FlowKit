# Dotnet First

Read this page when you want to know how much of EffectfulFlow is still intended to be runtime-neutral.

The current answer is: not much.

The library is now `.NET`-first:

- the core `Flow` execution model includes `CancellationToken`
- task interop is part of the main public surface
- runtime helpers are designed around `.NET` execution semantics

That does not mean every helper is task-specific. The core still composes ordinary `Result` and `Async` values directly. But portability is no longer the main design constraint.

The stable public surface is:

- `Flow`
- `Flow.Task`
- `Flow.Runtime`

If a future Fable-focused library appears, it should be a separate design rather than a forced portability layer over this API.

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the public API, then [`README.md`](../README.md) for the repo-level overview.
