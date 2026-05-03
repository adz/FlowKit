open System
open System.Threading
open System.Threading.Tasks
open FsFlow

type ProbeFailure(message: string) =
    inherit Exception(message)

module Assert =
    let equal<'value when 'value: equality> (expected: 'value) (actual: 'value) =
        if actual <> expected then
            raise (ProbeFailure(sprintf "Expected %A but got %A." expected actual))

let flowProbe () =
    let workflow : Flow<string, string, int> =
        Flow.read String.length

    workflow
    |> Flow.run "Ada"
    |> Assert.equal (Ok 3)

let asyncFlowProbe () =
    let workflow : AsyncFlow<int, string, int> =
        AsyncFlow.read id
        |> AsyncFlow.map ((*) 2)

    workflow
    |> AsyncFlow.run 21
    |> Async.RunSynchronously
    |> Assert.equal (Ok 42)

let taskFlowProbe () =
    let seen = ref CancellationToken.None

    let workflow : TaskFlow<unit, string, int> =
        TaskFlow.fromTask(
            ColdTask(fun cancellationToken ->
                seen.Value <- cancellationToken
                Task.FromResult 42)
        )

    let result =
        workflow
        |> TaskFlow.run () CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    Assert.equal (Ok 42) result
    Assert.equal CancellationToken.None seen.Value

[<EntryPoint>]
let main _ =
    flowProbe ()
    asyncFlowProbe ()
    taskFlowProbe ()
    0
