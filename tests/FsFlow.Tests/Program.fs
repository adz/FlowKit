namespace FsFlow.Tests

open System
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Sources
open FsFlow
open FsFlow.Net
open Swensen.Unquote
open Xunit

module Tests =
    let private publicInstanceMethodNames (targetType: Type) =
        targetType.GetMethods()
        |> Array.filter (fun methodInfo -> methodInfo.IsPublic && not methodInfo.IsSpecialName)
        |> Array.map _.Name
        |> Array.distinct
        |> Array.sort

    let private flowBuilderBindAndReturnFromArgumentNames () =
        typeof<FlowBuilder>.GetMethods()
        |> Array.filter (fun methodInfo ->
            methodInfo.IsPublic
            && not methodInfo.IsSpecialName
            && (methodInfo.Name = "Bind" || methodInfo.Name = "ReturnFrom"))
        |> Array.collect (fun methodInfo -> methodInfo.GetParameters())
        |> Array.map (fun parameterInfo -> parameterInfo.ParameterType.Name)
        |> Array.distinct
        |> Array.sort

    let private asyncFlowBuilderBindAndReturnFromArgumentNames () =
        typeof<AsyncFlowBuilder>.GetMethods()
        |> Array.filter (fun methodInfo ->
            methodInfo.IsPublic
            && not methodInfo.IsSpecialName
            && (methodInfo.Name = "Bind" || methodInfo.Name = "ReturnFrom"))
        |> Array.collect (fun methodInfo -> methodInfo.GetParameters())
        |> Array.map (fun parameterInfo -> parameterInfo.ParameterType.Name)
        |> Array.distinct
        |> Array.sort

    let private taskFlowBuilderBindAndReturnFromArgumentNames () =
        typeof<TaskFlowBuilder>.GetMethods()
        |> Array.filter (fun methodInfo ->
            methodInfo.IsPublic
            && not methodInfo.IsSpecialName
            && (methodInfo.Name = "Bind" || methodInfo.Name = "ReturnFrom"))
        |> Array.collect (fun methodInfo -> methodInfo.GetParameters())
        |> Array.map (fun parameterInfo -> parameterInfo.ParameterType.Name)
        |> Array.distinct
        |> Array.sort

    let private hasAsyncResultReturnFromOverload (builderType: Type) =
        builderType.GetMethods()
        |> Array.exists (fun methodInfo ->
            if not methodInfo.IsPublic || methodInfo.IsSpecialName || methodInfo.Name <> "ReturnFrom" then
                false
            else
                let parameters = methodInfo.GetParameters()

                if parameters.Length <> 1 || not parameters[0].ParameterType.IsGenericType then
                    false
                else
                    let asyncType = parameters[0].ParameterType

                    if asyncType.GetGenericTypeDefinition() <> typedefof<Async<_>> then
                        false
                    else
                        let asyncValueType = asyncType.GetGenericArguments()[0]

                        asyncValueType.IsGenericType
                        && asyncValueType.GetGenericTypeDefinition() = typedefof<Result<_, _>>)

    let private runFsiScript (scriptContents: string) =
        let scriptPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fsx")
        File.WriteAllText(scriptPath, scriptContents)

        try
            use childProcess =
                new Process(
                    StartInfo =
                        ProcessStartInfo(
                            FileName = "dotnet",
                            Arguments = $"fsi \"{scriptPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false
                        )
                )

            childProcess.Start() |> ignore

            let standardOutput = childProcess.StandardOutput.ReadToEnd()
            let standardError = childProcess.StandardError.ReadToEnd()
            childProcess.WaitForExit()

            childProcess.ExitCode, standardOutput + standardError
        finally
            File.Delete scriptPath

    type private SingleConsumptionValueTaskSource<'value>(value: 'value) as this =
        let consumptionCount = ref 0

        member _.AsValueTask() =
            ValueTask<'value>(this :> IValueTaskSource<'value>, 0s)

        member _.ConsumptionCount = consumptionCount.Value

        interface IValueTaskSource<'value> with
            member _.GetStatus(_token: int16) = ValueTaskSourceStatus.Succeeded

            member _.OnCompleted
                (
                    _continuation: Action<obj>,
                    _state: obj,
                    _token: int16,
                    _flags: ValueTaskSourceOnCompletedFlags
                ) =
                ()

            member _.GetResult(_token: int16) =
                let consumptions = Interlocked.Increment consumptionCount

                if consumptions > 1 then
                    invalidOp "ValueTask source consumed more than once."

                value

    [<Fact>]
    let ``Flow is sync result only`` () =
        let workflow : Flow<int, string, int> =
            Flow.env
            |> Flow.bind (fun value -> Flow.succeed(value * 2))

        test <@ Flow.run 21 workflow = Ok 42 @>

    [<Fact>]
    let ``Flow delay reruns from scratch`` () =
        let runs = ref 0

        let workflow : Flow<unit, string, int> =
            Flow.delay(fun () ->
                runs.Value <- runs.Value + 1
                Flow.succeed runs.Value)

        test <@ Flow.run () workflow = Ok 1 @>
        test <@ Flow.run () workflow = Ok 2 @>

    [<Fact>]
    let ``AsyncFlow runs as Async result`` () =
        let workflow : AsyncFlow<int, string, int> =
            AsyncFlow.read id
            |> AsyncFlow.bind (fun value ->
                AsyncFlow.fromAsync(async { return value * 2 }))

        let result =
            workflow
            |> AsyncFlow.run 21
            |> Async.RunSynchronously

        test <@ result = Ok 42 @>

    [<Fact>]
    let ``AsyncFlow can lift Flow`` () =
        let syncWorkflow : Flow<string, string, int> =
            Flow.read String.length

        let asyncWorkflow : AsyncFlow<string, string, int> =
            syncWorkflow
            |> AsyncFlow.fromFlow
            |> AsyncFlow.map ((+) 1)

        let result =
            asyncWorkflow
            |> AsyncFlow.toAsync "effect"
            |> Async.RunSynchronously

        test <@ result = Ok 7 @>

    [<Fact>]
    let ``shared combinators preserve sync and async environment semantics`` () =
        let syncBase : Flow<int, int, int> =
            Flow.read (fun env -> env + 1)
            |> Flow.map ((*) 2)
            |> Flow.bind (fun value -> Flow.succeed(value + 3))
            |> Flow.mapError String.length

        let syncWorkflow : Flow<string, int, int> =
            Flow.localEnv String.length syncBase

        let asyncBase : AsyncFlow<int, int, int> =
            AsyncFlow.read (fun env -> env + 1)
            |> AsyncFlow.map ((*) 2)
            |> AsyncFlow.bind (fun value -> AsyncFlow.succeed(value + 3))
            |> AsyncFlow.mapError String.length

        let asyncWorkflow : AsyncFlow<string, int, int> =
            AsyncFlow.localEnv String.length asyncBase

        let syncResult = Flow.run "flowkit" syncWorkflow

        let asyncResult =
            asyncWorkflow
            |> AsyncFlow.run "flowkit"
            |> Async.RunSynchronously

        test <@ syncResult = Ok 19 @>
        test <@ asyncResult = Ok 19 @>

    [<Fact>]
    let ``Flow composition helpers cover error tapping fallback and pairing`` () =
        let tappedErrors = ResizeArray<string>()

        let tapPreservesOriginalError =
            Flow.fail "primary"
            |> Flow.tapError (fun error ->
                tappedErrors.Add error
                Flow.succeed ())
            |> Flow.run ()

        let tapSkipsSuccess =
            Flow.succeed 42
            |> Flow.tapError (fun error ->
                tappedErrors.Add $"unexpected:{error}"
                Flow.succeed ())
            |> Flow.run ()

        let recovered =
            Flow.fail "missing"
            |> Flow.orElse (Flow.read (fun env -> env + 1))
            |> Flow.run 41

        let bypassesFallback =
            Flow.succeed 10
            |> Flow.orElse (Flow.succeed 99)
            |> Flow.run ()

        let zipped =
            Flow.zip (Flow.read (fun env -> env + 1)) (Flow.read (fun env -> env * 2))
            |> Flow.run 5

        let mapped =
            Flow.map2 (+) (Flow.read (fun env -> env + 1)) (Flow.read (fun env -> env * 2))
            |> Flow.run 5

        test <@ tapPreservesOriginalError = Error "primary" @>
        test <@ tapSkipsSuccess = Ok 42 @>
        test <@ List.ofSeq tappedErrors = [ "primary" ] @>
        test <@ recovered = Ok 42 @>
        test <@ bypassesFallback = Ok 10 @>
        test <@ zipped = Ok(6, 10) @>
        test <@ mapped = Ok 16 @>

    [<Fact>]
    let ``AsyncFlow composition helpers cover error tapping fallback and pairing`` () =
        let tappedErrors = ResizeArray<string>()

        let tapPreservesOriginalError =
            AsyncFlow.fail "primary"
            |> AsyncFlow.tapError (fun error ->
                tappedErrors.Add error
                AsyncFlow.succeed ())
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let tapSkipsSuccess =
            AsyncFlow.succeed 42
            |> AsyncFlow.tapError (fun error ->
                tappedErrors.Add $"unexpected:{error}"
                AsyncFlow.succeed ())
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let recovered =
            AsyncFlow.fail "missing"
            |> AsyncFlow.orElse (AsyncFlow.read (fun env -> env + 1))
            |> AsyncFlow.run 41
            |> Async.RunSynchronously

        let bypassesFallback =
            AsyncFlow.succeed 10
            |> AsyncFlow.orElse (AsyncFlow.succeed 99)
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let zipped =
            AsyncFlow.zip (AsyncFlow.read (fun env -> env + 1)) (AsyncFlow.read (fun env -> env * 2))
            |> AsyncFlow.run 5
            |> Async.RunSynchronously

        let mapped =
            AsyncFlow.map2 (+) (AsyncFlow.read (fun env -> env + 1)) (AsyncFlow.read (fun env -> env * 2))
            |> AsyncFlow.run 5
            |> Async.RunSynchronously

        test <@ tapPreservesOriginalError = Error "primary" @>
        test <@ tapSkipsSuccess = Ok 42 @>
        test <@ List.ofSeq tappedErrors = [ "primary" ] @>
        test <@ recovered = Ok 42 @>
        test <@ bypassesFallback = Ok 10 @>
        test <@ zipped = Ok(6, 10) @>
        test <@ mapped = Ok 16 @>

    [<Fact>]
    let ``ColdTask carries the runtime cancellation token into TaskFlow`` () =
        let seen = ref CancellationToken.None
        use cts = new CancellationTokenSource()

        let workflow : TaskFlow<unit, string, int> =
            TaskFlow.fromTask(
                ColdTask(fun cancellationToken ->
                    seen.Value <- cancellationToken
                    Task.FromResult 42)
            )

        let result =
            workflow
            |> TaskFlow.run () cts.Token
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ result = Ok 42 @>
        test <@ seen.Value = cts.Token @>

    [<Fact>]
    let ``ColdTask helpers adapt task and valuetask sources with the expected hot and cold semantics`` () =
        let startedTask = Task.FromResult 42
        let hotTask = ColdTask.fromTask startedTask

        let taskRun1 = hotTask |> ColdTask.run CancellationToken.None
        let taskRun2 = hotTask |> ColdTask.run CancellationToken.None

        test <@ obj.ReferenceEquals(startedTask, taskRun1) @>
        test <@ obj.ReferenceEquals(taskRun1, taskRun2) @>
        test <@ taskRun1.GetAwaiter().GetResult() = 42 @>

        let taskFactoryRuns = ref 0

        let coldTask =
            ColdTask.fromTaskFactory(fun () ->
                taskFactoryRuns.Value <- taskFactoryRuns.Value + 1
                Task.FromResult taskFactoryRuns.Value)

        let coldTaskRun1 = coldTask |> ColdTask.run CancellationToken.None |> fun task -> task.GetAwaiter().GetResult()
        let coldTaskRun2 = coldTask |> ColdTask.run CancellationToken.None |> fun task -> task.GetAwaiter().GetResult()

        test <@ coldTaskRun1 = 1 @>
        test <@ coldTaskRun2 = 2 @>

        let seen = ref CancellationToken.None
        let valueTaskFactoryRuns = ref 0

        let coldValueTask =
            ColdTask.fromValueTaskFactory(fun cancellationToken ->
                seen.Value <- cancellationToken
                valueTaskFactoryRuns.Value <- valueTaskFactoryRuns.Value + 1
                ValueTask<int>(valueTaskFactoryRuns.Value))

        use cts = new CancellationTokenSource()

        let coldValueTaskRun1 =
            coldValueTask
            |> ColdTask.run cts.Token
            |> fun task -> task.GetAwaiter().GetResult()

        let coldValueTaskRun2 =
            coldValueTask
            |> ColdTask.run CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ coldValueTaskRun1 = 1 @>
        test <@ coldValueTaskRun2 = 2 @>
        test <@ seen.Value = CancellationToken.None @>

        let startedValueTask = ValueTask<int>(99)
        let hotValueTask = ColdTask.fromValueTask startedValueTask

        let hotValueTaskRun1 = hotValueTask |> ColdTask.run CancellationToken.None
        let hotValueTaskRun2 = hotValueTask |> ColdTask.run CancellationToken.None

        test <@ obj.ReferenceEquals(hotValueTaskRun1, hotValueTaskRun2) @>
        test <@ hotValueTaskRun1.GetAwaiter().GetResult() = 99 @>

        let oneShotValueTaskSource = SingleConsumptionValueTaskSource 123

        let normalizedHotValueTask =
            oneShotValueTaskSource.AsValueTask()
            |> ColdTask.fromValueTask

        let normalizedRun1 =
            normalizedHotValueTask
            |> ColdTask.run CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let normalizedRun2 =
            normalizedHotValueTask
            |> ColdTask.run CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ normalizedRun1 = 123 @>
        test <@ normalizedRun2 = 123 @>
        test <@ oneShotValueTaskSource.ConsumptionCount = 1 @>

    [<Fact>]
    let ``ColdTask of Result is the typed failure cold task shape`` () =
        let seen = ref CancellationToken.None
        use cts = new CancellationTokenSource()

        let workflow : TaskFlow<unit, string, int> =
            TaskFlow.fromTaskResult(
                ColdTask(fun cancellationToken ->
                    seen.Value <- cancellationToken
                    Task.FromResult(Ok 42))
            )

        let result =
            workflow
            |> TaskFlow.run () cts.Token
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ result = Ok 42 @>
        test <@ seen.Value = cts.Token @>

    [<Fact>]
    let ``TaskFlow.fromTask requires the nominal ColdTask wrapper`` () =
        let fsFlowAssemblyPath = typeof<FlowBuilder>.Assembly.Location
        let fsFlowNetAssemblyPath = typeof<TaskFlowBuilder>.Assembly.Location

        let rawFactoryProbe =
            $"""
#r @"{fsFlowAssemblyPath}"
#r @"{fsFlowNetAssemblyPath}"
open System.Threading
open System.Threading.Tasks
open FsFlow.Net

let probe : TaskFlow<unit, string, int> =
    TaskFlow.fromTask(fun (_: CancellationToken) -> Task.FromResult 42)
"""

        let wrappedFactoryProbe =
            $"""
#r @"{fsFlowAssemblyPath}"
#r @"{fsFlowNetAssemblyPath}"
open System.Threading.Tasks
open FsFlow.Net

let probe : TaskFlow<unit, string, int> =
    TaskFlow.fromTask(ColdTask(fun _ -> Task.FromResult 42))
"""

        let rawExitCode, rawOutput = runFsiScript rawFactoryProbe
        let wrappedExitCode, wrappedOutput = runFsiScript wrappedFactoryProbe

        test <@ rawExitCode <> 0 @>
        test <@ rawOutput.Contains("error FS") @>
        test <@ wrappedExitCode = 0 @>
        test <@ wrappedOutput = "" @>

    [<Fact>]
    let ``TaskFlow can lift AsyncFlow`` () =
        let asyncWorkflow : AsyncFlow<int, string, int> =
            AsyncFlow.read id
            |> AsyncFlow.map (fun value -> value + 2)

        let taskWorkflow : TaskFlow<int, string, int> =
            asyncWorkflow
            |> TaskFlow.fromAsyncFlow
            |> TaskFlow.map (fun value -> value * 10)

        let result =
            taskWorkflow
            |> TaskFlow.toTask 4 CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ result = Ok 60 @>

    [<Fact>]
    let ``TaskFlow delay reruns from scratch`` () =
        let runs = ref 0

        let workflow : TaskFlow<unit, string, int> =
            TaskFlow.delay(fun () ->
                runs.Value <- runs.Value + 1
                TaskFlow.succeed runs.Value)

        let runOnce () =
            workflow
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ runOnce () = Ok 1 @>
        test <@ runOnce () = Ok 2 @>

    [<Fact>]
    let ``shared combinators preserve task environment and error semantics`` () =
        let baseWorkflow : TaskFlow<int, int, int> =
            TaskFlow.read (fun env -> env + 1)
            |> TaskFlow.map ((*) 2)
            |> TaskFlow.bind (fun value -> TaskFlow.succeed(value + 3))
            |> TaskFlow.mapError String.length

        let workflow : TaskFlow<string, int, int> =
            TaskFlow.localEnv String.length baseWorkflow

        let result =
            workflow
            |> TaskFlow.run "flowkit" CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ result = Ok 19 @>

    [<Fact>]
    let ``TaskFlow composition helpers cover error tapping fallback and pairing`` () =
        let tappedErrors = ResizeArray<string>()

        let tapPreservesOriginalError =
            TaskFlow.fail "primary"
            |> TaskFlow.tapError (fun error ->
                tappedErrors.Add error
                TaskFlow.succeed ())
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let tapSkipsSuccess =
            TaskFlow.succeed 42
            |> TaskFlow.tapError (fun error ->
                tappedErrors.Add $"unexpected:{error}"
                TaskFlow.succeed ())
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let recovered =
            TaskFlow.fail "missing"
            |> TaskFlow.orElse (TaskFlow.read (fun env -> env + 1))
            |> TaskFlow.run 41 CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let bypassesFallback =
            TaskFlow.succeed 10
            |> TaskFlow.orElse (TaskFlow.succeed 99)
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let zipped =
            TaskFlow.zip (TaskFlow.read (fun env -> env + 1)) (TaskFlow.read (fun env -> env * 2))
            |> TaskFlow.run 5 CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let mapped =
            TaskFlow.map2 (+) (TaskFlow.read (fun env -> env + 1)) (TaskFlow.read (fun env -> env * 2))
            |> TaskFlow.run 5 CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ tapPreservesOriginalError = Error "primary" @>
        test <@ tapSkipsSuccess = Ok 42 @>
        test <@ List.ofSeq tappedErrors = [ "primary" ] @>
        test <@ recovered = Ok 42 @>
        test <@ bypassesFallback = Ok 10 @>
        test <@ zipped = Ok(6, 10) @>
        test <@ mapped = Ok 16 @>

    [<Fact>]
    let ``Check covers the pure result surface`` () =
        test <@ Check.not (Check.okIf true) = Error () @>
        test <@ Check.not (Check.okIf false) = Ok () @>

        test <@ Check.``and`` (Check.okIf true) (Check.okIfSome (Some 10)) = Ok () @>
        test <@ Check.``and`` (Check.okIf true) (Check.okIf false) = Error () @>

        test <@ Check.``or`` (Check.okIf false) (Check.okIfSome (Some 10)) = Ok () @>
        test <@ Check.``or`` (Check.okIf false) (Check.okIf false) = Error () @>

        test <@ Check.all [ Check.okIf true; Check.okIf true; Check.okIf true ] = Ok () @>

        let allShortCircuits =
            seq {
                yield Check.okIf true
                yield Check.okIf false
                failwith "Check.all should short-circuit before the third item"
            }

        test <@ Check.all allShortCircuits = Error () @>

        test <@ Check.any [ Check.okIf false; Check.okIf false; Check.okIf true ] = Ok () @>

        let anyShortCircuits =
            seq {
                yield Check.okIf true
                failwith "Check.any should short-circuit before the second item"
            }

        test <@ Check.any anyShortCircuits = Ok () @>

        test <@ Check.okIf false |> Check.orElse "invalid" = Error "invalid" @>

        test <@ Check.okIf true = Ok () @>
        test <@ Check.okIf false = Error () @>
        test <@ Check.failIf true = Error () @>
        test <@ Check.failIf false = Ok () @>

        test <@ Check.okIfSome (Some 10) = Ok 10 @>
        test <@ Check.okIfSome None = Error () @>
        test <@ Check.failIfNone None = Error () @>
        test <@ Check.failIfNone (Some 7) = Ok 7 @>

        test <@ Check.okIfValueSome (ValueSome 11) = Ok 11 @>
        test <@ Check.okIfValueNone ValueNone = Ok () @>
        test <@ Check.failIfValueSome ValueNone = Ok () @>
        test <@ Check.failIfValueNone (ValueSome 8) = Ok 8 @>

        let nonNull = "flowkit"
        let nullString: string = null

        test <@ Check.okIfNotNull nonNull = Ok "flowkit" @>
        test <@ Check.okIfNull nullString = Ok () @>
        test <@ Check.failIfNotNull nullString = Error () @>
        test <@ Check.failIfNull nonNull = Error () @>

        test <@ Check.okIfNotEmpty [ 1; 2 ] |> Result.map Seq.toList = Ok [ 1; 2 ] @>
        test <@ Check.okIfEmpty Seq.empty = Ok () @>
        test <@ Check.failIfNotEmpty Seq.empty = Ok () @>
        test <@ Check.failIfEmpty Seq.empty = Error () @>

        test <@ Check.okIfEqual 3 3 = Ok () @>
        test <@ Check.okIfNotEqual 3 4 = Ok () @>
        test <@ Check.failIfEqual 3 4 = Ok () @>
        test <@ Check.failIfNotEqual 3 3 = Ok () @>

        test <@ Check.okIfNonEmptyStr "hello" = Ok "hello" @>
        test <@ Check.okIfEmptyStr "" = Ok () @>
        test <@ Check.okIfNotBlank "  x  " = Ok "  x  " @>
        test <@ Check.okIfBlank "   " = Ok () @>
        test <@ Check.failIfNonEmptyStr "" = Ok () @>
        test <@ Check.failIfEmptyStr "hello" = Ok "hello" @>
        test <@ Check.failIfNotBlank "   " = Ok () @>
        test <@ Check.failIfBlank "x" = Ok "x" @>

        test <@ Validate.okIf false |> Validate.orElse "invalid" = Error "invalid" @>
        test <@ Validate.okIf false |> Validate.orElseWith (fun () -> "generated") = Error "generated" @>

    [<Fact>]
    let ``validation graph names are explicit`` () =
        let path =
            [
                PathSegment.Key "user"
                PathSegment.Index 0
                PathSegment.Name "email"
            ]

        let diagnostic =
            {
                Path = path
                Error = "missing"
            }

        let graph : Diagnostics<string> =
            {
                Local = [ diagnostic ]
                Children = Map.empty
            }

        test <@ typeof<PathSegment>.Name = "PathSegment" @>
        test <@ typeof<Diagnostic<string>>.Name = "Diagnostic`1" @>
        test <@ typeof<Diagnostics<string>>.Name = "Diagnostics`1" @>
        test <@ graph.Local.Head.Path = path @>
        test <@ graph.Local.Head.Error = "missing" @>
        test <@ graph.Children.IsEmpty @>

    [<Fact>]
    let ``diagnostics merge recursively combines shared branches and flattens paths`` () =
        let makeDiagnostic (path: Path) (error: string) : Diagnostic<string> =
            {
                Path = path
                Error = error
            }

        let left =
            {
                Local = [ makeDiagnostic [] "left-root" ]
                Children =
                    Map.ofList
                        [
                            PathSegment.Key "user",
                            {
                                Local = [ makeDiagnostic [] "left-user" ]
                                Children =
                                    Map.ofList
                                        [
                                            PathSegment.Key "address",
                                            Diagnostics.singleton (makeDiagnostic [ PathSegment.Index 0 ] "left-address")
                                        ]
                            }
                        ]
            }

        let right =
            {
                Local = [ makeDiagnostic [] "right-root" ]
                Children =
                    Map.ofList
                        [
                            PathSegment.Key "user",
                            {
                                Local = [ makeDiagnostic [] "right-user" ]
                                Children =
                                    Map.ofList
                                        [
                                            PathSegment.Key "address",
                                            Diagnostics.singleton (makeDiagnostic [ PathSegment.Index 1 ] "right-address")
                                        ]
                            }
                        ]
            }

        let merged = Diagnostics.merge left right

        test <@ merged.Local = [ makeDiagnostic [] "left-root"; makeDiagnostic [] "right-root" ] @>

        match merged.Children |> Map.tryFind (PathSegment.Key "user") with
        | Some userBranch ->
            test <@ userBranch.Local = [ makeDiagnostic [] "left-user"; makeDiagnostic [] "right-user" ] @>

            match userBranch.Children |> Map.tryFind (PathSegment.Key "address") with
            | Some addressBranch ->
                let expectedAddress =
                    [
                        makeDiagnostic [ PathSegment.Index 0 ] "left-address"
                        makeDiagnostic [ PathSegment.Index 1 ] "right-address"
                    ]

                test <@ Diagnostics.flatten addressBranch = expectedAddress @>
            | None -> failwith "expected merged address branch"
        | None -> failwith "expected merged user branch"

        let expectedMerged =
            [
                makeDiagnostic [] "left-root"
                makeDiagnostic [] "right-root"
                makeDiagnostic [ PathSegment.Key "user" ] "left-user"
                makeDiagnostic [ PathSegment.Key "user" ] "right-user"
                makeDiagnostic [ PathSegment.Key "user"; PathSegment.Key "address"; PathSegment.Index 0 ] "left-address"
                makeDiagnostic [ PathSegment.Key "user"; PathSegment.Key "address"; PathSegment.Index 1 ] "right-address"
            ]

        test <@ Diagnostics.flatten merged = expectedMerged @>

    [<Fact>]
    let ``validate computation expression accumulates sibling failures and short-circuits sequentially`` () =
        let makeDiagnostic error =
            {
                Path = []
                Error = error
            }

        let leftRuns = ref 0
        let rightRuns = ref 0
        let sequentialRuns = ref 0

        let mergedWorkflow : Validation<int, string> =
            validate {
                let! left =
                    (leftRuns.Value <- leftRuns.Value + 1
                     Validation.fail (Diagnostics.singleton (makeDiagnostic "left")))

                and! right =
                    (rightRuns.Value <- rightRuns.Value + 1
                     Validation.fail (Diagnostics.singleton (makeDiagnostic "right")))

                return left + right
            }

        let sequentialWorkflow : Validation<int, string> =
            validate {
                let! _ = Validation.fail (Diagnostics.singleton (makeDiagnostic "parse"))
                let! _ =
                    (sequentialRuns.Value <- sequentialRuns.Value + 1
                     Validation.fail (Diagnostics.singleton (makeDiagnostic "should-not-run")))
                return 0
            }

        let liftedResultWorkflow : Validation<int, string> =
            validate {
                let! value = Ok 21
                return value + 1
            }

        let mergedResult = Validation.toResult mergedWorkflow
        let sequentialResult = Validation.toResult sequentialWorkflow
        let liftedResult = Validation.toResult liftedResultWorkflow
        let expectedSequential = Error (Diagnostics.singleton (makeDiagnostic "parse"))

        test <@ typeof<Validation<int, string>>.Name = "Validation`2" @>
        match mergedResult with
        | Ok _ -> failwith "expected merged workflow to fail"
        | Error diagnostics ->
            let flattened = Diagnostics.flatten diagnostics
            if flattened <> [ makeDiagnostic "left"; makeDiagnostic "right" ] then
                failwith "expected merged workflow diagnostics to flatten in order"

        test <@ leftRuns.Value = 1 @>
        test <@ rightRuns.Value = 1 @>
        test <@ sequentialResult = expectedSequential @>
        test <@ sequentialRuns.Value = 0 @>
        test <@ liftedResult = Ok 22 @>

    [<Fact>]
    let ``Validate bridges into flow, async, and task shapes`` () =
        let flowBridge =
            Validate.okIf false
            |> Flow.orElseFlow (Flow.read (fun env -> $"flow:{env}"))
            |> Flow.run "env"

        let asyncBridge =
            Validate.okIf false
            |> AsyncFlow.orElseAsync (async.Return "async")
            |> Async.RunSynchronously

        let asyncFlowBridge =
            Validate.okIf false
            |> AsyncFlow.orElseAsyncFlow (AsyncFlow.read (fun env -> $"async-flow:{env}"))
            |> AsyncFlow.run "env"
            |> Async.RunSynchronously

        let taskBridge =
            Validate.okIf false
            |> TaskFlow.orElseTask (Task.FromResult "task")
            |> fun task -> task.GetAwaiter().GetResult()

        let taskAsyncBridge =
            Validate.okIf false
            |> TaskFlow.orElseAsync (async.Return "task-async")
            |> fun task -> task.GetAwaiter().GetResult()

        let taskFlowBridge =
            Validate.okIf false
            |> TaskFlow.orElseTaskFlow (TaskFlow.read (fun env -> $"task-flow:{env}"))
            |> TaskFlow.run "env" CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let taskAsyncFlowBridge =
            Validate.okIf false
            |> TaskFlow.orElseAsyncFlow (AsyncFlow.read (fun env -> $"task-async-flow:{env}"))
            |> TaskFlow.run "env" CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ flowBridge = Error "flow:env" @>
        test <@ asyncBridge = Error "async" @>
        test <@ asyncFlowBridge = Error "async-flow:env" @>
        test <@ taskBridge = Error "task" @>
        test <@ taskAsyncBridge = Error "task-async" @>
        test <@ taskFlowBridge = Error "task-flow:env" @>
        test <@ taskAsyncFlowBridge = Error "task-async-flow:env" @>

    [<Fact>]
    let ``AsyncFlow runtime helpers cover timeout retry and release`` () =
        let timeoutResult =
            AsyncFlow.Runtime.sleep (TimeSpan.FromMilliseconds 20.0)
            |> AsyncFlow.Runtime.timeout (TimeSpan.FromMilliseconds 1.0) "timed out"
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let retryRuns = ref 0

        let retryWorkflow =
            let policy : RetryPolicy<string> =
                { MaxAttempts = 3
                  Delay = fun _ -> TimeSpan.Zero
                  ShouldRetry = fun error -> error = "transient" }

            AsyncFlow.delay(fun () ->
                retryRuns.Value <- retryRuns.Value + 1

                if retryRuns.Value < 2 then
                    AsyncFlow.fail "transient"
                else
                    AsyncFlow.succeed 42)
            |> AsyncFlow.Runtime.retry policy

        let retryResult =
            retryWorkflow
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let releaseCount = ref 0

        let acquireReleaseResult =
            AsyncFlow.Runtime.useWithAcquireRelease
                (AsyncFlow.succeed 7)
                (fun _ _ ->
                    releaseCount.Value <- releaseCount.Value + 1
                    Task.CompletedTask)
                (fun _ -> AsyncFlow.fail "boom")
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        test <@ timeoutResult = Error "timed out" @>
        test <@ retryResult = Ok 42 @>
        test <@ retryRuns.Value = 2 @>
        test <@ acquireReleaseResult = Error "boom" @>
        test <@ releaseCount.Value = 1 @>

    [<Fact>]
    let ``TaskFlow runtime helpers cover timeout retry and release`` () =
        let timeoutResult =
            TaskFlow.Runtime.sleep (TimeSpan.FromMilliseconds 20.0)
            |> TaskFlow.Runtime.timeout (TimeSpan.FromMilliseconds 1.0) "timed out"
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let retryRuns = ref 0

        let retryWorkflow =
            let policy : RetryPolicy<string> =
                { MaxAttempts = 3
                  Delay = fun _ -> TimeSpan.Zero
                  ShouldRetry = fun error -> error = "transient" }

            TaskFlow.delay(fun () ->
                retryRuns.Value <- retryRuns.Value + 1

                if retryRuns.Value < 2 then
                    TaskFlow.fail "transient"
                else
                    TaskFlow.succeed 42)
            |> TaskFlow.Runtime.retry policy

        let retryResult =
            retryWorkflow
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let releaseCount = ref 0

        let acquireReleaseResult =
            TaskFlow.Runtime.useWithAcquireRelease
                (TaskFlow.succeed 7)
                (fun _ _ ->
                    releaseCount.Value <- releaseCount.Value + 1
                    Task.CompletedTask)
                (fun _ -> TaskFlow.fail "boom")
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ timeoutResult = Error "timed out" @>
        test <@ retryResult = Ok 42 @>
        test <@ retryRuns.Value = 2 @>
        test <@ acquireReleaseResult = Error "boom" @>
        test <@ releaseCount.Value = 1 @>

    [<Fact>]
    let ``Flow traverse and sequence work as expected`` () =
        let values = [ 1; 2; 3 ]
        let workflow = values |> Flow.traverse (fun v -> Flow.succeed (v * 2))
        let result = Flow.run () workflow
        test <@ result = Ok [ 2; 4; 6 ] @>

        let flows = [ Flow.succeed 1; Flow.succeed 2 ]
        let sequenceResult = Flow.run () (Flow.sequence flows)
        test <@ sequenceResult = Ok [ 1; 2 ] @>

        let failWorkflow = [ 1; 2 ] |> Flow.traverse (fun v -> if v = 1 then Flow.fail "error" else Flow.succeed v)
        test <@ Flow.run () failWorkflow = Error "error" @>

    [<Fact>]
    let ``AsyncFlow traverse and sequence work as expected`` () =
        let values = [ 1; 2; 3 ]
        let workflow = values |> AsyncFlow.traverse (fun v -> AsyncFlow.succeed (v * 2))
        let result = AsyncFlow.run () workflow |> Async.RunSynchronously
        test <@ result = Ok [ 2; 4; 6 ] @>

        let flows = [ AsyncFlow.succeed 1; AsyncFlow.succeed 2 ]
        let sequenceResult = AsyncFlow.run () (AsyncFlow.sequence flows) |> Async.RunSynchronously
        test <@ sequenceResult = Ok [ 1; 2 ] @>

    [<Fact>]
    let ``TaskFlow traverse and sequence work as expected`` () =
        let values = [ 1; 2; 3 ]
        let workflow = values |> TaskFlow.traverse (fun v -> TaskFlow.succeed (v * 2))
        let result = TaskFlow.run () CancellationToken.None workflow |> fun t -> t.GetAwaiter().GetResult()
        test <@ result = Ok [ 2; 4; 6 ] @>

        let flows = [ TaskFlow.succeed 1; TaskFlow.succeed 2 ]
        let sequenceResult = TaskFlow.run () CancellationToken.None (TaskFlow.sequence flows) |> fun t -> t.GetAwaiter().GetResult()
        test <@ sequenceResult = Ok [ 1; 2 ] @>

    [<Fact>]
    let ``AsyncFlow timeout helpers work as expected`` () =
        let okResult = 
            AsyncFlow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
            |> AsyncFlow.Runtime.timeoutToOk (TimeSpan.FromMilliseconds 1.0) ()
            |> AsyncFlow.run ()
            |> Async.RunSynchronously
        test <@ okResult = Ok () @>

        let errorResult =
            AsyncFlow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
            |> AsyncFlow.Runtime.timeoutToError (TimeSpan.FromMilliseconds 1.0) "timed out"
            |> AsyncFlow.run ()
            |> Async.RunSynchronously
        test <@ errorResult = Error "timed out" @>

        let withResult =
            AsyncFlow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
            |> AsyncFlow.Runtime.timeoutWith (TimeSpan.FromMilliseconds 1.0) (fun () -> AsyncFlow.succeed ())
            |> AsyncFlow.run ()
            |> Async.RunSynchronously
        test <@ withResult = Ok () @>

    [<Fact>]
    let ``TaskFlow timeout helpers work as expected`` () =
        let okResult = 
            TaskFlow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
            |> TaskFlow.Runtime.timeoutToOk (TimeSpan.FromMilliseconds 1.0) ()
            |> TaskFlow.run () CancellationToken.None
            |> fun t -> t.GetAwaiter().GetResult()
        test <@ okResult = Ok () @>

        let errorResult =
            TaskFlow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
            |> TaskFlow.Runtime.timeoutToError (TimeSpan.FromMilliseconds 1.0) "timed out"
            |> TaskFlow.run () CancellationToken.None
            |> fun t -> t.GetAwaiter().GetResult()
        test <@ errorResult = Error "timed out" @>

        let withResult =
            TaskFlow.Runtime.sleep (TimeSpan.FromMilliseconds 50.0)
            |> TaskFlow.Runtime.timeoutWith (TimeSpan.FromMilliseconds 1.0) (fun () -> TaskFlow.succeed ())
            |> TaskFlow.run () CancellationToken.None
            |> fun t -> t.GetAwaiter().GetResult()
        test <@ withResult = Ok () @>

    [<Fact>]
    let ``flow computation expression is sync only`` () =
        let workflow : Flow<int, string, int> =
            flow {
                let! env = Flow.env
                let! doubled = Ok(env * 2)
                return doubled
            }

        let publicMethods = publicInstanceMethodNames typeof<FlowBuilder>
        let argumentTypeNames = flowBuilderBindAndReturnFromArgumentNames ()

        test <@ Flow.run 21 workflow = Ok 42 @>
        test <@ publicMethods |> Array.contains "Bind" @>
        test <@ publicMethods |> Array.contains "ReturnFrom" @>
        test <@ argumentTypeNames = [| "FSharpFunc`2"; "FSharpOption`1"; "FSharpResult`2"; "FSharpValueOption`1"; "Flow`3" |] @>

    [<Fact>]
    let ``option and valueoption inputs short-circuit with unit errors across builders`` () =
        let syncSome : Flow<int, unit, int> =
            flow {
                let! env = Flow.env
                let! value = Some(env + 1)
                return value * 2
            }

        let syncNone : Flow<int, unit, int> =
            flow {
                let! env = Flow.env
                let! value = None
                return env + value
            }

        let syncValueSome : Flow<int, unit, int> =
            flow {
                let! env = Flow.env
                let! value = ValueSome(env + 1)
                return value * 2
            }

        let syncValueNone : Flow<int, unit, int> =
            flow {
                let! env = Flow.env
                let! value = ValueNone
                return env + value
            }

        let asyncWorkflow : AsyncFlow<int, unit, int> =
            asyncFlow {
                let! env = AsyncFlow.env
                let! value = Some(env + 1)
                let! extra = ValueSome(value + 1)
                return extra * 2
            }

        let asyncReturnFromNone : AsyncFlow<unit, unit, int> =
            asyncFlow { return! None }

        let taskWorkflow : TaskFlow<int, unit, int> =
            taskFlow {
                let! env = TaskFlow.env
                let! value = Some(env + 1)
                let! extra = ValueSome(value + 1)
                return extra * 2
            }

        let taskReturnFromValueNone : TaskFlow<unit, unit, int> =
            taskFlow { return! ValueNone }

        let asyncArgumentTypeNames = asyncFlowBuilderBindAndReturnFromArgumentNames ()
        let taskArgumentTypeNames = taskFlowBuilderBindAndReturnFromArgumentNames ()

        test <@ Flow.run 20 syncSome = Ok 42 @>
        test <@ Flow.run 20 syncNone = Error() @>
        test <@ Flow.run 20 syncValueSome = Ok 42 @>
        test <@ Flow.run 20 syncValueNone = Error() @>
        test <@ asyncWorkflow |> AsyncFlow.run 19 |> Async.RunSynchronously = Ok 42 @>
        test <@ asyncReturnFromNone |> AsyncFlow.run () |> Async.RunSynchronously = Error() @>
        test <@ taskWorkflow |> TaskFlow.run 19 CancellationToken.None |> fun task -> task.GetAwaiter().GetResult() = Ok 42 @>
        test <@ taskReturnFromValueNone |> TaskFlow.run () CancellationToken.None |> fun task -> task.GetAwaiter().GetResult() = Error() @>
        test <@ asyncArgumentTypeNames |> Array.contains "FSharpOption`1" @>
        test <@ asyncArgumentTypeNames |> Array.contains "FSharpValueOption`1" @>
        test <@ taskArgumentTypeNames |> Array.contains "FSharpOption`1" @>
        test <@ taskArgumentTypeNames |> Array.contains "FSharpValueOption`1" @>

    [<Fact>]
    let ``option and valueoption implicit binding requires unit workflow errors`` () =
        let fsFlowAssemblyPath = typeof<FlowBuilder>.Assembly.Location
        let fsFlowNetAssemblyPath = typeof<TaskFlowBuilder>.Assembly.Location

        let flowProbe =
            $"""
#r @"{fsFlowAssemblyPath}"
open FsFlow

let probe : Flow<unit, string, int> =
    flow {{
        let! value = Some 42
        return value
    }}
"""

        let asyncProbe =
            $"""
#r @"{fsFlowAssemblyPath}"
open FsFlow

let probe : AsyncFlow<unit, string, int> =
    asyncFlow {{
        let! value = ValueSome 42
        return value
    }}
"""

        let taskProbe =
            $"""
#r @"{fsFlowAssemblyPath}"
#r @"{fsFlowNetAssemblyPath}"
open FsFlow
open FsFlow.Net

let probe : TaskFlow<unit, string, int> =
    taskFlow {{
        let! value = Some 42
        return value
    }}
"""

        let flowExitCode, flowOutput = runFsiScript flowProbe
        let asyncExitCode, asyncOutput = runFsiScript asyncProbe
        let taskExitCode, taskOutput = runFsiScript taskProbe

        test <@ flowExitCode <> 0 @>
        test <@ flowOutput.Contains("Flow<unit,unit,int>") @>
        test <@ asyncExitCode <> 0 @>
        test <@ asyncOutput.Contains("AsyncFlow<unit,unit,int>") @>
        test <@ taskExitCode <> 0 @>
        test <@ taskOutput.Contains("TaskFlow<unit,unit,int>") @>

    [<Fact>]
    let ``explicit option adapters support custom workflow errors across modules`` () =
        let syncSome =
            Some 21
            |> Flow.fromOption "missing value"
            |> Flow.map ((*) 2)
            |> Flow.run ()

        let syncNone =
            None
            |> Flow.fromOption "missing value"
            |> Flow.run ()

        let syncValueSome =
            ValueSome 21
            |> Flow.fromValueOption "missing value"
            |> Flow.map ((*) 2)
            |> Flow.run ()

        let syncValueNone =
            ValueNone
            |> Flow.fromValueOption "missing value"
            |> Flow.run ()

        let asyncSome =
            Some 21
            |> AsyncFlow.fromOption "missing value"
            |> AsyncFlow.map ((*) 2)
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let asyncNone =
            None
            |> AsyncFlow.fromOption "missing value"
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let asyncValueSome =
            ValueSome 21
            |> AsyncFlow.fromValueOption "missing value"
            |> AsyncFlow.map ((*) 2)
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let asyncValueNone =
            ValueNone
            |> AsyncFlow.fromValueOption "missing value"
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let taskSome =
            Some 21
            |> TaskFlow.fromOption "missing value"
            |> TaskFlow.map ((*) 2)
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let taskNone =
            None
            |> TaskFlow.fromOption "missing value"
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let taskValueSome =
            ValueSome 21
            |> TaskFlow.fromValueOption "missing value"
            |> TaskFlow.map ((*) 2)
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let taskValueNone =
            ValueNone
            |> TaskFlow.fromValueOption "missing value"
            |> TaskFlow.run () CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ syncSome = Ok 42 @>
        test <@ syncNone = Error "missing value" @>
        test <@ syncValueSome = Ok 42 @>
        test <@ syncValueNone = Error "missing value" @>
        test <@ asyncSome = Ok 42 @>
        test <@ asyncNone = Error "missing value" @>
        test <@ asyncValueSome = Ok 42 @>
        test <@ asyncValueNone = Error "missing value" @>
        test <@ taskSome = Ok 42 @>
        test <@ taskNone = Error "missing value" @>
        test <@ taskValueSome = Ok 42 @>
        test <@ taskValueNone = Error "missing value" @>

    [<Fact>]
    let ``flow computation expression rejects task-oriented binds even with FsFlow.Net referenced`` () =
        let fsFlowAssemblyPath = typeof<FlowBuilder>.Assembly.Location
        let fsFlowNetAssemblyPath = typeof<TaskFlowBuilder>.Assembly.Location

        let taskProbe =
            $"""
#r @"{fsFlowAssemblyPath}"
#r @"{fsFlowNetAssemblyPath}"
open System.Threading.Tasks
open FsFlow
open FsFlow.Net

let probe : Flow<unit, string, int> =
    flow {{
        let! value = Task.FromResult 42
        return value
    }}
"""

        let taskFlowProbe =
            $"""
#r @"{fsFlowAssemblyPath}"
#r @"{fsFlowNetAssemblyPath}"
open FsFlow
open FsFlow.Net

let probe : Flow<unit, string, int> =
    flow {{
        let! value = TaskFlow.succeed 42
        return value
    }}
"""

        let taskExitCode, taskOutput = runFsiScript taskProbe
        let taskFlowExitCode, taskFlowOutput = runFsiScript taskFlowProbe

        test <@ taskExitCode <> 0 @>
        test <@ taskOutput.Contains("Bind") @>
        test <@ taskFlowExitCode <> 0 @>
        test <@ taskFlowOutput.Contains("Bind") @>

    [<Fact>]
    let ``asyncFlow lives in FsFlow and composes sync flows`` () =
        let workflow : AsyncFlow<int, string, int> =
            asyncFlow {
                let! env = Flow.env
                let! baseValue = AsyncFlow.succeed(env + 1)
                return baseValue * 2
            }

        let result =
            workflow
            |> AsyncFlow.run 20
            |> Async.RunSynchronously

        test <@ typeof<AsyncFlowBuilder>.Namespace = "FsFlow" @>
        test <@ result = Ok 42 @>

    [<Fact>]
    let ``asyncFlow directly binds and returns Async and Async Result values`` () =
        let workflow : AsyncFlow<int, string, int> =
            asyncFlow {
                let! env = AsyncFlow.env
                let! baseValue = async { return env + 1 }
                let! adjustedValue = async { return Ok(baseValue * 2) }
                return adjustedValue + 2
            }

        let asyncReturnFrom : AsyncFlow<unit, string, int> =
            asyncFlow { return! async { return 42 } }

        let workflowResult =
            workflow
            |> AsyncFlow.run 19
            |> Async.RunSynchronously

        let asyncReturnFromResult =
            asyncReturnFrom
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        test <@ workflowResult = Ok 42 @>
        test <@ asyncReturnFromResult = Ok 42 @>
        test <@ hasAsyncResultReturnFromOverload typeof<AsyncFlowBuilder> @>

    [<Fact>]
    let ``taskFlow lives in FsFlow.Net and composes async flows`` () =
        let workflow : TaskFlow<int, string, int> =
            taskFlow {
                let! env = Flow.env
                let! baseValue = AsyncFlow.succeed(env + 1)
                return baseValue * 2
            }

        let result =
            workflow
            |> TaskFlow.run 20 CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ typeof<TaskFlowBuilder>.Namespace = "FsFlow.Net" @>
        test <@ result = Ok 42 @>

    [<Fact>]
    let ``taskFlow directly binds and returns Async, Task, and result-bearing values`` () =
        let resultTask (value: int) : Task<Result<int, string>> = Task.FromResult(Ok value)

        let workflow : TaskFlow<int, string, int> =
            taskFlow {
                let! env = TaskFlow.env
                do! Task.CompletedTask
                let! baseValue = async { return env + 1 }
                let! adjustedValue = async { return Ok(baseValue * 2) }
                return adjustedValue + 2
            }

        let asyncReturnFrom : TaskFlow<unit, string, int> =
            taskFlow { return! async { return 42 } }

        let taskWorkflow : TaskFlow<int, string, int> =
            taskFlow {
                let! env = TaskFlow.env
                do! Task.CompletedTask
                let! baseValue = Task.FromResult(env + 1)
                let! adjustedValue = resultTask (baseValue * 2)
                return adjustedValue + 2
            }

        let taskReturnFrom : TaskFlow<unit, string, unit> =
            taskFlow { return! Task.CompletedTask }

        let taskReturnFromResult : TaskFlow<unit, string, int> =
            taskFlow { return! resultTask 42 }

        let run flow environment =
            flow
            |> TaskFlow.run environment CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let workflowResult = run workflow 19
        let asyncReturnFromResult = run asyncReturnFrom ()
        let taskWorkflowResult = run taskWorkflow 19
        let taskReturnFromUnitResult = run taskReturnFrom ()
        let taskReturnFromResultResult = run taskReturnFromResult ()

        test <@ workflowResult = Ok 42 @>
        test <@ asyncReturnFromResult = Ok 42 @>
        test <@ taskWorkflowResult = Ok 42 @>
        test <@ taskReturnFromUnitResult = Ok() @>
        test <@ taskReturnFromResultResult = Ok 42 @>
        test <@ hasAsyncResultReturnFromOverload typeof<TaskFlowBuilder> @>

    [<Fact>]
    let ``taskFlow directly binds and returns ValueTask and result-bearing ValueTask values`` () =
        let resultValueTask (value: int) : ValueTask<Result<int, string>> = ValueTask<Result<int, string>>(Ok value)

        let workflow : TaskFlow<int, string, int> =
            taskFlow {
                let! env = TaskFlow.env
                do! ValueTask()
                let! baseValue = ValueTask<int>(env + 1)
                let! adjustedValue = resultValueTask (baseValue * 2)
                return adjustedValue + 2
            }

        let valueTaskReturnFrom : TaskFlow<unit, string, unit> =
            taskFlow { return! ValueTask() }

        let valueTaskReturnFromValue : TaskFlow<unit, string, int> =
            taskFlow { return! ValueTask<int>(42) }

        let valueTaskReturnFromResult : TaskFlow<unit, string, int> =
            taskFlow { return! resultValueTask 42 }

        let run flow environment =
            flow
            |> TaskFlow.run environment CancellationToken.None
            |> fun task -> task.GetAwaiter().GetResult()

        let workflowResult = run workflow 19
        let valueTaskReturnFromUnitResult = run valueTaskReturnFrom ()
        let valueTaskReturnFromValueResult = run valueTaskReturnFromValue ()
        let valueTaskReturnFromResultResult = run valueTaskReturnFromResult ()

        test <@ workflowResult = Ok 42 @>
        test <@ valueTaskReturnFromUnitResult = Ok() @>
        test <@ valueTaskReturnFromValueResult = Ok 42 @>
        test <@ valueTaskReturnFromResultResult = Ok 42 @>

    [<Fact>]
    let ``TaskFlow keeps a Task-backed execution backbone even when lifting ValueTask inputs`` () =
        let workflow : TaskFlow<int, string, int> =
            taskFlow {
                let! env = TaskFlow.env
                let! value = ValueTask<int>(env + 1)
                return value * 2
            }

        let runningTask = TaskFlow.run 20 CancellationToken.None workflow
        let result = runningTask.GetAwaiter().GetResult()

        test <@ runningTask.GetType() = typeof<Task<Result<int, string>>> @>
        test <@ result = Ok 42 @>

    [<Fact>]
    let ``taskFlow directly binds and returns ColdTask values`` () =
        let seen = ref CancellationToken.None
        use cts = new CancellationTokenSource()

        let resultColdTask (value: int) : ColdTask<Result<int, string>> =
            ColdTask(fun cancellationToken ->
                seen.Value <- cancellationToken
                Task.FromResult(Ok value))

        let workflow : TaskFlow<int, string, int> =
            taskFlow {
                let! env = TaskFlow.env
                let! baseValue =
                    ColdTask(fun cancellationToken ->
                        seen.Value <- cancellationToken
                        Task.FromResult(env + 1))

                let! adjustedValue = resultColdTask (baseValue * 2)
                return adjustedValue + 2
            }

        let coldTaskReturnFromValue : TaskFlow<unit, string, int> =
            taskFlow { return! ColdTask(fun _ -> Task.FromResult 42) }

        let coldTaskReturnFromResult : TaskFlow<unit, string, int> =
            taskFlow { return! resultColdTask 42 }

        let run flow environment cancellationToken =
            flow
            |> TaskFlow.run environment cancellationToken
            |> fun task -> task.GetAwaiter().GetResult()

        let workflowResult = run workflow 19 cts.Token
        let coldTaskReturnFromValueResult = run coldTaskReturnFromValue () cts.Token
        let coldTaskReturnFromResultResult = run coldTaskReturnFromResult () cts.Token

        test <@ workflowResult = Ok 42 @>
        test <@ coldTaskReturnFromValueResult = Ok 42 @>
        test <@ coldTaskReturnFromResultResult = Ok 42 @>
        test <@ seen.Value = cts.Token @>

    [<Fact>]
    let ``asyncFlow directly binds and returns Task values when FsFlow.Net is referenced`` () =
        let resultTask (value: int) : Task<Result<int, string>> = Task.FromResult(Ok value)

        let workflow : AsyncFlow<int, string, int> =
            asyncFlow {
                let! env = AsyncFlow.env
                do! Task.CompletedTask
                let! baseValue = Task.FromResult(env + 1)
                let! adjustedValue = resultTask (baseValue * 2)
                return adjustedValue + 2
            }

        let taskReturnFrom : AsyncFlow<unit, string, unit> =
            asyncFlow { return! Task.CompletedTask }

        let taskReturnFromResult : AsyncFlow<unit, string, int> =
            asyncFlow { return! resultTask 42 }

        let workflowResult =
            workflow
            |> AsyncFlow.run 19
            |> Async.RunSynchronously

        let taskReturnFromUnitResult =
            taskReturnFrom
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let taskReturnFromResultResult =
            taskReturnFromResult
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        test <@ workflowResult = Ok 42 @>
        test <@ taskReturnFromUnitResult = Ok() @>
        test <@ taskReturnFromResultResult = Ok 42 @>

    [<Fact>]
    let ``asyncFlow directly binds and returns ValueTask values when FsFlow.Net is referenced`` () =
        let resultValueTask (value: int) : ValueTask<Result<int, string>> = ValueTask<Result<int, string>>(Ok value)

        let workflow : AsyncFlow<int, string, int> =
            asyncFlow {
                let! env = AsyncFlow.env
                do! ValueTask()
                let! baseValue = ValueTask<int>(env + 1)
                let! adjustedValue = resultValueTask (baseValue * 2)
                return adjustedValue + 2
            }

        let valueTaskReturnFrom : AsyncFlow<unit, string, unit> =
            asyncFlow { return! ValueTask() }

        let valueTaskReturnFromValue : AsyncFlow<unit, string, int> =
            asyncFlow { return! ValueTask<int>(42) }

        let valueTaskReturnFromResult : AsyncFlow<unit, string, int> =
            asyncFlow { return! resultValueTask 42 }

        let workflowResult =
            workflow
            |> AsyncFlow.run 19
            |> Async.RunSynchronously

        let valueTaskReturnFromUnitResult =
            valueTaskReturnFrom
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let valueTaskReturnFromValueResult =
            valueTaskReturnFromValue
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        let valueTaskReturnFromResultResult =
            valueTaskReturnFromResult
            |> AsyncFlow.run ()
            |> Async.RunSynchronously

        test <@ workflowResult = Ok 42 @>
        test <@ valueTaskReturnFromUnitResult = Ok() @>
        test <@ valueTaskReturnFromValueResult = Ok 42 @>
        test <@ valueTaskReturnFromResultResult = Ok 42 @>

    [<Fact>]
    let ``asyncFlow directly binds and returns ColdTask values when FsFlow.Net is referenced`` () =
        let seen = ref CancellationToken.None
        use cts = new CancellationTokenSource()

        let resultColdTask (value: int) : ColdTask<Result<int, string>> =
            ColdTask(fun cancellationToken ->
                seen.Value <- cancellationToken
                Task.FromResult(Ok value))

        let workflow : AsyncFlow<int, string, int> =
            asyncFlow {
                let! env = AsyncFlow.env
                let! baseValue =
                    ColdTask(fun cancellationToken ->
                        seen.Value <- cancellationToken
                        Task.FromResult(env + 1))

                let! adjustedValue = resultColdTask (baseValue * 2)
                return adjustedValue + 2
            }

        let coldTaskReturnFromValue : AsyncFlow<unit, string, int> =
            asyncFlow { return! ColdTask(fun _ -> Task.FromResult 42) }

        let coldTaskReturnFromResult : AsyncFlow<unit, string, int> =
            asyncFlow { return! resultColdTask 42 }

        let workflowResult =
            workflow
            |> AsyncFlow.run 19
            |> fun operation -> Async.StartAsTask(operation, cancellationToken = cts.Token)
            |> fun task -> task.GetAwaiter().GetResult()

        let coldTaskReturnFromValueResult =
            coldTaskReturnFromValue
            |> AsyncFlow.run ()
            |> fun operation -> Async.StartAsTask(operation, cancellationToken = cts.Token)
            |> fun task -> task.GetAwaiter().GetResult()

        let coldTaskReturnFromResultResult =
            coldTaskReturnFromResult
            |> AsyncFlow.run ()
            |> fun operation -> Async.StartAsTask(operation, cancellationToken = cts.Token)
            |> fun task -> task.GetAwaiter().GetResult()

        test <@ workflowResult = Ok 42 @>
        test <@ coldTaskReturnFromValueResult = Ok 42 @>
        test <@ coldTaskReturnFromResultResult = Ok 42 @>
        test <@ seen.Value = cts.Token @>

module Program =
    [<EntryPoint>]
    let main _ = 0
