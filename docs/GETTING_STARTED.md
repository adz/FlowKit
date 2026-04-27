# Getting Started

Read this page when you want the fastest path from plain `Result` code to an application-shaped `flow {}` workflow.

Start with plain F# first, then introduce `Flow` only at the boundary where dependencies,
async work, or typed failures start.

## 1. Start With Pure Code

Here we have `validateName` which returns an ordinary `Result`:

```fsharp
type ValidationError =
    | MissingName

let validateName (name: string) =
    if System.String.IsNullOrWhiteSpace name then
        Error MissingName
    else
        Ok name
```

## 2. Introduce `Flow`

Use a flow to keep working on the successful path while staying in the same CE you will use
later for environment access and async or task boundaries:

```fsharp
Flow<'env, 'error, 'value>
```

Example:

```fsharp
let greet input : Flow<unit, ValidationError, string> =
    flow {
        let! name = validateName input
        return $"Hello {name}"
    }
```

The point of this step is simple: `validateName` stays an ordinary `Result` function, and
`flow {}` keeps the success path readable without forcing you into a separate CE for each
wrapper shape.

Read the type as:

- `unit`: the environment the flow needs, which here means no dependencies
- `'error`: the expected typed failure
- `'value`: the success value

## 3. Run The Flow Explicitly

Flows are cold. They do nothing until you run them and provide a cancellation token:

```fsharp
let result =
    greet "Ada"
    |> Flow.toAsync () CancellationToken.None
    |> Async.RunSynchronously
```

There is no async work in this flow yet, but the token is still required so the
flow can be composed with others that are.

Use `unit` for `'env` when the flow does not need dependencies yet.

## 4. Read From The Environment

The environment provides arguments a flow needs without having to pass them directly
until run time.

Use `Flow.read` when the flow only needs one projected value:

```fsharp
type AppEnv =
    { Prefix: string }

let greetWithPrefix input : Flow<AppEnv, ValidationError, string> =
    flow {
        let! name = validateName input
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {name}"
    }
```

Use `Flow.env` when you genuinely need the whole environment:

```fsharp
let describe : Flow<AppEnv, ValidationError, string> =
    flow {
        let! env = Flow.env
        return env.Prefix
    }
```

## 5. Compose Smaller Flows Into Bigger Environments

Use `Flow.localEnv` when a smaller flow should run inside a larger outer environment:

```fsharp
type SmallEnv = { Prefix: string }
type BigEnv = { App: SmallEnv; RequestId: string }

let greet : Flow<SmallEnv, ValidationError, string> =
    flow {
        let! prefix = Flow.read _.Prefix
        return $"{prefix} world"
    }

let greetInBigEnv : Flow<BigEnv, ValidationError, string> =
    greet |> Flow.localEnv _.App
```

Think of `Flow.localEnv` as letting an inner flow read a derived 'local' view of the environment.

## 6. Cross `.NET Task` Boundaries Explicitly

Task interop lives under `Flow.Task`.

Tasks are hot: once you have a `Task<'value>`, it has already been created and may already be
running.

Started task values bind directly inside `flow {}`:

```fsharp
let workflow : Flow<unit, string, int> =
    flow {
        let! value = Task.FromResult 42
        return value
    }
```

Tasks can be made cold by defining a helper that takes the runtime cancellation token instead
of creating the task up front.

That is usually the better shape when you control the helper yourself. Cold tasks start at
flow execution time, receive the runtime cancellation token, and compose more predictably.

`ColdTask<'value>` is the named alias for `CancellationToken -> Task<'value>`:

```fsharp
let coldTask : ColdTask<int> =
    fun _ct -> Task.FromResult 42

let workflow : Flow<unit, string, int> =
    flow {
        let! value = coldTask
        return value
    }
```

Use `Flow.Task` when you want the boundary shape to stay explicit:

```fsharp
let load : Flow<unit, string, int> = Flow.Task.fromCold coldTask
```

The type annotation on `load` is shown for clarity here. It is not required.

Use hot task helpers only when you already have a started task value on purpose:

```fsharp
let started = Task.FromResult 42
let load = Flow.Task.fromHot started
```

Read [`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) when you want the full map of
which `Task` and `Async` shapes bind directly and which ones should stay explicit.

## 7. Let Flow Pass The Cancellation Token For You

Once your helper has the `ColdTask<'value>` shape, `Flow` can pass the runtime token through
for you:

```fsharp
let readAll path : ColdTask<string> =
    fun (ct: CancellationToken) ->
        System.IO.File.ReadAllTextAsync(path, ct)

let readConfigs leftPath rightPath : Flow<unit, string, string * string> =
    flow {
        let! left = readAll leftPath
        let! right = readAll rightPath
        return left, right
    }
```

`flow {}` binds `readAll leftPath` directly and passes in the same cancellation token that
you gave to `Flow.toAsync`.

## 8. Access The Cancellation Token Only Where Needed

The same BCL call can be written by reading the token explicitly inside the flow:

```fsharp
let readConfig path : Flow<unit, string, string> =
    flow {
        let! ct = Flow.Runtime.cancellationToken
        return!
            System.IO.File.ReadAllTextAsync(path, ct)
            |> Flow.Task.fromHot
    }
```

This works, but it is more ceremony. Prefer the previous shape when you can keep the
operation cold and let `Flow` provide the token at run time.

## 9. Use `ColdTaskResult` For Cold Task Helpers With Typed Failures

If your cold helper returns `Result<'value, 'error>`, the matching alias is:

```fsharp
ColdTaskResult<'value, 'error>
```

which is just:

```fsharp
CancellationToken -> Task<Result<'value, 'error>>
```

Use it with `Flow.Task.fromColdResult`:

```fsharp
let loadText path : ColdTaskResult<string, string> =
    fun ct ->
        task {
            let! text = System.IO.File.ReadAllTextAsync(path, ct)
            return Ok text
        }

let readConfig path : Flow<unit, string, string> =
    loadText path
    |> Flow.Task.fromColdResult
```

This one stays explicit. Keeping `ColdTaskResult` explicit avoids builder ambiguity with
`ColdTask<Result<'value, 'error>>`, which leads to worse compiler errors.

## 10. Combine Runtime Helpers In A Real Workflow

The runtime helpers matter most when they work together in a complete workflow. Logging is
just another dependency, so read it from the environment once and use it directly:

```fsharp
type AppError =
    | GatewayFailed of string
    | TimedOut
    | Canceled of string

type Response =
    { StatusCode: int
      Body: string }

type RequestEnv =
    { AttemptCount: int ref
      Log: string -> unit
      Ping: string * CancellationToken -> Task<Result<Response, string>> }

let fetchResponse url : Flow<RequestEnv, AppError, Response> =
    flow {
        let! ct = Flow.Runtime.cancellationToken
        let! log = Flow.read _.Log
        let! ping = Flow.read _.Ping
        let! attempts = Flow.read _.AttemptCount

        log (sprintf "request attempt=%d url=%s" (attempts.Value + 1) url)

        let! response =
            ping(url, ct)
            |> Flow.Task.fromHotResult
            |> Flow.mapError GatewayFailed

        return response
    }
    |> Flow.Runtime.retry
        { MaxAttempts = 3
          Delay = fun attempt -> System.TimeSpan.FromMilliseconds(float (attempt * 25))
          ShouldRetry = function GatewayFailed _ -> true | _ -> false }
    |> Flow.Runtime.timeout (System.TimeSpan.FromSeconds 2.0) TimedOut
    |> Flow.Runtime.catchCancellation (fun _ -> Canceled "request canceled")

let program : Flow<RequestEnv, AppError, unit> =
    let urls =
        [| "https://service.local/users"
           "https://service.local/orders"
           "https://service.local/invoices" |]

    flow {
        let! log = Flow.read _.Log

        log "starting batch fetch"

        let! users = fetchResponse urls[0]
        printfn "%s -> %d %s" urls[0] users.StatusCode users.Body

        let! orders = fetchResponse urls[1]
        printfn "%s -> %d %s" urls[1] orders.StatusCode orders.Body

        let! invoices = fetchResponse urls[2]
        printfn "%s -> %d %s" urls[2] invoices.StatusCode invoices.Body
    }
```

This example shows a realistic shape:

- `fetchResponse` wraps the `.NET` call with logging, retry, timeout, and cancellation handling
- `program` calls it three times and prints the responses

For ordinary resource lifetimes, prefer plain `use` or `use!` inside `flow {}`. Reach for
`Flow.Runtime.useWithAcquireRelease` only when you need custom acquire and release behavior.

## Next

Read [`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for the full task and async
interop shapes, then [`docs/ENV_SLICING.md`](./ENV_SLICING.md) for smaller environment design,
then [`examples/README.md`](../examples/README.md) to see complete flows.
