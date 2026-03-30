# Coming From FsToolkit

If you already use FsToolkit, the important question is not "is this better in theory?"

The practical question is:

"When does `Effect<'env, 'error, 'value>` produce cleaner application code than `Async<Result<'value, 'error>>` with helper builders?"

## The Short Answer

FsToolkit is still a strong fit when:

- your main problem is wrapper composition
- dependencies are already trivial
- you do not need a first-class environment story
- `Async<Result<_,_>>` already matches your application boundary cleanly

Effect.FS starts to help when:

- you want dependencies visible in the type, not just threaded manually
- you are mixing `Result`, `Async`, `Async<Result<_,_>>`, and `Task`
- logging, retry, timeout, and resource safety are part of the workflow itself
- you want one effect type instead of several local wrapper conventions

## Mental Model Shift

With FsToolkit-style code, the center is often:

```fsharp
Async<Result<'value, 'error>>
```

With Effect.FS, the center becomes:

```fsharp
Effect<'env, 'error, 'value>
```

The extra type parameter matters because it makes dependencies explicit instead of implicit.

## A Simple Comparison

FsToolkit-style code often looks roughly like this:

```fsharp
let runCommand config =
    asyncResult {
        let! validated = validate config |> AsyncResult.ofResult
        let! payload = loadPayload validated
        do! AsyncResult.mapError InfrastructureError (writeAudit validated)
        return payload
    }
```

That is fine as long as:

- dependency access is simple
- the wrapper stack is stable
- operational behavior stays outside the core workflow

The same shape in Effect.FS becomes:

```fsharp
let runCommand : Effect<AppEnv, AppError, Payload> =
    effect {
        let! env = Effect.environment<AppEnv, AppError>
        let! validated = validate env.Config |> Result.mapError ValidationFailed
        do! Effect.log (fun e entry -> e.WriteLog entry) LogLevel.Information "validated command"
        let! payload = loadPayload validated
        return payload
    }
```

The difference is not just syntax.

The workflow now says, in its type:

- what environment it needs
- what typed failures it produces
- what value it returns

## Compatibility Story

The library deliberately supports interop rather than demanding a flag day migration.

Use these bridges when moving from FsToolkit-style code:

- `Effect.fromAsyncResult` to lift existing `Async<Result<_,_>>`
- `Effect.toAsyncResult` to expose an effect back as `Async<Result<_,_>>`
- direct `let!` binding of `Result`, `Async`, `Async<Result<_,_>>`, and `Task`

That means you can migrate one workflow at a time instead of rewriting a whole codebase.

## Where Effect.FS Really Helps

The strongest cases are:

- application services with 2-5 dependencies
- workflows that combine validation, IO, logging, and retries
- systems that touch both F# `Async` and .NET `Task`
- codebases where dependency flow is becoming hard to see
- domains where expected failures should stay typed and local

## Where It May Not Be Worth It

Do not force Effect.FS into places where it adds ceremony:

- pure functions
- tiny scripts
- code that is already clear with plain `Result`
- boundaries that naturally want `Task<'T>` and nothing more

## Recommended Adoption Path

1. Keep pure validation and transformation in plain F# functions.
2. Introduce `Effect` at the application boundary where dependencies and IO begin.
3. Lift existing `Async<Result<_,_>>` code with `Effect.fromAsyncResult`.
4. Move logging, retry, and timeout behavior into the effect workflow only where it improves readability.
5. Keep exceptions and typed errors distinct. Translate deliberately.

That path gives you the upside without turning migration into a rewrite project.
