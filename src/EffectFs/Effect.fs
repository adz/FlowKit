namespace EffectFs

open System
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Represents a cold workflow that depends on an environment, can fail with a typed error,
/// and can succeed with a value.
/// </summary>
/// <example>
/// <code lang="fsharp">
/// let workflow : Effect&lt;string, string, int&gt; =
///     effect {
///         let! text = Effect.environment
///         return text.Length
///     }
/// </code>
/// </example>
type Effect<'env, 'error, 'value> =
    private
    | Effect of ('env -> CancellationToken -> Async<Result<'value, 'error>>)

/// <summary>
/// Log levels used by <c>Effect.log</c> and <c>Effect.logWith</c>.
/// </summary>
/// <example>
/// <code lang="fsharp">
/// let level = LogLevel.Information
/// </code>
/// </example>
[<RequireQualifiedAccess>]
type LogLevel =
    | Trace
    | Debug
    | Information
    | Warning
    | Error
    | Critical

/// <summary>
/// A log entry written through the environment.
/// </summary>
/// <example>
/// <code lang="fsharp">
/// let entry =
///     { Level = LogLevel.Information
///       Message = "Started"
///       TimestampUtc = DateTimeOffset.UtcNow }
/// </code>
/// </example>
type LogEntry =
    { Level: LogLevel
      Message: string
      TimestampUtc: DateTimeOffset }

/// <summary>
/// Defines how <c>Effect.retry</c> should repeat typed failures.
/// </summary>
/// <example>
/// <code lang="fsharp">
/// let policy =
///     { MaxAttempts = 3
///       Delay = fun _ -> TimeSpan.Zero
///       ShouldRetry = ((=) "retry") }
/// </code>
/// </example>
type RetryPolicy<'error> =
    { MaxAttempts: int
      Delay: int -> TimeSpan
      ShouldRetry: 'error -> bool }

/// <summary>
/// Helpers for constructing retry policies.
/// </summary>
[<RequireQualifiedAccess>]
module RetryPolicy =
    /// <summary>
    /// Creates a retry policy with no delay between attempts.
    /// The first execution counts as attempt 1.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let policy = RetryPolicy.noDelay 3
    /// </code>
    /// </example>
    let noDelay (maxAttempts: int) : RetryPolicy<'error> =
        { MaxAttempts = maxAttempts
          Delay = fun _ -> TimeSpan.Zero
          ShouldRetry = fun _ -> true }

/// <summary>
/// Core functions for constructing, transforming, and running effects.
/// </summary>
[<RequireQualifiedAccess>]
module Effect =
    /// <summary>
    /// Runs an effect with an environment and cancellation token.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let result =
    ///     Effect.succeed 42
    ///     |> Effect.run () CancellationToken.None
    ///     |> Async.RunSynchronously
    /// </code>
    /// </example>
    let run
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (Effect operation: Effect<'env, 'error, 'value>)
        : Async<Result<'value, 'error>> =
        operation environment cancellationToken

    /// <summary>
    /// Creates an effect that succeeds with the given value.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Effect.succeed 42
    /// </code>
    /// </example>
    let succeed (value: 'value) : Effect<'env, 'error, 'value> =
        Effect(fun _ _ -> async.Return(Ok value))

    /// <summary>
    /// Creates an effect that fails with the given typed error.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow : Effect&lt;unit, string, int&gt; = Effect.fail "no value"
    /// </code>
    /// </example>
    let fail (error: 'error) : Effect<'env, 'error, 'value> =
        Effect(fun _ _ -> async.Return(Error error))

    /// <summary>
    /// Lifts a <c>Result</c> into an effect.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Ok 42 |> Effect.fromResult
    /// </code>
    /// </example>
    let fromResult (result: Result<'value, 'error>) : Effect<'env, 'error, 'value> =
        Effect(fun _ _ -> async.Return result)

    /// <summary>
    /// Alias for <c>fromResult</c>.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Ok 42 |> Effect.ofResult
    /// </code>
    /// </example>
    let ofResult (result: Result<'value, 'error>) : Effect<'env, 'error, 'value> =
        fromResult result

    /// <summary>
    /// Lifts an <c>Async&lt;'value&gt;</c> into an effect that cannot fail in the typed channel.
    /// Exceptions still propagate unless you catch them later.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = async { return 42 } |> Effect.fromAsync
    /// </code>
    /// </example>
    let fromAsync (operation: Async<'value>) : Effect<'env, 'error, 'value> =
        Effect(fun _ _ ->
            async {
                let! value = operation
                return Ok value
            })

    /// <summary>
    /// Alias for <c>fromAsync</c>.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = async { return 42 } |> Effect.ofAsync
    /// </code>
    /// </example>
    let ofAsync (operation: Async<'value>) : Effect<'env, 'error, 'value> =
        fromAsync operation

    /// <summary>
    /// Lifts an <c>Async&lt;Result&lt;_,_&gt;&gt;</c> into an effect.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = async { return Ok 42 } |> Effect.fromAsyncResult
    /// </code>
    /// </example>
    let fromAsyncResult (operation: Async<Result<'value, 'error>>) : Effect<'env, 'error, 'value> =
        Effect(fun _ _ -> operation)

    /// <summary>
    /// Lifts a cold task factory that returns <c>Task&lt;Result&lt;_,_&gt;&gt;</c>.
    /// The task is created when the effect executes.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.fromTaskResult(fun _ -> Task.FromResult(Ok 42))
    /// </code>
    /// </example>
    let fromTaskResult
        (factory: CancellationToken -> Task<Result<'value, 'error>>)
        : Effect<'env, 'error, 'value> =
        Effect(fun _ cancellationToken ->
            async {
                return! factory cancellationToken |> Async.AwaitTask
            })

    /// <summary>
    /// Alias for <c>fromTaskResult</c> that emphasizes the task source is cold.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.fromColdTaskResult(fun _ -> Task.FromResult(Ok 42))
    /// </code>
    /// </example>
    let fromColdTaskResult
        (factory: CancellationToken -> Task<Result<'value, 'error>>)
        : Effect<'env, 'error, 'value> =
        fromTaskResult factory

    /// <summary>
    /// Lifts a cold task factory that returns <c>Task&lt;'value&gt;</c>.
    /// The task is created when the effect executes.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.fromTask(fun _ -> Task.FromResult 42)
    /// </code>
    /// </example>
    let fromTask (factory: CancellationToken -> Task<'value>) : Effect<'env, 'error, 'value> =
        Effect(fun _ cancellationToken ->
            async {
                let! value = factory cancellationToken |> Async.AwaitTask
                return Ok value
            })

    /// <summary>
    /// Alias for <c>fromTask</c> that emphasizes the task source is cold.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.fromColdTask(fun _ -> Task.FromResult 42)
    /// </code>
    /// </example>
    let fromColdTask (factory: CancellationToken -> Task<'value>) : Effect<'env, 'error, 'value> =
        fromTask factory

    /// <summary>
    /// Alias for <c>fromTask</c>.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.ofTask(fun _ -> Task.FromResult 42)
    /// </code>
    /// </example>
    let ofTask (factory: CancellationToken -> Task<'value>) : Effect<'env, 'error, 'value> =
        fromTask factory

    /// <summary>
    /// Lifts an already-created <c>Task&lt;'value&gt;</c>.
    /// Use this only when the task value already exists on purpose.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let taskValue = Task.FromResult 42
    /// let workflow = Effect.fromTaskValue taskValue
    /// </code>
    /// </example>
    let fromTaskValue (task: Task<'value>) : Effect<'env, 'error, 'value> =
        fromTask (fun _ -> task)

    /// <summary>
    /// Lifts an already-created <c>Task&lt;Result&lt;_,_&gt;&gt;</c>.
    /// Use this only when the task value already exists on purpose.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let taskValue = Task.FromResult(Ok 42)
    /// let workflow = Effect.fromTaskResultValue taskValue
    /// </code>
    /// </example>
    let fromTaskResultValue (task: Task<Result<'value, 'error>>) : Effect<'env, 'error, 'value> =
        fromTaskResult (fun _ -> task)

    /// <summary>
    /// Lifts an already-created <c>Task</c> and returns <c>unit</c> on success.
    /// Use this only when the task value already exists on purpose.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Effect.fromTaskUnit Task.CompletedTask
    /// </code>
    /// </example>
    let fromTaskUnit (task: Task) : Effect<'env, 'error, unit> =
        Effect(fun _ _ ->
            async {
                do! task |> Async.AwaitTask
                return Ok ()
            })

    /// <summary>
    /// Lifts a cold task factory that returns <c>Task</c> and succeeds with <c>unit</c>.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Effect.fromColdTaskUnit(fun _ -> Task.CompletedTask)
    /// </code>
    /// </example>
    let fromColdTaskUnit (factory: CancellationToken -> Task) : Effect<'env, 'error, unit> =
        Effect(fun _ cancellationToken ->
            async {
                do! factory cancellationToken |> Async.AwaitTask
                return Ok ()
            })

    /// <summary>
    /// Reads the current environment.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow : Effect&lt;string, string, string&gt; = Effect.ask
    /// </code>
    /// </example>
    let ask<'env, 'error> : Effect<'env, 'error, 'env> =
        Effect(fun environment _ -> async.Return(Ok environment))

    /// <summary>
    /// Alias for <c>ask</c>.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow : Effect&lt;string, string, string&gt; = Effect.environment
    /// </code>
    /// </example>
    let environment<'env, 'error> : Effect<'env, 'error, 'env> =
        ask<'env, 'error>

    /// <summary>
    /// Projects a value from the environment without changing the environment type.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let length : Effect&lt;string, string, int&gt; =
    ///     Effect.read String.length
    /// </code>
    /// </example>
    let read (projection: 'env -> 'value) : Effect<'env, 'error, 'value> =
        Effect(fun environment _ -> async.Return(Ok(projection environment)))

    /// <summary>
    /// Reads the cancellation token passed to the current execution.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow : Effect&lt;unit, string, CancellationToken&gt; =
    ///     Effect.cancellationToken
    /// </code>
    /// </example>
    let cancellationToken<'env, 'error> : Effect<'env, 'error, CancellationToken> =
        Effect(fun _ token -> async.Return(Ok token))

    /// <summary>
    /// Transforms the success value of an effect.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Effect.succeed 21 |> Effect.map ((*) 2)
    /// </code>
    /// </example>
    let map
        (mapper: 'value -> 'next)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'next> =
        Effect(fun environment cancellationToken ->
            async {
                let! result = run environment cancellationToken effect
                return Result.map mapper result
            })

    /// <summary>
    /// Chains two effects together.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.succeed 21
    ///     |> Effect.bind (fun value -> Effect.succeed (value * 2))
    /// </code>
    /// </example>
    let bind
        (binder: 'value -> Effect<'env, 'error, 'next>)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'next> =
        Effect(fun environment cancellationToken ->
            async {
                let! result = run environment cancellationToken effect

                match result with
                | Ok value -> return! run environment cancellationToken (binder value)
                | Error error -> return Error error
            })

    /// <summary>
    /// Runs a follow-up effect for the success value and then returns the original value.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.succeed 42
    ///     |> Effect.tap (fun _ -> Effect.succeed ())
    /// </code>
    /// </example>
    let tap
        (binder: 'value -> Effect<'env, 'error, unit>)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        bind
            (fun value ->
                binder value
                |> map (fun () -> value))
            effect

    /// <summary>
    /// Binds the environment once and continues with a new effect.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow : Effect&lt;string, string, int&gt; =
    ///     Effect.environmentWith(fun env -> Effect.succeed env.Length)
    /// </code>
    /// </example>
    let environmentWith
        (binder: 'env -> Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        bind binder environment

    /// <summary>
    /// Transforms the typed error of an effect.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.fail "bad"
    ///     |> Effect.mapError (fun error -> $"Error: {error}")
    /// </code>
    /// </example>
    let mapError
        (mapper: 'error -> 'nextError)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'nextError, 'value> =
        Effect(fun environment cancellationToken ->
            async {
                let! result = run environment cancellationToken effect
                return Result.mapError mapper result
            })

    /// <summary>
    /// Catches exceptions thrown during effect execution and converts them to typed errors.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.fromAsync(async { return failwith "boom" })
    ///     |> Effect.catch (fun ex -> ex.Message)
    /// </code>
    /// </example>
    let catch
        (handler: exn -> 'error)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        Effect(fun environment cancellationToken ->
            async {
                try
                    return! run environment cancellationToken effect
                with error ->
                    return Error(handler error)
            })

    /// <summary>
    /// Catches <c>OperationCanceledException</c> and converts it to a typed error.
    /// Other exceptions still propagate.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.ensureNotCanceled "canceled"
    ///     |> Effect.catchCancellation (fun _ -> "canceled")
    /// </code>
    /// </example>
    let catchCancellation
        (handler: OperationCanceledException -> 'error)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        Effect(fun environment cancellationToken ->
            async {
                try
                    return! run environment cancellationToken effect
                with :? OperationCanceledException as error ->
                    return Error(handler error)
            })

    /// <summary>
    /// Supplies the environment ahead of time and returns an effect that no longer needs one.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.read String.length
    ///     |> Effect.provide "effect"
    /// </code>
    /// </example>
    let provide (environment: 'env) (effect: Effect<'env, 'error, 'value>) : Effect<unit, 'error, 'value> =
        Effect(fun () cancellationToken -> run environment cancellationToken effect)

    /// <summary>
    /// Runs an effect that needs a smaller environment inside a larger environment.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.read String.length
    ///     |> Effect.withEnvironment snd
    /// </code>
    /// </example>
    let withEnvironment
        (mapping: 'outerEnvironment -> 'innerEnvironment)
        (effect: Effect<'innerEnvironment, 'error, 'value>)
        : Effect<'outerEnvironment, 'error, 'value> =
        Effect(fun environment cancellationToken ->
            environment
            |> mapping
            |> fun innerEnvironment -> run innerEnvironment cancellationToken effect)

    /// <summary>
    /// Delays for the given duration while observing the current cancellation token.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Effect.sleep (TimeSpan.FromMilliseconds 50.0)
    /// </code>
    /// </example>
    let sleep (delay: TimeSpan) : Effect<'env, 'error, unit> =
        Effect(fun _ cancellationToken ->
            async {
                do! Task.Delay(delay, cancellationToken) |> Async.AwaitTask
                return Ok ()
            })

    /// <summary>
    /// Fails with the provided typed error if the current cancellation token is already canceled.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Effect.ensureNotCanceled "canceled"
    /// </code>
    /// </example>
    let ensureNotCanceled (canceledError: 'error) : Effect<'env, 'error, unit> =
        Effect(fun _ cancellationToken ->
            async {
                if cancellationToken.IsCancellationRequested then
                    return Error canceledError
                else
                    return Ok ()
            })

    /// <summary>
    /// Writes a fixed log message through a writer found in the environment.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow : Effect&lt;ResizeArray&lt;string&gt;, string, unit&gt; =
    ///     Effect.log
    ///         (fun sink entry -> sink.Add entry.Message)
    ///         LogLevel.Information
    ///         "Started"
    /// </code>
    /// </example>
    let log
        (writer: 'env -> LogEntry -> unit)
        (level: LogLevel)
        (message: string)
        : Effect<'env, 'error, unit> =
        Effect(fun environment _ ->
            async {
                writer
                    environment
                    { Level = level
                      Message = message
                      TimestampUtc = DateTimeOffset.UtcNow }

                return Ok ()
            })

    /// <summary>
    /// Writes a log message that is computed from the current environment.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow : Effect&lt;string, string, unit&gt; =
    ///     Effect.logWith
    ///         (fun _ _ -> ())
    ///         LogLevel.Information
    ///         (fun env -> $"Value: {env}")
    /// </code>
    /// </example>
    let logWith
        (writer: 'env -> LogEntry -> unit)
        (level: LogLevel)
        (messageFactory: 'env -> string)
        : Effect<'env, 'error, unit> =
        Effect(fun environment _ ->
            async {
                writer
                    environment
                    { Level = level
                      Message = messageFactory environment
                      TimestampUtc = DateTimeOffset.UtcNow }

                return Ok ()
            })

    /// <summary>
    /// Runs compensation code after the effect completes, whether it succeeds, fails, or throws.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.succeed 42
    ///     |> Effect.tryFinally (fun () -> ())
    /// </code>
    /// </example>
    let tryFinally
        (compensation: unit -> unit)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        Effect(fun environment cancellationToken ->
            async {
                try
                    return! run environment cancellationToken effect
                finally
                    compensation ()
            })

    /// <summary>
    /// Acquires a resource, runs a workflow with it, and always calls the release function.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.bracket
    ///         (Effect.succeed "resource")
    ///         (fun _ -> ())
    ///         (fun value -> Effect.succeed value.Length)
    /// </code>
    /// </example>
    let bracket
        (acquire: Effect<'env, 'error, 'resource>)
        (release: 'resource -> unit)
        (useResource: 'resource -> Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        bind
            (fun resource ->
                useResource resource
                |> tryFinally (fun () -> release resource))
            acquire

    /// <summary>
    /// Acquires a resource, runs a workflow with it, and always calls an asynchronous release function.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.bracketAsync
    ///         (Effect.succeed "resource")
    ///         (fun _ _ -> Task.CompletedTask)
    ///         (fun value -> Effect.succeed value.Length)
    /// </code>
    /// </example>
    let bracketAsync
        (acquire: Effect<'env, 'error, 'resource>)
        (release: 'resource -> CancellationToken -> Task)
        (useResource: 'resource -> Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        bind
            (fun resource ->
                Effect(fun environment cancellationToken ->
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

    /// <summary>
    /// Runs a workflow with an <c>IAsyncDisposable</c> resource and disposes it asynchronously afterwards.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.usingAsync resource (fun _ -> Effect.succeed 42)
    /// </code>
    /// </example>
    let usingAsync
        (resource: 'resource)
        (useResource: 'resource -> Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> when 'resource :> IAsyncDisposable =
        bracketAsync
            (succeed resource)
            (fun acquired _ -> acquired.DisposeAsync().AsTask())
            useResource

    /// <summary>
    /// Fails with the provided timeout error if the effect does not complete in time.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.sleep (TimeSpan.FromSeconds 1)
    ///     |> Effect.timeout (TimeSpan.FromMilliseconds 10) "timed out"
    /// </code>
    /// </example>
    let timeout
        (after: TimeSpan)
        (timeoutError: 'error)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        Effect(fun environment cancellationToken ->
            async {
                try
                    let! child =
                        Async.StartChild(
                            run environment cancellationToken effect,
                            millisecondsTimeout = int after.TotalMilliseconds
                        )

                    return! child
                with :? TimeoutException ->
                    return Error timeoutError
            })

    /// <summary>
    /// Retries an effect when it fails with a typed error accepted by the policy.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.fail "retry"
    ///     |> Effect.retry (RetryPolicy.noDelay 3)
    /// </code>
    /// </example>
    let retry
        (policy: RetryPolicy<'error>)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'value> =
        let rec loop attempt =
            Effect(fun environment cancellationToken ->
                async {
                    let! result = run environment cancellationToken effect

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

    /// <summary>
    /// Delays the creation of an effect until execution time.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow = Effect.delay(fun () -> Effect.succeed 42)
    /// </code>
    /// </example>
    let delay (factory: unit -> Effect<'env, 'error, 'value>) : Effect<'env, 'error, 'value> =
        Effect(fun environment cancellationToken ->
            run environment cancellationToken (factory ()))

    /// <summary>
    /// Executes an effect with an environment and no cancellation token.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let result =
    ///     Effect.succeed 42
    ///     |> Effect.execute ()
    ///     |> Async.RunSynchronously
    /// </code>
    /// </example>
    let execute (environment: 'env) (effect: Effect<'env, 'error, 'value>) : Async<Result<'value, 'error>> =
        run environment CancellationToken.None effect

    /// <summary>
    /// Executes an effect with an environment and an explicit cancellation token.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// use cts = new CancellationTokenSource()
    /// let result =
    ///     Effect.succeed 42
    ///     |> Effect.executeWithCancellation () cts.Token
    ///     |> Async.RunSynchronously
    /// </code>
    /// </example>
    let executeWithCancellation
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (effect: Effect<'env, 'error, 'value>)
        : Async<Result<'value, 'error>> =
        run environment cancellationToken effect

    /// <summary>
    /// Converts an effect back to <c>Async&lt;Result&lt;_,_&gt;&gt;</c> by supplying the environment.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let asyncResult =
    ///     Effect.succeed 42
    ///     |> Effect.toAsyncResult ()
    /// </code>
    /// </example>
    let toAsyncResult (environment: 'env) (effect: Effect<'env, 'error, 'value>) : Async<Result<'value, 'error>> =
        execute environment effect

/// <summary>
/// Computation expression builder for composing effects.
/// </summary>
/// <example>
/// <code lang="fsharp">
/// let workflow =
///     effect {
///         let! value = Ok 42
///         return value + 1
///     }
/// </code>
/// </example>
type EffectBuilder() =
    /// <summary>
    /// Returns a successful value from the computation expression.
    /// </summary>
    member _.Return(value: 'value) : Effect<'env, 'error, 'value> =
        Effect.succeed value

    /// <summary>
    /// Returns an existing effect from the computation expression.
    /// </summary>
    member _.ReturnFrom(effect: Effect<'env, 'error, 'value>) : Effect<'env, 'error, 'value> =
        effect

    /// <summary>
    /// Returns a <c>Result</c> from the computation expression.
    /// </summary>
    member _.ReturnFrom(result: Result<'value, 'error>) : Effect<'env, 'error, 'value> =
        Effect.fromResult result

    /// <summary>
    /// Returns an <c>Async&lt;'value&gt;</c> from the computation expression.
    /// </summary>
    member _.ReturnFrom(operation: Async<'value>) : Effect<'env, 'error, 'value> =
        Effect.fromAsync operation

    /// <summary>
    /// Returns an <c>Async&lt;Result&lt;_,_&gt;&gt;</c> from the computation expression.
    /// </summary>
    member _.ReturnFrom(operation: Async<Result<'value, 'error>>) : Effect<'env, 'error, 'value> =
        Effect.fromAsyncResult operation

    /// <summary>
    /// Returns a <c>Task&lt;Result&lt;_,_&gt;&gt;</c> from the computation expression.
    /// </summary>
    member _.ReturnFrom(task: Task<Result<'value, 'error>>) : Effect<'env, 'error, 'value> =
        Effect.fromTaskResultValue task

    /// <summary>
    /// Returns a <c>Task&lt;'value&gt;</c> from the computation expression.
    /// </summary>
    member _.ReturnFrom(task: Task<'value>) : Effect<'env, 'error, 'value> =
        Effect.fromTaskValue task

    /// <summary>
    /// Returns a <c>Task</c> from the computation expression.
    /// </summary>
    member _.ReturnFrom(task: Task) : Effect<'env, 'error, unit> =
        Effect.fromTaskUnit task

    /// <summary>
    /// Binds an effect inside the computation expression.
    /// </summary>
    member _.Bind
        (
            effect: Effect<'env, 'error, 'value>,
            binder: 'value -> Effect<'env, 'error, 'next>
        ) : Effect<'env, 'error, 'next> =
        Effect.bind binder effect

    /// <summary>
    /// Binds a <c>Result</c> inside the computation expression.
    /// </summary>
    member _.Bind
        (
            result: Result<'value, 'error>,
            binder: 'value -> Effect<'env, 'error, 'next>
        ) : Effect<'env, 'error, 'next> =
        Effect.bind binder (Effect.fromResult result)

    /// <summary>
    /// Binds an <c>Async&lt;'value&gt;</c> inside the computation expression.
    /// </summary>
    member _.Bind
        (
            operation: Async<'value>,
            binder: 'value -> Effect<'env, 'error, 'next>
        ) : Effect<'env, 'error, 'next> =
        Effect.bind binder (Effect.fromAsync operation)

    /// <summary>
    /// Binds an <c>Async&lt;Result&lt;_,_&gt;&gt;</c> inside the computation expression.
    /// </summary>
    member _.Bind
        (
            operation: Async<Result<'value, 'error>>,
            binder: 'value -> Effect<'env, 'error, 'next>
        ) : Effect<'env, 'error, 'next> =
        Effect.bind binder (Effect.fromAsyncResult operation)

    /// <summary>
    /// Binds a <c>Task&lt;Result&lt;_,_&gt;&gt;</c> inside the computation expression.
    /// </summary>
    member _.Bind
        (
            task: Task<Result<'value, 'error>>,
            binder: 'value -> Effect<'env, 'error, 'next>
        ) : Effect<'env, 'error, 'next> =
        Effect.bind binder (Effect.fromTaskResultValue task)

    /// <summary>
    /// Binds a <c>Task&lt;'value&gt;</c> inside the computation expression.
    /// </summary>
    member _.Bind
        (
            task: Task<'value>,
            binder: 'value -> Effect<'env, 'error, 'next>
        ) : Effect<'env, 'error, 'next> =
        Effect.bind binder (Effect.fromTaskValue task)

    /// <summary>
    /// Binds a <c>Task</c> inside the computation expression.
    /// </summary>
    member _.Bind
        (
            task: Task,
            binder: unit -> Effect<'env, 'error, 'next>
        ) : Effect<'env, 'error, 'next> =
        Effect.bind binder (Effect.fromTaskUnit task)

    /// <summary>
    /// Returns a successful <c>unit</c> value from the computation expression.
    /// </summary>
    member _.Zero() : Effect<'env, 'error, unit> =
        Effect.succeed ()

    /// <summary>
    /// Delays effect creation inside the computation expression.
    /// </summary>
    member _.Delay(factory: unit -> Effect<'env, 'error, 'value>) : Effect<'env, 'error, 'value> =
        Effect.delay factory

    /// <summary>
    /// Combines two effects when the left side returns <c>unit</c>.
    /// </summary>
    member _.Combine
        (
            left: Effect<'env, 'error, unit>,
            right: Effect<'env, 'error, 'value>
        ) : Effect<'env, 'error, 'value> =
        Effect.bind (fun () -> right) left

    /// <summary>
    /// Catches exceptions inside the computation expression and maps them to typed errors.
    /// </summary>
    member _.TryWith
        (
            effect: Effect<'env, 'error, 'value>,
            handler: exn -> 'error
        ) : Effect<'env, 'error, 'value> =
        Effect.catch handler effect

    /// <summary>
    /// Runs compensation code after the computation expression completes.
    /// </summary>
    member _.TryFinally
        (
            effect: Effect<'env, 'error, 'value>,
            compensation: unit -> unit
        ) : Effect<'env, 'error, 'value> =
        Effect.tryFinally compensation effect

    /// <summary>
    /// Uses and disposes an <c>IDisposable</c> resource inside the computation expression.
    /// </summary>
    member _.Using
        (
            resource: 'resource,
            binder: 'resource -> Effect<'env, 'error, 'value>
        ) : Effect<'env, 'error, 'value> when 'resource :> IDisposable =
        Effect.tryFinally
            (fun () ->
                if not (isNull (box resource)) then
                    resource.Dispose())
            (binder resource)

    /// <summary>
    /// Runs a while loop inside the computation expression.
    /// </summary>
    member this.While
        (
            guard: unit -> bool,
            body: Effect<'env, 'error, unit>
        ) : Effect<'env, 'error, unit> =
        if guard () then
            this.Bind(body, fun () -> this.While(guard, body))
        else
            this.Zero()

    /// <summary>
    /// Runs a for loop inside the computation expression.
    /// </summary>
    member this.For
        (
            sequence: seq<'value>,
            binder: 'value -> Effect<'env, 'error, unit>
        ) : Effect<'env, 'error, unit> =
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
module EffectBuilderModule =
    /// <summary>
    /// Computation expression entry point for composing effects.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     effect {
    ///         let! value = Ok 42
    ///         return value
    ///     }
    /// </code>
    /// </example>
    let effect = EffectBuilder()
