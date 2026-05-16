# Capability Redesign: The LLM-Friendly "Dual-Track" Model

## The Goal
To build **FsFlow** as an LLM-friendly (Agent-Legible) effect boundary library that bridges the gap between ZIO's strict effect discipline and .NET's pragmatism. The library must provide clear compiler guarantees ("Static Honesty") without fighting the .NET `IServiceProvider` ecosystem.

## The Problem with the Current Approach
Currently, FsFlow uses a `Resolve<'T>` token that is intercepted by heavily overloaded `Bind` methods in the computation expression (CE) builder.
1. **Agent Noise**: `Resolve<'T>` is a phantom token, not a real `Flow`. It requires "magic" CE overloads, making it hard for LLMs to compose outside of a `flow { }` block.
2. **Naming Ambiguity**: "Service" and "Resolve" are overloaded terms. In ZIO, `service` means a strict capability. In .NET, `service` often means an untyped bag of DI registrations.

## The Solution: The "Three Pillars" of Access
We will delete the `Resolve<'T>` CE machinery and replace it with standard, explicit generic F# functions. We will standardize on ecosystem-familiar names so LLMs can instantly recognize the intent.

### 1. `Flow.read` (Record Access)
* **Signal**: Haskell / F# Reader Monad.
* **Usage**: When the environment is a plain data record.
```fsharp
type Config = { Port: int }

// Agent knows: "I am projecting a property from a record."
let getPort = Flow.read (fun e -> e.Config.Port)
```

### 2. `Flow.service<'T>` (Nominal Capability / Honest)
* **Signal**: ZIO / Effect-TS.
* **Usage**: When we need 100% compiler verification that a capability exists (`IHas<'T>`).
```fsharp
type IHas<'service> =
    abstract member Service: 'service

// Agent knows: "I must statically prove this environment has an IClock."
let getClock = Flow.service<IClock>()
// Inferred constraint: 'env :> IHas<IClock>
```

### 3. `Flow.inject<'T>` (DI Bag / Pragmatic)
* **Signal**: Angular / NestJS / ASP.NET.
* **Usage**: When the environment is an `IServiceProvider`. Trades compile-time safety for edge pragmatism.
```fsharp
// Agent knows: "I am pulling this dynamically from a DI container."
let getDb = Flow.inject<IDb>()
// Inferred constraint: 'env :> IServiceProvider
```

## The "Honest Bridge" (Wiring DI to Capabilities)
How do we maintain "Static Honesty" in core logic but still use ASP.NET's DI? We use the "Honest Bridge" pattern at the application edge. Agents are excellent at generating these boilerplate implementations.

```fsharp
// 1. Core Logic (Honest)
let saveOrder cmd = flow {
    let! db = Flow.service<IDb>() // Requires 'env :> IHas<IDb>
    return! db.Save cmd
}

// 2. Application Edge (The Bridge)
type AppEnv(sp: IServiceProvider) =
    interface IHas<IDb> with member _.Service = sp.GetRequiredService<IDb>()
    interface IHas<IClock> with member _.Service = sp.GetRequiredService<IClock>()

// 3. Execution
let runEdge (sp: IServiceProvider) =
    Flow.run (AppEnv(sp)) (saveOrder myCmd)
```

## Execution Plan

### Step 1: Replace `Requires<'T>` with `IHas<'T>`
* Rename `Requires<'T>` to `IHas<'T>` in `Core.fs` (or `Capability.fs`).
* Adjust the property from `Dep` to `Service`.

### Step 2: Implement the Accessors
Add these directly to the `Flow` module (`Flow.fs`), `TaskFlow` module, and `AsyncFlow` module:

```fsharp
/// Extract a capability from a statically typed environment.
let inline service<'T, 'env, 'error when 'env :> IHas<'T>> () : Flow<'env, 'error, 'T> =
    Flow.read (fun (env: 'env) -> env.Service)

/// Inject a service from a dynamic IServiceProvider.
let inline inject<'T, 'env, 'error when 'env :> IServiceProvider> () : Flow<'env, 'error, 'T> =
    Flow.read (fun (env: 'env) -> 
        let svc = env.GetService(typeof<'T>)
        if isNull (box svc) then 
            failwith $"Service {typeof<'T>.Name} was not registered in the IServiceProvider."
        else 
            unbox<'T> svc
    )
```

### Step 3: Exorcise the CE Ghosts (Delete `Resolve`)
* Remove the `Resolve<'dep>` and `Resolve<'dep, 'value>` types from `Core.fs`.
* Delete **all** `Bind` overloads in `FlowBuilder.fs` and `AsyncFlowBuilder` that intercept `Resolve`. This will delete hundreds of lines of code and vastly simplify the CE engine.
* The CE will now only need the standard `Bind(flow: Flow<'env, 'error, 'value>, binder)` implementation, as `service`, `inject`, and `read` all return first-class `Flow` values.

## Conclusion
This redesign makes FsFlow simpler internally and significantly more legible to both humans and LLMs. By providing both `service` (Honest) and `inject` (Pragmatic), FsFlow natively supports the ZIO mindset while playing perfectly with the .NET DI ecosystem.