---
weight: 50
title: "Nominal Capability Contracts"
description: Small named interfaces for reusable helpers and public capability boundaries.
type: docs
---


In FsFlow, the environment (`'env`) represents the "world" your effects need to run. Depending on the size of your application and your need for strictness, FsFlow provides three "levels" of environment access.

## Level 1: Records (The Pragmatic Way)

For local logic or small applications, a plain F# record is often the best choice. It is direct, easy to mock, and requires zero ceremony.

### Accessing via `Flow.read`

Use `Flow.read` to project a specific property from your environment record.

```fsharp
type AppConfig = { ConnectionString: string }
type AppEnv = { Config: AppConfig }

let getConnString = Flow.read (fun e -> e.Config.ConnectionString)
```

**Full Example:** [CapabilityExamples.fs (Level 1)](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.Examples/CapabilityExamples.fs)

---

## Level 2: Nominal Capabilities (The Honest Way)

As your application grows, you may want "Static Honesty"—where the type signature of a function clearly advertises every external service it needs. This approach keeps dependencies explicit and verifiable by the compiler.

### The `IHas<'T>` Pattern

Standardize on the `IHas<'T>` interface to "slice" your environment into specific capabilities.

```fsharp
type IOrderRepo = abstract member Save : Order -> unit

// A capability contract
type IHasOrders = inherit IHas<IOrderRepo>
```

### Accessing via `Flow.service<'T>()`

Use `Flow.service` to request a capability. The compiler will automatically infer that the environment must implement `IHas<'T>`.

```fsharp
let saveOrder order : Flow<#IHasOrders, Error, unit> =
    flow {
        let! repo = Flow.service<IOrderRepo, _, _>()
        repo.Save order
    }
```

**Full Example:** [CapabilityExamples.fs (Level 2)](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.Examples/CapabilityExamples.fs)

---

## Level 3: Dependency Injection (The Edge Way)

At the application "Edge" (e.g., an ASP.NET Controller or an Azure Function), you are often working with a standard .NET `IServiceProvider`. FsFlow allows you to lean into this ecosystem without losing the benefits of an effect system.

### Accessing via `Flow.inject<'T>()`

Use `Flow.inject` to pull a service directly from the DI container. This trades compile-time safety for maximum pragmatism at the host boundary.

```fsharp
let handleRequest = flow {
    let! logger = Flow.inject<ILogger<MyController>, _, _>()
    let! db = Flow.inject<IDbContext, _, _>()
    // ...
}
```

*Note: If a service is missing from the DI container, `Flow.inject` will fail with a **Defect (Die)**, treating it as a developer configuration error.*

**Full Example:** [CapabilityExamples.fs (Level 3)](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.Examples/CapabilityExamples.fs)

---

## The "Honest Bridge"

You can combine Level 2 and Level 3 using the "Honest Bridge" pattern. Your core logic stays "Honest" (Level 2), but your Application Edge satisfies those requirements using DI (Level 3).

```fsharp
// 1. Core Logic (Honest/Level 2)
let processOrder cmd = flow {
    let! db = Flow.service<IOrderRepo, _, _>() 
    return! db.Save cmd
}

// 2. Host Edge (Bridge)
type AppEnv(sp: IServiceProvider) =
    interface IHas<IOrderRepo> with member _.Service = sp.GetRequiredService<IOrderRepo>()

// 3. Running
let result = Flow.run (AppEnv(sp)) (processOrder myCmd)
```

**Full Example:** [CapabilityExamples.fs (Honest Bridge)](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.Examples/CapabilityExamples.fs)


## Summary: Choosing Your Level

| Level | Name | Primary Accessor | Environment Requirement |
| :--- | :--- | :--- | :--- |
| **1** | **Direct** | `Flow.read` | Plain Record/Data |
| **2** | **Honest** | `Flow.service` | `IHas<'T>` |
| **3** | **Pragmatic**| `Flow.inject` | `IServiceProvider` |
