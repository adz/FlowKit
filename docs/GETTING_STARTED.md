# Getting Started

Read this page when you want to write your first small Effect.FS workflow.

The goal is to keep pure code as plain F#, then introduce `Effect` only at the boundary where dependencies, async work, or typed failures start.

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

You do not need `Effect` for code that is already clear as plain `Result`.

## 2. Introduce `Effect` At The Boundary

When the workflow needs dependencies or async work, return:

```fsharp
Effect<'env, 'error, 'value>
```

Example:

```fsharp
type AppEnv =
    { Prefix: string }

type AppError =
    | ValidationFailed of ValidationError

let greet input : Effect<AppEnv, AppError, string> =
    effect {
        let! env = Effect.environment
        let! name = validateName input |> Result.mapError ValidationFailed
        return $"{env.Prefix} {name}"
    }
```

Read the type as:

- `'env`: the environment the workflow needs
- `'error`: the expected typed failure
- `'value`: the success value

## 3. Run The Effect Explicitly

Effects are cold. They do nothing until you execute them:

```fsharp
let result =
    greet "Ada"
    |> Effect.execute { Prefix = "Hello" }
    |> Async.RunSynchronously
```

Result:

```fsharp
Ok "Hello Ada"
```

## 4. Bind Existing Wrapper Types Directly

Inside `effect {}` you can bind directly from common F# and .NET shapes:

```fsharp
let workflow : Effect<unit, string, int> =
    effect {
        let! a = Ok 1
        let! b = async { return 2 }
        let! c = System.Threading.Tasks.Task.FromResult 3
        return a + b + c
    }
```

This is the main ergonomic goal of the library: keep the workflow close to the happy path even when the inputs come in different wrapper shapes.

## 5. Access The Environment Explicitly

Use the environment helpers when the workflow depends on external context:

- `Effect.environment` to read the whole environment
- `Effect.read` to project one value from it
- `Effect.withEnvironment` to run a smaller effect inside a larger environment
- `Effect.provide` to pre-supply the environment

Example:

```fsharp
let prefixLength : Effect<AppEnv, AppError, int> =
    Effect.read (fun env -> env.Prefix.Length)
```

## 6. Prefer The Inferred `environment` Form

Most of the time you can write:

```fsharp
let! env = Effect.environment
```

The longer form:

```fsharp
let! env = Effect.environment<AppEnv, AppError>
```

is only needed when F# cannot infer the environment and error types yet.

## 7. Use `environmentWith` When It Reads Better

If the workflow uses the environment throughout, this can be a cleaner shape:

```fsharp
let greet : Effect<AppEnv, AppError, string> =
    Effect.environmentWith(fun env ->
        effect {
            return $"{env.Prefix} world"
        })
```

Use it when it reduces repetition. Do not force it into every workflow.

## 8. Add Operational Helpers Only Where They Help

Effect.FS includes helpers for common application concerns:

- `Effect.retry`
- `Effect.timeout`
- `Effect.log`
- `Effect.logWith`
- `Effect.bracket`
- `Effect.bracketAsync`
- `Effect.usingAsync`

Add them when they make the workflow clearer. Keep the core flow small and direct.

## 9. Migrate One Workflow At A Time

If you already have `Async<Result<_,_>>` code, start at the boundary:

- lift old code with `Effect.fromAsyncResult`
- run an effect as `Async<Result<_,_>>` with `Effect.toAsyncResult`
- use `AsyncResultCompat` during migration if that keeps the transition simpler

## Next

Read [`examples/README.md`](../examples/README.md) to see complete workflows, then [`docs/FSTOOLKIT_MIGRATION.md`](./FSTOOLKIT_MIGRATION.md) if you are moving from FsToolkit.
