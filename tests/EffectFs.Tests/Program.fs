open System
open System.Threading
open System.Threading.Tasks
open EffectFs

type TestFailure(message: string) =
    inherit Exception(message)

module Assert =
    let equal<'value when 'value: equality> (expected: 'value) (actual: 'value) : unit =
        if actual <> expected then
            raise (TestFailure(sprintf "Expected %A but got %A." expected actual))

    let true' (value: bool) : unit =
        equal true value

module Tests =
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

    let effectExpressionBindsValues () : unit =
        let workflow : Effect<unit, string, int> =
            effect {
                let! value = Effect.succeed 40
                let! other = Effect.succeed 2
                return value + other
            }

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let askReturnsTheEnvironment () : unit =
        let workflow : Effect<int, string, int> =
            effect {
                let! value = Effect.ask<int, string>
                return value * 2
            }

        let result =
            workflow
            |> Effect.execute 21
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let readProjectsFromTheEnvironment () : unit =
        let workflow : Effect<string, string, int> =
            Effect.read String.length

        let result =
            workflow
            |> Effect.execute "effect"
            |> Async.RunSynchronously

        Assert.equal (Ok 6) result

    let ofResultLiftsValidationFailures () : unit =
        let validatePort (value: int) : Result<int, string> =
            if value > 0 then Ok value else Error "port must be positive"

        let result =
            validatePort 0
            |> Effect.ofResult
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Error "port must be positive") result

    let effectExpressionBindsResultAsyncAndTaskDirectly () : unit =
        let workflow : Effect<unit, string, int> =
            effect {
                let! a = Ok 20
                let! b = async { return 20 }
                let! c = Task.FromResult 2
                return a + b + c
            }

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let provideSuppliesEnvironmentExplicitly () : unit =
        let workflow : Effect<unit, string, int> =
            effect {
                let! text = Effect.environment<string, string>
                return text.Length
            }
            |> Effect.provide "effect"

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 6) result

    let environmentWithBindsEnvironmentOnceAsLambdaParam () : unit =
        let workflow : Effect<string, string, int> =
            Effect.environmentWith(fun env ->
                effect {
                    return env.Length
                })

        let result =
            workflow
            |> Effect.execute "effect"
            |> Async.RunSynchronously

        Assert.equal (Ok 6) result

    let withEnvironmentProjectsLargerDependencyContext () : unit =
        let workflow : Effect<int * string, string, int> =
            Effect.read String.length
            |> Effect.withEnvironment snd

        let result =
            workflow
            |> Effect.execute (42, "effect")
            |> Async.RunSynchronously

        Assert.equal (Ok 6) result

    let asyncResultCompatibilityRoundTrips () : unit =
        let workflow : Effect<unit, string, int> =
            async { return Ok 42 }
            |> Effect.fromAsyncResult

        let result =
            workflow
            |> Effect.toAsyncResult ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let taskResultCompatibilityRoundTrips () : unit =
        let workflow : Effect<unit, string, int> =
            Task.FromResult(Ok 42)
            |> Effect.fromTaskResultValue

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let asyncResultCompatModuleProvidesMigrationPath () : unit =
        let workflow : Effect<unit, string, int> =
            async { return Ok 21 }
            |> AsyncResultCompat.ofAsyncResult
            |> AsyncResultCompat.map ((*) 2)

        let result =
            workflow
            |> AsyncResultCompat.toAsyncResult ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let logWritesThroughEnvironmentDependency () : unit =
        let messages = ResizeArray<string>()

        let writer (sink: ResizeArray<string>) (entry: LogEntry) =
            sink.Add(sprintf "%A:%s" entry.Level entry.Message)

        let workflow : Effect<ResizeArray<string>, string, unit> =
            effect {
                do! Effect.log writer LogLevel.Information "hello"
                do! Effect.logWith writer LogLevel.Warning (fun sink -> sprintf "count=%d" sink.Count)
            }

        let result =
            workflow
            |> Effect.execute messages
            |> Async.RunSynchronously

        Assert.equal (Ok ()) result
        Assert.equal [ "Information:hello"; "Warning:count=1" ] (List.ofSeq messages)

    let taskInteropRemainsColdUntilExecution () : unit =
        let started = ref false

        let workflow : Effect<unit, string, int> =
            Effect.ofTask(fun (_: CancellationToken) ->
                started.Value <- true
                Task.FromResult 42)

        Assert.equal false started.Value

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal true started.Value
        Assert.equal (Ok 42) result

    let coldTaskAliasesRemainColdUntilExecution () : unit =
        let valueStarted = ref false
        let resultStarted = ref false
        let unitStarted = ref false

        let valueWorkflow : Effect<unit, string, int> =
            Effect.fromColdTask(fun _ ->
                valueStarted.Value <- true
                Task.FromResult 42)

        let resultWorkflow : Effect<unit, string, int> =
            Effect.fromColdTaskResult(fun _ ->
                resultStarted.Value <- true
                Task.FromResult(Ok 42))

        let unitWorkflow : Effect<unit, string, unit> =
            Effect.fromColdTaskUnit(fun _ ->
                unitStarted.Value <- true
                Task.CompletedTask)

        Assert.equal false valueStarted.Value
        Assert.equal false resultStarted.Value
        Assert.equal false unitStarted.Value

        let valueResult =
            valueWorkflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        let resultResult =
            resultWorkflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        let unitResult =
            unitWorkflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal true valueStarted.Value
        Assert.equal true resultStarted.Value
        Assert.equal true unitStarted.Value
        Assert.equal (Ok 42) valueResult
        Assert.equal (Ok 42) resultResult
        Assert.equal (Ok ()) unitResult

    let hotTaskValueStartsBeforeExecution () : unit =
        let started = ref false

        let hotTask =
            started.Value <- true
            Task.FromResult 42

        let workflow : Effect<unit, string, int> =
            Effect.fromTaskValue hotTask

        Assert.equal true started.Value

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let effectExpressionCanNormalizeAsyncAsyncResult () : unit =
        let nested : Async<Async<Result<int, string>>> =
            async {
                return async { return Ok 42 }
            }

        let workflow : Effect<unit, string, int> =
            effect {
                let! next = nested
                let! (value: int) = next
                return value
            }

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let effectExpressionCanNormalizeResultOfAsync () : unit =
        let nested : Result<Async<int>, string> =
            Ok(async { return 42 })

        let workflow : Effect<unit, string, int> =
            effect {
                let! next = nested
                let! value = next
                return value
            }

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let effectExpressionCanNormalizeNestedResults () : unit =
        let nested : Result<Result<int, string>, string> =
            Ok(Ok 42)

        let workflow : Effect<unit, string, int> =
            effect {
                let! next = nested
                let! value = next
                return value
            }

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result

    let executeWithCancellationPassesTokenToTaskFactory () : unit =
        let seen = ref CancellationToken.None
        use cts = new CancellationTokenSource()

        let workflow : Effect<unit, string, int> =
            Effect.fromTask(fun cancellationToken ->
                seen.Value <- cancellationToken
                Task.FromResult 42)

        let result =
            workflow
            |> Effect.executeWithCancellation () cts.Token
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result
        Assert.equal cts.Token seen.Value

    let ensureNotCanceledTurnsCanceledTokenIntoTypedError () : unit =
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let result =
            Effect.ensureNotCanceled "canceled"
            |> Effect.executeWithCancellation () cts.Token
            |> Async.RunSynchronously

        Assert.equal (Error "canceled") result

    let catchCancellationTurnsTaskCancellationIntoTypedError () : unit =
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let workflow : Effect<unit, string, int> =
            Effect.fromTask(fun cancellationToken ->
                task {
                    do! Task.Delay(50, cancellationToken)
                    return 42
                })
            |> Effect.catchCancellation (fun _ -> "canceled")

        let result =
            workflow
            |> Effect.executeWithCancellation () cts.Token
            |> Async.RunSynchronously

        Assert.equal (Error "canceled") result

    let timeoutTurnsSlowWorkIntoTypedError () : unit =
        let workflow : Effect<unit, string, int> =
            Effect.fromAsync(async {
                do! Async.Sleep 100
                return 42
            })
            |> Effect.timeout (TimeSpan.FromMilliseconds 10) "timed out"

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Error "timed out") result

    let timeoutDoesNotCancelUnderlyingWorkByItself () : unit =
        let completed = TaskCompletionSource<unit>()

        let workflow : Effect<unit, string, int> =
            Effect.fromTask(fun _ ->
                task {
                    do! Task.Delay 50
                    completed.SetResult ()
                    return 42
                })
            |> Effect.timeout (TimeSpan.FromMilliseconds 10) "timed out"

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Error "timed out") result
        Assert.true' (completed.Task.Wait 200)

    let retryRepeatsFailuresUntilSuccess () : unit =
        let attempts = ref 0

        let workflow : Effect<unit, string, int> =
            Effect.delay(fun () ->
                attempts.Value <- attempts.Value + 1

                if attempts.Value < 3 then
                    Effect.fail "retry"
                else
                    Effect.succeed 42)
            |> Effect.retry
                { MaxAttempts = 3
                  Delay = fun _ -> TimeSpan.Zero
                  ShouldRetry = ((=) "retry") }

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result
        Assert.equal 3 attempts.Value

    let bracketReleasesResourcesOnSuccess () : unit =
        let disposed = ref false

        let workflow : Effect<unit, string, int> =
            Effect.bracket
                (Effect.succeed "resource")
                (fun _ -> disposed.Value <- true)
                (fun resource -> Effect.succeed resource.Length)

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 8) result
        Assert.true' disposed.Value

    let bracketReleasesResourcesOnTypedFailure () : unit =
        let disposed = ref false

        let workflow : Effect<unit, string, int> =
            Effect.bracket
                (Effect.succeed "resource")
                (fun _ -> disposed.Value <- true)
                (fun _ -> Effect.fail "failed")

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Error "failed") result
        Assert.true' disposed.Value

    let bracketAsyncReleasesResourcesOnSuccess () : unit =
        let disposed = ref false

        let workflow : Effect<unit, string, int> =
            Effect.bracketAsync
                (Effect.succeed "resource")
                (fun _ _ ->
                    disposed.Value <- true
                    Task.CompletedTask)
                (fun resource -> Effect.succeed resource.Length)

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 8) result
        Assert.true' disposed.Value

    let bracketAsyncReleasesResourcesOnTypedFailure () : unit =
        let disposed = ref false

        let workflow : Effect<unit, string, int> =
            Effect.bracketAsync
                (Effect.succeed "resource")
                (fun _ _ ->
                    disposed.Value <- true
                    Task.CompletedTask)
                (fun _ -> Effect.fail "failed")

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Error "failed") result
        Assert.true' disposed.Value

    let bracketAsyncReleasesResourcesOnCancellation () : unit =
        let disposed = ref false
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let workflow : Effect<unit, string, int> =
            Effect.bracketAsync
                (Effect.succeed "resource")
                (fun _ _ ->
                    disposed.Value <- true
                    Task.CompletedTask)
                (fun _ ->
                    Effect.fromTask(fun cancellationToken ->
                        task {
                            do! Task.Delay(50, cancellationToken)
                            return 42
                        }))
            |> Effect.catchCancellation (fun _ -> "canceled")

        let result =
            workflow
            |> Effect.executeWithCancellation () cts.Token
            |> Async.RunSynchronously

        Assert.equal (Error "canceled") result
        Assert.true' disposed.Value

    let usingAsyncDisposesResourcesOnSuccess () : unit =
        let resource = AsyncDisposableFlag()

        let workflow : Effect<unit, string, int> =
            Effect.usingAsync resource (fun _ -> Effect.succeed 42)

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Ok 42) result
        Assert.true' resource.Disposed.Value

    let usingAsyncDisposesResourcesOnTypedFailure () : unit =
        let resource = AsyncDisposableFlag()

        let workflow : Effect<unit, string, int> =
            Effect.usingAsync resource (fun _ -> Effect.fail "failed")

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Error "failed") result
        Assert.true' resource.Disposed.Value

    let catchConvertsSynchronousExceptionsIntoTypedErrors () : unit =
        let workflow : Effect<unit, string, int> =
            Effect.delay(fun () ->
                invalidOp "boom"
                Effect.succeed 42)
            |> Effect.catch (fun error -> error.Message)

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Error "boom") result

    let catchConvertsAsynchronousExceptionsIntoTypedErrors () : unit =
        let workflow : Effect<unit, string, int> =
            Effect.fromTask(fun _ ->
                task {
                    return raise (InvalidOperationException "boom")
                })
            |> Effect.catch (fun error -> error.GetBaseException().Message)

        let result =
            workflow
            |> Effect.execute ()
            |> Async.RunSynchronously

        Assert.equal (Error "boom") result

[<EntryPoint>]
let main _ =
    let results =
        [ Tests.run "effect expression binds values" Tests.effectExpressionBindsValues
          Tests.run "ask returns the environment" Tests.askReturnsTheEnvironment
          Tests.run "read projects from the environment" Tests.readProjectsFromTheEnvironment
          Tests.run "ofResult lifts validation failures" Tests.ofResultLiftsValidationFailures
          Tests.run "effect expression binds Result Async and Task directly" Tests.effectExpressionBindsResultAsyncAndTaskDirectly
          Tests.run "provide supplies environment explicitly" Tests.provideSuppliesEnvironmentExplicitly
          Tests.run "environmentWith binds environment once as lambda param" Tests.environmentWithBindsEnvironmentOnceAsLambdaParam
          Tests.run "withEnvironment projects larger dependency context" Tests.withEnvironmentProjectsLargerDependencyContext
          Tests.run "Async Result compatibility round trips" Tests.asyncResultCompatibilityRoundTrips
          Tests.run "Task Result compatibility round trips" Tests.taskResultCompatibilityRoundTrips
          Tests.run "AsyncResultCompat provides migration path" Tests.asyncResultCompatModuleProvidesMigrationPath
          Tests.run "log writes through environment dependency" Tests.logWritesThroughEnvironmentDependency
          Tests.run "task interop remains cold until execution" Tests.taskInteropRemainsColdUntilExecution
          Tests.run "cold task aliases remain cold until execution" Tests.coldTaskAliasesRemainColdUntilExecution
          Tests.run "hot task value starts before execution" Tests.hotTaskValueStartsBeforeExecution
          Tests.run "effect expression can normalize Async Async Result" Tests.effectExpressionCanNormalizeAsyncAsyncResult
          Tests.run "effect expression can normalize Result of Async" Tests.effectExpressionCanNormalizeResultOfAsync
          Tests.run "effect expression can normalize nested Results" Tests.effectExpressionCanNormalizeNestedResults
          Tests.run "executeWithCancellation passes token to task factory" Tests.executeWithCancellationPassesTokenToTaskFactory
          Tests.run "ensureNotCanceled turns canceled token into typed error" Tests.ensureNotCanceledTurnsCanceledTokenIntoTypedError
          Tests.run "catchCancellation turns task cancellation into typed error" Tests.catchCancellationTurnsTaskCancellationIntoTypedError
          Tests.run "timeout turns slow work into typed error" Tests.timeoutTurnsSlowWorkIntoTypedError
          Tests.run "timeout does not cancel underlying work by itself" Tests.timeoutDoesNotCancelUnderlyingWorkByItself
          Tests.run "retry repeats failures until success" Tests.retryRepeatsFailuresUntilSuccess
          Tests.run "bracket releases resources on success" Tests.bracketReleasesResourcesOnSuccess
          Tests.run "bracket releases resources on typed failure" Tests.bracketReleasesResourcesOnTypedFailure
          Tests.run "bracketAsync releases resources on success" Tests.bracketAsyncReleasesResourcesOnSuccess
          Tests.run "bracketAsync releases resources on typed failure" Tests.bracketAsyncReleasesResourcesOnTypedFailure
          Tests.run "bracketAsync releases resources on cancellation" Tests.bracketAsyncReleasesResourcesOnCancellation
          Tests.run "usingAsync disposes resources on success" Tests.usingAsyncDisposesResourcesOnSuccess
          Tests.run "usingAsync disposes resources on typed failure" Tests.usingAsyncDisposesResourcesOnTypedFailure
          Tests.run "catch converts synchronous exceptions into typed errors" Tests.catchConvertsSynchronousExceptionsIntoTypedErrors
          Tests.run "catch converts asynchronous exceptions into typed errors" Tests.catchConvertsAsynchronousExceptionsIntoTypedErrors ]

    if List.forall id results then 0 else 1
