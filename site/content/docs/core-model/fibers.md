---
weight: 22
title: Fibers
description: Lightweight logical threads and structured concurrency in FsFlow.
type: docs
---


In FsFlow, a **Fiber** is a lightweight, logical thread of execution. It represents a running [**`Flow`**]({{< relref "/reference/flow/t-flow.md" >}}) that has been started and is operating in the background.

## The Mental Model

While a `Flow` is **cold** (a description of work that hasn't started yet), a **Fiber** is **hot** (the work is currently being executed).

When you "fork" a flow, you are effectively saying: "Start this work in the background, give me a handle to it, and let me continue with my own work." That handle is the Fiber.

## Structured Concurrency

Fibers are the foundation of **Structured Concurrency** in FsFlow. Unlike "fire-and-forget" background tasks, Fibers allow you to maintain a parent-child relationship between workflows, ensuring that background work is always accounted for and safely cleaned up.

The three primary operations for managing Fibers are:

- [**`Flow.fork`**]({{< relref "/reference/flow/m-flow-fork.md" >}}): Starts a flow in the background and returns a `Fiber` handle.
- [**`Flow.join`**]({{< relref "/reference/flow/m-flow-join.md" >}}): Suspends the current flow until the fiber completes, returning its final outcome.
- [**`Flow.interrupt`**]({{< relref "/reference/flow/m-flow-interrupt.md" >}}): Signals a fiber to stop immediately and waits for it to finish its cleanup.

## Why Fibers?

Fibers provide several critical advantages over raw `Task` or `Async` objects:

### 1. Algebraic Interruption
In standard .NET development, cancellation requires manually passing a `CancellationToken` through every function. In FsFlow, interruption is built into the model. When a Fiber is interrupted, the engine ensures that the workflow stops at the next available yield point and—crucially—runs all registered cleanup logic (via `ensuring` or `onExit`).

### 2. Typed Outcomes
A `Fiber<'error, 'value>` is generic. It "remembers" the error type and success type of the workflow it is running. When you `join` a fiber, the compiler ensures that you handle its potential failures just as you would with any other `Flow`.

### 3. Safety and Leak Prevention
Because Fibers are tied to the FsFlow execution engine, they respect the lifecycle of their parent. This makes it easier to avoid "orphan" background tasks that continue to consume resources after the main application has moved on or failed.

## Underlying Implementation

On .NET, a Fiber is essentially a wrapper around a `Task<Exit<'value, 'error>>` and a `CancellationTokenSource`. On Fable (JavaScript), it wraps an `Async<Exit<'value, 'error>>`.

```fsharp
type Fiber<'error, 'value> =
    {
        ExitTask: Task<Exit<'value, 'error>> // The running work
        InterruptSource: CancellationTokenSource // The kill switch
    }
```

This abstraction allows FsFlow to provide a unified concurrency model that works identically across different platforms while respecting their native execution primitives.

## Concurrency Primitives

Most of the time, you won't manage Fibers manually. Instead, you will use high-level parallel combinators that use Fibers under the hood:

- `Flow.zipPar`: Runs two flows concurrently in separate fibers and waits for both.
- `Flow.race`: Runs two flows concurrently and returns the result of the winner, interrupting the loser.
- `Flow.collectPar`: Runs a collection of flows in parallel.
