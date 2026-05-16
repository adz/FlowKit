---
weight: 30
title: "Tutorial: Capabilities"
description: Using nominal interface contracts as the environment.
---

# Tutorial: Capabilities

Nominal Capability Contracts use F# interfaces to name app dependencies. This lets the compiler
check that your environment implements the required capabilities and makes it easy to reuse
helpers across different workflows.

In this tutorial, we will refactor our workflow to use **Honest Capabilities** (`IHas<'T>`) instead of concrete records.

## 1. Define Capability Interfaces

First, define your service interfaces and their corresponding `IHas<'T>` contracts.

```fsharp
open System
open System.Threading.Tasks
open FsFlow

type IOrderRepository =
    abstract member Save : Order -> unit

type IEmailSender =
    abstract member SendConfirmation : Order -> unit

// Define the "Has" contracts
type IHasOrders = inherit IHas<IOrderRepository>
type IHasEmail = inherit IHas<IEmailSender>

// You can also group them into a larger contract
type IAppCaps =
    inherit IHasOrders
    inherit IHasEmail
```

## 2. Write Helpers using `Flow.service`

Helper functions can now request exactly which app capabilities they need. The compiler will infer the environment requirement automatically.

```fsharp
let saveOrder order : Flow<#IHasOrders, _, _> =
    flow {
        let! repo = Flow.service<IOrderRepository>()
        repo.Save order
    }

let sendEmail order : Flow<#IHasEmail, _, _> =
    flow {
        let! sender = Flow.service<IEmailSender>()
        sender.SendConfirmation order
    }
```

## 3. Compose the Main Workflow

The main workflow combines these helpers. F# type inference will correctly aggregate the requirements into a single environment constraint.

```fsharp
let placeOrder order =
    flow {
        do! saveOrder order
        do! sendEmail order
        return order.Id
    }
// Signature: val placeOrder : order -> Flow<'env, 'err, Guid> 
//             when 'env :> IHasOrders and 'env :> IHasEmail
```

## 4. Implement the Environment

Your application environment is now just a type that implements the required `IHas<'T>` interfaces.

```fsharp
type AppEnv =
    { Orders: IOrderRepository
      Email: IEmailSender }
    interface IHas<IOrderRepository> with member x.Service = x.Orders
    interface IHas<IEmailSender> with member x.Service = x.Email

[<EntryPoint>]
let main _ =
    let env =
        { Orders = InMemoryOrders()
          Email = ConsoleEmail() }
    let order = { Id = Guid.NewGuid(); Total = 99.99m }

    task {
        let! result = Flow.run env (placeOrder order)
        printfn "Result: %A" result
    } |> ignore
```

## Why use Capabilities?

- **Refactor Safety**: If you add a new app capability to a helper, the compiler will immediately tell you every call site that needs to be updated.
- **Granular Dependencies**: Helpers only ask for what they actually need, making the code easier to reason about and test.
- **Reusable Logic**: You can write general-purpose helpers that work on any environment that provides the required `IHas<'T>` implementations.

## Next Steps

For enterprise applications that use standard .NET dependency injection, proceed to the **[AppHost](./app-host/)** tutorial to see how to bridge `IServiceProvider` into the FsFlow world using `Flow.inject`.
