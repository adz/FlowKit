---
title: "RuntimeContext"
---

The `RuntimeContext` type and module split host services from application dependencies and carry the cancellation token for task-based execution.

## Core type

- [`RuntimeContext`](./t-runtimecontext-2.md): 
 Captures the two-context shape of a task workflow execution:
 runtime services, application capabilities, and the cancellation token for the current run.
 

## Module functions

- [`RuntimeContext.create`](./m-runtimecontext-create.md): Creates a runtime context from the supplied runtime services, environment, and cancellation token.
- [`RuntimeContext.runtime`](./m-runtimecontext-runtime.md): Reads the runtime half of a runtime context.
- [`RuntimeContext.environment`](./m-runtimecontext-environment.md): Reads the application environment half of a runtime context.
- [`RuntimeContext.cancellationToken`](./m-runtimecontext-cancellationtoken.md): Reads the cancellation token stored in a runtime context.
- [`RuntimeContext.mapRuntime`](./m-runtimecontext-mapruntime.md): Maps the runtime half of a runtime context.
- [`RuntimeContext.mapEnvironment`](./m-runtimecontext-mapenvironment.md): Maps the application environment half of a runtime context.
- [`RuntimeContext.withRuntime`](./m-runtimecontext-withruntime.md): Replaces the runtime half of a runtime context.
- [`RuntimeContext.withEnvironment`](./m-runtimecontext-withenvironment.md): Replaces the environment half of a runtime context.

