# Effect.FS vs Effect-TS

This project is inspired by Effect-TS, but the goal is not to reproduce that ecosystem in F#.

The useful comparison is about direction, not parity.

Effect-TS is a broad, mature effect platform. Effect.FS is currently a small F#-native core that is trying to make ordinary application code feel better than today's common `Async<Result<_,_>>` style.

## What Effect.FS Takes Seriously From Effect-TS

The inspiration is real in a few places:

- effect values as a central abstraction
- environment / dependency access as part of the model
- typed success and error channels
- practical helpers around operational concerns like timeout and retry

That said, the host language changes everything.

## Why The F# Version Has To Be Different

F# has:

- computation expressions
- `Async`
- `Task`
- strong existing habits around `Result`
- an ecosystem shaped by wrappers like `Async<Result<_,_>>`

So the design question is not:

"How close can this get to Effect-TS?"

It is:

"What is the most natural way to bring effect-style capabilities into normal F# code?"

## Comparable Features Today

Current `Effect.FS` features that map cleanly to core effect-system ideas:

- cold effect values
- typed environment parameter
- typed business error channel
- typed success value
- composition through `effect {}`
- direct binding of `Result`, `Async`, `Async<Result<_,_>>`, and `Task`
- explicit execution with environment and cancellation token
- error mapping and exception capture helpers
- environment projection
- logging helpers
- retry, timeout, and resource-safety helpers

The current core type:

```fsharp
Effect<'env, 'error, 'value>
```

is conceptually similar to a three-parameter effect type in other ecosystems, but the real F# question is whether the CE and interop story are actually better than stacked wrappers.

## Important Differences

### 1. F# ergonomics matter more than conceptual similarity

Effect-TS can rely on TypeScript generators and its own runtime conventions.

Effect.FS has to feel good inside ordinary F# code. That means the project should be judged heavily on:

- builder ergonomics
- naming
- clarity of type flow
- direct handling of `Result`, `Async`, `Async<Result<_,_>>`, and `Task`

### 2. .NET interop is a first-class requirement

This project should integrate cleanly with the rest of .NET, not ask users to isolate themselves inside a separate world.

That means:

- easy use with existing libraries
- straightforward cancellation handling
- minimal friction around `Task`
- a realistic story for mixed F# / C# codebases

### 3. Dependency access should stay explicit

Effect-TS has a rich service/context story.

Effect.FS deliberately stays smaller and more explicit. The current shape is:

- an environment type in the effect itself
- `Effect.environment`, `Effect.read`, and `Effect.withEnvironment`
- no hidden service locator
- no requirement to adopt a runtime container

That is a better fit for early F# usability work than importing a large service platform wholesale.

### 4. The main comparison target is often FsToolkit, not Effect-TS

For many F# users, the practical question is not "should I replace Effect-TS?"

It is:

- is this better than `Async<Result<_,_>>`?
- is this better than FsToolkit-style workflows?
- does this improve dependency and logging ergonomics enough to justify learning it?

That is the comparison Effect.FS has to win first.

## Missing Features Relative to Effect-TS

Effect-TS is much broader today. Missing areas include:

- richer context/service systems
- structured concurrency runtime
- resource scopes and finalizers
- schedules and retry machinery beyond the current minimal helpers
- streams, channels, and broader runtime primitives
- integrated observability tooling
- a mature platform ecosystem

At the moment, `Effect.FS` should be viewed as an F#-focused experiment in practical effect ergonomics, not as a feature-peer to Effect-TS.

## Current Conclusion

- If you want a mature broad effect platform today, Effect-TS is far ahead.
- If you want an F#-native effect library, the real standard is not conceptual ambition alone. It is whether the DX is clearly better than current F# patterns.
- The relevant near-term contest for Effect.FS is FsToolkit-style application code, not the full breadth of the Effect-TS runtime.

That is the standard this project should optimize for.
