# Effect.FS And Effect-TS

Read this page if you already know Effect-TS and want to place Effect.FS correctly.

Effect.FS is inspired by Effect-TS, but it is not trying to recreate that ecosystem in F#.

## What Carries Over

These ideas are shared:

- effect values as the center of workflow composition
- typed success and error channels
- explicit dependency access
- helpers for operational concerns such as retry and timeout

## What Is Different

Effect.FS is built for ordinary F# application code, which means:

- computation expressions instead of generator-based syntax
- first-class interop with `Result`, `Async`, and `Task`
- explicit environment records rather than a richer service runtime
- a smaller surface aimed at application workflows, not a broad runtime platform

## How To Compare Them

If you are choosing a library for F#, the useful comparison is usually not "how close is this to Effect-TS?"

The useful comparison is:

- is this clearer than `Async<Result<_,_>>`?
- is this clearer than FsToolkit for the workflows you actually write?
- does the environment model help enough to justify the extra abstraction?

## What Effect-TS Still Has That Effect.FS Does Not

Effect-TS is much broader and more mature. Effect.FS does not try to match:

- a richer service and context system
- structured concurrency runtime features
- broader runtime primitives such as streams and channels
- integrated observability tooling
- a larger ecosystem of packages and patterns

## Practical Takeaway

Use Effect.FS if you want a small F#-native effect library that improves the readability of application workflows involving dependencies, typed failures, and mixed async boundaries.

Do not evaluate it as a feature-peer to Effect-TS. Evaluate it against the F# code you would otherwise write.

## Next

If you are deciding whether to adopt the library, read [`docs/FSTOOLKIT_MIGRATION.md`](./FSTOOLKIT_MIGRATION.md) and [`examples/README.md`](../examples/README.md).
