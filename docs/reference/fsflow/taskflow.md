---
title: TaskFlow
description: Source-documented task workflow surface in FsFlow.
---

# TaskFlow

This page shows the source-documented task-oriented surface: the runtime context, cold task helper, task flow module, and the task-specific runtime helpers.

## Runtime context

- type `RuntimeContext`: Captures the two-context shape of a task workflow execution: runtime services, application capabilities, and the cancellation token for the current run. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L14)
- module `RuntimeContext`: Helpers for building and reshaping `RuntimeContext{runtime, env}` values. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L29)
- `RuntimeContext.create`: Creates a runtime context from the supplied runtime services, environment, and cancellation token. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L31)
- `RuntimeContext.runtime`: Reads the runtime half of a runtime context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L43)
- `RuntimeContext.environment`: Reads the application environment half of a runtime context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L46)
- `RuntimeContext.cancellationToken`: Reads the cancellation token stored in a runtime context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L49)
- `RuntimeContext.mapRuntime`: Maps the runtime half of a runtime context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L52)
- `RuntimeContext.mapEnvironment`: Maps the application environment half of a runtime context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L63)
- `RuntimeContext.withRuntime`: Replaces the runtime half of a runtime context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L74)
- `RuntimeContext.withEnvironment`: Replaces the environment half of a runtime context. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs#L81)

## ColdTask

- type `ColdTask`: Represents delayed task work that can observe a runtime cancellation token when it is started. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L23)
- module `ColdTask`: Core functions for creating and executing cold tasks. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L31)
- `ColdTask.run` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L55)
- `ColdTask.create` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L32)
- `ColdTask.fromTaskFactory` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L35)
- `ColdTask.fromTask` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L38)
- `ColdTask.fromValueTaskFactory` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L41)
- `ColdTask.fromValueTaskFactoryWithoutCancellation` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L46)
- `ColdTask.fromValueTask` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L51)

## TaskFlow

- type `TaskFlow`: Represents a cold task-based workflow that reads an environment, observes a runtime cancellation token, returns a typed result, and is executed explicitly through `TaskFlow.run`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L15)
- module `TaskFlow`: Core functions for creating, composing, executing, and adapting task flows. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L63)
- `TaskFlow.run` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L64)
- `TaskFlow.runContext`: Runs a task flow against a runtime context and its cancellation token. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L72)
- `TaskFlow.toTask` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L78)
- `TaskFlow.succeed` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L85)
- `TaskFlow.fail` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L88)
- `TaskFlow.fromResult` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L91)
- `TaskFlow.fromOption` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L94)
- `TaskFlow.fromValueOption` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L99)
- `TaskFlow.orElseTask` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L104)
- `TaskFlow.orElseAsync` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L116)
- `TaskFlow.orElseFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L128)
- `TaskFlow.orElseAsyncFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L142)
- `TaskFlow.orElseTaskFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L160)
- `TaskFlow.fromFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L176)
- `TaskFlow.fromAsyncFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L179)
- `TaskFlow.fromTask` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L184)
- `TaskFlow.fromTaskResult` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L191)
- `TaskFlow.env` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L196)
- `TaskFlow.read` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L199)
- `TaskFlow.readRuntime`: Reads the runtime half of a runtime-context environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L203)
- `TaskFlow.readEnvironment`: Reads the application environment half of a runtime-context environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L209)
- `TaskFlow.map` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L214)
- `TaskFlow.bind` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L229)
- `TaskFlow.tap` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L248)
- `TaskFlow.tapError` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L258)
- `TaskFlow.mapError` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L276)
- `TaskFlow.catch` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L291)
- `TaskFlow.orElse` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L303)
- `TaskFlow.zip` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L316)
- `TaskFlow.map2` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L326)
- `TaskFlow.localEnv` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L334)
- `TaskFlow.delay` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L361)
- `TaskFlow.traverse`: Transforms a sequence of values into a task flow and stops at the first failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L369)
- `TaskFlow.sequence`: Transforms a sequence of task flows into a task flow of a sequence and stops at the first failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L392)

## Task runtime helpers

- `TaskFlow.Runtime`: Task-native runtime helpers for operational concerns like logging, timeout, retry, and scoped cleanup. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L406)
- `TaskFlow.Runtime.cancellationToken`: Reads the current runtime cancellation token. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L408)
- `TaskFlow.Runtime.catchCancellation`: Converts an `OperationCanceledException` into a typed error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L412)
- `TaskFlow.Runtime.ensureNotCanceled`: Returns a typed error immediately when the runtime token is already canceled. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L425)
- `TaskFlow.Runtime.sleep`: Suspends the flow for the specified duration while observing cancellation. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L433)
- `TaskFlow.Runtime.log`: Writes a fixed log message through the environment-provided logger. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L441)
- `TaskFlow.Runtime.logWith`: Writes a log message computed from the current environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L456)
- `TaskFlow.Runtime.useWithAcquireRelease`: Acquires a resource, uses it, and always runs the release action. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L471)
- `TaskFlow.Runtime.timeout`: Fails with the supplied error when the flow does not complete before the timeout. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L496)
- `TaskFlow.Runtime.timeoutToOk`: Returns the supplied success value when the flow times out. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L514)
- `TaskFlow.Runtime.timeoutToError`: Forwards to `timeout` for a typed failure on timeout. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L532)
- `TaskFlow.Runtime.timeoutWith`: Runs a fallback flow when the original flow times out. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L540)
- `TaskFlow.Runtime.retry`: Retries a flow according to the supplied policy. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L558)

## TaskFlowSpec

- type `TaskFlowSpec`: Describes a task-flow program that is built against a runtime context and later executed with a cancellation token. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L592)
- module `TaskFlowSpec`: Helpers for creating and running `TaskFlowSpec{runtime, env, error, value}` values. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L607)
- `TaskFlowSpec.create`: Creates a task-flow spec from runtime services, application dependencies, and a build function. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L609)
- `TaskFlowSpec.run`: Runs the spec with the supplied cancellation token. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L621)

## Capabilities and layers

- module `Capability`: Capability helpers for record-based environments and .NET service-provider interop. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L633)
- `Capability.MissingCapability`: Describes a missing service-provider capability. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L635)
- `Capability.service`: Reads a capability from a record-based environment projection. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L642)
- `Capability.runtime`: Reads a capability from the runtime half of a two-context runtime environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L646)
- `Capability.environment`: Reads a capability from the application half of a two-context runtime environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L652)
- `Capability.serviceFromProvider`: Reads a service from `IServiceProvider` and fails when it is not registered. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L658)
- type `Layer`: Layer helpers for deriving an environment in one flow and consuming it in another. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L675)

## Entry points

The task-specific builder entry points stay as syntax on top of the module surface, while the extension modules handle the extra task and async interop shapes.

- `Builders.asyncFlow`: The .NET-extended `asyncFlow { }` computation expression. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L1112)
- `Builders.taskFlow`: The .NET `taskFlow { }` computation expression. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L1117)
- module `TaskFlowBuilderExtensions` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L907)
- module `AsyncFlowBuilderExtensions` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L1034)

## Source

- [TaskFlow.fs](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs)
- [Runtime.fs](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Runtime.fs)
