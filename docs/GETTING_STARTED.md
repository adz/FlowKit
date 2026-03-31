# Getting Started

Read this page when you want to write your first small Flow.

The goal is to keep pure code as plain F#, then introduce `Flow` only at the boundary where dependencies, async work, or typed failures start.

## 1. Start With Pure Code

Keep pure validation as ordinary `Result` functions:

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

Start with the smallest useful flow:

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

Read the type as:

- `'env`: the environment the flow needs
- `'error`: the expected typed failure
- `'value`: the success value

## 3. Run The Flow Explicitly

Flows are cold. They do nothing until you run them:

```fsharp
let result =
    greet "Ada"
    |> Flow.run () System.Threading.CancellationToken.None
    |> Async.RunSynchronously
```

Use `unit` for `'env` when the flow does not need dependencies yet.

## 4. Read From The Environment

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

Use `Flow.mapEnv` when a smaller flow should run inside a larger outer environment:

```fsharp
type SmallEnv = { Prefix: string }
type BigEnv = { App: SmallEnv; RequestId: string }

let greet : Flow<SmallEnv, ValidationError, string> =
    flow {
        let! prefix = Flow.read _.Prefix
        return $"{prefix} world"
    }

let greetInBigEnv : Flow<BigEnv, ValidationError, string> =
    greet |> Flow.mapEnv _.App
```

## 6. Cross `.NET Task` Boundaries Explicitly

Task interop lives under `Flow.Task`.

Started task values bind directly inside `flow {}`:

```fsharp
let workflow : Flow<unit, string, int> =
    flow {
        let! value = System.Threading.Tasks.Task.FromResult 42
        return value
    }
```

Use `Flow.Task` when you want the boundary shape to stay explicit.

Use cold task helpers when work should start at flow execution time:

```fsharp
let load : Flow<unit, string, int> =
    Flow.Task.fromCold(fun _ ->
        System.Threading.Tasks.Task.FromResult 42)
```

Use hot task helpers only when you already have a started task value on purpose:

```fsharp
let started = System.Threading.Tasks.Task.FromResult 42
let load = Flow.Task.fromHot started
```

## 7. Access The Cancellation Token Only Where Needed

The token is already part of `Flow.run`, but some dependencies need it inside the flow:

```fsharp
let ping : Flow<AppEnv, string, Response> =
    flow {
        let! ct = Flow.Runtime.cancellationToken
        let! gateway = Flow.read _.Gateway
        return! gateway.Ping(ct) |> Flow.Task.fromColdResult
    }
```

## 8. Use Runtime Helpers Where They Clarify The Flow

Runtime helpers live under `Flow.Runtime`:

- `Flow.Runtime.retry`
- `Flow.Runtime.timeout`
- `Flow.Runtime.log`
- `Flow.Runtime.logWith`
- `Flow.Runtime.catchCancellation`
- `Flow.Runtime.useWithAcquireRelease`

For normal resource lifetimes, prefer `use` and `use!` directly inside `flow {}`.

## Next

Read [`examples/README.md`](../examples/README.md) to see complete flows, then [`docs/FSTOOLKIT_MIGRATION.md`](./FSTOOLKIT_MIGRATION.md) if you are moving from `Async<Result<_,_>>`.
