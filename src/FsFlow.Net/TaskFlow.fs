namespace FsFlow.Net

open System
open System.Threading
open System.Threading.Tasks
open FsFlow

/// <summary>
/// Represents a cold task-based workflow that depends on an environment,
/// observes a runtime cancellation token, can fail with a typed error,
/// and can succeed with a value.
/// </summary>
/// <typeparam name="env">The type of the environment dependency.</typeparam>
/// <typeparam name="error">The type of the failure value.</typeparam>
/// <typeparam name="value">The type of the success value.</typeparam>
type TaskFlow<'env, 'error, 'value> =
    private
    | TaskFlow of ('env -> CancellationToken -> Task<Result<'value, 'error>>)

/// <summary>
/// Represents delayed task work that can observe a runtime cancellation token
/// when it is started.
/// </summary>
/// <typeparam name="value">The type of the produced task value.</typeparam>
type ColdTask<'value> =
    | ColdTask of (CancellationToken -> Task<'value>)

/// <summary>
/// Core functions for creating and executing cold tasks.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module ColdTask =
    let run (cancellationToken: CancellationToken) (ColdTask operation: ColdTask<'value>) : Task<'value> =
        operation cancellationToken

/// <summary>
/// Core functions for creating, composing, and executing task flows.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TaskFlow =
    let run
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (TaskFlow operation: TaskFlow<'env, 'error, 'value>)
        : Task<Result<'value, 'error>> =
        operation environment cancellationToken

    let toTask
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (flow: TaskFlow<'env, 'error, 'value>)
        : Task<Result<'value, 'error>> =
        run environment cancellationToken flow

    let succeed (value: 'value) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun _ _ -> Task.FromResult(Ok value))

    let fail (error: 'error) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun _ _ -> Task.FromResult(Error error))

    let fromResult (result: Result<'value, 'error>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun _ _ -> Task.FromResult result)

    let fromFlow (flow: Flow<'env, 'error, 'value>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun environment _ -> Task.FromResult(Flow.run environment flow))

    let fromAsyncFlow (flow: AsyncFlow<'env, 'error, 'value>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun environment cancellationToken ->
            AsyncFlow.run environment flow
            |> fun operation -> Async.StartAsTask(operation, cancellationToken = cancellationToken))

    let fromTask (coldTask: ColdTask<'value>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun _ cancellationToken ->
            task {
                let! value = ColdTask.run cancellationToken coldTask
                return Ok value
            })

    let fromTaskResult
        (coldTask: ColdTask<Result<'value, 'error>>)
        : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun _ cancellationToken -> ColdTask.run cancellationToken coldTask)

    let env<'env, 'error> : TaskFlow<'env, 'error, 'env> =
        TaskFlow(fun environment _ -> Task.FromResult(Ok environment))

    let read (projection: 'env -> 'value) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun environment _ -> Task.FromResult(Ok(projection environment)))

    let map
        (mapper: 'value -> 'next)
        (flow: TaskFlow<'env, 'error, 'value>)
        : TaskFlow<'env, 'error, 'next> =
        TaskFlow(fun environment cancellationToken ->
            InternalCombinatorCore.mapWith
                (fun mapOutcome operation ->
                    task {
                        let! result = operation
                        return mapOutcome result
                    })
                mapper
                (fun (environment, cancellationToken) -> run environment cancellationToken flow)
                (environment, cancellationToken))

    let bind
        (binder: 'value -> TaskFlow<'env, 'error, 'next>)
        (flow: TaskFlow<'env, 'error, 'value>)
        : TaskFlow<'env, 'error, 'next> =
        TaskFlow(fun environment cancellationToken ->
            InternalCombinatorCore.bindWith
                (fun operation onSuccess onError ->
                    task {
                        let! result = operation

                        match result with
                        | Ok value -> return! onSuccess value
                        | Error error -> return! onError error
                    })
                (fun (environment, cancellationToken) value -> binder value |> run environment cancellationToken)
                (Error >> Task.FromResult)
                (fun (environment, cancellationToken) -> run environment cancellationToken flow)
                (environment, cancellationToken))

    let tap
        (binder: 'value -> TaskFlow<'env, 'error, unit>)
        (flow: TaskFlow<'env, 'error, 'value>)
        : TaskFlow<'env, 'error, 'value> =
        bind
            (fun value ->
                binder value
                |> map (fun () -> value))
            flow

    let mapError
        (mapper: 'error -> 'nextError)
        (flow: TaskFlow<'env, 'error, 'value>)
        : TaskFlow<'env, 'nextError, 'value> =
        TaskFlow(fun environment cancellationToken ->
            InternalCombinatorCore.mapErrorWith
                (fun mapOutcome operation ->
                    task {
                        let! result = operation
                        return mapOutcome result
                    })
                mapper
                (fun (environment, cancellationToken) -> run environment cancellationToken flow)
                (environment, cancellationToken))

    let catch
        (handler: exn -> 'error)
        (flow: TaskFlow<'env, 'error, 'value>)
        : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun environment cancellationToken ->
            task {
                try
                    return! run environment cancellationToken flow
                with error ->
                    return Error(handler error)
            })

    let localEnv
        (mapping: 'outerEnvironment -> 'innerEnvironment)
        (flow: TaskFlow<'innerEnvironment, 'error, 'value>)
        : TaskFlow<'outerEnvironment, 'error, 'value> =
        TaskFlow(fun environment cancellationToken ->
            InternalCombinatorCore.localEnvWith
                (fun (environment, cancellationToken) innerFlow -> run environment cancellationToken innerFlow)
                (fun (environment, cancellationToken) -> mapping environment, cancellationToken)
                flow
                (environment, cancellationToken))

    let delay (factory: unit -> TaskFlow<'env, 'error, 'value>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun environment cancellationToken ->
            InternalCombinatorCore.delayWith
                (fun (environment, cancellationToken) delayedFlow -> run environment cancellationToken delayedFlow)
                factory
                (environment, cancellationToken))

/// <summary>
/// Computation expression builder for task-based <see cref="T:FsFlow.Net.TaskFlow`3" /> workflows.
/// </summary>
type TaskFlowBuilder() =
    member _.Return(value: 'value) : TaskFlow<'env, 'error, 'value> =
        TaskFlow.succeed value

    member _.ReturnFrom(flow: TaskFlow<'env, 'error, 'value>) : TaskFlow<'env, 'error, 'value> =
        flow

    member _.ReturnFrom(flow: AsyncFlow<'env, 'error, 'value>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow.fromAsyncFlow flow

    member _.ReturnFrom(flow: Flow<'env, 'error, 'value>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow.fromFlow flow

    member _.ReturnFrom(result: Result<'value, 'error>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow.fromResult result

    member _.Zero() : TaskFlow<'env, 'error, unit> =
        TaskFlow.succeed ()

    member _.Bind
        (
            flow: TaskFlow<'env, 'error, 'value>,
            binder: 'value -> TaskFlow<'env, 'error, 'next>
        ) : TaskFlow<'env, 'error, 'next> =
        TaskFlow.bind binder flow

    member _.Bind
        (
            flow: AsyncFlow<'env, 'error, 'value>,
            binder: 'value -> TaskFlow<'env, 'error, 'next>
        ) : TaskFlow<'env, 'error, 'next> =
        flow
        |> TaskFlow.fromAsyncFlow
        |> TaskFlow.bind binder

    member _.Bind
        (
            flow: Flow<'env, 'error, 'value>,
            binder: 'value -> TaskFlow<'env, 'error, 'next>
        ) : TaskFlow<'env, 'error, 'next> =
        flow
        |> TaskFlow.fromFlow
        |> TaskFlow.bind binder

    member _.Bind
        (
            result: Result<'value, 'error>,
            binder: 'value -> TaskFlow<'env, 'error, 'next>
        ) : TaskFlow<'env, 'error, 'next> =
        result
        |> TaskFlow.fromResult
        |> TaskFlow.bind binder

    member _.Delay(factory: unit -> TaskFlow<'env, 'error, 'value>) : TaskFlow<'env, 'error, 'value> =
        TaskFlow.delay factory

    member _.Run(flow: TaskFlow<'env, 'error, 'value>) : TaskFlow<'env, 'error, 'value> =
        flow

    member _.Combine
        (
            first: TaskFlow<'env, 'error, unit>,
            second: TaskFlow<'env, 'error, 'value>
        ) : TaskFlow<'env, 'error, 'value> =
        first
        |> TaskFlow.bind (fun () -> second)

    member _.TryWith
        (
            flow: TaskFlow<'env, 'error, 'value>,
            handler: exn -> TaskFlow<'env, 'error, 'value>
        ) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun environment cancellationToken ->
            task {
                try
                    return! TaskFlow.run environment cancellationToken flow
                with error ->
                    return! TaskFlow.run environment cancellationToken (handler error)
            })

    member _.TryFinally
        (
            flow: TaskFlow<'env, 'error, 'value>,
            compensation: unit -> unit
        ) : TaskFlow<'env, 'error, 'value> =
        TaskFlow(fun environment cancellationToken ->
            task {
                try
                    return! TaskFlow.run environment cancellationToken flow
                finally
                    compensation ()
            })

    member this.Using
        (
            resource: 'resource,
            binder: 'resource -> TaskFlow<'env, 'error, 'value>
        ) : TaskFlow<'env, 'error, 'value>
        when 'resource :> IDisposable =
        this.TryFinally(
            binder resource,
            fun () ->
                if not (isNull (box resource)) then
                    resource.Dispose()
        )

    member this.While
        (
            guard: unit -> bool,
            body: TaskFlow<'env, 'error, unit>
        ) : TaskFlow<'env, 'error, unit> =
        if guard () then
            this.Bind(body, fun () -> this.While(guard, body))
        else
            this.Zero()

    member this.For
        (
            sequence: seq<'value>,
            binder: 'value -> TaskFlow<'env, 'error, unit>
        ) : TaskFlow<'env, 'error, unit> =
        this.Using(
            sequence.GetEnumerator(),
            fun enumerator -> this.While(enumerator.MoveNext, this.Delay(fun () -> binder enumerator.Current))
        )

[<AutoOpen>]
module Builders =
    /// <summary>
    /// The .NET <c>taskFlow { }</c> computation expression.
    /// </summary>
    let taskFlow = TaskFlowBuilder()
