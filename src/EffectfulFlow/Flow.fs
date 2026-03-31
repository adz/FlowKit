namespace EffectfulFlow

open System
open System.Threading
open System.Threading.Tasks

/// Represents a cold workflow that depends on an environment, observes cancellation,
/// can fail with a typed error, and can succeed with a value.
type Flow<'env, 'error, 'value> =
    private
    | Flow of ('env -> CancellationToken -> Async<Result<'value, 'error>>)

/// Log levels used by runtime logging helpers.
[<RequireQualifiedAccess>]
type LogLevel =
    | Trace
    | Debug
    | Information
    | Warning
    | Error
    | Critical

/// A log entry written through a runtime logger.
type LogEntry =
    { Level: LogLevel
      Message: string
      TimestampUtc: DateTimeOffset }

/// Defines how runtime retry helpers should repeat typed failures.
type RetryPolicy<'error> =
    { MaxAttempts: int
      Delay: int -> TimeSpan
      ShouldRetry: 'error -> bool }

[<RequireQualifiedAccess>]
module RetryPolicy =
    let noDelay (maxAttempts: int) : RetryPolicy<'error> =
        { MaxAttempts = maxAttempts
          Delay = fun _ -> TimeSpan.Zero
          ShouldRetry = fun _ -> true }

[<RequireQualifiedAccess>]
module Flow =
    let run
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (Flow operation: Flow<'env, 'error, 'value>)
        : Async<Result<'value, 'error>> =
        operation environment cancellationToken

    let succeed (value: 'value) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ -> async.Return(Ok value))

    let value (item: 'value) : Flow<'env, 'error, 'value> =
        succeed item

    let fail (error: 'error) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ -> async.Return(Error error))

    let fromResult (result: Result<'value, 'error>) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ -> async.Return result)

    let fromAsync (operation: Async<'value>) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ ->
            async {
                let! item = operation
                return Ok item
            })

    let fromAsyncResult (operation: Async<Result<'value, 'error>>) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ -> operation)

    let env<'env, 'error> : Flow<'env, 'error, 'env> =
        Flow(fun environment _ -> async.Return(Ok environment))

    let read (projection: 'env -> 'value) : Flow<'env, 'error, 'value> =
        Flow(fun environment _ -> async.Return(Ok(projection environment)))

    let map
        (mapper: 'value -> 'next)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'next> =
        Flow(fun environment cancellationToken ->
            async {
                let! result = run environment cancellationToken flow
                return Result.map mapper result
            })

    let bind
        (binder: 'value -> Flow<'env, 'error, 'next>)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'next> =
        Flow(fun environment cancellationToken ->
            async {
                let! result = run environment cancellationToken flow

                match result with
                | Ok item -> return! run environment cancellationToken (binder item)
                | Error error -> return Error error
            })

    let tap
        (binder: 'value -> Flow<'env, 'error, unit>)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'value> =
        bind
            (fun item ->
                binder item
                |> map (fun () -> item))
            flow

    let mapError
        (mapper: 'error -> 'nextError)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'nextError, 'value> =
        Flow(fun environment cancellationToken ->
            async {
                let! result = run environment cancellationToken flow
                return Result.mapError mapper result
            })

    let catch
        (handler: exn -> 'error)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'value> =
        Flow(fun environment cancellationToken ->
            async {
                try
                    return! run environment cancellationToken flow
                with error ->
                    return Error(handler error)
            })

    let mapEnv
        (mapping: 'outerEnvironment -> 'innerEnvironment)
        (flow: Flow<'innerEnvironment, 'error, 'value>)
        : Flow<'outerEnvironment, 'error, 'value> =
        Flow(fun environment cancellationToken ->
            environment
            |> mapping
            |> fun innerEnvironment -> run innerEnvironment cancellationToken flow)

    let tryFinally
        (compensation: unit -> unit)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'value> =
        Flow(fun environment cancellationToken ->
            async {
                try
                    return! run environment cancellationToken flow
                finally
                    compensation ()
            })

    let delay (factory: unit -> Flow<'env, 'error, 'value>) : Flow<'env, 'error, 'value> =
        Flow(fun environment cancellationToken ->
            run environment cancellationToken (factory ()))

    let toAsyncResult
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (flow: Flow<'env, 'error, 'value>)
        : Async<Result<'value, 'error>> =
        run environment cancellationToken flow

    [<RequireQualifiedAccess>]
    module Task =
        let fromColdResult
            (factory: CancellationToken -> Task<Result<'value, 'error>>)
            : Flow<'env, 'error, 'value> =
            Flow(fun _ cancellationToken ->
                async {
                    let! result = factory cancellationToken |> Async.AwaitTask
                    return result
                })

        let fromHotResult
            (task: Task<Result<'value, 'error>>)
            : Flow<'env, 'error, 'value> =
            fromColdResult (fun _ -> task)

        let fromCold
            (factory: CancellationToken -> Task<'value>)
            : Flow<'env, 'error, 'value> =
            Flow(fun _ cancellationToken ->
                async {
                    let! item = factory cancellationToken |> Async.AwaitTask
                    return Ok item
                })

        let fromHot (task: Task<'value>) : Flow<'env, 'error, 'value> =
            fromCold (fun _ -> task)

        let fromColdUnit
            (factory: CancellationToken -> Task)
            : Flow<'env, 'error, unit> =
            Flow(fun _ cancellationToken ->
                async {
                    do! factory cancellationToken |> Async.AwaitTask
                    return Ok ()
                })

        let fromHotUnit (task: Task) : Flow<'env, 'error, unit> =
            fromColdUnit (fun _ -> task)

    [<RequireQualifiedAccess>]
    module Runtime =
        let cancellationToken<'env, 'error> : Flow<'env, 'error, CancellationToken> =
            Flow(fun _ cancellationToken -> async.Return(Ok cancellationToken))

        let catchCancellation
            (handler: OperationCanceledException -> 'error)
            (flow: Flow<'env, 'error, 'value>)
            : Flow<'env, 'error, 'value> =
            Flow(fun environment cancellationToken ->
                async {
                    try
                        return! run environment cancellationToken flow
                    with :? OperationCanceledException as error ->
                        return Error(handler error)
                })

        let ensureNotCanceled (canceledError: 'error) : Flow<'env, 'error, unit> =
            Flow(fun _ cancellationToken ->
                async {
                    if cancellationToken.IsCancellationRequested then
                        return Error canceledError
                    else
                        return Ok ()
                })

        let sleep (delay: TimeSpan) : Flow<'env, 'error, unit> =
            Task.fromColdUnit (fun cancellationToken -> Task.Delay(delay, cancellationToken))

        let log
            (writer: 'env -> LogEntry -> unit)
            (level: LogLevel)
            (message: string)
            : Flow<'env, 'error, unit> =
            Flow(fun environment _ ->
                async {
                    writer
                        environment
                        { Level = level
                          Message = message
                          TimestampUtc = DateTimeOffset.UtcNow }

                    return Ok ()
                })

        let logWith
            (writer: 'env -> LogEntry -> unit)
            (level: LogLevel)
            (messageFactory: 'env -> string)
            : Flow<'env, 'error, unit> =
            Flow(fun environment _ ->
                async {
                    writer
                        environment
                        { Level = level
                          Message = messageFactory environment
                          TimestampUtc = DateTimeOffset.UtcNow }

                    return Ok ()
                })

        let useWithAcquireRelease
            (acquire: Flow<'env, 'error, 'resource>)
            (release: 'resource -> CancellationToken -> Task)
            (useResource: 'resource -> Flow<'env, 'error, 'value>)
            : Flow<'env, 'error, 'value> =
            bind
                (fun resource ->
                    Flow(fun environment cancellationToken ->
                        async {
                            let! result =
                                run environment cancellationToken (useResource resource)
                                |> Async.Catch

                            do! release resource cancellationToken |> Async.AwaitTask

                            match result with
                            | Choice1Of2 value -> return value
                            | Choice2Of2 error -> return raise error
                        }))
                acquire

        let timeout
            (after: TimeSpan)
            (timeoutError: 'error)
            (flow: Flow<'env, 'error, 'value>)
            : Flow<'env, 'error, 'value> =
            Flow(fun environment cancellationToken ->
                async {
                    try
                        let! child =
                            Async.StartChild(
                                run environment cancellationToken flow,
                                millisecondsTimeout = int after.TotalMilliseconds
                            )

                        return! child
                    with :? TimeoutException ->
                        return Error timeoutError
                })

        let retry
            (policy: RetryPolicy<'error>)
            (flow: Flow<'env, 'error, 'value>)
            : Flow<'env, 'error, 'value> =
            let rec loop attempt =
                Flow(fun environment cancellationToken ->
                    async {
                        let! result = run environment cancellationToken flow

                        match result with
                        | Ok value -> return Ok value
                        | Error error when attempt < policy.MaxAttempts && policy.ShouldRetry error ->
                            let delay = policy.Delay attempt

                            if delay > TimeSpan.Zero then
                                do! Task.Delay(delay, cancellationToken) |> Async.AwaitTask

                            return! run environment cancellationToken (loop (attempt + 1))
                        | Error error ->
                            return Error error
                    })

            if policy.MaxAttempts < 1 then
                invalidArg (nameof policy.MaxAttempts) "RetryPolicy.MaxAttempts must be at least 1."

            loop 1

type FlowBuilder() =
    let disposeResource (resource: obj) (cancellationToken: CancellationToken) =
        match resource with
        | :? IAsyncDisposable as asyncDisposable ->
            asyncDisposable.DisposeAsync().AsTask()
        | :? IDisposable as disposable ->
            disposable.Dispose()
            Task.CompletedTask
        | _ ->
            invalidArg (nameof resource) "Flow use/use! requires IDisposable or IAsyncDisposable."

    member _.Return(value: 'value) : Flow<'env, 'error, 'value> =
        Flow.succeed value

    member _.ReturnFrom(flow: Flow<'env, 'error, 'value>) : Flow<'env, 'error, 'value> =
        flow

    member _.ReturnFrom(result: Result<'value, 'error>) : Flow<'env, 'error, 'value> =
        Flow.fromResult result

    member _.ReturnFrom(operation: Async<'value>) : Flow<'env, 'error, 'value> =
        Flow.fromAsync operation

    member _.ReturnFrom(operation: Async<Result<'value, 'error>>) : Flow<'env, 'error, 'value> =
        Flow.fromAsyncResult operation

    member _.Bind
        (
            flow: Flow<'env, 'error, 'value>,
            binder: 'value -> Flow<'env, 'error, 'next>
        ) : Flow<'env, 'error, 'next> =
        Flow.bind binder flow

    member _.Bind
        (
            result: Result<'value, 'error>,
            binder: 'value -> Flow<'env, 'error, 'next>
        ) : Flow<'env, 'error, 'next> =
        Flow.bind binder (Flow.fromResult result)

    member _.Bind
        (
            operation: Async<'value>,
            binder: 'value -> Flow<'env, 'error, 'next>
        ) : Flow<'env, 'error, 'next> =
        Flow.bind binder (Flow.fromAsync operation)

    member _.Bind
        (
            operation: Async<Result<'value, 'error>>,
            binder: 'value -> Flow<'env, 'error, 'next>
        ) : Flow<'env, 'error, 'next> =
        Flow.bind binder (Flow.fromAsyncResult operation)

    member _.Bind
        (
            operation: Task<'value>,
            binder: 'value -> Flow<'env, 'error, 'next>
        ) : Flow<'env, 'error, 'next> =
        Flow.bind binder (Flow.Task.fromHot operation)

    member _.Bind
        (
            operation: Task<Result<'value, 'error>>,
            binder: 'value -> Flow<'env, 'error, 'next>
        ) : Flow<'env, 'error, 'next> =
        Flow.bind binder (Flow.Task.fromHotResult operation)

    member _.Zero() : Flow<'env, 'error, unit> =
        Flow.succeed ()

    member _.Delay(factory: unit -> Flow<'env, 'error, 'value>) : Flow<'env, 'error, 'value> =
        Flow.delay factory

    member _.Combine(left: Flow<'env, 'error, unit>, right: Flow<'env, 'error, 'value>) : Flow<'env, 'error, 'value> =
        Flow.bind (fun () -> right) left

    member _.TryWith
        (
            flow: Flow<'env, 'error, 'value>,
            handler: exn -> 'error
        ) : Flow<'env, 'error, 'value> =
        Flow.catch handler flow

    member _.TryFinally
        (
            flow: Flow<'env, 'error, 'value>,
            compensation: unit -> unit
        ) : Flow<'env, 'error, 'value> =
        Flow.tryFinally compensation flow

    member this.Using
        (
            resource: 'resource,
            binder: 'resource -> Flow<'env, 'error, 'value>
        ) : Flow<'env, 'error, 'value> =
        Flow.Runtime.useWithAcquireRelease
            (Flow.succeed resource)
            (fun acquired cancellationToken -> disposeResource (box acquired) cancellationToken)
            binder

    member this.While
        (
            guard: unit -> bool,
            body: Flow<'env, 'error, unit>
        ) : Flow<'env, 'error, unit> =
        if guard () then
            this.Bind(body, fun () -> this.While(guard, body))
        else
            this.Zero()

    member this.For
        (
            sequence: seq<'value>,
            binder: 'value -> Flow<'env, 'error, unit>
        ) : Flow<'env, 'error, unit> =
        let values = Seq.toArray sequence
        let mutable index = 0

        this.While(
            (fun () -> index < values.Length),
            this.Delay(fun () ->
                let value = values[index]
                index <- index + 1
                binder value)
        )

[<AutoOpen>]
module FlowBuilderModule =
    let flow = FlowBuilder()
