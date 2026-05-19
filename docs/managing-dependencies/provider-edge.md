---
weight: 60
title: Standard .NET AppHost Plus DI
description: Using IServiceProvider at the application edge while keeping workflows typed.
---

# Standard .NET AppHost Plus DI

`IServiceProvider` belongs at the outer edge of the system.

Using FsFlow with standard .NET Dependency Injection gives you:

- Host registrations that you can turn into typed values once.
- A familiar entry point for ASP.NET, Workers, and other DI-driven runtimes.
- A bridge from dynamic host registrations to typed, honest core logic.

## Pragmatic Access: `Flow.inject`

`Flow.inject<'T>()` pulls a service directly from the `IServiceProvider` in the environment.

```fsharp
let handleRequest = flow {
    let! db = Flow.inject<MyDbContext, _, _>()
    let! api = Flow.inject<IExternalApi, _, _>()
    // ...
}
```

This is the most ergonomic way to use FsFlow inside existing .NET applications.

**Full Example:** [CapabilityExamples.fs (Level 3)](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.Examples/CapabilityExamples.fs)

### Failure Behavior

Unlike the honest `Flow.service`, `Flow.inject` assumes that your DI container is correctly configured. If a requested service is missing, it will fail with a **Defect (Die)**. This is idiomatic for .NET applications, where a missing registration is considered a developer configuration error that should be caught early.

## The Honest Bridge

The strongest architectural pattern is to use `Flow.inject` (or manual mapping) to satisfy strict `IHas<'T>` requirements at the host boundary.

```fsharp
// 1. Core Logic (Honest)
let processOrder cmd = flow {
    let! db = Flow.service<IOrderRepo, _, _>()
    return! db.Save cmd
}

// 2. Host Edge (Bridge)
type AppEnv(sp: IServiceProvider) =
    interface IHas<IOrderRepo> with member _.Service = sp.GetRequiredService<IOrderRepo>()

// 3. Application Host (e.g. ASP.NET Controller)
[<HttpPost>]
member this.Post(cmd: Command) =
    let env = AppEnv(this.HttpContext.RequestServices)
    Flow.run env (processOrder cmd)
```

**Full Example:** [CapabilityExamples.fs (Honest Bridge)](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.Examples/CapabilityExamples.fs)


By using the bridge:
1. Your **Core Logic** stays 100% testable and decoupled from DI.
2. Your **Host Edge** remains pragmatic and lean.
3. The **Compiler** verifies that every service required by your logic is actually provided by the host environment.

## When to use `Flow.inject` Directly

Use `Flow.inject` directly when:
- You are writing "glue code" at the application edge.
- You are prototyping quickly.
- You are inside a Controller or Middleware where the environment is already `IServiceProvider`.

## Summary

Keep `IServiceProvider` at the edge. Map it to strict `IHas<'T>` contracts or plain records as soon as you enter your core domain logic. This preserves the **Effect Discipline** of your application while taking full advantage of the .NET ecosystem.
