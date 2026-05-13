---
weight: 30
title: "Level 2: RuntimeContext"
description: Splitting runtime services from application dependencies.
---

# Level 2: RuntimeContext

Use `RuntimeContext<'runtime, 'env>` when the host concerns and application concerns should be separate.

This is the right level when:

- logging and metrics come from the host
- your application dependencies belong to the feature or boundary
- the runtime should be shared across multiple areas
- you want the cancellation token carried with the task execution model

`RuntimeContext` is not a replacement for area-scoped records. It is a deliberate second axis.

## What Goes Where

- `Runtime` holds operational concerns such as logging, metrics, tracing, and clocks.
- `Environment` holds application dependencies.
- `CancellationToken` belongs to the active run.

```fsharp
type RuntimeServices =
    { Log : LogEntry -> unit
      Clock : IClock }

type ApiDeps =
    { Orders : IOrderRepository
      Email : IEmailSender }

let context =
    RuntimeContext.create runtime apiDeps cancellationToken
```

## Reading The Split

`Resolver.runtime` and `Resolver.environment` are the `Flow`-level readers for this split.

```fsharp
let workflow : Flow<RuntimeContext<RuntimeServices, ApiDeps>, string, Guid> =
    flow {
        let! runtime = Resolver.runtime()
        let! deps = Resolver.environment()

        runtime.Log { Level = LogLevel.Information; Message = "starting"; TimestampUtc = runtime.Clock.UtcNow() }

        let! order = deps.Orders.Create()
        do! deps.Email.SendConfirmation order
        return order.Id
    }
```

## Task-Based Helpers

On the task-based surface, `TaskFlow` exposes matching helpers:

- `TaskFlow.readRuntime`
- `TaskFlow.readEnvironment`

Those are the direct equivalents when the workflow itself is task-shaped.

## What Works With RuntimeContext

Works with any environment:

- `Flow.read`
- `Flow.localEnv`
- `Flow.provideLayer`
- `Resolver.resolve`
- `Resolver.fromProvider`

RuntimeContext-specific:

- `RuntimeContext.create`
- `RuntimeContext.runtime`
- `RuntimeContext.environment`
- `RuntimeContext.cancellationToken`
- `Resolver.runtime`
- `Resolver.environment`
- `TaskFlow.readRuntime`
- `TaskFlow.readEnvironment`

This is the key distinction: the general helpers work on any environment, while the runtime split helpers only make sense when the environment is actually a `RuntimeContext`.

## When To Stop

If the runtime/app split is only there because “it sounds cleaner,” stop and use level 1 records.

Use `RuntimeContext` only when the host-owned services really need their own lane.

See the [RuntimeContext reference](../../reference/runtime/) for the constructors and mapping helpers, and the [Resolver reference](../../reference/capability/) for the `runtime`, `environment`, and `resolve` readers.
