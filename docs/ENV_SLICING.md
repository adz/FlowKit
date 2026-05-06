# Environment Slicing

This page shows two ways to keep an FsFlow workflow honest about dependencies:
small record environments with `localEnv`, and interface-based capability environments.
For task-oriented work, the same idea can split into runtime services and application capabilities
with `RuntimeContext<'runtime, 'env>`.

The common goal is the same in both styles: each flow should depend on the smallest environment
it actually needs.

## Start With A Small Environment Record

For most code in this repo, this is the best default:

```fsharp
type FetchResponseEnv =
    { Gateway: IPingGateway
      AttemptCount: int ref
      Log: string -> unit }

let fetchResponse (plan: RequestPlan) : TaskFlow<FetchResponseEnv, AppError, Response> =
    taskFlow {
        let! gateway = TaskFlow.read _.Gateway
        let! attempts = TaskFlow.read _.AttemptCount
        let! log = TaskFlow.read _.Log

        log (sprintf "gateway call attempt=%d url=%s" (attempts.Value + 1) plan.Url)

        let! response = gateway.Ping plan
        return response
    }
```

This keeps the flow signature honest without forcing it to depend on the whole application
environment.

## Project From A Larger Application Environment

When the real application environment is larger, project it down:

```fsharp
type AppEnv =
    { Gateway: IPingGateway
      AuditStore: IAuditStore
      AttemptCount: int ref
      Log: string -> unit }

let fetchResponseInAppEnv plan : TaskFlow<AppEnv, AppError, Response> =
    fetchResponse plan
    |> TaskFlow.localEnv (fun env ->
        { Gateway = env.Gateway
          AttemptCount = env.AttemptCount
          Log = env.Log })
```

This is the simplest way to compose bigger programs from smaller flows.

## Split Runtime Services From Application Capabilities

When a task boundary needs both operational services and application dependencies, use
`RuntimeContext<'runtime, 'env>` rather than forcing everything into one record:

```fsharp
type RuntimeServices =
    { Log: string -> unit }

type AppEnv =
    { Gateway: IPingGateway
      AttemptCount: int ref }

let fetchResponse : TaskFlow<RuntimeContext<RuntimeServices, AppEnv>, AppError, Response> =
    taskFlow {
        let! log = TaskFlow.readRuntime _.Log
        let! gateway = TaskFlow.read _.Gateway

        log "starting request"
        return! gateway.Ping()
    }
```

Use this shape when operational concerns and app dependencies deserve different lifetimes or ownership.

## Interface-Based Capability Environments

Another option is to describe capabilities through interfaces. This is particularly 
useful when the environment needs to satisfy multiple contracts simultaneously.

In F#, when an argument must implement multiple interfaces, you use **explicit generic 
constraints** on the type parameter list. Accessing members from these interfaces 
often requires an explicit upcast or a small inline helper.

```fsharp
type IHasGateway =
    abstract Gateway: IPingGateway

type IHasAttempts =
    abstract AttemptCount: int ref

type IHasLog =
    abstract Log: string -> unit

// Use explicit constraints on the 'env type parameter
let fetchResponse<'env
    when 'env :> IHasGateway
     and 'env :> IHasAttempts
     and 'env :> IHasLog>
    (plan: RequestPlan)
    (env: 'env)
    =
    task {
        // Upcast to access specific interface members
        let gateway = (env :> IHasGateway).Gateway
        let attempts = (env :> IHasAttempts).AttemptCount
        let log = (env :> IHasLog).Log

        log (sprintf "gateway call attempt=%d url=%s" (attempts.Value + 1) plan.Url)
        return! gateway.Ping(plan, CancellationToken.None)
    }
```

Or, more concisely, you can use **inline helpers** to project the capabilities:

```fsharp
let inline gateway (env: #IHasGateway) = env.Gateway
let inline attempts (env: #IHasAttempts) = env.AttemptCount
let inline log (env: #IHasLog) = env.Log

let fetchResponse plan env =
    task {
        let g = gateway env
        let a = attempts env
        let l = log env
        // ...
    }
```

This style allows you to pass any object (including a custom record or a class) 
that implements all required interfaces.

## Where Interface Slicing Helps

Interface-based slicing is useful when:

- several application environments should satisfy the same capability contract
- you already have infrastructure dependencies expressed as interfaces
- you want module boundaries to talk in terms of capabilities rather than record fields

## Where Record Slicing Helps

Record-based slicing is useful when:

- you want straightforward code and predictable compiler errors
- you want to teach the library without SRTP or flexible-type syntax
- most flows live inside one application and only need projection from a larger env
- `localEnv` already gives you the composition step cleanly

## Capabilities and Service Discovery

For complex applications, FsFlow provides a structured way to manage dependencies through
the `Capability` module. This lets you keep the projection logic separate from the workflow
body while still staying explicit about the environment shape.

```fsharp
type ILogger = abstract Log : string -> unit

let log message =
    taskFlow {
        let! logger = Capability.service _.Logger
        return logger.Log message
    }
```

`Capability.service` reads a value from a record projection, so the workflow only depends
on the field it actually needs.

## Layering and Composition

Layers allow you to build up environments modularly. A `Layer<'env, 'provider>` describes how
to produce an environment segment `'provider` given a base environment `'env`.

```fsharp
type Layer<'env, 'provider> = 'env -> 'provider
```

You can compose layers to create the final application environment:

```fsharp
let loggerLayer : Layer<unit, ILogger> = 
    fun () -> ConsoleLogger() :> ILogger

let databaseLayer : Layer<ILogger, IDatabase> = 
    fun logger -> SqlDatabase(logger) :> IDatabase

// Compose layers
let appLayer = 
    Layer.compose loggerLayer databaseLayer
```

This "onion" style of environment construction ensures that each component only sees the 
dependencies it needs, and the order of construction is explicit and type-safe.

## Runtime Context vs. Application Environment

In `TaskFlow`, we distinguish between:

1.  **Runtime Context (`'runtime`)**: Low-level operational services like logging, 
    cancellation, and retry policies. These are usually provided by the infrastructure.
2.  **Application Environment (`'env`)**: High-level domain services like repositories,
    gateways, and business logic dependencies.

The `RuntimeContext<'runtime, 'env>` type carries both, allowing you to use 
`TaskFlow.readRuntime` and `TaskFlow.read` to access the appropriate half.

## Why Layering?

- **Testability**: You can swap out a single layer with a mock without changing the rest 
  of the environment.
- **Modularity**: Components can define their own environment requirements independently.
- **Explicit Dependencies**: The type signature of a `Layer` tells you exactly what it 
  needs to start and what it provides to the rest of the app.

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the main workflow model, then
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for task and async boundaries, then
[`docs/TROUBLESHOOTING_TYPES.md`](./TROUBLESHOOTING_TYPES.md) if you start pushing the type
system harder.
