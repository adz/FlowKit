open System
open System.Threading
open System.Threading.Tasks
open EffectfulFlow

type TestFailure(message: string) =
    inherit Exception(message)

module Assert =
    let equal<'value when 'value: equality> (expected: 'value) (actual: 'value) : unit =
        if actual <> expected then
            raise (TestFailure(sprintf "Expected %A but got %A." expected actual))

    let true' (value: bool) : unit =
        equal true value

module Tests =
    type DisposableFlag() =
        let disposed = ref false

        member _.Disposed = disposed

        interface IDisposable with
            member _.Dispose() =
                disposed.Value <- true

    type AsyncDisposableFlag() =
        let disposed = ref false

        member _.Disposed = disposed

        interface IAsyncDisposable with
            member _.DisposeAsync() =
                disposed.Value <- true
                ValueTask()

    let run (name: string) (test: unit -> unit) : bool =
        try
            test ()
            printfn "[pass] %s" name
            true
        with error ->
            eprintfn "[fail] %s: %s" name error.Message
            false

    let execute<'env, 'error, 'value>
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (workflow: Flow<'env, 'error, 'value>)
        : Result<'value, 'error> =
        workflow
        |> Flow.run environment cancellationToken
        |> Async.RunSynchronously

    let executeUnit<'error, 'value> (workflow: Flow<unit, 'error, 'value>) : Result<'value, 'error> =
        execute () CancellationToken.None workflow

    let flowExpressionBindsValues () : unit =
        let workflow : Flow<unit, string, int> =
            flow {
                let! value = Flow.succeed 40
                let! other = Flow.succeed 2
                return value + other
            }

        Assert.equal (Ok 42) (executeUnit workflow)

    let envReturnsTheEnvironment () : unit =
        let workflow : Flow<int, string, int> =
            flow {
                let! value = Flow.env<int, string>
                return value * 2
            }

        Assert.equal (Ok 42) (execute 21 CancellationToken.None workflow)

    let readProjectsFromTheEnvironment () : unit =
        let workflow : Flow<string, string, int> =
            Flow.read String.length

        Assert.equal (Ok 6) (execute "effect" CancellationToken.None workflow)

    let fromResultLiftsValidationFailures () : unit =
        let validatePort (value: int) : Result<int, string> =
            if value > 0 then Ok value else Error "port must be positive"

        let result =
            validatePort 0
            |> Flow.fromResult
            |> executeUnit

        Assert.equal (Error "port must be positive") result

    let flowExpressionBindsResultAndAsyncDirectly () : unit =
        let workflow : Flow<unit, string, int> =
            flow {
                let! a = Ok 20
                let! b = async { return 22 }
                return a + b
            }

        Assert.equal (Ok 42) (executeUnit workflow)

    let flowExpressionBindsTaskDirectly () : unit =
        let workflow : Flow<unit, string, int> =
            flow {
                let! value = Task.FromResult 42
                return value
            }

        Assert.equal (Ok 42) (executeUnit workflow)

    let flowExpressionBindsTaskResultDirectly () : unit =
        let workflow : Flow<unit, string, int> =
            flow {
                let! (value: int) = Task.FromResult(Ok 42)
                return value
            }

        Assert.equal (Ok 42) (executeUnit workflow)

    let mapEnvProjectsLargerDependencyContext () : unit =
        let workflow : Flow<int * string, string, int> =
            Flow.read String.length
            |> Flow.mapEnv snd

        Assert.equal (Ok 6) (execute (42, "effect") CancellationToken.None workflow)

    let asyncResultRoundTrips () : unit =
        let workflow : Flow<unit, string, int> =
            async { return Ok 42 }
            |> Flow.fromAsyncResult

        let result =
            workflow
            |> Flow.toAsyncResult () CancellationToken.None
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let taskResultRoundTrips () : unit =
        let workflow : Flow<unit, string, int> =
            Task.FromResult(Ok 42)
            |> Flow.Task.fromHotResult

        Assert.equal (Ok 42) (executeUnit workflow)

    let logWritesThroughEnvironmentDependency () : unit =
        let messages = ResizeArray<string>()

        let writer (sink: ResizeArray<string>) (entry: LogEntry) =
            sink.Add(sprintf "%A:%s" entry.Level entry.Message)

        let workflow : Flow<ResizeArray<string>, string, unit> =
            flow {
                do! Flow.Runtime.log writer LogLevel.Information "hello"
                do! Flow.Runtime.logWith writer LogLevel.Warning (fun sink -> sprintf "count=%d" sink.Count)
            }

        let result = execute messages CancellationToken.None workflow

        Assert.equal (Ok ()) result
        Assert.equal [ "Information:hello"; "Warning:count=1" ] (List.ofSeq messages)

    let coldTaskRemainsColdUntilExecution () : unit =
        let started = ref false

        let workflow : Flow<unit, string, int> =
            Flow.Task.fromCold(fun (_: CancellationToken) ->
                started.Value <- true
                Task.FromResult 42)

        Assert.equal false started.Value
        Assert.equal (Ok 42) (executeUnit workflow)
        Assert.equal true started.Value

    let hotTaskStartsBeforeExecution () : unit =
        let started = ref false

        let hotTask =
            started.Value <- true
            Task.FromResult 42

        let workflow : Flow<unit, string, int> =
            Flow.Task.fromHot hotTask

        Assert.equal true started.Value
        Assert.equal (Ok 42) (executeUnit workflow)

    let flowExpressionCanNormalizeAsyncAsyncResult () : unit =
        let nested : Async<Async<Result<int, string>>> =
            async {
                return async { return Ok 42 }
            }

        let workflow : Flow<unit, string, int> =
            flow {
                let! next = nested
                let! (value: int) = next
                return value
            }

        Assert.equal (Ok 42) (executeUnit workflow)

    let flowExpressionCanNormalizeResultOfAsync () : unit =
        let nested : Result<Async<int>, string> =
            Ok(async { return 42 })

        let workflow : Flow<unit, string, int> =
            flow {
                let! next = nested
                let! value = next
                return value
            }

        Assert.equal (Ok 42) (executeUnit workflow)

    let flowExpressionCanNormalizeNestedResults () : unit =
        let nested : Result<Result<int, string>, string> =
            Ok(Ok 42)

        let workflow : Flow<unit, string, int> =
            flow {
                let! next = nested
                let! value = next
                return value
            }

        Assert.equal (Ok 42) (executeUnit workflow)

    let runPassesTokenToColdTaskFactory () : unit =
        let seen = ref CancellationToken.None
        use cts = new CancellationTokenSource()

        let workflow : Flow<unit, string, int> =
            Flow.Task.fromCold(fun cancellationToken ->
                seen.Value <- cancellationToken
                Task.FromResult 42)

        let result = execute () cts.Token workflow

        Assert.equal (Ok 42) result
        Assert.equal cts.Token seen.Value

    let cancellationTokenReadsCurrentToken () : unit =
        use cts = new CancellationTokenSource()

        let workflow : Flow<unit, string, CancellationToken> =
            Flow.Runtime.cancellationToken

        let result = execute () cts.Token workflow

        Assert.equal (Ok cts.Token) result

    let ensureNotCanceledTurnsCanceledTokenIntoTypedError () : unit =
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let result =
            Flow.Runtime.ensureNotCanceled "canceled"
            |> execute () cts.Token

        Assert.equal (Error "canceled") result

    let catchCancellationTurnsTaskCancellationIntoTypedError () : unit =
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let workflow : Flow<unit, string, int> =
            Flow.Task.fromCold(fun cancellationToken ->
                task {
                    do! Task.Delay(50, cancellationToken)
                    return 42
                })
            |> Flow.Runtime.catchCancellation (fun _ -> "canceled")

        let result = execute () cts.Token workflow

        Assert.equal (Error "canceled") result

    let timeoutTurnsSlowWorkIntoTypedError () : unit =
        let workflow : Flow<unit, string, int> =
            Flow.fromAsync(async {
                do! Async.Sleep 100
                return 42
            })
            |> Flow.Runtime.timeout (TimeSpan.FromMilliseconds 10.0) "timed out"

        Assert.equal (Error "timed out") (executeUnit workflow)

    let timeoutDoesNotCancelUnderlyingWorkByItself () : unit =
        let completed = TaskCompletionSource<unit>()

        let workflow : Flow<unit, string, int> =
            Flow.Task.fromCold(fun _ ->
                task {
                    do! Task.Delay 50
                    completed.SetResult ()
                    return 42
                })
            |> Flow.Runtime.timeout (TimeSpan.FromMilliseconds 10.0) "timed out"

        let result = executeUnit workflow

        Assert.equal (Error "timed out") result
        Assert.true' (completed.Task.Wait 200)

    let sleepObservesCancellation () : unit =
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let workflow : Flow<unit, string, unit> =
            Flow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
            |> Flow.Runtime.catchCancellation (fun _ -> "canceled")

        let result = execute () cts.Token workflow

        Assert.equal (Error "canceled") result

    let retryRepeatsFailuresUntilSuccess () : unit =
        let attempts = ref 0

        let workflow : Flow<unit, string, int> =
            Flow.delay(fun () ->
                attempts.Value <- attempts.Value + 1

                if attempts.Value < 3 then
                    Flow.fail "retry"
                else
                    Flow.succeed 42)
            |> Flow.Runtime.retry
                { MaxAttempts = 3
                  Delay = fun _ -> TimeSpan.Zero
                  ShouldRetry = ((=) "retry") }

        let result = executeUnit workflow

        Assert.equal (Ok 42) result
        Assert.equal 3 attempts.Value

    let useWithAcquireReleaseReleasesResourcesOnSuccess () : unit =
        let disposed = ref false

        let workflow : Flow<unit, string, int> =
            Flow.Runtime.useWithAcquireRelease
                (Flow.succeed "resource")
                (fun _ _ ->
                    disposed.Value <- true
                    Task.CompletedTask)
                (fun resource -> Flow.succeed resource.Length)

        let result = executeUnit workflow

        Assert.equal (Ok 8) result
        Assert.true' disposed.Value

    let useWithAcquireReleaseReleasesResourcesOnTypedFailure () : unit =
        let disposed = ref false

        let workflow : Flow<unit, string, int> =
            Flow.Runtime.useWithAcquireRelease
                (Flow.succeed "resource")
                (fun _ _ ->
                    disposed.Value <- true
                    Task.CompletedTask)
                (fun _ -> Flow.fail "failed")

        let result = executeUnit workflow

        Assert.equal (Error "failed") result
        Assert.true' disposed.Value

    let flowUseDisposesIDisposableResources () : unit =
        let resource = new DisposableFlag()

        let workflow : Flow<unit, string, int> =
            flow {
                use _ = resource
                return 42
            }

        let result = executeUnit workflow

        Assert.equal (Ok 42) result
        Assert.true' resource.Disposed.Value

    let flowUseDisposesIAsyncDisposableResources () : unit =
        let resource = new AsyncDisposableFlag()

        let workflow : Flow<unit, string, int> =
            flow {
                use _ = resource
                return 42
            }

        let result = executeUnit workflow

        Assert.equal (Ok 42) result
        Assert.true' resource.Disposed.Value

    let flowUseBangDisposesFlowAcquiredResources () : unit =
        let resource = new AsyncDisposableFlag()

        let acquire : Flow<unit, string, AsyncDisposableFlag> =
            Flow.succeed resource

        let workflow : Flow<unit, string, int> =
            flow {
                use! _ = acquire
                return 42
            }

        let result = executeUnit workflow

        Assert.equal (Ok 42) result
        Assert.true' resource.Disposed.Value

    let catchConvertsSynchronousExceptionsIntoTypedErrors () : unit =
        let workflow : Flow<unit, string, int> =
            Flow.delay(fun () ->
                invalidOp "boom"
                Flow.succeed 42)
            |> Flow.catch (fun error -> error.Message)

        Assert.equal (Error "boom") (executeUnit workflow)

    let catchConvertsAsynchronousExceptionsIntoTypedErrors () : unit =
        let workflow : Flow<unit, string, int> =
            Flow.Task.fromCold(fun _ ->
                task {
                    return raise (InvalidOperationException "boom")
                })
            |> Flow.catch (fun error -> error.GetBaseException().Message)

        Assert.equal (Error "boom") (executeUnit workflow)

[<EntryPoint>]
let main _ =
    let results =
        [ Tests.run "flow expression binds values" Tests.flowExpressionBindsValues
          Tests.run "env returns the environment" Tests.envReturnsTheEnvironment
          Tests.run "read projects from the environment" Tests.readProjectsFromTheEnvironment
          Tests.run "fromResult lifts validation failures" Tests.fromResultLiftsValidationFailures
          Tests.run "flow expression binds Result and Async directly" Tests.flowExpressionBindsResultAndAsyncDirectly
          Tests.run "flow expression binds Task directly" Tests.flowExpressionBindsTaskDirectly
          Tests.run "flow expression binds Task Result directly" Tests.flowExpressionBindsTaskResultDirectly
          Tests.run "mapEnv projects larger dependency context" Tests.mapEnvProjectsLargerDependencyContext
          Tests.run "Async Result round trips" Tests.asyncResultRoundTrips
          Tests.run "Task Result round trips" Tests.taskResultRoundTrips
          Tests.run "log writes through environment dependency" Tests.logWritesThroughEnvironmentDependency
          Tests.run "cold task remains cold until execution" Tests.coldTaskRemainsColdUntilExecution
          Tests.run "hot task starts before execution" Tests.hotTaskStartsBeforeExecution
          Tests.run "flow expression can normalize Async Async Result" Tests.flowExpressionCanNormalizeAsyncAsyncResult
          Tests.run "flow expression can normalize Result of Async" Tests.flowExpressionCanNormalizeResultOfAsync
          Tests.run "flow expression can normalize nested Results" Tests.flowExpressionCanNormalizeNestedResults
          Tests.run "run passes token to cold task factory" Tests.runPassesTokenToColdTaskFactory
          Tests.run "cancellationToken reads current token" Tests.cancellationTokenReadsCurrentToken
          Tests.run "ensureNotCanceled turns canceled token into typed error" Tests.ensureNotCanceledTurnsCanceledTokenIntoTypedError
          Tests.run "catchCancellation turns task cancellation into typed error" Tests.catchCancellationTurnsTaskCancellationIntoTypedError
          Tests.run "timeout turns slow work into typed error" Tests.timeoutTurnsSlowWorkIntoTypedError
          Tests.run "timeout does not cancel underlying work by itself" Tests.timeoutDoesNotCancelUnderlyingWorkByItself
          Tests.run "sleep observes cancellation" Tests.sleepObservesCancellation
          Tests.run "retry repeats failures until success" Tests.retryRepeatsFailuresUntilSuccess
          Tests.run "useWithAcquireRelease releases resources on success" Tests.useWithAcquireReleaseReleasesResourcesOnSuccess
          Tests.run "useWithAcquireRelease releases resources on typed failure" Tests.useWithAcquireReleaseReleasesResourcesOnTypedFailure
          Tests.run "flow use disposes IDisposable resources" Tests.flowUseDisposesIDisposableResources
          Tests.run "flow use disposes IAsyncDisposable resources" Tests.flowUseDisposesIAsyncDisposableResources
          Tests.run "flow use! disposes flow acquired resources" Tests.flowUseBangDisposesFlowAcquiredResources
          Tests.run "catch converts synchronous exceptions into typed errors" Tests.catchConvertsSynchronousExceptionsIntoTypedErrors
          Tests.run "catch converts asynchronous exceptions into typed errors" Tests.catchConvertsAsynchronousExceptionsIntoTypedErrors ]

    if List.forall id results then 0 else 1
