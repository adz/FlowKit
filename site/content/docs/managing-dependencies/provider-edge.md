---
weight: 40
title: "Level 3: Provider Edge"
description: Using IServiceProvider only at the host boundary.
type: docs
---


`IServiceProvider` belongs at the outer edge of the system.

Use it when:

- the host container is the source of truth
- you are inside ASP.NET, a worker, or another DI-driven runtime
- you need a fallback path from registrations to runtime objects

This is not the primary dependency model for workflows. It is the bridge from host registration to typed flows.

## The Lookup

`Resolver.fromProvider` asks the provider for a service and fails with `MissingCapability` when the service is not registered.

```fsharp
let sendEmail : Flow<IServiceProvider, MissingCapability, IEmailSender> =
    Resolver.fromProvider<IEmailSender>
```

That shape is useful at the boundary because it preserves the failure in the type.

## The Tradeoff

The win is ergonomics:

- very little boilerplate
- easy .NET host integration
- familiar to teams already using Microsoft DI

The cost is honesty:

- the workflow is typed against the provider, not the exact service set
- missing registrations surface at runtime

That is acceptable at the edge, not inside the core of the application.

## Provider To Record

The strongest use of provider access is to map it into a record boundary once and stop there.

```fsharp
type ApiDeps =
    { Orders : IOrderRepository
      Email : IEmailSender
      Clock : IClock }

let mapApiDeps (sp: IServiceProvider) =
    { Orders = sp.GetRequiredService<IOrderRepository>()
      Email = sp.GetRequiredService<IEmailSender>()
      Clock = sp.GetRequiredService<IClock>() }
```

Once you have the record, the rest of the workflow should usually stay on level 1 or 2.

## When Not To Use It

Do not use `IServiceProvider` as the default shape for reusable helpers.

If a helper is reusable, prefer:

- a boundary record
- `RuntimeContext`
- or a small nominal `Requires<'dep>` contract

The provider is the edge, not the center.

See the [Resolver reference](../../reference/capability/) for `Resolver.fromProvider` and `MissingCapability`.
