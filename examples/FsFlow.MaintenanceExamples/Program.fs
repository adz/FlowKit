open System
open System.Threading
open System.Threading.Tasks
open FsFlow

let runFlow label env workflow =
    let result = Flow.run env workflow
    printfn "%s: %A" label result

let runAsyncFlow label env workflow =
    let result =
        workflow
        |> AsyncFlow.run env
        |> Async.RunSynchronously

    printfn "%s: %A" label result

let runTaskFlow label env workflow =
    let result =
        workflow
        |> TaskFlow.run env CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    printfn "%s: %A" label result

let syncExample : Flow<int, string, int> =
    Flow.read id // Flow<int, string, int>
    |> Flow.map ((+) 1)

let asyncExample : AsyncFlow<int, string, int> =
    asyncFlow {
        let! value = syncExample // AsyncFlow<int, string, int>
        return value * 2
    }

let taskExample : TaskFlow<int, string, int> =
    taskFlow {
        let! env = TaskFlow.env // TaskFlow<int, string, int>
        let! suffix = ColdTask(fun _ -> Task.FromResult 5) // TaskFlow<int, string, int>
        return env + suffix
    }

[<EntryPoint>]
let main _ =
    runFlow "Flow" 20 syncExample
    runAsyncFlow "AsyncFlow" 20 asyncExample
    runTaskFlow "TaskFlow" 20 taskExample
    0
