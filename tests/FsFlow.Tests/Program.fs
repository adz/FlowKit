namespace FsFlow.Tests

open System
open System.Threading
open System.Threading.Tasks
open FsFlow
open Xunit
open Swensen.Unquote

module Tests =
    type DisposableFlag() =
        let disposed = ref false
        member _.Disposed = disposed
        interface IDisposable with
            member _.Dispose() = disposed.Value <- true

    type AsyncDisposableFlag() =
        let disposed = ref false
        member _.Disposed = disposed
        interface IAsyncDisposable with
            member _.DisposeAsync() =
                disposed.Value <- true
                ValueTask()

    let execute<'env, 'error, 'value>
        (environment: 'env)
        (cancellationToken: CancellationToken)
        (workflow: Flow<'env, 'error, 'value>)
        : Result<'value, 'error> =
        workflow
        |> Flow.toAsync environment cancellationToken
        |> Async.RunSynchronously

    let executeUnit<'error, 'value> (workflow: Flow<unit, 'error, 'value>) : Result<'value, 'error> =
        execute () CancellationToken.None workflow

    [<Fact>]
    let ``flow expression binds values`` () =
        let workflow : Flow<unit, string, int> =
            flow {
                let! value = Flow.succeed 40
                let! other = Flow.succeed 2
                return value + other
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``env returns the environment`` () =
        let workflow : Flow<int, string, int> =
            flow {
                let! value = Flow.env<int, string>
                return value * 2
            }
        test <@ execute 21 CancellationToken.None workflow = Ok 42 @>

    [<Fact>]
    let ``read projects from the environment`` () =
        let workflow : Flow<string, string, int> =
            Flow.read String.length
        test <@ execute "effect" CancellationToken.None workflow = Ok 6 @>

    [<Fact>]
    let ``fromResult lifts validation failures`` () =
        let validatePort (value: int) : Result<int, string> =
            if value > 0 then Ok value else Error "port must be positive"

        let result =
            validatePort 0
            |> Flow.fromResult
            |> executeUnit
        test <@ result = Error "port must be positive" @>

    [<Fact>]
    let ``flow expression binds Result and Async directly`` () =
        let workflow : Flow<unit, string, int> =
            flow {
                let! a = Ok 20
                let! b = async { return 22 }
                return a + b
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``flow expression binds Task directly`` () =
        let workflow : Flow<unit, string, int> =
            flow {
                let! value = Task.FromResult 42
                return value
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``flow expression binds Task Result directly`` () =
        let workflow : Flow<unit, string, int> =
            flow {
                let! (value: int) = Task.FromResult(Ok 42)
                return value
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``flow expression binds ColdTask directly`` () =
        let readValue : ColdTask<int> =
            fun _ -> Task.FromResult 42

        let workflow : Flow<unit, string, int> =
            flow {
                let! value = readValue
                return value
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``ColdTaskResult alias works with explicit helper`` () =
        let readValue : ColdTaskResult<int, string> =
            fun _ -> Task.FromResult(Ok 42)

        let workflow : Flow<unit, string, int> =
            readValue
            |> Flow.Task.fromColdResult
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``flow expression return! ColdTask directly`` () =
        let readValue : ColdTask<int> =
            fun _ -> Task.FromResult 42

        let workflow : Flow<unit, string, int> =
            flow {
                return! readValue
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``ColdTaskResult alias works with explicit return! helper`` () =
        let readValue : ColdTaskResult<int, string> =
            fun _ -> Task.FromResult(Ok 42)

        let workflow : Flow<unit, string, int> =
            flow {
                return! Flow.Task.fromColdResult readValue
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``localEnv projects larger dependency context`` () =
        let workflow : Flow<int * string, string, int> =
            Flow.read String.length
            |> Flow.localEnv snd
        test <@ execute (42, "effect") CancellationToken.None workflow = Ok 6 @>

    [<Fact>]
    let ``Async Result round trips`` () =
        let workflow : Flow<unit, string, int> =
            async { return Ok 42 }
            |> Flow.fromAsyncResult

        let result =
            workflow
            |> Flow.toAsyncResult () CancellationToken.None
            |> Async.RunSynchronously
        test <@ result = Ok 42 @>

    [<Fact>]
    let ``Task Result round trips`` () =
        let workflow : Flow<unit, string, int> =
            Task.FromResult(Ok 42)
            |> Flow.Task.fromHotResult
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``log writes through environment dependency`` () =
        let messages = ResizeArray<string>()

        let writer (sink: ResizeArray<string>) (entry: LogEntry) =
            sink.Add(sprintf "%A:%s" entry.Level entry.Message)

        let workflow : Flow<ResizeArray<string>, string, unit> =
            flow {
                do! Flow.Runtime.log writer LogLevel.Information "hello"
                do! Flow.Runtime.logWith writer LogLevel.Warning (fun sink -> sprintf "count=%d" sink.Count)
            }

        let result = execute messages CancellationToken.None workflow
        test <@ result = Ok () @>
        test <@ List.ofSeq messages = [ "Information:hello"; "Warning:count=1" ] @>

    [<Fact>]
    let ``cold task remains cold until execution`` () =
        let started = ref false

        let workflow : Flow<unit, string, int> =
            Flow.Task.fromCold(fun (_: CancellationToken) ->
                started.Value <- true
                Task.FromResult 42)

        test <@ started.Value = false @>
        test <@ executeUnit workflow = Ok 42 @>
        test <@ started.Value = true @>

    [<Fact>]
    let ``hot task starts before execution`` () =
        let started = ref false

        let hotTask =
            started.Value <- true
            Task.FromResult 42

        let workflow : Flow<unit, string, int> =
            Flow.Task.fromHot hotTask

        test <@ started.Value = true @>
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``flow expression can normalize Async Async Result`` () =
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
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``flow expression can normalize Result of Async`` () =
        let nested : Result<Async<int>, string> =
            Ok(async { return 42 })

        let workflow : Flow<unit, string, int> =
            flow {
                let! next = nested
                let! value = next
                return value
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``flow expression can normalize nested Results`` () =
        let nested : Result<Result<int, string>, string> =
            Ok(Ok 42)

        let workflow : Flow<unit, string, int> =
            flow {
                let! next = nested
                let! value = next
                return value
            }
        test <@ executeUnit workflow = Ok 42 @>

    [<Fact>]
    let ``run passes token to cold task factory`` () =
        let seen = ref CancellationToken.None
        use cts = new CancellationTokenSource()

        let workflow : Flow<unit, string, int> =
            Flow.Task.fromCold(fun cancellationToken ->
                seen.Value <- cancellationToken
                Task.FromResult 42)

        let result = execute () cts.Token workflow
        test <@ result = Ok 42 @>
        test <@ seen.Value = cts.Token @>

    [<Fact>]
    let ``flow expression passes token to cold task factory directly`` () =
        let seen = ref CancellationToken.None
        use cts = new CancellationTokenSource()

        let readValue : ColdTask<int> =
            fun cancellationToken ->
                seen.Value <- cancellationToken
                Task.FromResult 42

        let workflow : Flow<unit, string, int> =
            flow {
                let! value = readValue
                return value
            }

        let result = execute () cts.Token workflow
        test <@ result = Ok 42 @>
        test <@ seen.Value = cts.Token @>

    [<Fact>]
    let ``cancellationToken reads current token`` () =
        use cts = new CancellationTokenSource()

        let workflow : Flow<unit, string, CancellationToken> =
            Flow.Runtime.cancellationToken

        let result = execute () cts.Token workflow
        test <@ result = Ok cts.Token @>

    [<Fact>]
    let ``ensureNotCanceled turns canceled token into typed error`` () =
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let result =
            Flow.Runtime.ensureNotCanceled "canceled"
            |> execute () cts.Token
        test <@ result = Error "canceled" @>

    [<Fact>]
    let ``catchCancellation turns task cancellation into typed error`` () =
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
        test <@ result = Error "canceled" @>

    [<Fact>]
    let ``timeout turns slow work into typed error`` () =
        let workflow : Flow<unit, string, int> =
            Flow.fromAsync(async {
                do! Async.Sleep 100
                return 42
            })
            |> Flow.Runtime.timeout (TimeSpan.FromMilliseconds 10.0) "timed out"
        test <@ executeUnit workflow = Error "timed out" @>

    [<Fact>]
    let ``timeout does not cancel underlying work by itself`` () =
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
        test <@ result = Error "timed out" @>
        test <@ completed.Task.Wait 200 = true @>

    [<Fact>]
    let ``sleep observes cancellation`` () =
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let workflow : Flow<unit, string, unit> =
            Flow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
            |> Flow.Runtime.catchCancellation (fun _ -> "canceled")

        let result = execute () cts.Token workflow
        test <@ result = Error "canceled" @>

    [<Fact>]
    let ``retry repeats failures until success`` () =
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
        test <@ result = Ok 42 @>
        test <@ attempts.Value = 3 @>

    [<Fact>]
    let ``useWithAcquireRelease releases resources on success`` () =
        let disposed = ref false

        let workflow : Flow<unit, string, int> =
            Flow.Runtime.useWithAcquireRelease
                (Flow.succeed "resource")
                (fun _ _ ->
                    disposed.Value <- true
                    Task.CompletedTask)
                (fun resource -> Flow.succeed resource.Length)

        let result = executeUnit workflow
        test <@ result = Ok 8 @>
        test <@ disposed.Value = true @>

    [<Fact>]
    let ``useWithAcquireRelease releases resources on typed failure`` () =
        let disposed = ref false

        let workflow : Flow<unit, string, int> =
            Flow.Runtime.useWithAcquireRelease
                (Flow.succeed "resource")
                (fun _ _ ->
                    disposed.Value <- true
                    Task.CompletedTask)
                (fun _ -> Flow.fail "failed")

        let result = executeUnit workflow
        test <@ result = Error "failed" @>
        test <@ disposed.Value = true @>

    [<Fact>]
    let ``useWithAcquireRelease releases resources on cancellation`` () =
        let disposed = ref false
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let workflow : Flow<unit, string, int> =
            Flow.Runtime.useWithAcquireRelease
                (Flow.succeed "resource")
                (fun _ _ ->
                    disposed.Value <- true
                    Task.CompletedTask)
                (fun _ ->
                    Flow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
                    |> Flow.map (fun () -> 42))
            |> Flow.Runtime.catchCancellation (fun _ -> "canceled")

        let result = execute () cts.Token workflow
        test <@ result = Error "canceled" @>
        test <@ disposed.Value = true @>

    [<Fact>]
    let ``flow use disposes IDisposable resources`` () =
        let resource = new DisposableFlag()

        let workflow : Flow<unit, string, int> =
            flow {
                use _ = resource
                return 42
            }

        let result = executeUnit workflow
        test <@ result = Ok 42 @>
        test <@ resource.Disposed.Value = true @>

    [<Fact>]
    let ``flow use disposes IAsyncDisposable resources`` () =
        let resource = new AsyncDisposableFlag()

        let workflow : Flow<unit, string, int> =
            flow {
                use _ = resource
                return 42
            }

        let result = executeUnit workflow
        test <@ result = Ok 42 @>
        test <@ resource.Disposed.Value = true @>

    [<Fact>]
    let ``flow use! disposes flow acquired resources`` () =
        let resource = new AsyncDisposableFlag()

        let acquire : Flow<unit, string, AsyncDisposableFlag> =
            Flow.succeed resource

        let workflow : Flow<unit, string, int> =
            flow {
                use! _ = acquire
                return 42
            }

        let result = executeUnit workflow
        test <@ result = Ok 42 @>
        test <@ resource.Disposed.Value = true @>

    [<Fact>]
    let ``catch converts synchronous exceptions into typed errors`` () =
        let workflow : Flow<unit, string, int> =
            Flow.delay(fun () ->
                invalidOp "boom"
                Flow.succeed 42)
            |> Flow.catch (fun error -> error.Message)

        test <@ executeUnit workflow = Error "boom" @>

    [<Fact>]
    let ``catch converts asynchronous exceptions into typed errors`` () =
        let workflow : Flow<unit, string, int> =
            Flow.Task.fromCold(fun _ ->
                task {
                    return raise (InvalidOperationException "boom")
                })
            |> Flow.catch (fun error -> error.GetBaseException().Message)

        test <@ executeUnit workflow = Error "boom" @>

module Program =
    [<EntryPoint>]
    let main _ = 0
