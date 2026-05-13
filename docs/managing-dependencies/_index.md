---
weight: 40
title: Managing Dependencies
description: A staged guide to dependency management in FsFlow.
---

# Managing Dependencies

FsFlow does not force one dependency style on every workflow. It gives you a ladder:

1. area-scoped records at the boundary
2. `RuntimeContext<'runtime, 'env>` when the host and app should be separate
3. `IServiceProvider` at the outer edge when the host container is the source of truth
4. nominal `Requires<'dep>` helpers when reuse beats plain record passing

Start with the shallowest level that fits the boundary you are designing. Move deeper only when you can name the pressure that makes the simpler shape awkward.

## Read In Order

| Level | Shape | Best fit | Main APIs | Page |
| :--- | :--- | :--- | :--- | :--- |
| 1 | Area-scoped records | Controllers, jobs, integrations, feature boundaries | `Flow.read`, `Flow.localEnv`, `Flow.provideLayer` | [Level 1: Area-Scoped Records](./env-slicing/) |
| 2 | `RuntimeContext<'runtime, 'env>` | Host services and app dependencies need separate ownership | `RuntimeContext.create`, `Resolver.runtime`, `Resolver.environment`, `TaskFlow.readRuntime`, `TaskFlow.readEnvironment` | [Level 2: RuntimeContext](./runtime-context/) |
| 3 | `IServiceProvider` edge | ASP.NET, hosted services, DI-heavy hosts | `Resolver.fromProvider`, `MissingCapability` | [Level 3: Provider Edge](./provider-edge/) |
| 4 | Nominal capability helpers | Reusable helpers that need a named contract | `Requires<'dep>`, `Resolver.resolve` | [Level 4: Nominal Capability Helpers](./capability-contracts/) |

## The Pedagogical Order

1. Start with records that are scoped to a controller, job, or integration boundary.
2. Split runtime services from application dependencies only when that separation is real.
3. Use provider lookup only when the host container must remain the root of truth.
4. Reach for nominal helpers only when a shared contract is clearly paying rent.

## What `RuntimeContext` Is Doing Here

`RuntimeContext<'runtime, 'env>` is not a replacement for level 1. It is a different split:

- `Runtime` holds operational concerns such as logging, metrics, tracing, and clocks.
- `Environment` holds the application dependencies.
- `CancellationToken` belongs to the current task run.

That split matters when one host should feed many areas, or when the app record should stay focused while the runtime keeps expanding.

## Bridges, Not A Fifth Level

`Flow.localEnv` and `Flow.provideLayer` are bridges between shapes, not another dependency model.

- Use `localEnv` when a larger record can be projected to a smaller record.
- Use `provideLayer` when one flow builds the environment needed by another.
- Use `Resolver.resolve` when the workflow should read a dependency through the same vocabulary regardless of which level you are on.

```fsharp
let controllerWorkflow : Flow<ControllerDeps, Error, string> =
    flow {
        let! logger = Flow.read _.Logger
        logger.Info "starting"
        return "ok"
    }
```

The deeper levels exist to keep the boundary honest. They are not a license to make every workflow look the same.
