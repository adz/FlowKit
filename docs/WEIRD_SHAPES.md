# Weird Shapes

Read this page when a dependency returns an awkward nested wrapper shape and you want to normalize it without spreading that shape through the rest of the code.

The rule is simple:

- normalize the awkward shape once near the boundary
- unwrap one layer at a time with `let!`
- keep the rest of the workflow in ordinary `Effect` form

## Why This Works

Inside `effect {}`, each `let!` removes one wrapper layer.

That means you usually do not need a special helper for every nested combination. A short sequence of explicit steps is often easier to review and maintain.

If overload resolution gets ambiguous, add a type annotation to the intermediate value.

## Example: `Async<Async<Result<_,_>>>`

```fsharp
let workflow : Effect<unit, string, int> =
    effect {
        let! (next: Async<Result<int, string>>) =
            async {
                return async { return Ok 42 }
            }

        let! value = next
        return value
    }
```

## Example: `Result<Async<_>, _>`

```fsharp
let workflow : Effect<unit, string, int> =
    effect {
        let! (next: Async<int>) = Ok(async { return 42 })
        let! value = next
        return value
    }
```

## Example: `Result<Result<_,_>, _>`

```fsharp
let workflow : Effect<unit, string, int> =
    effect {
        let! (next: Result<int, string>) = Ok(Ok 42)
        let! value = next
        return value
    }
```

## Prefer Boundary Normalization

If a legacy dependency returns a strange wrapper shape, normalize it once near the boundary:

```fsharp
let loadUser id : Effect<AppEnv, AppError, User> =
    effect {
        let! env = Effect.environment
        let! next = env.LegacyApi.BeginLoad id

        let! result =
            async {
                let! value = next
                return Result.mapError LegacyFailed value
            }

        return result
    }
```

After that point, keep the rest of the application in plain `Effect`.

## Cold And Hot Tasks

Task temperature matters.

Cold task source:

```fsharp
let workflow =
    Effect.fromColdTask(fun cancellationToken ->
        client.Fetch(cancellationToken))
```

The task is created when the effect executes.

Hot task value:

```fsharp
let started = client.Fetch(System.Threading.CancellationToken.None)
let workflow = Effect.fromTaskValue started
```

The task may already be running before the effect executes.

Use these rules:

- use `fromColdTask`, `fromColdTaskResult`, and `fromColdTaskUnit` when work should start at effect execution time
- use `fromTaskValue`, `fromTaskResultValue`, and `fromTaskUnit` only when you already have a task value on purpose
- if a dependency call directly returns a task, prefer wrapping the call in a cold factory

## When To Add A Helper

Add a helper only when all of these are true:

- the same shape appears repeatedly
- the helper name is clearer than two explicit `let!` steps
- the helper represents a real boundary pattern, not a one-off shape

## Next

Read [`examples/EffectFs.MaintenanceExamples/Program.fs`](../examples/EffectFs.MaintenanceExamples/Program.fs) to see these cases running, then return to [`examples/README.md`](../examples/README.md) for the broader examples.
