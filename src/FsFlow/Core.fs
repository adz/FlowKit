namespace FsFlow

open System
open System.Threading
open System.Threading.Tasks

#if FABLE_COMPILER
open Fable.Core
#endif

/// <summary>
/// Represents the cause of a failed workflow.
/// </summary>
/// <typeparam name="error">The type of the domain-specific failure value.</typeparam>
[<RequireQualifiedAccess>]
type Cause<'error> =
    /// <summary>An expected domain-specific failure.</summary>
    | Fail of 'error
    /// <summary>An unexpected defect or panic (e.g., an exception).</summary>
    | Die of exn
    /// <summary>An administrative signal to stop the workflow (e.g., cancellation).</summary>
    | Interrupt

/// <summary>
/// Represents the final outcome of a workflow execution.
/// </summary>
/// <typeparam name="value">The type of the success value.</typeparam>
/// <typeparam name="error">The type of the domain-specific failure value.</typeparam>
[<RequireQualifiedAccess>]
type Exit<'value, 'error> =
    /// <summary>The workflow completed successfully.</summary>
    | Success of 'value
    /// <summary>The workflow failed due to a specific cause.</summary>
    | Failure of Cause<'error>

[<RequireQualifiedAccess>]
module Cause =
    let map (mapper: 'e -> 'f) (cause: Cause<'e>) : Cause<'f> =
        match cause with
        | Cause.Fail e -> Cause.Fail (mapper e)
        | Cause.Die ex -> Cause.Die ex
        | Cause.Interrupt -> Cause.Interrupt

[<RequireQualifiedAccess>]
module Exit =
    let map (mapper: 'v -> 'w) (exit: Exit<'v, 'e>) : Exit<'w, 'e> =
        match exit with
        | Exit.Success v -> Exit.Success (mapper v)
        | Exit.Failure c -> Exit.Failure c

    let bind (binder: 'v -> Exit<'w, 'e>) (exit: Exit<'v, 'e>) : Exit<'w, 'e> =
        match exit with
        | Exit.Success v -> binder v
        | Exit.Failure c -> Exit.Failure c

    let mapError (mapper: 'e -> 'f) (exit: Exit<'v, 'e>) : Exit<'v, 'f> =
        match exit with
        | Exit.Success v -> Exit.Success v
        | Exit.Failure c -> Exit.Failure (Cause.map mapper c)

    let fromResult (result: Result<'v, 'e>) : Exit<'v, 'e> =
        match result with
        | Ok v -> Exit.Success v
        | Error e -> Exit.Failure (Cause.Fail e)

    let toResult (exit: Exit<'v, 'e>) : Result<'v, 'e> =
        match exit with
        | Exit.Success v -> Ok v
        | Exit.Failure (Cause.Fail e) -> Error e
        | Exit.Failure (Cause.Die ex) -> raise ex
        | Exit.Failure Cause.Interrupt -> raise (OperationCanceledException("Workflow was interrupted"))

/// <summary>
/// Represents a handle to a running workflow.
/// </summary>
/// <typeparam name="error">The failure type of the running workflow.</typeparam>
/// <typeparam name="value">The success type of the running workflow.</typeparam>
type Fiber<'error, 'value> =
    {
        /// <summary>The task that completes when the workflow finishes execution.</summary>
#if FABLE_COMPILER
        ExitTask: Async<Exit<'value, 'error>>
#else
        ExitTask: Task<Exit<'value, 'error>>
#endif
        /// <summary>The source used to signal interruption to the running workflow.</summary>
        InterruptSource: CancellationTokenSource
    }

#if !FABLE_COMPILER
/// <summary>
/// Represents delayed task work that can observe a runtime cancellation token when it is started.
/// </summary>
/// <typeparam name="value">The type of the produced task value.</typeparam>
type internal ColdTask<'value> =
    | ColdTask of (CancellationToken -> Task<'value>)

/// <summary>
/// Core functions for creating and executing cold tasks.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module internal ColdTask =
    let create (operation: CancellationToken -> Task<'value>) : ColdTask<'value> =
        ColdTask operation

    let fromTaskFactory (factory: unit -> Task<'value>) : ColdTask<'value> =
        create (fun _ -> factory ())

    let fromTask (startedTask: Task<'value>) : ColdTask<'value> =
        fromTaskFactory (fun () -> startedTask)

    let fromValueTaskFactory
        (factory: CancellationToken -> ValueTask<'value>)
        : ColdTask<'value> =
        create (fun cancellationToken -> factory cancellationToken |> _.AsTask())

    let fromValueTaskFactoryWithoutCancellation
        (factory: unit -> ValueTask<'value>)
        : ColdTask<'value> =
        create (fun _ -> factory () |> _.AsTask())

    let fromValueTask (startedValueTask: ValueTask<'value>) : ColdTask<'value> =
        let startedTask = startedValueTask.AsTask()
        fromTask startedTask

    let run (cancellationToken: CancellationToken) (ColdTask operation: ColdTask<'value>) : Task<'value> =
        operation cancellationToken
#endif

/// <summary>
/// Represents the portable execution shape used by the unified <see cref="T:FsFlow.Flow`3" />.
/// </summary>
#if FABLE_COMPILER
type Effect<'value, 'error> = Async<Exit<'value, 'error>>
#else
type Effect<'value, 'error> = ValueTask<Exit<'value, 'error>>
#endif

/// <summary>
/// Represents a cold workflow that reads an environment, returns a typed result, and is executed
/// explicitly through <c>Flow.run</c>.
/// </summary>
/// <typeparam name="env">The type of the environment dependency.</typeparam>
/// <typeparam name="error">The type of the failure value.</typeparam>
/// <typeparam name="value">The type of the success value.</typeparam>
type Flow<'env, 'error, 'value> =
    internal
    | Flow of ('env -> CancellationToken -> Effect<'value, 'error>)

/// <summary>
/// Represents a cold async workflow that reads an environment, returns a typed result,
/// and is used internally to implement the unified <c>Flow</c> surface.
/// </summary>
/// <typeparam name="env">The type of the environment dependency.</typeparam>
/// <typeparam name="error">The type of the failure value.</typeparam>
/// <typeparam name="value">The type of the success value.</typeparam>
type internal AsyncFlow<'env, 'error, 'value> =
    private
    | AsyncFlow of ('env -> Async<Exit<'value, 'error>>)

/// <summary>
/// Represents a cold task-based workflow that reads an environment, observes a runtime cancellation token,
/// returns a typed result, and is executed explicitly through <c>TaskFlow.run</c>.
/// </summary>
/// <typeparam name="env">The type of the environment dependency.</typeparam>
/// <typeparam name="error">The type of the failure value.</typeparam>
/// <typeparam name="value">The type of the success value.</typeparam>
type internal TaskFlow<'env, 'error, 'value> =
    private
    | TaskFlow of ('env -> CancellationToken -> Task<Exit<'value, 'error>>)

/// <summary>
/// Log levels used by runtime logging helpers and environment-provided logging functions.
/// </summary>
[<RequireQualifiedAccess>]
type LogLevel =
    | Trace
    | Debug
    | Information
    | Warning
    | Error
    | Critical

/// <summary>
/// A structured log entry written through a runtime logger.
/// </summary>
type LogEntry =
    {
      Level: LogLevel
      Message: string
      TimestampUtc: DateTimeOffset
    }

/// <summary>
/// Defines how runtime retry helpers repeat typed failures in a controlled way.
/// </summary>
type RetryPolicy<'error> =
    {
      MaxAttempts: int
      Delay: int -> TimeSpan
      ShouldRetry: 'error -> bool
    }

/// <summary>
/// Standard retry policies for runtime helpers.
/// </summary>
[<RequireQualifiedAccess>]
module RetryPolicy =
    let noDelay (maxAttempts: int) : RetryPolicy<'error> =
        { MaxAttempts = maxAttempts
          Delay = fun _ -> TimeSpan.Zero
          ShouldRetry = fun _ -> true }

/// <summary>Describes the capability contract for a single dependency.</summary>
/// <remarks>
/// Named capability interfaces inherit this contract once and then expose the dependency through a
/// member such as <c>Clock</c> or <c>Logger</c>. Workflow builders can accept any environment
/// that implements <c>Requires&lt;'dep&gt;</c>, which lets larger runtimes satisfy smaller
/// boundaries.
/// </remarks>
/// <typeparam name="dep">The dependency type exposed by the environment.</typeparam>
/// <example>
/// <code>
/// type IClock =
///     abstract UtcNow : unit -&gt; DateTimeOffset
///
/// type ClockRequires =
///     inherit Requires&lt;IClock&gt;
///     abstract Clock : IClock
/// </code>
/// </example>
type Requires<'dep> =
    abstract Dep : 'dep

/// <summary>Request token for binding a whole dependency inside a workflow.</summary>
/// <remarks>
/// Use this token when a workflow needs the dependency itself rather than a value projected from
/// that dependency. The <c>flow {}</c> builder and its internal compatibility helpers
/// read it from any environment that implements <c>Requires&lt;'dep&gt;</c>.
/// </remarks>
/// <typeparam name="dep">The dependency type to read from the environment.</typeparam>
/// <example>
/// <code>
/// type IClock =
///     abstract UtcNow : unit -&gt; DateTimeOffset
///
/// type ClockRequires =
///     inherit Requires&lt;IClock&gt;
///     abstract Clock : IClock
///
/// let readClock : Flow&lt;#ClockRequires, unit, IClock&gt; =
///     flow {
///         let! clock = Resolve&lt;IClock&gt;
///         return clock
///     }
/// </code>
/// </example>
[<Struct>]
type Resolve<'dep> =
    | Resolve

/// <summary>Request token for projecting a value from a dependency.</summary>
/// <remarks>
/// Builders read the dependency from the environment, apply the projection, and then reuse the
/// existing lift/bind behavior for the projected value. If the projection returns a
/// <c>Result</c>, <c>Async</c>, <c>Task</c>, <c>ValueTask</c>, <c>ColdTask</c>, <c>option</c>, or
/// <c>voption</c>, the existing workflow rules still apply.
/// </remarks>
/// <typeparam name="dep">The dependency type to read from the environment.</typeparam>
/// <typeparam name="value">The projected value type.</typeparam>
/// <example>
/// <code>
/// type IClock =
///     abstract UtcNow : unit -&gt; DateTimeOffset
///
/// type ClockRequires =
///     inherit Requires&lt;IClock&gt;
///     abstract Clock : IClock
///
/// let readClockNow : Flow&lt;#ClockRequires, unit, DateTimeOffset&gt; =
///     flow {
///         let! now = Resolve&lt;IClock&gt; _.UtcNow
///         return now
///     }
/// </code>
/// </example>
[<Struct>]
type Resolve<'dep, 'value> =
    | Resolve of ('dep -> 'value)

/// <summary>Describes a missing service-provider capability.</summary>
type MissingCapability =
    {
        CapabilityType: Type
    }
