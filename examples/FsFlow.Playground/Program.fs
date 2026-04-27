open System
open System.Threading
open System.Threading.Tasks
open FsFlow
open FsFlow.Net

type AppEnv =
    { Prefix: string
      Name: string
      LoadSuffix: ColdTask<string> }

let greetingFlow : Flow<AppEnv, string, string> =
    Flow.read (fun env -> $"{env.Prefix} {env.Name}")

let greetingAsyncFlow : AsyncFlow<AppEnv, string, string> =
    greetingFlow
    |> AsyncFlow.fromFlow
    |> AsyncFlow.map (fun greeting -> greeting.ToUpperInvariant())

let greetingTaskFlow : TaskFlow<AppEnv, string, string> =
    TaskFlow.read _.LoadSuffix
    |> TaskFlow.bind TaskFlow.fromTask
    |> TaskFlow.map (fun suffix -> suffix)

[<EntryPoint>]
let main _ =
    let env =
        { Prefix = "Hello"
          Name = "Ada"
          LoadSuffix = ColdTask(fun _ -> Task.FromResult "!") }

    let syncResult =
        greetingFlow
        |> Flow.run env

    let asyncResult =
        greetingAsyncFlow
        |> AsyncFlow.run env
        |> Async.RunSynchronously

    let taskResult =
        greetingTaskFlow
        |> TaskFlow.run env CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    printfn "Flow: %A" syncResult
    printfn "AsyncFlow: %A" asyncResult
    printfn "TaskFlow suffix: %A" taskResult
    0
