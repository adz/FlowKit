open System
open System.Threading
open System.Threading.Tasks
open FsFlow
open FsFlow.Net

type AppConfig =
    { Prefix: string
      Name: string
      Delay: TimeSpan }

type AppEnvironment =
    { Config: AppConfig
      LoadSuffix: ColdTask<string> }

let validateConfig : Flow<AppConfig, string, string> =
    Flow.read (fun config ->
        if String.IsNullOrWhiteSpace config.Name then
            Error "name is required"
        else
            Ok $"{config.Prefix} {config.Name}")
    |> Flow.bind Flow.fromResult

let greetAsync : AsyncFlow<AppEnvironment, string, string> =
    validateConfig
    |> AsyncFlow.fromFlow
    |> AsyncFlow.localEnv _.Config

let greetTask : TaskFlow<AppEnvironment, string, string> =
    TaskFlow.fromAsyncFlow greetAsync
    |> TaskFlow.bind (fun greeting ->
        TaskFlow.read _.LoadSuffix
        |> TaskFlow.bind (fun loadSuffix ->
            TaskFlow.fromTask loadSuffix
            |> TaskFlow.map (fun suffix -> $"{greeting}{suffix}")))

[<EntryPoint>]
let main _ =
    let environment =
        { Config =
            { Prefix = "Hello"
              Name = "Ada"
              Delay = TimeSpan.FromMilliseconds 10.0 }
          LoadSuffix = ColdTask(fun _ -> Task.FromResult "!") }

    let syncResult =
        validateConfig
        |> Flow.run environment.Config

    let asyncResult =
        greetAsync
        |> AsyncFlow.run environment
        |> Async.RunSynchronously

    let taskResult =
        greetTask
        |> TaskFlow.run environment CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    printfn "Flow result: %A" syncResult
    printfn "AsyncFlow result: %A" asyncResult
    printfn "TaskFlow result: %A" taskResult
    0
