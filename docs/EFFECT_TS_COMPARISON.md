# EffectfulFlow And Effect-TS

Read this page when you already know Effect-TS and want to place EffectfulFlow correctly.

EffectfulFlow presents its core abstraction as a composable `Flow`, but it is still not trying to recreate the full Effect-TS ecosystem in F#.

## What Carries Over

These ideas are shared:

- typed success and error channels
- explicit dependency access
- compositional workflow values
- runtime helpers for retry, timeout, and cancellation-aware execution

## What Is Different

EffectfulFlow is aimed at ordinary F# application code:

- `flow {}` instead of generator-based syntax
- first-class interop with `Result`, `Async`, and `.NET Task`
- explicit environment reads such as `Flow.read _.Gateway`
- a much smaller surface focused on application flows rather than a broader runtime platform

## What Effect-TS Still Has That EffectfulFlow Does Not

Effect-TS is much broader and more mature. EffectfulFlow does not try to match:

- a richer service and context system
- structured concurrency runtime features
- broader runtime primitives such as streams and channels
- integrated observability tooling
- a large package ecosystem

## Practical Comparison

The useful question for F# users is not "how close is this to Effect-TS?"

The useful questions are:

- is this clearer than `Async<Result<_,_>>` for the flows you actually write?
- do explicit env requirements help enough to justify the abstraction?
- does `Flow.Task` and `Flow.Runtime` make mixed `.NET` code easier to keep readable?

## Practical Takeaway

Use EffectfulFlow if you want a small F#-native library for composable flows with explicit dependencies, typed failures, explicit cancellation, and direct `.NET` interop.

Do not evaluate it as a feature-peer to Effect-TS. Evaluate it against the F# code you would otherwise write.

## Next

If you are deciding whether to adopt the library, read [`docs/FSTOOLKIT_MIGRATION.md`](./FSTOOLKIT_MIGRATION.md) and [`examples/README.md`](../examples/README.md).
