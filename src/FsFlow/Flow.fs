namespace FsFlow

open System
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Represents a cold workflow that depends on an environment, observes cancellation,
/// can fail with a typed error, and can succeed with a value.
/// </summary>
/// <typeparam name="env">The type of the environment dependency.</typeparam>
/// <typeparam name="error">The type of the failure value.</typeparam>
/// <typeparam name="value">The type of the success value.</typeparam>
type Flow<'env, 'error, 'value> =
    private
    | Flow of ('env -> CancellationToken -> Async<Result<'value, 'error>>)

/// <summary>
/// Represents a cold task factory that starts when given the runtime cancellation token.
/// </summary>
type ColdTask<'value> = CancellationToken -> Task<'value>

/// <summary>
/// Represents a cold task factory that starts when given the runtime cancellation token
/// and returns a typed success or failure.
/// </summary>
type ColdTaskResult<'value, 'error> = CancellationToken -> Task<Result<'value, 'error>>

/// <summary>
/// Log levels used by runtime logging helpers.
/// </summary>
[<RequireQualifiedAccess>]
type LogLevel =
    /// <summary>Extremely detailed tracing messages.</summary>
    | Trace
    /// <summary>Detailed debugging information.</summary>
    | Debug
    /// <summary>General informational messages.</summary>
    | Information
    /// <summary>Warning messages for non-critical issues.</summary>
    | Warning
    /// <summary>Error messages for failures.</summary>
    | Error
    /// <summary>Critical error messages for system crashes.</summary>
    | Critical

/// <summary>
/// A log entry written through a runtime logger.
/// </summary>
type LogEntry =
    {
      /// <summary>The severity level of the log.</summary>
      Level: LogLevel
      /// <summary>The message content of the log.</summary>
      Message: string
      /// <summary>The timestamp in UTC when the log was created.</summary>
      TimestampUtc: DateTimeOffset }

/// <summary>
/// Defines how runtime retry helpers should repeat typed failures.
/// </summary>
type RetryPolicy<'error> =
    {
      /// <summary>The maximum number of attempts, including the initial run.</summary>
      MaxAttempts: int
      /// <summary>A function providing the delay duration based on the attempt number (1-based).</summary>
      Delay: int -> TimeSpan
      /// <summary>A predicate that determines if a specific error should trigger a retry.</summary>
      ShouldRetry: 'error -> bool }

/// <summary>
/// Standard retry policies.
/// </summary>
[<RequireQualifiedAccess>]
module RetryPolicy =
    /// <summary>
    /// Creates a policy that retries immediately without delay.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of attempts.</param>
    /// <example>
    /// <code>
    /// let policy = RetryPolicy.noDelay 3
    /// </code>
    /// </example>
    let noDelay (maxAttempts: int) : RetryPolicy<'error> =
        { MaxAttempts = maxAttempts
          Delay = fun _ -> TimeSpan.Zero
          ShouldRetry = fun _ -> true }

/// <summary>
/// Core functions for creating, composing, and executing flows.
/// </summary>
[<RequireQualifiedAccess>]
module Flow =
    /// <summary>
    /// Converts a flow into an async result using the provided environment and cancellation token.
    /// </summary>
    /// <param name="environment">The environment required by the flow.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <param name="flow">The flow to convert.</param>
    /// <example>
    /// <code>
    /// let result = Flow.toAsync env cts.Token myFlow |> Async.RunSynchronously
    /// </code>
    /// </example>
    let toAsync
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (Flow operation: Flow<'env, 'error, 'value>)
        : Async<Result<'value, 'error>> =
        operation environment cancellationToken

    /// <summary>
    /// Lifts a success value into a flow.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.succeed 42
    /// </code>
    /// </example>
    let succeed (value: 'value) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ -> async.Return(Ok value))

    /// <summary>
    /// Alias for <see cref="succeed"/>.
    /// </summary>
    let value (item: 'value) : Flow<'env, 'error, 'value> =
        succeed item

    /// <summary>
    /// Lifts a failure error into a flow.
    /// </summary>
    /// <param name="error">The error value.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.fail "Something went wrong"
    /// </code>
    /// </example>
    let fail (error: 'error) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ -> async.Return(Error error))

    /// <summary>
    /// Lifts a <see cref="Result"/> into a flow.
    /// </summary>
    /// <param name="result">The result to lift.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.fromResult (Ok 42)
    /// </code>
    /// </example>
    let fromResult (result: Result<'value, 'error>) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ -> async.Return result)

    /// <summary>
    /// Lifts an <see cref="Async"/> operation into a flow.
    /// </summary>
    /// <param name="operation">The async operation.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.fromAsync (async { return 42 })
    /// </code>
    /// </example>
    let fromAsync (operation: Async<'value>) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ ->
            async {
                let! item = operation
                return Ok item
            })

    /// <summary>
    /// Lifts an <see cref="Async"/> of <see cref="Result"/> into a flow.
    /// </summary>
    /// <param name="operation">The async result operation.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.fromAsyncResult (async { return Ok 42 })
    /// </code>
    /// </example>
    let fromAsyncResult (operation: Async<Result<'value, 'error>>) : Flow<'env, 'error, 'value> =
        Flow(fun _ _ -> operation)

    /// <summary>
    /// Reads the entire environment from the flow.
    /// </summary>
    /// <example>
    /// <code>
    /// let flow = flow {
    ///     let! env = Flow.env
    ///     return env.SomeProperty
    /// }
    /// </code>
    /// </example>
    let env<'env, 'error> : Flow<'env, 'error, 'env> =
        Flow(fun environment _ -> async.Return(Ok environment))

    /// <summary>
    /// Reads a projection of the environment from the flow.
    /// </summary>
    /// <param name="projection">The function to project the environment.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.read (fun env -> env.ApiKey)
    /// </code>
    /// </example>
    let read (projection: 'env -> 'value) : Flow<'env, 'error, 'value> =
        Flow(fun environment _ -> async.Return(Ok(projection environment)))

    /// <summary>
    /// Maps the success value of a flow.
    /// </summary>
    /// <param name="mapper">The function to map the success value.</param>
    /// <param name="flow">The source flow.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.succeed 42 |> Flow.map (fun x -> x + 1)
    /// </code>
    /// </example>
    let map
        (mapper: 'value -> 'next)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'next> =
        Flow(fun environment cancellationToken ->
            async {
                let! result = toAsync environment cancellationToken flow
                return Result.map mapper result
            })

    /// <summary>
    /// Chains another flow based on the success value of the current flow.
    /// </summary>
    /// <param name="binder">The function to produce the next flow.</param>
    /// <param name="flow">The source flow.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.succeed 42 |> Flow.bind (fun x -> Flow.succeed (x + 1))
    /// </code>
    /// </example>
    let bind
        (binder: 'value -> Flow<'env, 'error, 'next>)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'next> =
        Flow(fun environment cancellationToken ->
            async {
                let! result = toAsync environment cancellationToken flow

                match result with
                | Ok item -> return! toAsync environment cancellationToken (binder item)
                | Error error -> return Error error
            })

    /// <summary>
    /// Performs a side effect with the success value of a flow, returning the original value.
    /// </summary>
    /// <param name="binder">The side-effecting flow.</param>
    /// <param name="flow">The source flow.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.succeed 42 |> Flow.tap (fun x -> Flow.Runtime.log writer LogLevel.Info (sprintf "%d" x))
    /// </code>
    /// </example>
    let tap
        (binder: 'value -> Flow<'env, 'error, unit>)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'value> =
        bind
            (fun item ->
                binder item
                |> map (fun () -> item))
            flow

    /// <summary>
    /// Maps the error value of a flow.
    /// </summary>
    /// <param name="mapper">The function to map the error value.</param>
    /// <param name="flow">The source flow.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.fail "error" |> Flow.mapError (fun e -> Exception(e))
    /// </code>
    /// </example>
    let mapError
        (mapper: 'error -> 'nextError)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'nextError, 'value> =
        Flow(fun environment cancellationToken ->
            async {
                let! result = toAsync environment cancellationToken flow
                return Result.mapError mapper result
            })

    /// <summary>
    /// Catches exceptions thrown by the flow and converts them into a typed error.
    /// </summary>
    /// <param name="handler">The function to convert the exception.</param>
    /// <param name="flow">The source flow.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.fromAsync (async { failwith "boom" }) |> Flow.catch (fun e -> e.Message)
    /// </code>
    /// </example>
    let catch
        (handler: exn -> 'error)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'value> =
        Flow(fun environment cancellationToken ->
            async {
                try
                    return! toAsync environment cancellationToken flow
                with error ->
                    return Error(handler error)
            })

    /// <summary>
    /// Runs a flow with a sub-section or projection of the current environment.
    /// </summary>
    /// <param name="mapping">The function to project the environment.</param>
    /// <param name="flow">The flow requiring the inner environment.</param>
    /// <example>
    /// <code>
    /// let innerFlow : Flow&lt;Config, string, unit&gt; = ...
    /// let outerFlow : Flow&lt;AppEnv, string, unit&gt; = innerFlow |> Flow.localEnv (fun env -> env.Config)
    /// </code>
    /// </example>
    let localEnv
        (mapping: 'outerEnvironment -> 'innerEnvironment)
        (flow: Flow<'innerEnvironment, 'error, 'value>)
        : Flow<'outerEnvironment, 'error, 'value> =
        Flow(fun environment cancellationToken ->
            environment
            |> mapping
            |> fun innerEnvironment -> toAsync innerEnvironment cancellationToken flow)

    /// <summary>
    /// Ensures a compensation action is executed after the flow completes, regardless of success or failure.
    /// </summary>
    /// <param name="compensation">The action to execute.</param>
    /// <param name="flow">The source flow.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.succeed 42 |> Flow.tryFinally (fun () -> printfn "done")
    /// </code>
    /// </example>
    let tryFinally
        (compensation: unit -> unit)
        (flow: Flow<'env, 'error, 'value>)
        : Flow<'env, 'error, 'value> =
        Flow(fun environment cancellationToken ->
            async {
                try
                    return! toAsync environment cancellationToken flow
                finally
                    compensation ()
            })

    /// <summary>
    /// Defers the creation of a flow until it is executed.
    /// </summary>
    /// <param name="factory">The function that creates the flow.</param>
    /// <example>
    /// <code>
    /// let flow = Flow.delay (fun () -> Flow.succeed DateTime.Now)
    /// </code>
    /// </example>
    let delay (factory: unit -> Flow<'env, 'error, 'value>) : Flow<'env, 'error, 'value> =
        Flow(fun environment cancellationToken ->
            toAsync environment cancellationToken (factory ()))

    /// <summary>
    /// Alias for <see cref="toAsync"/>.
    /// </summary>
    let toAsyncResult
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (flow: Flow<'env, 'error, 'value>)
        : Async<Result<'value, 'error>> =
        toAsync environment cancellationToken flow

    /// <summary>
    /// Helpers for interoperating with .NET Tasks.
    /// </summary>
    [<RequireQualifiedAccess>]
    module Task =
        /// <summary>
        /// Lifts a cold task factory that returns a result into a flow.
        /// </summary>
        /// <param name="factory">The cold task factory.</param>
        /// <remarks>The factory is called at flow execution time with the runtime cancellation token.</remarks>
        /// <example>
        /// <code>
        /// let flow = Flow.Task.fromColdResult (fun ct -> httpClient.GetAsync(url, ct) |> taskResult)
        /// </code>
        /// </example>
        let fromColdResult
            (factory: ColdTaskResult<'value, 'error>)
            : Flow<'env, 'error, 'value> =
            Flow(fun _ cancellationToken ->
                async {
                    let! result = factory cancellationToken |> Async.AwaitTask
                    return result
                })

        /// <summary>
        /// Lifts an already started (hot) task that returns a result into a flow.
        /// </summary>
        /// <param name="task">The hot task.</param>
        /// <remarks>The task has already been created before the flow runs, so Flow does not control when it starts.</remarks>
        let fromHotResult
            (task: Task<Result<'value, 'error>>)
            : Flow<'env, 'error, 'value> =
            fromColdResult (fun _ -> task)

        /// <summary>
        /// Lifts a cold task factory into a flow.
        /// </summary>
        /// <param name="factory">The cold task factory.</param>
        /// <remarks>The factory is called at flow execution time with the runtime cancellation token.</remarks>
        let fromCold
            (factory: ColdTask<'value>)
            : Flow<'env, 'error, 'value> =
            Flow(fun _ cancellationToken ->
                async {
                    let! item = factory cancellationToken |> Async.AwaitTask
                    return Ok item
                })

        /// <summary>
        /// Lifts an already started (hot) task into a flow.
        /// </summary>
        /// <param name="task">The hot task.</param>
        /// <remarks>The task has already been created before the flow runs, so Flow does not control when it starts.</remarks>
        let fromHot (task: Task<'value>) : Flow<'env, 'error, 'value> =
            fromCold (fun _ -> task)

        /// <summary>
        /// Lifts a cold task factory (unit returning) into a flow.
        /// </summary>
        /// <param name="factory">The cold task factory.</param>
        /// <remarks>The factory is called at flow execution time with the runtime cancellation token.</remarks>
        let fromColdUnit
            (factory: CancellationToken -> Task)
            : Flow<'env, 'error, unit> =
            Flow(fun _ cancellationToken ->
                async {
                    do! factory cancellationToken |> Async.AwaitTask
                    return Ok ()
                })

        /// <summary>
        /// Lifts an already started (hot) task (unit returning) into a flow.
        /// </summary>
        /// <param name="task">The hot task.</param>
        /// <remarks>The task has already been created before the flow runs, so Flow does not control when it starts.</remarks>
        let fromHotUnit (task: Task) : Flow<'env, 'error, unit> =
            fromColdUnit (fun _ -> task)

    /// <summary>
    /// Runtime helpers for operational concerns like logging, timeout, retry, and cleanup.
    /// </summary>
    [<RequireQualifiedAccess>]
    module Runtime =
        /// <summary>
        /// Reads the current cancellation token from the flow.
        /// </summary>
        /// <remarks>This observes the runtime token; it does not translate cancellation into a typed error by itself.</remarks>
        let cancellationToken<'env, 'error> : Flow<'env, 'error, CancellationToken> =
            Flow(fun _ cancellationToken -> async.Return(Ok cancellationToken))

        /// <summary>
        /// Catches <see cref="OperationCanceledException"/> and converts it into a typed error.
        /// </summary>
        /// <param name="handler">The function to convert the exception.</param>
        /// <param name="flow">The source flow.</param>
        /// <remarks>This translates cancellation exceptions raised during execution. It does not pre-check the token.</remarks>
        let catchCancellation
            (handler: OperationCanceledException -> 'error)
            (flow: Flow<'env, 'error, 'value>)
            : Flow<'env, 'error, 'value> =
            Flow(fun environment cancellationToken ->
                async {
                    try
                        return! toAsync environment cancellationToken flow
                    with :? OperationCanceledException as error ->
                        return Error(handler error)
                })

        /// <summary>
        /// Checks if cancellation has been requested and returns a typed error if it has.
        /// </summary>
        /// <param name="canceledError">The error to return if canceled.</param>
        /// <remarks>This observes the current token state and returns a typed error immediately instead of waiting for an exception.</remarks>
        let ensureNotCanceled (canceledError: 'error) : Flow<'env, 'error, unit> =
            Flow(fun _ cancellationToken ->
                async {
                    if cancellationToken.IsCancellationRequested then
                        return Error canceledError
                    else
                        return Ok ()
                })

        /// <summary>
        /// Suspends the flow for the specified duration, observing cancellation.
        /// </summary>
        /// <param name="delay">The duration to sleep.</param>
        /// <remarks>If the runtime token is canceled, the underlying task raises cancellation which can be translated with <see cref="catchCancellation"/>.</remarks>
        let sleep (delay: TimeSpan) : Flow<'env, 'error, unit> =
            Task.fromColdUnit (fun cancellationToken -> Task.Delay(delay, cancellationToken))

        /// <summary>
        /// Writes a log entry using the writer provided by the environment.
        /// </summary>
        /// <param name="writer">The logging function extracted from the environment.</param>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message.</param>
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

        /// <summary>
        /// Writes a log entry using a message produced from the environment.
        /// </summary>
        /// <param name="writer">The logging function extracted from the environment.</param>
        /// <param name="level">The log level.</param>
        /// <param name="messageFactory">The function to produce the message from the environment.</param>
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

        /// <summary>
        /// Safely acquires a resource, uses it, and ensures it is released via a task-based action.
        /// </summary>
        /// <param name="acquire">The flow that acquires the resource.</param>
        /// <param name="release">The function that releases the resource.</param>
        /// <param name="useResource">The flow that uses the resource.</param>
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
                                toAsync environment cancellationToken (useResource resource)
                                |> Async.Catch

                            do! release resource cancellationToken |> Async.AwaitTask

                            match result with
                            | Choice1Of2 value -> return value
                            | Choice2Of2 error -> return raise error
                        }))
                acquire

        /// <summary>
        /// Wraps a flow with a timeout. If the flow does not complete within the specified duration, returns a typed error.
        /// </summary>
        /// <param name="after">The duration after which to timeout.</param>
        /// <param name="timeoutError">The error to return on timeout.</param>
        /// <param name="flow">The flow to wrap.</param>
        /// <remarks>This helper translates timeout into a typed error. It does not automatically cancel the underlying work on timeout.</remarks>
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
                                toAsync environment cancellationToken flow,
                                millisecondsTimeout = int after.TotalMilliseconds
                            )

                        return! child
                    with :? TimeoutException ->
                        return Error timeoutError
                })

        /// <summary>
        /// Retries a flow according to the specified policy.
        /// </summary>
        /// <param name="policy">The retry policy.</param>
        /// <param name="flow">The flow to retry.</param>
        let retry
            (policy: RetryPolicy<'error>)
            (flow: Flow<'env, 'error, 'value>)
            : Flow<'env, 'error, 'value> =
            let rec loop attempt =
                Flow(fun environment cancellationToken ->
                    async {
                        let! result = toAsync environment cancellationToken flow

                        match result with
                        | Ok value -> return Ok value
                        | Error error when attempt < policy.MaxAttempts && policy.ShouldRetry error ->
                            let delay = policy.Delay attempt

                            if delay > TimeSpan.Zero then
                                do! Task.Delay(delay, cancellationToken) |> Async.AwaitTask

                            return! toAsync environment cancellationToken (loop (attempt + 1))
                        | Error error ->
                            return Error error
                    })

            if policy.MaxAttempts < 1 then
                invalidArg (nameof policy.MaxAttempts) "RetryPolicy.MaxAttempts must be at least 1."

            loop 1

/// <summary>
/// Computation expression builder for <see cref="Flow"/>.
/// </summary>
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

    member _.ReturnFrom(operation: ColdTask<'value>) : Flow<'env, 'error, 'value> =
        Flow.Task.fromCold operation

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

    member _.Bind
        (
            operation: ColdTask<'value>,
            binder: 'value -> Flow<'env, 'error, 'next>
        ) : Flow<'env, 'error, 'next> =
        Flow.bind binder (Flow.Task.fromCold operation)

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

/// <summary>
/// Entry point for the <see cref="Flow"/> computation expression.
/// </summary>
[<AutoOpen>]
module FlowBuilderModule =
    /// <summary>
    /// Builds a flow using a computation expression.
    /// </summary>
    let flow = FlowBuilder()
