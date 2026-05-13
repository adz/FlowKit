---
weight: 20
title: "Level 1: Area-Scoped Records"
description: The first and simplest dependency shape in FsFlow.
type: docs
---


The first dependency shape in FsFlow is a record scoped to a boundary, not one global application bag.

That boundary is usually one of these:

- an ASP.NET controller
- a background job
- an integration adapter
- a feature module with a stable internal surface

The point is not to avoid records. The point is to avoid one record that owns every concern in the system.

## Why This Is The Right Default

Area-scoped records keep ownership local.

- Controllers can depend on controller-shaped records.
- Jobs can depend on job-shaped records.
- Integrations can depend on adapter-shaped records.

That gives you a concrete boundary to map at the edge and a smaller surface to test.

## The Shape

```fsharp
type SubmitOrderDeps =
    { Orders : IOrderRepository
      Email : IEmailSender
      Log : LogEntry -> unit }

type BillingJobDeps =
    { Invoices : IInvoiceRepository
      Clock : IClock
      Log : LogEntry -> unit }

type InventorySyncDeps =
    { Api : IInventoryApi
      Store : IInventoryStore }
```

Each record is for one area. Each area owns its own mapper.

## Boundary Mapping

The important move is to map from a bigger composition-root shape into the area record at the boundary.

```fsharp
type AppDeps =
    { Orders : IOrderRepository
      Email : IEmailSender
      Invoices : IInvoiceRepository
      Clock : IClock
      Log : LogEntry -> unit
      Api : IInventoryApi
      Store : IInventoryStore }

let mapSubmitOrderDeps (app: AppDeps) =
    { Orders = app.Orders
      Email = app.Email
      Log = app.Log }

let mapBillingJobDeps (app: AppDeps) =
    { Invoices = app.Invoices
      Clock = app.Clock
      Log = app.Log }
```

That mapping is the architectural win. It keeps the feature boundary visible and avoids handing a giant record to every workflow.

## Reading Fields

The `Flow.read` helper projects what a workflow needs.

```fsharp
let submitOrder : Flow<SubmitOrderDeps, string, Guid> =
    flow {
        let! deps = Flow.env
        let! order = deps.Orders.Create()
        do! deps.Email.SendConfirmation order
        return order.Id
    }
```

For simple projections, `Flow.read _.Field` stays the cleanest option.

```fsharp
let billingPing : Flow<BillingJobDeps, string, unit> =
    flow {
        let! clock = Flow.read _.Clock
        let! logger = Flow.read _.Log

        logger { Level = LogLevel.Information; Message = $"Tick {clock.UtcNow()}" ; TimestampUtc = clock.UtcNow() }
    }
```

## Narrowing A Larger Record

`Flow.localEnv` is for projecting a bigger boundary down to a smaller one.

```fsharp
type SmallDeps = { Log : LogEntry -> unit }

let smallWorkflow : Flow<SmallDeps, string, unit> = flow { let! _ = Flow.read _.Log in return () }

let biggerWorkflow : Flow<AppDeps, string, unit> =
    smallWorkflow
    |> Flow.localEnv mapSubmitOrderDeps
```

Use this when a sub-workflow genuinely needs a smaller view and you already have the larger record in hand.

## Where Layers Fit

`Flow.provideLayer` is still a bridge, but it is not the main level 1 story.

Use it when one flow builds the environment for another:

```fsharp
let buildDeps : Flow<HostInput, string, AppDeps> = ...
let runJob : Flow<AppDeps, string, unit> = ...

let executable = runJob |> Flow.provideLayer buildDeps
```

That keeps level 1 about boundary-scoped records and lets layers stay a composition tool.

See the [Flow reference](../../reference/flow/) for the record-level projection helpers and layer bridge APIs.
