# Coming From FsToolkit

If you already use FsToolkit, the real question is not:

"Is this abstraction more ambitious?"

The real question is:

"When does `Effect<'env, 'error, 'value>` produce cleaner application code than `Async<Result<'value, 'error>>` with helper builders?"

## Short Answer

FsToolkit remains a strong fit when:

- the main problem is wrapper composition
- dependencies are trivial
- the natural application boundary is already `Async<Result<_,_>>`

Effect.FS starts to pay for itself when:

- dependencies should be explicit in the type
- workflows mix `Result`, `Async`, `Async<Result<_,_>>`, `Task`, and `Task<Result<_,_>>`
- retry, timeout, cleanup, and logging belong close to the workflow
- expected failures should stay typed without pretending every exception is a domain error

## Mental Model Shift

With FsToolkit-style code, the center is often:

```fsharp
Async<Result<'value, 'error>>
```

With Effect.FS, the center becomes:

```fsharp
Effect<'env, 'error, 'value>
```

The extra type parameter matters because it makes dependency requirements first-class.

## Simple Comparison

A typical FsToolkit-style shape:

```fsharp
let runCommand config =
    asyncResult {
        let! validated = validate config |> AsyncResult.ofResult
        let! payload = loadPayload validated
        do! AsyncResult.mapError InfrastructureError (writeAudit validated)
        return payload
    }
```

The same kind of use case in Effect.FS:

```fsharp
let runCommand : Effect<AppEnv, AppError, Payload> =
    effect {
        let! env = Effect.environment<AppEnv, AppError>
        let! validated = validate env.Config |> Result.mapError ValidationFailed
        do! Effect.log (fun e entry -> e.WriteLog entry) LogLevel.Information "validated command"
        let! payload = env.Gateway.Load(validated, CancellationToken.None) |> Effect.fromTaskResultValue
        return payload
    }
```

The gain is not only syntax.

The workflow type now tells you:

- what environment it needs
- what failures it produces
- what value it returns

## Migration Surface

The compatibility strategy is partial and deliberate.

Available bridges:

- `Effect.fromAsyncResult`
- `Effect.toAsyncResult`
- `AsyncResultCompat.ofAsyncResult`
- `AsyncResultCompat.toAsyncResult`

Inside `effect {}` you can also bind directly from:

- `Result`
- `Async`
- `Async<Result<_,_>>`
- `Task`
- `Task<Result<_,_>>`

That means migration can happen one workflow at a time.

## Where Effect.FS Really Helps

The strongest cases are:

- application services with a visible dependency graph
- workflows that combine validation, IO, logging, and retries
- codebases that touch both `Async` and `Task`
- systems where operational concerns have started to blur the happy path

## Where It May Not Be Worth It

Do not force Effect.FS into places where it only adds ceremony:

- pure functions
- tiny scripts
- boundaries that are already clear as `Task<'T>`
- code that is already easy to read with plain `Result`

## Open Discussion For FsToolkit Users

The migration path is implemented, but two product decisions still deserve discussion:

- whether `AsyncResultCompat` belongs in the core package or a separate compatibility package
- whether the library should ever offer a more FsToolkit-shaped convenience layer, or stay opinionated around the current explicit core
