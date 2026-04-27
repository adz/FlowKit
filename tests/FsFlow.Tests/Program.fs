namespace FsFlow.Tests

open System
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks
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
        test <@ argumentTypeNames = [| "FSharpFunc`2"; "FSharpResult`2"; "Flow`3" |] @>

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

module Program =
    [<EntryPoint>]
    let main _ = 0
