# Migration Guide

Read this page when you already have `Async<Result<_,_>>` or FsToolkit-style workflows and want to adopt `Flow` incrementally without rewriting the whole application.

The migration path is: keep the code that is already honest, wrap the awkward boundary, then move one workflow at a time to `Flow<'env, 'error, 'value>`.

## Start From The Existing Shape

A typical pre-Flow use case already looks like this:

```fsharp
type AppEnv =
    { LoadUser: int -> Async<Result<string, string>>
      Prefix: string }

let greet userId (env: AppEnv) : Async<Result<string, string>> =
    async {
        let! loaded = env.LoadUser userId

        match loaded with
        | Error error ->
            return Error error
        | Ok name ->
            return Ok $"{env.Prefix} {name}"
    }
```

Do not rewrite pure validation or domain logic first.
Move the workflow boundary first.

## Step 1. Wrap The Existing `Async<Result<_,_>>`

If the old helper already returns `Async<Result<_,_>>`, keep it and lift it directly:

```fsharp
let greet userId : Flow<AppEnv, string, string> =
    flow {
        let! loadUser = Flow.read _.LoadUser
        let! name =
            loadUser userId
            |> Flow.fromAsyncResult

        let! prefix = Flow.read _.Prefix
        return $"{prefix} {name}"
    }
```

This is usually the smallest useful migration.

## Step 2. Keep Validation Plain

Do not convert plain `Result` helpers just because the outer workflow moved to `Flow`.

```fsharp
let validateName name =
    if System.String.IsNullOrWhiteSpace name then
        Error "missing name"
    else
        Ok name

let greet userId : Flow<AppEnv, string, string> =
    flow {
        let! loadUser = Flow.read _.LoadUser
        let! loadedName =
            loadUser userId
            |> Flow.fromAsyncResult

        let! validName = validateName loadedName
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

Keep the pure parts pure.
Use `flow {}` where dependencies, async work, or typed failure translation meet.

## Step 3. Introduce Task Boundaries Explicitly

When a dependency is really `.NET Task`-based, make that boundary visible:

```fsharp
type AppEnv =
    { LoadUser: int -> Task<Result<string, string>>
      Prefix: string }

let greet userId : Flow<AppEnv, string, string> =
    flow {
        let! loadUser = Flow.read _.LoadUser
        let! loadedName =
            loadUser userId
            |> Flow.Task.fromHotResult

        let! prefix = Flow.read _.Prefix
        return $"{prefix} {loadedName}"
    }
```

Use `fromHot*` when you already have a started task value.
Use `fromCold*` when the helper should start only when the flow runs.

## Step 4. Keep Execution At The App Edge

The old style:

```fsharp
greet 42 env |> Async.RunSynchronously
```

The migrated style:

```fsharp
greet 42
|> Flow.toAsync env cancellationToken
|> Async.RunSynchronously
```

That is the main runtime change: execution becomes explicit through `Flow.toAsync`.

## Step 5. Migrate One Use Case At A Time

You do not need a flag day migration.

- Keep existing `Async<Result<_,_>>` helpers and lift them with `Flow.fromAsyncResult`.
- Keep pure `Result` helpers unchanged.
- Move one application workflow at a time to `Flow`.
- Only switch environment passing once a workflow genuinely benefits from `Flow.read`, `Flow.env`, or `Flow.localEnv`.

If you already use FsToolkit computation expressions, treat the existing workflow as an honest wrapper shape first.
The most important question is not which CE you used before.
The important question is whether this particular workflow gets clearer once dependencies, typed errors, and async or task boundaries live in one place.

## Next

Read [`docs/TINY_EXAMPLES.md`](./TINY_EXAMPLES.md) for the smallest `Flow` shapes,
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for hot versus cold task boundaries,
and [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for a fuller application-shaped walkthrough.
