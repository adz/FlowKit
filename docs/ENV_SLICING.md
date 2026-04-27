# Environment Slicing

This page shows two ways to keep `Flow<'env, 'error, 'value>` honest about dependencies:
small record environments with `Flow.localEnv`, and interface-based capability environments.

The common goal is the same in both styles: each flow should depend on the smallest environment
it actually needs.

## Start With A Small Environment Record

For most code in this repo, this is the best default:

```fsharp
type FetchResponseEnv =
    { Gateway: IPingGateway
      AttemptCount: int ref
      Log: string -> unit }

let fetchResponse (plan: RequestPlan) : Flow<FetchResponseEnv, AppError, Response> =
    flow {
        let! gateway = Flow.read _.Gateway
        let! attempts = Flow.read _.AttemptCount
        let! log = Flow.read _.Log
        let! ct = Flow.Runtime.cancellationToken

        log (sprintf "gateway call attempt=%d url=%s" (attempts.Value + 1) plan.Url)

        let! response =
            gateway.Ping(plan, ct)
            |> Flow.Task.fromHotResult
            |> Flow.mapError GatewayFailed

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

let fetchResponseInAppEnv plan : Flow<AppEnv, AppError, Response> =
    fetchResponse plan
    |> Flow.localEnv (fun env ->
        { Gateway = env.Gateway
          AttemptCount = env.AttemptCount
          Log = env.Log })
```

This is the simplest way to compose bigger programs from smaller flows.

## Interface-Based Capability Environments

Another option is to describe capabilities through interfaces:

```fsharp
type IHasGateway =
    abstract Gateway: IPingGateway

type IHasAttempts =
    abstract AttemptCount: int ref

type IHasLog =
    abstract Log: string -> unit

let fetchResponse
    (plan: RequestPlan)
    (env: #IHasGateway & #IHasAttempts & #IHasLog)
    =
    task {
        let gateway = env.Gateway
        let attempts = env.AttemptCount
        let log = env.Log

        log (sprintf "gateway call attempt=%d url=%s" (attempts.Value + 1) plan.Url)
        return! gateway.Ping(plan, CancellationToken.None)
    }
```

This style can work well when several different environment carriers should satisfy the same
capability contract.

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
- `Flow.localEnv` already gives you the composition step cleanly

## Recommended Default For This Repo

Prefer small record environments plus `Flow.localEnv`.

That matches the rest of the library better:

- the signatures stay explicit
- the code stays easy to read
- tests only need the fields a flow actually reads
- composition stays obvious at the call site

Use interface-based slicing only when there is a real need for shared capability contracts
across multiple environment carriers.

## A Note On Effects

In this library, explicit effects usually mean explicit required capabilities in `'env`, not a
separate effect algebra.

For example:

- gateway I/O is explicit through `Gateway` in the environment
- persistence is explicit through `AuditStore`
- logging is explicit through `Log`
- cancellation is explicit through `Flow.toAsync` and `Flow.Runtime.cancellationToken`

Small environment slices already make those requirements visible without adding another layer
of abstraction.

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the main workflow model, then
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for task and async boundaries, then
[`docs/TROUBLESHOOTING_TYPES.md`](./TROUBLESHOOTING_TYPES.md) if you start pushing the type
system harder.
